using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Threading;
using System.IO;

namespace CrossTalkClient
{
	public partial class Client : Form
	{
		bool connected = false;
		bool disconnecting = false; // USED TO STOP SENDING DATA DURING DISCONNECT OPERATIONS
		UdpUser client;
		CancellationTokenSource udpReceiveTaskToken;

		Dictionary<int, MMDevice> inputs = new Dictionary<int, MMDevice>();
		//List<int> inputs = new List<int>();

		Dictionary<int, MMDevice> outputs = new Dictionary<int, MMDevice>();
		//List<int> outputs = new List<int>();

		WasapiOut output = null;
		//WaveOut output = null;

		WasapiCapture input = null;
		//WaveIn input = null;

		int inputLatency = 10;
		int outputLatency = 10;

		List<shipment> bytesSent = new List<shipment>();

		int transmitMode = 0;
		int? PTTkey = null;
		bool askingForPTTkey = false;
		bool PTTopen = false;
		Label askForPTTKeyBox;


		WaveFormat outputFormat;
		WaveFormat internalFormatStereo = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
		MeteringSampleProvider outputMeter;
		int audioFormatID;

		List<float> integrationCollection = new List<float>();
		static int integration = 5; //ms
		int samplesPrIntegration = 240;

		List<float> inputSamples = new List<float>();
		int sampleSize = 9600; // Samples pr UDP-shipment

		int bitsPrSample = 32;
		int sampleRate = 48000;
		int channels = 2;

		int inputBitsPrSample;
		int inputSampleRate;
		int inputChannels;
		int inputMode; // 0: LR, 1: LL, 2: RR, 3: RL
		WaveFormat inputFormat;

		System.Windows.Forms.Timer outputBufferTimer;

		Dictionary<int, bool> loopListen = new Dictionary<int, bool>();
		Dictionary<int, bool> loopTalk = new Dictionary<int, bool>();

		// CLIENT DATA
		string GUID;


		private void ConnectToServer(object sender, EventArgs e)
		{
			if (connected)
			{
				disconnectFromServer();
			}
			else
			{
				client = UdpUser.ConnectTo(serverAddr.Text, 32123);

				if (client != null)
				{

					client.Send(Encoding.ASCII.GetBytes("HLO" + GUID));

					Thread.Sleep(100);

					// SEND AUDIO FORMAT REQUEST
					byte[] cmd = GetCmdBytes(CMDS.SetClientAudioFormat, (byte)audioFormatSelector.SelectedIndex);
					client.Send(cmd);


					//wait for reply messages from server and send them to console
					udpReceiveTaskToken = new CancellationTokenSource();
					Task.Factory.StartNew(async () =>
					{
						while (true)
						{
							try
							{
								Received received = await client.Receive();

								ClientReceivedData(received.Payload, received.Sender);

							}
							catch (Exception ex)
							{
								Console.WriteLine(ex);
							}
						}
					});
				}
			}
		}

		private void disconnectFromServer()
		{
			disconnecting = true;
			client.Send(Encoding.ASCII.GetBytes("BYE SERVER!"));
		}



		private void setClientConnectedStatus(bool status)
		{
			connected = status;
			if (status)
			{
				connectButton.Text = "DISCONNECT";
				connectButton.Invalidate();
			}
			else
			{
				udpReceiveTaskToken.Cancel();
				connectButton.Text = "CONNECT";
				connectButton.Invalidate();
			}
		}

		private void ClientReceivedData(byte[] data, IPEndPoint sender)
		{
			if (Encoding.ASCII.GetString(data) == "HELLO CLIENT!")
			{
				Console.WriteLine("CLIENT RECEIVED: " + Encoding.ASCII.GetString(data));
				Invoke(new Action(() =>
				{
					setClientConnectedStatus(true);
					disconnecting = false;
				}));
			}
			else if (Encoding.ASCII.GetString(data) == "BYE CLIENT!")
			{
				Console.WriteLine("CLIENT RECEIVED: " + Encoding.ASCII.GetString(data));
				Invoke(new Action(() =>
				{
					setClientConnectedStatus(false);
				}));
			}
			else if (Encoding.ASCII.GetString(data).Substring(0,3) == "LPN")
			{
				Console.WriteLine("CLIENT RECEIVED: " + Encoding.ASCII.GetString(data));
				string str = Encoding.ASCII.GetString(data).Substring(3);

				// UPDATE LOOP NAME
				int loopnumber = int.Parse(str.Substring(0, 2));
				string loopname = str.Substring(2);
				Invoke(new Action(() =>
				{
					listenButtons[loopnumber].Text = loopname;
					listenButtons[loopnumber].Invalidate();
					talkButtons[loopnumber].Text = loopname;
					talkButtons[loopnumber].Invalidate();
				}));
			}
			else if (data.Length == 3)
			{
				// RECEIVED COMMAND
				Console.WriteLine("CLIENT RECEIVED COMMAND: " + data[0] + " " + data[1] + " " + data[2]);
				ExecuteCmd(data, sender);
			}
			else
			{
				// ASSUME SOUND IS RECEIVED
				// IF LOOPBACK IS ACTIVE, DISCARD DATA
				if (!loopback)
				{
					BufferSound(Decode(data, audioFormatID), outputBuffer, false, true);
				}
			}
		}

		

		private void outputBufferTimerCheck(object sender, EventArgs e)
		{
			if (outputBuffer.BufferedDuration < TimeSpan.FromMilliseconds(40) && output.PlaybackState != PlaybackState.Paused)
			{
				output.Pause();
				Console.WriteLine(DateTime.Now.TimeOfDay + ": OUTPUT PAUSED");
			}
			else if (outputBuffer.BufferedDuration > TimeSpan.FromMilliseconds(40) && output.PlaybackState != PlaybackState.Playing)
			{
				output.Play();
				Console.WriteLine(DateTime.Now.TimeOfDay + ": OUTPUT PLAYING");
			}

			// UPDATE OUTPUT STATE INDICATORS
			switch(output.PlaybackState)
			{
				case PlaybackState.Playing:
					outputPlayingIndicator.turnOnUpper();
					outputPausedIndicator.turnOffUpper();
					outputStoppedIndicator.turnOffUpper();
					break;
				case PlaybackState.Paused:
					outputPlayingIndicator.turnOffUpper();
					outputPausedIndicator.turnOnUpper();
					outputStoppedIndicator.turnOffUpper();
					break;
				case PlaybackState.Stopped:
					outputPlayingIndicator.turnOffUpper();
					outputPausedIndicator.turnOffUpper();
					outputStoppedIndicator.turnOnUpper();
					break;
			}
		}

		private void LoadData()
		{
			// TRY TO FIND GUID
			string path = AppDomain.CurrentDomain.BaseDirectory + "guid.cfg";
			if (File.Exists(path))
			{
				GUID = File.ReadAllText(path);
			}
			else
			{
				string newGuid = Guid.NewGuid().ToString();
				GUID = newGuid;
				File.WriteAllText(path, newGuid);
			}
		}

		private void GetAudioInputs()
		{
			if (inputs.Count > 0)
			{
				inputs.Clear();
			}

			MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
			int i = 0;
			foreach (MMDevice wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
			{
				Console.WriteLine($"{wasapi.ID} {wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}");
				inputs.Add(i, wasapi);
				audioInputSelector.Items.Add(wasapi.FriendlyName);
				i++;
			}

			if (inputs.Count > 0)
			{
				audioInputSelector.SelectedIndex = 0;
			}
		}

		private void AudioInput_SelectedIndexChanged(object sender, EventArgs e)
		{
			// START CAPTURING SOUND FROM SELECTED DEVICE
			MMDevice inputDevice = inputs[audioInputSelector.SelectedIndex];


			if(input != null) input.StopRecording();

			input = new WasapiCapture(inputDevice, true, inputLatency);

			inputBitsPrSample = input.WaveFormat.BitsPerSample;
			inputSampleRate = input.WaveFormat.SampleRate;
			inputChannels = input.WaveFormat.Channels;
			inputFormat = WaveFormat.CreateIeeeFloatWaveFormat(inputSampleRate, inputChannels);

			inputBuffer = new BufferedWaveProvider(inputFormat);
			inputBuffer.ReadFully = true;
			inputBuffer.DiscardOnBufferOverflow = true;

			inputResampler = new WdlResamplingSampleProvider(inputBuffer.ToSampleProvider(), internalFormatStereo.SampleRate);

			SetInputMode(0);

			Console.WriteLine("SET INPUT FORMAT: "
				+ "Sample Rate: " + inputSampleRate
				+ ", BitsPrSasmple: " + inputBitsPrSample
				+ ", Channels: " + inputChannels);

			input.DataAvailable += waveIn_DataAvailable;
			input.StartRecording();
		}

		


		private void GetAudioOutputs()
		{
			if (outputs.Count > 0)
			{
				outputs.Clear();
			}
			
			MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
			int i = 0;
			foreach (MMDevice wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
			{
				Console.WriteLine($"{wasapi.ID} {wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}");
				outputs.Add(i, wasapi);
				audioOutputSelector.Items.Add(wasapi.FriendlyName);
				i++;
			}

			if (outputs.Count > 0)
			{
				audioOutputSelector.SelectedIndex = 0;
			}
		}

		private void AudioOutput_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (output != null && output.PlaybackState != PlaybackState.Stopped)
			{
				output.Pause();
			}

			output = new WasapiOut(outputs[audioOutputSelector.SelectedIndex], AudioClientShareMode.Shared, true, outputLatency);

			bitsPrSample = output.OutputWaveFormat.BitsPerSample;
			sampleRate = output.OutputWaveFormat.SampleRate;
			channels = output.OutputWaveFormat.Channels;


			// Set the WaveFormat
			outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

			// (Re)Setup the mixer and buffers
			if (outputBufferTimer != null) outputBufferTimer.Stop();

			outputBuffer = new BufferedWaveProvider(internalFormatStereo);
			outputBuffer.ReadFully = true;
			outputBuffer.DiscardOnBufferOverflow = true;

			WdlResamplingSampleProvider resampler = new WdlResamplingSampleProvider(outputBuffer.ToSampleProvider(), outputFormat.SampleRate);

			outputMeter = new MeteringSampleProvider(resampler, samplesPrIntegration);
			outputMeter.StreamVolume += (a, b) => RunOutputMeter(a, b, meter);


			output.Init(outputMeter);
			output.Play();



			outputBufferTimer = new System.Windows.Forms.Timer();
			outputBufferTimer.Interval = mainServerInterval;
			outputBufferTimer.Tick += outputBufferTimerCheck;
			outputBufferTimer.Start();


			Console.WriteLine("SET OUTPUT FORMAT: "
				+ "Sample Rate: " + sampleRate
				+ ", BitsPrSasmple: " + bitsPrSample
				+ ", Channels: " + channels);
		}

		private void FormatSelectorInit()
		{
		/**
		 * AudioFormats:
		 * 0: PCM 32bit float 48kHz
		 * 1: G722 48khz stereo
		 * 2: G722 24khz mono
		 * 3: G722 16khz mono
		 * 4: OPUS 128 kbit/s stereo
		 * 5: OPUS 64 kbit/s stereo
		 * 6: OPUS 128 kbit/s mono
		 * 7: OPUS 64 kbit/s mono
		 * 8: OPUS 32 kbit/s mono
		 * 9: OPUS 16 kbit/s mono
		 **/
			audioFormatSelector.Items.Add("PCM 32bit float 48kHz (3000 kbit/s)");
			audioFormatSelector.Items.Add("G722 48kHz Stereo (384 kbit/s)");
			audioFormatSelector.Items.Add("G722 24kHz Mono (96 kbit/s)");
			audioFormatSelector.Items.Add("G722 16kHz Mono (64 kbit/s)");
			audioFormatSelector.Items.Add("OPUS stereo 128 kbit/s");
			audioFormatSelector.Items.Add("OPUS stereo 64 kbit/s");
			audioFormatSelector.Items.Add("OPUS mono 128 kbit/s");
			audioFormatSelector.Items.Add("OPUS mono 64 kbit/s");
			audioFormatSelector.Items.Add("OPUS mono 32 kbit/s");
			audioFormatSelector.Items.Add("OPUS mono 16 kbit/s");

			audioFormatSelector.SelectedIndex = 4;
		}

		private void AudioFormat_SelectedIndexChanged(object sender, EventArgs e)
		{
			audioFormatID = audioFormatSelector.SelectedIndex;
			SetupCodecResamplers(audioFormatSelector.SelectedIndex);

			if (connected)
			{
				byte[] cmd = GetCmdBytes(CMDS.SetClientAudioFormat, (byte)audioFormatSelector.SelectedIndex);
				client.Send(cmd);
			}

			if (outputBuffer != null)
			{
				FlushBuffer(outputBuffer);
			}
		}

		private void toggleLoopListen(Object sender, EventArgs e, int loop)
		{
			MocrButton button = (MocrButton)sender;
			if (connected)
			{
				if (loopListen[loop])
				{
					client.Send(GetCmdBytes(CMDS.StopListeningToLoop, (byte)loop));
				}
				else
				{
					client.Send(GetCmdBytes(CMDS.StartListeningToLoop, (byte)loop));
				}
			}
		}

		private void toggleLoopTalk(Object sender, EventArgs e, int loop)
		{
			string tks = "";
			foreach (KeyValuePair<int, bool> kvp in loopTalk)
			{
				tks += kvp.Value.ToString() + " ";
			}
			Console.WriteLine("TALKS1: " + tks);
			MocrButton button = (MocrButton)sender;
			if (connected)
			{
				if (loopTalk[loop])
				{
					for (int i = 0; i < numLoops; i++)
					{
						loopTalk[i] = false;
					}
					loopTalk[loop] = false;
					client.Send(GetCmdBytes(CMDS.StopTalkingToLoop, (byte)loop));
				}
				else
				{
					for (int i = 0; i < numLoops; i++)
					{
						loopTalk[i] = false;
					}
					loopTalk[loop] = true;
					client.Send(GetCmdBytes(CMDS.StartTalkingToLoop, (byte)loop));
				}
			}

			tks = "";
			foreach(KeyValuePair<int, bool> kvp in loopTalk)
			{
				tks += kvp.Value.ToString() + " ";
			}

			Console.WriteLine("TALKS2: " + tks);
		}

		private void SetInputMode(int mode)
		{
			if(inputFormat.Channels == 1)
			{
				// TURN OFF ALL MODES
				inputModeButtons[0].setLitState(false);
				inputModeButtons[1].setLitState(false);
				inputModeButtons[2].setLitState(false);
				inputModeButtons[3].setLitState(false);
			}
			else
			{
				inputMode = mode;
				inputModeButtons[0].setLitState(false);
				inputModeButtons[1].setLitState(false);
				inputModeButtons[2].setLitState(false);
				inputModeButtons[3].setLitState(false);

				inputModeButtons[mode].setLitState(true);
			}
		}

		private void UpdateBufferDisp()
		{
			int inBufferDuration = (int)Math.Floor(inputBuffer.BufferedDuration.TotalMilliseconds);
			if (inBufferDuration > 9999) inBufferDuration = 9999;

			inBufferDisp.setValue(inBufferDuration.ToString());

			int outBufferDuration = (int)Math.Floor(outputBuffer.BufferedDuration.TotalMilliseconds);
			if (outBufferDuration > 9999) outBufferDuration = 9999;

			outBufferDisp.setValue(outBufferDuration.ToString());

			if (bytesSent.Count >= 50)
			{
				// TRIM Shipments down to last 10
				while (bytesSent.Count > 50)
				{
					bytesSent.RemoveAt(0);
				}

				// SUM ALL BYTES
				long sum = 0;
				foreach (shipment s in bytesSent)
				{
					sum += s.size;
				}

				// Find timespan
				DateTime oldest = bytesSent[0].time;
				DateTime newest = bytesSent[49].time;
				TimeSpan span = newest - oldest;

				// Bytes pr. second
				double bytesps = sum / span.TotalSeconds;

				// Bits pr. second
				int bps = (int)Math.Ceiling((bytesps * 8) / 1024);
				bandwidthDisp.setValue(bps.ToString());
			}
			else
			{
				bandwidthDisp.setValue("");
			}
		}

		private void FlushBuffer(object sender, EventArgs e, BufferedWaveProvider buffer)
		{
			FlushBuffer(buffer);
		}

		private void FlushBuffer(BufferedWaveProvider buffer)
		{
			buffer.ClearBuffer();
		}

		private void ToggleLoopback(object sender, EventArgs e)
		{
			if(loopback)
			{
				loopbackButton.setLightColor(MocrButton.color.BLANK);
				loopbackButton.setLitState(false);
				loopback = false;
			}
			else
			{
				loopbackButton.setLightColor(MocrButton.color.AMBER);
				loopbackButton.setLitState(true);
				loopback = true;
			}
		}

		private void SetTransmitMode(object sender, EventArgs e, int mode)
		{
			transmitMode = mode;
			switch(mode)
			{
				case 0: // OPEN
					transmitOpenButton.setLitState(true);
					transmitPTTButton.setLitState(false);
					break;
				case 1: // PTT
					transmitOpenButton.setLitState(false);
					transmitPTTButton.setLitState(true);
					if(PTTkey == null)
					{
						AskForPTTKey();
					}
					break;
			}
		}

		private void AskForPTTKey(object sender, EventArgs e) { AskForPTTKey(); }

		private void AskForPTTKey()
		{
			askingForPTTkey = true;
			askForPTTKeyBox.Visible = true;
			askForPTTKeyBox.BringToFront();
		}
	
	}
}
