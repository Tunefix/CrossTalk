using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		bool processAudio = true;

		private void processIncomingAudioData(byte[] data, Client sender)
		{
			addIncomingAudioToClientBuffer(sender.decoder.Decode(data, sender.audioFormat), sender);
		}

		private void addIncomingAudioToClientBuffer(byte[] data, Client sender)
		{
			sender.inputStream.AddSamples(data, 0, data.Length);

			// Check for length of buffer and trow away samples if needed
			double buffer_length = sender.inputStream.BufferedDuration.TotalMilliseconds;
			if(buffer_length > 400)
			{
				int bytes_to_trow = (int)((sampleRate / 1000) * channels * 200);
				//int bytes_to_trow = (int)((sampleRate / 1000) * channels * (buffer_length - 400));
				byte[] null_buffer = new byte[bytes_to_trow];
				sender.inputStream.Read(null_buffer, 0, bytes_to_trow);
			}
		}

		private void FetchAudio()
		{
			// DETERMINE MAX AND MIN CLIENTS INPUT BUFFER LENGTHS
			processAudio = true;
			double maxBuffer = 0;
			double minBuffer = double.MaxValue;
			double bufferLength;

			// NOMINAL NUMBER OF BYTES TO GET FROM BUFFERS
			int bytes = 2 * (int)Math.Ceiling(mainServerInterval * (serverFormat.SampleRate / 1000d) * 4 * serverFormat.Channels);

			// ALIGN BY WHOLE SAMPLES 2ch * 4bytes pr channel
			while (bytes % 8 != 0)
			{
				bytes++;
			}


			Monitor.Enter(clientsLock);

			foreach (KeyValuePair<string, Client> kvp in clients)
			{
				Client c = kvp.Value;

				TimeSpan packet = DateTime.Now - c.lastPacket;

				if (c.IsConnected() && packet.TotalMilliseconds < 200)
				{
					bufferLength = c.inputStream.BufferedBytes;
					if (bufferLength > maxBuffer) maxBuffer = bufferLength;
					if (bufferLength < minBuffer) minBuffer = bufferLength;
				}
			}


			// IF THE CLINET WITH THE LEAST AMOUNT OF AUDIO BUFFERED DON'T
			// HAVE ENOUGH, DON'T PROCESS AUDIO THIS TICK
			if (minBuffer < bytes)
			{
				processAudio = false;
			}

			// UNLESS - THE CLIENT WITH THE MOST AMOUNT OF AUDIO HAS MORE
			// THAN DOUBLE THE NUMBER OF BYTES REQUIRED
			if (maxBuffer > bytes * 2)
			{
				//processAudio = true;
			}

			if (processAudio)
			{
				foreach (KeyValuePair<string, Client> kvp in clients)
				{
					Client c = kvp.Value;
					if (c.IsConnected())
					{
						c.currentSamples = new byte[bytes];
						c.inputStream.Read(c.currentSamples, 0, bytes);
					}
				}
			}
			
			Monitor.Exit(clientsLock);
		}

		private void MixAudio()
		{
			List<float> output;
			List<float> pfl;
			int currentSamples = 0;
			int connectedClients = 0;

			// MIX CLIENTS AUDIO
			lock (clientsLock)
			{
				foreach (KeyValuePair<string, Client> kvp in clients)
				{
					Client c = kvp.Value;
					if (c.IsConnected())
					{
						connectedClients++;
						output = new List<float>();
						for(int i = 0; i < c.currentSamples.Length / 4; i++) { output.Add(0f); }

						lock (c.sourcesLock)
						{
							foreach (int loop in c.GetSources())
							{
								Loop l = loops[loop];
								lock (l.talkersLock)
								{
									foreach (string talker in l.talkers)
									{
										if (clients[talker].IsConnected() && (c.guid != talker || NminusOne == false))
										{
											output = mixSamples(output, clients[talker]);
										}
									}
								}
							}
						}

						c.outgoingSamples = Converters.floats2bytes(output.ToArray());
					}
					else
					{
						c.outgoingSamples = null;
					}
					currentSamples = c.currentSamples.Length;
				}

				// MIX LOOP AUDIO FOR METERING AND SERVER PFL
				pfl = new List<float>();
				for (int i = 0; i < currentSamples / 4; i++) { pfl.Add(0f); }

				foreach (Loop loop in loops)
				{
					output = new List<float>();
					for (int i = 0; i < currentSamples / 4; i++) { output.Add(0f); }

					foreach (string talker in loop.talkers)
					{
						output = mixSamples(output, clients[talker]);
					}
					loop.outgoingSamples = Converters.floats2bytes(output.ToArray());

					if(loop.pfl)
					{
						pfl = mixSamples(pfl, output);
					}
				}
				byte[] bytes = Converters.floats2bytes(pfl.ToArray());

				// If no clients are connected, ignore pfl
				if (connectedClients > 0)
				{
					pflBuffer.AddSamples(bytes, 0, bytes.Length);

					// Keep pflBuffer below 200ms
					if (pflBuffer.BufferedDuration.TotalMilliseconds > 200)
					{
						double bytes_to_read = (sampleRate / 1000) * channels * (pflBuffer.BufferedDuration.TotalMilliseconds - 200);
						byte[] void_array = new byte[(int)bytes_to_read];
						pflBuffer.Read(void_array, 0, (int)bytes_to_read);

						Logger.WriteLine("Trew away " + bytes_to_read.ToString() + " bytes from pflBuffer");
					}
				}
			}
		}

		private void SendAudio()
		{
			// SEND AUDIO TO CLIENTS
			lock (clientsLock)
			{
				foreach (KeyValuePair<string, Client> kvp in clients)
				{
					Client c = kvp.Value;
					if (c.IsConnected() && c.outgoingSamples != null && c.outgoingSamples.Length > 0)
					{
						byte[] encoded_data = c.encoder.Encode(c.outgoingSamples, c.audioFormat);
						if (encoded_data != null)
						{
							udpServer.Reply(encoded_data, c.endpoint);
						}
					}
				}
			}

			// SEND AUDIO TO PFL
		}

		private List<float> mixSamples(List<float> samples, Client client)
		{
			List<float> output = new List<float>();

			int bytes = client.currentSamples.Length;
			int sampleCount = samples.Count;

			while (output.Count < bytes / 4)
			{
				output.Add(0f);
			}

			for (int i = 0; i < bytes / 4; i++)
			{
				if (sampleCount <= i)
				{
					output[i] = BitConverter.ToSingle(client.currentSamples, i * 4);
				}
				else
				{
					output[i] = samples[i] + BitConverter.ToSingle(client.currentSamples, i * 4);
				}
			}

			return output;
		}

		private List<float> mixSamples(List<float> samples, List<float> samples2)
		{
			List<float> output = new List<float>();

			for (int i = 0; i < samples.Count; i++)
			{
				output.Add(samples[i] + samples2[i]);
			}

			return output;
		}
	}
}
