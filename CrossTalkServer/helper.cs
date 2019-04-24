using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	static class Helper
	{
		public enum Align { LEFT, RIGHT, CENTER };
		public enum ButtonType { NORMAL, DSKY, TINY_PUSH};

		static Random gen = new Random(DateTime.Now.Millisecond);

		static public String prtlen(String s, int l) { return prtlen(s, l, Align.RIGHT); }
		static public String prtlen(String s, int l, Align a)
		{
			if (s.Length < l)
			{
				while (s.Length < l)
				{
					switch (a)
					{
						case Align.RIGHT:
							s = " " + s;
							break;
						case Align.CENTER:
							s = " " + s + " ";
							if (s.Length > l)
							{
								s.Substring(0, s.Length - 1);
							}
							break;
						case Align.LEFT:
						default:
							s += " ";
							break;
					}
				}
			}
			else
			{
				s = s.Substring(0, l);
			}
			return s;
		}


		static public String timeString(double t) { return timeString(t, true, 2); }
		static public String timeString(double t, int h_len) { return timeString(t, true, h_len); }
		static public String timeString(double t, bool show_hrs) { return timeString(t, show_hrs, 2); }
		static public String timeString(double t, bool show_hrs, int h_len)
		{
			String output = "";
			double hrs;
			double min;
			double sec;
			double ts = t; // tmp sec
			String hrs_s;
			String min_s;
			String sec_s;

			hrs = Math.Floor(ts / (60 * 60));
			ts = ts - (hrs * (60 * 60));
			hrs_s = hrs.ToString();
			while (hrs_s.Length < h_len) { hrs_s = "0" + hrs_s; }

			min = Math.Floor(ts / 60);
			ts = ts - (min * 60);
			min_s = min.ToString();
			while (min_s.Length < 2) { min_s = "0" + min_s; }

			sec = Math.Floor(ts);
			sec_s = sec.ToString();
			while (sec_s.Length < 2) { sec_s = "0" + sec_s; }

			if (show_hrs)
			{
				output = hrs_s + ":" + min_s + ":" + sec_s;
			}
			else
			{
				min = min + (hrs * 60);
				min_s = min.ToString();
				while (min_s.Length < 2) { min_s = "0" + min_s; }

				ts = ts - sec;
				ts = Math.Round(ts * 100f);
				string tsStr = ts.ToString();
				if (tsStr.Length == 1) tsStr += "0";
				sec_s = sec_s + "." + tsStr;
				
				output = min_s + ":" + sec_s;
			}

			return output;
		}

		static public String toFixed(double? d, int p) { return toFixed(d, p, false); }
		static public String toFixed(double? d, int p, bool showPlus)
		{
			NumberFormatInfo format = new NumberFormatInfo();
			format.NumberGroupSeparator = "";
			format.NumberDecimalDigits = p;
			format.NumberDecimalSeparator = ".";
			
			String r;
			if (d == null)
			{
				r = "";
			}
			else
			{
				double d2  = d.Value;
				String b = Math.Floor(d2).ToString(format);
				r = Math.Round(d2, p).ToString(format);

				// Check that d isn't whole number, if so; add '.'
				int index = r.IndexOf(".");
				if (index == -1)
				{
					r += ".";
				}

				int extraSigns = 1; // The decimal sign
				//if (d2 < 0) { extraSigns++; } // The minus sign

				if(showPlus && d2 > 0)
				{
					extraSigns++;
					r = "+" + r;
				}

				while (r.Length < b.Length + extraSigns + p)
				{
					r += "0";
				}
			}
			return r;
		}

		static public double rad2deg(double rad)
		{
			return rad * (180 / Math.PI);
		}

		static public double deg2rad(double deg)
		{
			return deg * (Math.PI / 180);
		}

		static public List<KeyValuePair<double, double?>> limit(List<KeyValuePair<double, double?>> data, int count)
		{
			List<KeyValuePair<double, double?>> output = new List<KeyValuePair<double, double?>>();
			if (data[count + 1].Value != null)
			{
				// Find last value key
				int index = 599;
				while (index > count)
				{
					if (data[index].Value != null)
					{
						break;
					}
					index--;
				}

				for (int i = (index - count); i < index; i++)
				{
					if (data[i].Value != null)
					{
						output.Add(new KeyValuePair<double, double?>(i, data[i].Value));
					}
					else
					{
						output.Add(new KeyValuePair<double, double?>(i, null));
					}
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (data[i].Value != null)
					{
						output.Add(new KeyValuePair<double, double?>(i, data[i].Value));
					}
					else
					{
						output.Add(new KeyValuePair<double, double?>(i, null));
					}
				}
			}

			return output;
		}

		static public string int2str(int i, int length, String pad)
		{
			String str = i.ToString();
			String pre = "";
			
			if (i < 0)
			{
				str = str.Substring(1);
				pre = "-";
			}

			while (str.Length < length)
			{
				str = pad + str;
			}

			str = pre + str;

			return str;
		}
	}
}
