using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public partial class Client : Form
	{
		// METERING
		static float returnTime = 1700f; //ms
		static float returnDist = 20f; //dB
		float returnPrSec = (returnDist / returnTime) * 1000f;
		double currentDbFS = -60;
		double currentOutDbFS = -60;
		DateTime lastOutputMeter = DateTime.Now;
		TimeSpan OutputMeterSpan;

		private void RunOutputMeter(Object sender, StreamVolumeEventArgs e, VerticalMeter meter)
		{
			OutputMeterSpan = DateTime.Now - lastOutputMeter;
			lastOutputMeter = DateTime.Now;
			// Return Time
			currentOutDbFS -= returnPrSec * (OutputMeterSpan.TotalMilliseconds / 1000d);

			double max = 0;
			if (e.MaxSampleValues[0] < 1)
			{
				max = 20 * Math.Log10(e.MaxSampleValues[0]);
			}
			else if (e.MaxSampleValues[0] >= 1)
			{
				max = 20;
			}

			if (currentOutDbFS < max) currentOutDbFS = max;

			if (!float.IsNaN(e.MaxSampleValues[0]))
			{
				Invoke(new Action(() =>
				{
					meter.setValue2((int)currentOutDbFS + 18);
				}));
			}
			else
			{
				Invoke(new Action(() => { meter.setValue2(-40); }));
			}
		}

		private void IntergrateAndUpdateMeter(List<float> samples, VerticalMeter meter)
		{
			float val;
			double max = 0;
			double sum = 0;
			foreach(float f in samples)
			{
				if (f == float.MinValue)
				{
					val = float.MaxValue;
				}
				else
				{
					val = Math.Abs(f);
				}
				sum += val;

				if (val > max) max = val;
			}

			double avg = sum / (double)samples.Count;

			

			double dbfs = Math.Log10(max) * 20;

			// Return Time
			currentDbFS -= returnPrSec * (integration / 1000d);
			if (currentDbFS < dbfs) currentDbFS = dbfs;

			double dbu = currentDbFS + 18;

			meter.setValue1((float)dbu);

			//label12.Text = max.ToString();
			//label13.Text = dbfs.ToString();
		}
	}
}
