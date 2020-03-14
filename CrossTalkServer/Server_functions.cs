using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	public partial class Server : Form
	{
		// SERVER
		UdpListener udpServer;
		static int serverSampleRate = 48000;
		static int serverChannels = 2;
		WaveFormat serverFormat = WaveFormat.CreateIeeeFloatWaveFormat(serverSampleRate, serverChannels);

		// If true NminusOne sends the audio from talker back to the talker if the talker listens to the loop
		// he is talking on. Should be set no true in production.
		bool NminusOne = true;

		// AUDIO OUTPUT
		BufferedWaveProvider pflBuffer;

		// MAIN SERVER LOOP
		int mainServerInterval = 10; // Run the loop every this milliseconds
		int minimumServerInterval = 10;
		List<int> loopTimes = new List<int>();
		Object loopTimesLock = new Object();

		// LOOPS
		int numLoops = 16;
		List<Loop> loops = new List<Loop>();
		List<int> PflLoops = new List<int>();

		// CLIENTS
		Dictionary<string, Client> clients = new Dictionary<string, Client>();
		private Object clientsLock = new Object();

		// STATUS TIMER
		System.Windows.Forms.Timer statusTimer;

		// METERS
		static float returnTime = 1700f; //ms
		static float returnDist = 20f; //dB
		float returnPrSec = (returnDist / returnTime) * 1000f;

		// SERVER MEASURING
		List<double> TimingSeg1 = new List<double>();
		List<double> TimingSeg2 = new List<double>();
		List<double> TimingSeg3 = new List<double>();
		List<double> TimingSeg4 = new List<double>();
		int timingMaxValues = 1000;


		private void ServerInit()
		{
			// MAKE LOOPS
			for (int i = 0; i < numLoops; i++)
			{
				loops.Add(new Loop(serverSampleRate, serverChannels));
			}

			// START THE UDP-LISTENER
			udpServer = new UdpListener();
			StartListening();

			// SET UP THE CMD-EXECUTIVE
			CMD.ServerInit(clients, udpServer, loops);

			// SET UP AUDIO
			

			// START THE SERVER
			Thread serverMainThread = new Thread(ServerMain);
			serverMainThread.Priority = ThreadPriority.AboveNormal;
			serverMainThread.Start();

			// START THE STATUS TIMER
			statusTimer = new System.Windows.Forms.Timer();
			statusTimer.Interval = 250;
			statusTimer.Tick += statusTimerTick;
			statusTimer.Start();
		}

		private void statusTimerTick(object sender, EventArgs e)
		{
			loopIntervalDisp.setValue(mainServerInterval.ToString());

			UpdateClientView();

			DisplayLoopTimeStats();

			List<List<double>> chartData = new List<List<double>>();
			chartData.Add(TimingSeg1);
			chartData.Add(TimingSeg2);
			chartData.Add(TimingSeg3);
			chartData.Add(TimingSeg4);
			lineGraph.SetData(chartData);

			int outBufferDuration = (int)Math.Floor(pflBuffer.BufferedDuration.TotalMilliseconds);
			if (outBufferDuration > 9999) outBufferDuration = 9999;

			outBufferDisp.setValue(outBufferDuration.ToString());
		}

		private TimeSpan GetSpan(DateTime start)
		{
			return start - DateTime.Now;
		}

		private void ServerMain()
		{
			DateTime start;
			DateTime end;
			TimeSpan duration;
			double sleepTime;

			DateTime timeTMPa;
			DateTime timeTMPb;

			while (true)
			{
				start = DateTime.UtcNow;

				// DO STUFF
				try { FetchAudio(); }catch(Exception e) { Logger.WriteLine("An error occurred: " + e); }
				timeTMPa = DateTime.UtcNow;
				duration = timeTMPa - start;
				AddDoubleToList(TimingSeg1, duration.TotalMilliseconds, timingMaxValues);

				if (processAudio)
				{
					try { MixAudio(); } catch (Exception e) { Logger.WriteLine("An error occurred: " + e); }
					timeTMPb = DateTime.UtcNow;
					duration = timeTMPb - timeTMPa;
					AddDoubleToList(TimingSeg2, duration.TotalMilliseconds, timingMaxValues);

					try { SendAudio(); } catch (Exception e) { Logger.WriteLine("An error occurred: " + e); }
					timeTMPa = DateTime.UtcNow;
					duration = timeTMPa - timeTMPb;
					AddDoubleToList(TimingSeg3, duration.TotalMilliseconds, timingMaxValues);

					try { UpdateMeters(); } catch (Exception e) { Logger.WriteLine("An error occurred: " + e); }
					timeTMPb = DateTime.UtcNow;
					duration = timeTMPb - timeTMPa;
					AddDoubleToList(TimingSeg4, duration.TotalMilliseconds, timingMaxValues);
				}
				else
				{
					AddDoubleToList(TimingSeg2, 0d, timingMaxValues);
					AddDoubleToList(TimingSeg3, 0d, timingMaxValues);
					AddDoubleToList(TimingSeg4, 0d, timingMaxValues);
				}

				end = DateTime.UtcNow;

				duration = end - start;
				sleepTime = mainServerInterval - duration.TotalMilliseconds;
				if (sleepTime < 0) sleepTime = 0;

				lock (loopTimesLock)
				{
					// STORE LOOP DUR
					loopTimes.Add((int)Math.Round(duration.TotalMilliseconds));
					if (loopTimes.Count > 100) loopTimes.RemoveAt(0);

					// DISPLAY LOOP DUR
					loopTimeDisp.setValue(loopTimes.Max().ToString());
				}

				// CHECK FOR SLOW EXECUTION
				if(duration.TotalMilliseconds > mainServerInterval)
				{
					// NOTIFY OF SLOW EXECUTION
					//Console.WriteLine("=== SLOW EXECUTION ===");
					//Console.WriteLine("ServerMainLoop used " + Math.Round(duration.TotalMilliseconds, 2) + " on the last run.");

					// TRY TO REMEDY THE SITUATION BY RUNNING SLOWER, BUT HANDLING MORE DATA EACH TIME
					mainServerInterval++;
					//Console.WriteLine("Increasing mainServerInterval to " + mainServerInterval + " ms.");
					loopIntervalDisp.setValue(mainServerInterval.ToString());
				}
				else if(mainServerInterval > minimumServerInterval && duration.TotalMilliseconds < (mainServerInterval / 2f))
				{
					// NOTIFY OF FAST EXECUTION
					//Console.WriteLine("=== FAST EXECUTION ===");
					//Console.WriteLine("ServerMainLoop used " + Math.Round(duration.TotalMilliseconds, 2) + " on the last run.");

					// INCREASE SPEED TO GET LOWER LATENCY
					mainServerInterval--;
					//Console.WriteLine("Decreasing mainServerInterval to " + mainServerInterval + " ms.");
					loopIntervalDisp.setValue(mainServerInterval.ToString());
				}

				// SLEEP THE REMAINDER OF THE INTERVAL
				Thread.Sleep((int)Math.Floor(sleepTime));
			}
		}

		private void StartListening()
		{
			Task.Factory.StartNew(async () =>
			{
				while (true)
				{
					Received received = await udpServer.Receive();
					ServerReceivedData(received.Payload, received.Sender);
				}
			});
		}

		private Client GetClient(IPEndPoint endPoint)
		{
			foreach(KeyValuePair<string, Client> kvp in clients)
			{
				if(kvp.Value.endpoint.Equals(endPoint))
				{
					return kvp.Value;
				}
			}
			return null;
		}

		private void ServerReceivedData(byte[] data, IPEndPoint sender)
		{
			// GET CLIENT
			Client c = GetClient(sender);

			if (Encoding.ASCII.GetString(data).Substring(0, 3) == "HLO") // HELLO
			{
				// CLIENT CONNECTED
				string client_guid = Encoding.ASCII.GetString(data).Substring(3);

				Logger.WriteLine("SERVER RECEIVED: " + Encoding.ASCII.GetString(data));
				udpServer.Reply(Encoding.ASCII.GetBytes("HELLO CLIENT!"), sender);

				lock (clientsLock)
				{
					if (clients.ContainsKey(client_guid))
					{
						clients[client_guid].SetConnected(true);
						clients[client_guid].endpoint = sender;

						// RESEND ALL CLIENT LOOP LIGHTS STATUSES
						InitializeClientLoopButtons(client_guid);

						// RESEND ALL LOOP NAMES
						SendAllLoopNames(sender);
					}
					else
					{
						clients.Add(client_guid, new Client(sender, serverSampleRate, serverChannels, client_guid));

						// SEND ALL CLIENT LOOP LIGHTS STATUSES (Just in case)
						InitializeClientLoopButtons(client_guid);

						// SEND ALL LOOP NAMES
						SendAllLoopNames(sender);
					}
					// SET LAST PACKET TIME
					clients[client_guid].lastPacket = DateTime.Now;
				}
			}
			else if (c != null)
			{
				// SET LAST PACKET TIME
				clients[c.guid].lastPacket = DateTime.Now;

				if (!c.IsConnected())
				{
					// CLIENT WOKE TO LIFE AGAIN
					c.SetConnected(true);
					clients[c.guid].endpoint = sender;
					InitializeClientLoopButtons(c.guid);
				}

				if (Encoding.ASCII.GetString(data) == "BYE SERVER!")
				{
					// CLIENT CONNECTED
					Logger.WriteLine("SERVER RECEIVED: " + Encoding.ASCII.GetString(data));

					lock (clientsLock)
					{
						// MARK AS DISCONNECTED
						clients[c.guid].SetConnected(false);

						// TURN OFF ALL CLIENTS LOOP LIGHTS
						for (int i = 0; i < numLoops; i++)
						{
							udpServer.Reply(CMD.GetCmdBytes(CMD.CMDS.TurnOffLoopListenLight, (byte)i), sender);
							udpServer.Reply(CMD.GetCmdBytes(CMD.CMDS.TurnOffLoopTalkLight, (byte)i), sender);
							udpServer.Reply(CMD.GetCmdBytes(CMD.CMDS.TurnOffLoopTalkFlash, (byte)i), sender);
						}
					}

					udpServer.Reply(Encoding.ASCII.GetBytes("BYE CLIENT!"), sender);
				}
				else if (data.Length == 3)
				{
					// RECEIVED COMMAND
					Logger.WriteLine("SERVER RECEIVED COMMAND: " + data[0] + " " + data[1] + " " + data[2]);
					CMD.ExecuteCmd(data, c.guid);
				}
				else
				{
					// ASSUME SAMPLE RECEIVED
					lock (clientsLock)
					{
						processIncomingAudioData(data, clients[c.guid]);
					}
				}
			}
		}

		private void InitializeClientLoopButtons(string client)
		{
			lock (clientsLock)
			{
				foreach (int i in clients[client].GetSources())
				{
					udpServer.Reply(CMD.GetCmdBytes(CMD.CMDS.TurnOnLoopListenLight, (byte)i), clients[client].endpoint);
				}

				for(int i = 0; i < loops.Count; i++)
				{
					if(loops[i].talkers.Count > 0)
					{
						udpServer.Reply(CMD.GetCmdBytes(CMD.CMDS.TurnOnLoopTalkLight, (byte)i), clients[client].endpoint);
					}
				}

				foreach (int i in clients[client].destinations)
				{
					udpServer.Reply(CMD.GetCmdBytes(CMD.CMDS.TurnOnLoopTalkFlash, (byte)i), clients[client].endpoint);
				}
			}
		}

		private void TogglePFL(int loop)
		{
			if(loops[loop].pfl)
			{
				loops[loop].pfl = false;
				meterListenButtons[loop].setLitState(false);
			}
			else
			{
				loops[loop].pfl = true;
				meterListenButtons[loop].setLitState(true);
			}
		}

		private void UpdateClientView()
		{
			/*Dictionary<int, Label> clientViewName = new Dictionary<int, Label>();
			Dictionary<int, Label> clientViewTalkListen = new Dictionary<int, Label>();
			Dictionary<int, SegDisp> clientViewInBuffer = new Dictionary<int, SegDisp>();
			Dictionary<int, SegDisp> clientViewInTime = new Dictionary<int, SegDisp>();*/

			Client c;
			int i = 0;
			lock (clientsLock)
			{
				
				foreach (KeyValuePair<string, Client> kvp in clients)
				{
					
					
					// SHOW MAX 20 CLIENTS
					if (i > 19) break;

					c = kvp.Value;
					if (c.IsConnected())
					{
						clientDiodeConnected[i].SetLitState(true);
					}
					else
					{
						clientDiodeConnected[i].SetLitState(false);
					}

					for (int d = 0; d < numLoops; d++)
					{
						if(c.GetSources().Contains(d))
						{
							clientDiodeListen[i][d].SetLitState(true);
						}
						else
						{
							clientDiodeListen[i][d].SetLitState(false);
						}
						
						if (c.destinations.Contains(d))
						{
							clientDiodeTalk[i][d].SetLitState(true);
						}
						else
						{
							clientDiodeTalk[i][d].SetLitState(false);
						}
					}

					clientViewName[i].Text = c.endpoint.Address.ToString();
					clientViewFormat[i].Text = c.audioFormat.ToString();
					clientViewInBuffer[i].setValue(c.inputStream.BufferedDuration.TotalMilliseconds.ToString());


					if (c.lastPacket != null)
					{
						double time = Math.Round((DateTime.Now - c.lastPacket).TotalMilliseconds);
						clientViewInTime[i].setValue(time.ToString());
						// MARK TIMEOUT CLIENTS AS NOT CONNECTED
						if (time > 20000)
						{
							c.SetConnected(false);
						}
					}
					else
					{
						clientViewInTime[i].setValue("");
					}

					i++;
				}
				
			}

			// CLEAR OUT SUPERFLOUS LINES
			while(i < 20)
			{
				clientViewName[i].Text = "";
				clientViewFormat[i].Text = "";
				
				clientViewInBuffer[i].setValue("");
				clientViewInTime[i].setValue("");
				i++;
			}

			
		}

		private void LoopNameUpdate(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
			{
				TextBox box = (TextBox)sender;

				// UPDATE CLIENTS WITH NEW NAME
				foreach (KeyValuePair<string, Client> kvp in clients)
				{
					Client c = kvp.Value;
					if (c.IsConnected())
					{
						string loop = "01";
						if (int.Parse(box.Tag.ToString()) < 10)
						{
							loop = "0" + box.Tag.ToString();
						}
						else
						{
							loop = box.Tag.ToString();
						}

						udpServer.Reply(Encoding.ASCII.GetBytes("LPN" + loop + box.Text), c.endpoint);
					}
				}

				box.BackColor = Color.FromArgb(32, 32, 32);
			}
		}

		private void LoopNameChange(object sender, EventArgs e)
		{
			// MARK AS CHANGED
			TextBox box = (TextBox)sender;
			box.BackColor = Color.FromArgb(128, 32, 32);
		}

		private void SendAllLoopNames(IPEndPoint client)
		{
			for(int i = 0; i < numLoops; i++)
			{
				string loop = "01";
				if (i < 10)
				{
					loop = "0" + i.ToString();
				}
				else
				{
					loop = i.ToString();
				}
				udpServer.Reply(Encoding.ASCII.GetBytes("LPN" + loop + loopNames[i].Text), client);
			}
		}

		private void SaveLoopNames()
		{
			string path = AppDomain.CurrentDomain.BaseDirectory + "loops.cfg";
			List<string> names = new List<string>();
			
			foreach(TextBox box in loopNames)
			{
				names.Add(box.Text);
			}

			File.WriteAllLines(path, names.ToArray());
		}

		private void FlushBuffer(object sender, EventArgs e, BufferedWaveProvider buffer)
		{
			FlushBuffer(buffer);
		}

		private void FlushBuffer(BufferedWaveProvider buffer)
		{
			buffer.ClearBuffer();
		}

		private void ToggleNminusOne(object sender, EventArgs e)
		{
			if(NminusOne)
			{
				NminusOne = false;
				NminusOneButton.setLitState(false);
			}
			else
			{
				NminusOne = true;
				NminusOneButton.setLitState(true);
			}
		}

		private void DisplayLoopTimeStats()
		{
			Dictionary<int, int> counts = new Dictionary<int, int>();
			for(int i = 0; i < 51; i++)
			{
				counts.Add(i, 0);
			}

			lock(loopTimesLock)
			{
				foreach (int i in loopTimes)
				{
					if (i < 51)
					{
						counts[i]++;
					}
					else
					{
						counts[50]++;
					}
				}
			}

			foreach(KeyValuePair<int, int> kvp in counts)
			{
				loopTimeMeters[kvp.Key].SetValue(kvp.Value);
			}
		}

		private void AddDoubleToList(List<double> list, double d, int maxListLength)
		{
			list.Add(d);
			if (list.Count > maxListLength) list.RemoveAt(0);
		}
	}
}
