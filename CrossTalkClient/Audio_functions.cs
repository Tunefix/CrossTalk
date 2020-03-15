using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public partial class Client : Form
	{
		DateTime lastAudioInput = DateTime.Now;
		byte[] overflowBytes = new byte[0];

		int inputGain = 0; // In dB. Can be both positive and negative.
		float inputGainMultiplier = 1; // The actual value multiplied with input samples. Calculated.

		int outputGain = 0; // In dB. Can be both positive and negative.
		float outputGainMultiplier = 1; // The actual value multiplied with output samples. Calculated.

		/// <summary>
		/// If loopback is set to true, the audio data is not transmitted to the server,
		/// but is instead put directly into the ouputBuffer but still goes through the encoder
		/// and decoder.
		/// Set loopback to true for local testing of the Client.
		/// This could be a button on an advanced client panel.
		/// </summary>
		bool loopback = false; 

		BufferedWaveProvider outputBuffer;
		BufferedWaveProvider inputBuffer;
		WdlResamplingSampleProvider inputResampler;
		/***
		 * To acheive good results, it is only possible to resample audio in-transit.
		 * This means in practice:
		 *   outputBuffer -> resampler -> metering -> output
		 *   input byte[] -> input processing -> inputBuffer -> resampler -> SendToServer
		 ***/

		private void MoveInputGain(int offset)
		{
			SetInputGain(inputGain + offset);
			StoreSetting("input_gain", inputGain.ToString());
		}
		private void SetInputGain(int gain)
		{
			inputGain = gain;
			inputGainDisp.setValue(inputGain.ToString());
			// The base is the 6th root of 2, but we can't do that here so we do the inverse (2 to the 1/6th power) (~1.122462048)
			inputGainMultiplier = (float)Math.Pow(Math.Pow(2d, 1d / 6d), inputGain);
		}

		private void MoveOutputGain(int offset)
		{
			SetOutputGain(outputGain + offset);
			StoreSetting("output_gain", outputGain.ToString());
		}
		private void SetOutputGain(int gain)
		{
			outputGain = gain;
			outputGainDisp.setValue(outputGain.ToString());
			// The base is the 6th root of 2, but we can't do that here so we do the inverse (2 to the 1/6th power) (~1.122462048)
			outputGainMultiplier = (float)Math.Pow(Math.Pow(2d, 1d / 6d), outputGain);
		}

		private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
		{
			byte[] bytes = new byte[e.BytesRecorded];
			Buffer.BlockCopy(e.Buffer, 0, bytes, 0, e.BytesRecorded);
			PutAudioInInputbuffer(bytes);
		}

		private void PutAudioInInputbuffer(byte[] bytes)
		{
			inputBuffer.AddSamples(bytes, 0, bytes.Length);
		}

		private void initialAudioProccessing(byte[] bytes)
		{
			float[] floats = Converters.bytes2floats(bytes);

			for (int i = 0; i < floats.Length; i += inputChannels)
			{
				// WE ARE NOW ONLY PROCESSING THE FIRST CHANNEL IN THE INPUT
				if (inputFormat.Channels == 1)
				{
					ProcessFloatSample(floats[i]);
				}
				else
				{
					switch(inputMode)
					{
						case 0: // LR
							ProcessFloatSample(floats[i], floats[i + 1]);
							break;
						case 1: // LL
							ProcessFloatSample(floats[i], floats[i]);
							break;
						case 2: // RR
							ProcessFloatSample(floats[i + 1], floats[i + 1]);
							break;
						case 3: // RL
							ProcessFloatSample(floats[i + 1], floats[i]);
							break;
					}
				}
			}
		}

		private float AddInputGain(float sample)
		{
			return sample * inputGainMultiplier;
		}


		private void ProcessFloatSample(float sample) { ProcessFloatSample(sample, sample); }
		private void ProcessFloatSample(float sampleL, float sampleR)
		{
			// APPLY INPUT GAIN
			float gSampleL = AddInputGain(sampleL);
			float gSampleR = AddInputGain(sampleR);


			// MAKE COMBINED SAMPLE FOR INTEGRATION
			float sample = gSampleL + gSampleR;

			lock (integrationCollection)
			{
				integrationCollection.Add(sample);
				if (integrationCollection.Count >= samplesPrIntegration)
				{
					IntergrateAndUpdateMeter(integrationCollection.ToList(), meter);
					integrationCollection.Clear();
				}
			}

			lock (inputSamples)
			{
				inputSamples.Add(gSampleL);
				inputSamples.Add(gSampleR);

				if (inputSamples.Count >= sampleSize)
				{
					if (transmitMode == 1 && !PTTopen)
					{
						// TRANSMIT SILENCE
						for(int i = 0; i < inputSamples.Count; i++)
						{
							inputSamples[i] = 0f;
						}
					}
						if (loopback)
						{
							byte[] bytes = Encode(inputSamples.ToArray(), audioFormatID);
							if (bytes != null)
							{
								BufferSound(Decode(bytes, audioFormatID), outputBuffer, false, true);
							}
						}
						else if (connected && !disconnecting)
						{
							byte[] bytes = Encode(inputSamples.ToArray(), audioFormatID);
							if (bytes != null)
							{
								client.Send(bytes);
								bytesSent.Add(new shipment(DateTime.Now, bytes.Length));
							}
							//Console.WriteLine(DateTime.Now.TimeOfDay + ": SHIPPING AUDIO, " + inputSamples.Count + " floats");
						}
					
					inputSamples.Clear();
				}
			}
		}

		private void BufferSound(byte[] bytes, BufferedWaveProvider buffer, bool mono2stereo)
		{
			BufferSound(Converters.bytes2floats(bytes), buffer, mono2stereo);
		}
		private void BufferSound(byte[] bytes, BufferedWaveProvider buffer, bool mono2stereo, bool outputGain)
		{
			BufferSound(Converters.bytes2floats(bytes), buffer, mono2stereo, outputGain);
		}

		private void BufferSound(float[] floats, BufferedWaveProvider buffer, bool mono2stereo)
		{
			BufferSound(floats, buffer, mono2stereo, false);
		}
		private void BufferSound(float[] floats, BufferedWaveProvider buffer, bool mono2stereo, bool outputGain)
		{
			List<byte> fs = new List<byte>();
			int i;
			byte[] vb;
			float multi;

			if(outputGain)
			{
				multi = outputGainMultiplier;
			}
			else
			{
				multi = 1;
			}

			// If mono2stereo is enabled (using a mono-codec), step one each time
			// If mono2stereo is false (using a stereo-codec), step two each time (read l + r sample)
			int a = mono2stereo ? 1 : 2;
			for (i = 0; i < floats.Length; i += 2)
			{
				// get bytes
				vb = BitConverter.GetBytes(floats[i] * multi);

				// Left channel
				if (outputMode == 2)
				{
					// RIGHT OUTPUT ONLY, ADD SILENCE TO LEFT
					byte[] sb = BitConverter.GetBytes(0f);
					fs.Add(sb[0]);
					fs.Add(sb[1]);
					fs.Add(sb[2]);
					fs.Add(sb[3]);
				}
				else
				{
					fs.Add(vb[0]);
					fs.Add(vb[1]);
					fs.Add(vb[2]);
					fs.Add(vb[3]);
				}

				// Right channel
				if (!mono2stereo)
				{
					// Fetch sample for right channel
					vb = BitConverter.GetBytes(floats[i + 1] * multi);
				}

				if (outputMode == 1)
				{
					// LEFT OUTPUT ONLY, ADD SILENCE TO RIGHT
					byte[] sb = BitConverter.GetBytes(0f);
					fs.Add(sb[0]);
					fs.Add(sb[1]);
					fs.Add(sb[2]);
					fs.Add(sb[3]);
				}
				else
				{
					// Right channel
					fs.Add(vb[0]);
					fs.Add(vb[1]);
					fs.Add(vb[2]);
					fs.Add(vb[3]);
				}
			}

			// EMPTY CHANNEL IF OUTPUT MODE IS NOT LR
			if(outputMode == 1)
			{
				// LEFT OUTPUT ONLY

			}
			if(outputMode == 2)
			{
				// RIGHT OUTPUT ONLY
			}

			addSamplesToBuffer(fs, buffer);
		}

		private void addSamplesToBuffer(List<byte> samples, BufferedWaveProvider buffer)
		{
			try
			{
				buffer.AddSamples(samples.ToArray(), 0, samples.Count);
				//Console.WriteLine(DateTime.Now.TimeOfDay + ": ADDED " + (samples.Count / 4) + " samples to output buffer.");

				// Check Buffer length and trim if necessary
				double buffer_millisecs = buffer.BufferedDuration.TotalMilliseconds;
				if (buffer_millisecs > 400)
				{
					double bytes_to_read = (sampleRate / 1000) * channels * 200;
					//double bytes_to_read = (sampleRate / 1000) * channels * (buffer_millisecs - 400);
					byte[] void_array = new byte[(int)bytes_to_read];
					buffer.Read(void_array, 0, (int)bytes_to_read);

					Logger.WriteLine("Trew away " + bytes_to_read.ToString() + " bytes");
				}

			}
			catch
			{
				Logger.WriteLine("Form Closed, exception thrown, ignore.");
			}
		}


		private void FetchAudioFromInputBuffer()
		{
			if (inputBuffer != null)
			{
				double samplesToFetch = (inputBuffer.BufferedDuration.TotalMilliseconds - 50d) * ((internalFormatStereo.SampleRate * internalFormatStereo.Channels) / 1000d);

				// CHECK FOR WHOLE NUMBER
				if (samplesToFetch - (int)samplesToFetch != 0)
				{
					throw new ApplicationException("FetchAudioFromInputBuffer cannot fetch fractional samples. Adjust either internal sample rate or main server interval.");
				}

				// ONLY FETCH AUDIO IF BUFFER CONAINS ENOUGH
				if (inputBuffer.BufferedBytes > samplesToFetch && inputBuffer.BufferedDuration.TotalMilliseconds > 100)
				{
					float[] input = new float[(int)samplesToFetch];
					inputResampler.Read(input, 0, (int)samplesToFetch);

					initialAudioProccessing(Converters.floats2bytes(input));
				}
			}
		}

	}
}

