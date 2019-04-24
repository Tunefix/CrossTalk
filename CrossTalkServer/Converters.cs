using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossTalkServer
{
	static class Converters
	{
		static public byte[] floats2bytes(float[] input)
		{
			byte[] output = new byte[input.Length * 4];
			Buffer.BlockCopy(input, 0, output, 0, output.Length);

			return output;
		}

		static public float[] bytes2floats(byte[] input)
		{
			if (input.Length % 4 != 0)
			{
				Console.WriteLine(new StackFrame(1, true).GetMethod().Name);
				throw new ArgumentException("Bytes2Floats input is not a multiple of 4, this will create trouble. Input was " + input.Length + " bytes.");
			}

			float[] output = new float[input.Length / 4];
			Buffer.BlockCopy(input, 0, output, 0, input.Length);

			return output;
		}

		static public short[] floats2shorts(float[] floats, bool audio)
		{
			List<short> output = new List<short>();
			foreach (float f in floats)
			{
				output.Add((short)(f * Int16.MaxValue));
			}
			return output.ToArray();
		}

		static public float[] shorts2floats(short[] shorts, bool audio)
		{
			List<float> output = new List<float>();
			foreach (short s in shorts)
			{
				output.Add((float)(s / (float)Int16.MaxValue));
			}
			return output.ToArray();
		}

		static public byte[] shorts2bytes(short[] input)
		{
			byte[] output = new byte[input.Length * 2];
			Buffer.BlockCopy(input, 0, output, 0, output.Length);

			return output;
		}

		static public short[] bytes2shorts(byte[] input)
		{
			if (input.Length % 2 != 0)
			{
				Console.WriteLine(new StackFrame(1, true).GetMethod().Name);
				throw new ArgumentException("Bytes2Shorts input is not a multiple of 2, this will create trouble. Input was " + input.Length + " bytes.");
			}

			short[] output = new short[input.Length / 2];
			Buffer.BlockCopy(input, 0, output, 0, input.Length);

			return output;
		}

		/// <summary>
		/// Converts mono float to stereo float
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		static public byte[] mono2stereo(byte[] input)
		{
			List<byte> bytes = new List<byte>();
			for(int i = 0; i < input.Length; i += 4)
			{
				// LEFT
				bytes.Add(input[i]);
				bytes.Add(input[i + 1]);
				bytes.Add(input[i + 2]);
				bytes.Add(input[i + 3]);

				// Right
				bytes.Add(input[i]);
				bytes.Add(input[i + 1]);
				bytes.Add(input[i + 2]);
				bytes.Add(input[i + 3]);
			}

			return bytes.ToArray();
		}

		static public float[] mono2stereo(float[] input)
		{
			List<float> bytes = new List<float>();
			for (int i = 0; i < input.Length; i++)
			{
				// LEFT
				bytes.Add(input[i]);

				// Right
				bytes.Add(input[i]);
			}

			return bytes.ToArray();
		}

		/// <summary>
		/// Converts stereo float to mono float
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		static public byte[] stereo2mono(byte[] input)
		{
			List<byte> bytes = new List<byte>();
			byte[] b;
			float l;
			float r;
			float s;

			for (int i = 0; i < input.Length; i += 8)
			{
				l = BitConverter.ToSingle(input, i);
				r = BitConverter.ToSingle(input, i + 4);
				s = (l + r) / 2;

				b = BitConverter.GetBytes(s);

				bytes.Add(b[0]);
				bytes.Add(b[1]);
				bytes.Add(b[2]);
				bytes.Add(b[3]);
			}

			return bytes.ToArray();
		}

		static public float[] stereo2mono(float[] input)
		{
			List<float> output = new List<float>();
			float l;
			float r;
			float s;

			for (int i = 0; i < input.Length; i += 2)
			{
				l = input[i];
				r = input[i + 1];
				s = (l + r) / 2;

				output.Add(s);
			}

			return output.ToArray();
		}

		/// <summary>
		/// Takes an short[] with stereo samples (LRLRLRLR)
		/// And returns two short[] with LLLL and RRRR
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		static public List<short[]> deMuxStereo(short[] st)
		{
			List<short> l = new List<short>();
			List<short> r = new List<short>();

			for (int i = 0; i < st.Length; i += 2)
			{
				l.Add(st[i]);
				r.Add(st[i + 1]);
			}
			List<short[]> output = new List<short[]>();
			output.Add(l.ToArray());
			output.Add(r.ToArray());
			return output;
		}

		/// <summary>
		/// Takes an short[] with stereo samples (LRLRLRLR)
		/// And returns two short[] with LLLL and RRRR
		/// </summary>
		/// <param name="st"></param>
		/// <returns></returns>
		static public short[] MuxDualMono(short[] l, short[] r)
		{
			List<short> output = new List<short>();

			for (int i = 0; i < l.Length; i++)
			{
				output.Add(l[i]);
				output.Add(r[i]);
			}

			return output.ToArray();
		}
	}
}
