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
		private void UpdateMeters()
		{
			for (int i = 0; i < numLoops; i++)
			{
				SetMeterLevel(loops[i].outgoingSamples, i / 2, (i % 2) + 1);
			}
		}

		private void SetMeterLevel(byte[] samples, int meter, int scale)
		{
			float avg = 0;
			float sum = 0;
			float max = 0;
			short count = 0;
			float val;

			float[] values = Converters.bytes2floats(samples);

			for (int i = 0; i < values.Length - 3; i += 4)
			{
				val = values[i];

				if (val == float.MinValue)
				{
					val = float.MaxValue;
				}
				else
				{
					val = Math.Abs(val);
				}
				sum += val;

				if (val > max) max = val;

				count++;
			}

			if (sum == 0)
			{
				avg = 0.001f;
			}
			else
			{
				avg = sum / count;
			}

			double dbfs = Math.Log10(max) * 20;

			// Return Time




			if (scale == 1)
			{
				double currentDbFS = meters[meter].getValue1() - 18 - (returnPrSec * (mainServerInterval / 1000d));
				if (currentDbFS < dbfs) currentDbFS = dbfs;
				double dbu = currentDbFS + 18;
				meters[meter].setValue1((float)dbu);
			}
			else
			{
				double currentDbFS = meters[meter].getValue2() - 18 - (returnPrSec * (mainServerInterval / 1000d));
				if (currentDbFS < dbfs) currentDbFS = dbfs;
				double dbu = currentDbFS + 18;
				meters[meter].setValue2((float)dbu);
			}

		}
	}
}
