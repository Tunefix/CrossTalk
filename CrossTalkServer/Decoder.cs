using Concentus.Enums;
using Concentus.Structs;
using NAudio.Codecs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CrossTalkServer
{
	class Decoder
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

		G722Codec decode1L = new G722Codec();
		G722Codec decode1R = new G722Codec();
		G722CodecState decode1Lstate = new G722CodecState(64000, G722Flags.None);
		G722CodecState decode1Rstate = new G722CodecState(64000, G722Flags.None);

		G722Codec decode2 = new G722Codec();
		G722CodecState decode2State = new G722CodecState(64000, G722Flags.None);
		Resampler format2upSampler = new Resampler(WaveFormat.CreateIeeeFloatWaveFormat(16000, 2), WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

		
		OpusDecoder opus_decoder = OpusDecoder.Create(48000, 2);

		int format;

		public byte[] Decode(byte[] data, int format)
		{
			byte[] output = null;
			switch(format)
			{
				case 0: // PCM 32 bit float 48kHz Stereo
					output = DecodePassThrough(data);
					break;
				case 1: // G722 48kHz stereo
					output = Format1(data);
					break;
				case 2: // G722 24kHz mono
					output = Format2(data);
					break;
				case 3: // G722 16kHz mono
					output = Format2(data);
					break;
				case 4: // OPUS (128 kbit/s stereo)
				case 6: // OPUS (128 kbit/s mono)
					return DecodeOpus(data, 320);
				case 5: // OPUS (64 kbit/s stereo)
				case 7: // OPUS (64 kbit/s mono)
					return DecodeOpus(data, 160);
				case 8: // OPUS (32 kbit/s mono)
					return DecodeOpus(data, 80);
				case 9: // OPUS (16 kbit/s mono)
					return DecodeOpus(data, 40);
				default:
					output = DecodePassThrough(data);
					break;
			}
			return output;
		}

		public void SetFormat(int format)
		{
			this.format = format;
			switch(format)
			{
				case 2:
					format2upSampler = new Resampler(WaveFormat.CreateIeeeFloatWaveFormat(24000, 2), WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
					break;
				case 3:
					format2upSampler = new Resampler(WaveFormat.CreateIeeeFloatWaveFormat(16000, 2), WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
					break;
			}
		}

		private byte[] DecodePassThrough(byte[] data)
		{
			return data;
		}

		private byte[] Format1(byte[] data)
		{
			// SPLIT INTO TWO PARTS
			byte[] L = new byte[data.Length / 2];
			byte[] R = new byte[data.Length / 2];

			Array.Copy(data, L, L.Length);
			Array.Copy(data, L.Length, R, 0, R.Length);


			short[] outputL = new short[L.Length * 2];
			short[] outputR = new short[R.Length * 2];
			decode1L.Decode(decode1Lstate, outputL, L, L.Length);
			decode1R.Decode(decode1Rstate, outputR, R, R.Length);

			short[] output = Converters.MuxDualMono(outputL, outputR);

			return Converters.floats2bytes(Converters.shorts2floats(output, true));
		}

		private byte[] Format2(byte[] data)
		{
			short[] output = new short[data.Length * 2];
			decode2.Decode(decode2State, output, data, data.Length);

			float[] f = Converters.shorts2floats(output, true);
			float[] s = Converters.mono2stereo(f);

			byte[] o = format2upSampler.inputResample(Converters.floats2bytes(s), s.Length);

			return o;
		}

		private byte[] DecodeOpus(byte[] data, int packetSize)
		{
			// Decoding loop
			int frames = data.Length / packetSize;
			int frameSize = 960; // must be same as framesize used in input, you can use OpusPacketInfo.GetNumSamples() to determine this dynamically
			short[] outputBuffer;
			List<float> outData = new List<float>();

			for (int i = 0; i < frames; i++)
			{
				outputBuffer = new short[frameSize * 2];

				int thisFrameSize = opus_decoder.Decode(data, i * packetSize, packetSize, outputBuffer, 0, frameSize, false);

				outData.AddRange(Converters.shorts2floats(outputBuffer, true));
			}

			return Converters.floats2bytes(outData.ToArray());
		}

	}

	class Encoder
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

		G722Codec encode1L = new G722Codec();
		G722Codec encode1R = new G722Codec();
		G722CodecState encode1Lstate = new G722CodecState(64000, G722Flags.None);
		G722CodecState encode1Rstate = new G722CodecState(64000, G722Flags.None);

		G722Codec encode2 = new G722Codec();
		G722CodecState encode2State = new G722CodecState(64000, G722Flags.None);
		Resampler format2DownSampler = new Resampler(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), WaveFormat.CreateIeeeFloatWaveFormat(16000, 2));

		OpusEncoder opus_encoder = OpusEncoder.Create(48000, 2, OpusApplication.OPUS_APPLICATION_AUDIO);
		List<short> opus_data = new List<short>();
		List<byte> opus_compdata = new List<byte>();

		int format;

		public byte[] Encode(byte[] data, int format)
		{
			switch(format)
			{
				case 0: // PCM 32bit float 48kHz
					return Format0(data);
				case 1: // G722 (48kHz stereo)
					return Format1(data);
				case 2: // G722 (24kHz mono)
				case 3: // G722 (16kHz mono)
					return Format2(data);
				case 4: // OPUS (128 kbit/s stereo)
				case 5: // OPUS (64 kbit/s stereo)
				case 6: // OPUS (128 kbit/s mono)
				case 7: // OPUS (64 kbit/s mono)
				case 8: // OPUS (32 kbit/s mono)
				case 9: // OPUS (16 kbit/s mono)
					return EncodeOpus(data);
				default:
					return Format0(data);
			}
		}

		public void SetFormat(int format)
		{
			this.format = format;
			switch (format)
			{
				case 2:
					format2DownSampler = new Resampler(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), WaveFormat.CreateIeeeFloatWaveFormat(24000, 2));
					break;
				case 3:
					format2DownSampler = new Resampler(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), WaveFormat.CreateIeeeFloatWaveFormat(16000, 2));
					break;
				case 4:
					opus_encoder.Bitrate = 128000;
					opus_encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
					opus_encoder.UseVBR = false;
					opus_encoder.ForceChannels = 2;
					break;
				case 5:
					opus_encoder.Bitrate = 64000;
					opus_encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
					opus_encoder.UseVBR = false;
					opus_encoder.ForceChannels = 2;
					break;
				case 6:
					opus_encoder.Bitrate = 128000;
					opus_encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
					opus_encoder.UseVBR = false;
					opus_encoder.ForceChannels = 1;
					break;
				case 7:
					opus_encoder.Bitrate = 64000;
					opus_encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_FULLBAND;
					opus_encoder.UseVBR = false;
					opus_encoder.ForceChannels = 1;
					break;
				case 8:
					opus_encoder.Bitrate = 32000;
					opus_encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
					opus_encoder.UseVBR = false;
					opus_encoder.ForceChannels = 1;
					break;
				case 9:
					opus_encoder.Bitrate = 16000;
					opus_encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND;
					opus_encoder.UseVBR = false;
					opus_encoder.ForceChannels = 1;
					break;
			}
		}

		private byte[] Format0(byte[] data)
		{
			return data;
		}

		private byte[] Format1(byte[] data)
		{
			short[] shorts = Converters.floats2shorts(Converters.bytes2floats(data), true);

			// DeMux into Left and Right
			List<short[]> lr = Converters.deMuxStereo(shorts);

			// CALCULATE NUMBER OF BYTES IN OUTPUT
			int bytes = (int)(lr[0].Length / 2);
			byte[] outputL = new byte[bytes];
			byte[] outputR = new byte[bytes];

			encode1L.Encode(encode1Lstate, outputL, lr[0], lr[0].Length);
			encode1R.Encode(encode1Rstate, outputR, lr[1], lr[1].Length);

			byte[] output = new byte[outputL.Length + outputR.Length];
			Array.Copy(outputL, output, outputL.Length);
			Array.Copy(outputR, 0, output, outputL.Length, outputR.Length);
			return output;
		}

		private byte[] Format2(byte[] data)
		{
			byte[] rs = format2DownSampler.inputResample(data, data.Length / 4);
			float[] f = Converters.bytes2floats(rs);

			float[] mono = Converters.stereo2mono(f);

			short[] shorts = Converters.floats2shorts(mono, true);

			int bytes = (int)(shorts.Length / 2);
			byte[] output = new byte[bytes];

			encode2.Encode(encode2State, output, shorts, shorts.Length);
			return output;
		}


		/**
		 * FORMAT 3, 4, 5
		 * OPUS 64kbit/s, 32 kbit/s, 16 kbit/s
		 **/
		private byte[] EncodeOpus(byte[] data_b)
		{
			float[] data = Converters.bytes2floats(data_b);

			// STORE UP DATA UNTIL WE HAVE ENOUGH TO ENCODE A FRAME
			opus_data.AddRange(Converters.floats2shorts(data, true));

			// CLEAR THE COMPRESSED DATA
			opus_compdata.Clear();

			while (opus_data.Count > 960 * 2)
			{

				// Encoding loop
				short[] inputAudioSamples = opus_data.GetRange(0, 960 * 2).ToArray();

				opus_data.RemoveRange(0, 960 * 2);

				byte[] outputBuffer = new byte[1000];
				int frameSize = 960;

				int thisPacketSize = opus_encoder.Encode(inputAudioSamples, 0, frameSize, outputBuffer, 0, outputBuffer.Length); // this throws OpusException on a failure, rather than returning a negative number


				byte[] truncArray = new byte[thisPacketSize];

				Array.Copy(outputBuffer, truncArray, truncArray.Length);

				opus_compdata.AddRange(truncArray);
			}

			if (opus_compdata.Count > 0)
			{
				return opus_compdata.ToArray();
			}
			return null;
		}
	}
}
