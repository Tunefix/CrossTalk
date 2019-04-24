using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	public partial class Server : Form
	{
		Dictionary<int, MMDevice> outputs = new Dictionary<int, MMDevice>();
		WasapiOut output = null;
		WaveFormat outputFormat;
		WaveFormat internalFormatStereo = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

		ComboBox audioOutputSelector;

		int bitsPrSample = 32;
		int sampleRate = 48000;
		int channels = 2;

		int outputLatency = 10;

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

			pflBuffer = new BufferedWaveProvider(internalFormatStereo);
			pflBuffer.ReadFully = true;
			pflBuffer.DiscardOnBufferOverflow = true;

			WdlResamplingSampleProvider resampler = new WdlResamplingSampleProvider(pflBuffer.ToSampleProvider(), outputFormat.SampleRate);



			output.Init(resampler);
			output.Play();

			Console.WriteLine("SET OUTPUT FORMAT: "
				+ "Sample Rate: " + sampleRate
				+ ", BitsPrSasmple: " + bitsPrSample
				+ ", Channels: " + channels);
		}
	}
}
