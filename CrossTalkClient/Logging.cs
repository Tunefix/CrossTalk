using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossTalkClient
{
	class Logging
	{
		FileStream logfile;
		public Logging()
		{
			// Empty existing log.txt
			File.WriteAllText("log.txt", "");

			
		}

		public void WriteLine(string line)
		{
			Write(line + Environment.NewLine);
		}

		public void Write(string line)
		{
			// Open the log.txt for writing
			logfile = File.OpenWrite("log.txt");

			// Go to end of file
			logfile.Position = logfile.Length;

			// Prepend date and time
			DateTime now = DateTime.Now;
			string year = now.Year.ToString();
			string month = now.Month < 10 ? "0" + now.Month.ToString() : now.Month.ToString();
			string day = now.Day < 10 ? "0" + now.Day.ToString() : now.Day.ToString();
			string hrs = now.Hour < 10 ? "0" + now.Hour.ToString() : now.Hour.ToString();
			string min = now.Minute < 10 ? "0" + now.Minute.ToString() : now.Minute.ToString();
			string sec = now.Second < 10 ? "0" + now.Second.ToString() : now.Second.ToString();

			string msec = "";
			if(now.Millisecond < 10)
			{
				msec = "00" + now.Millisecond;
			}
			else if(now.Millisecond < 100)
			{
				msec = "0" + now.Millisecond;
			}
			else
			{
				msec = now.Millisecond.ToString();
			}


			string datetime = year + "." + month + "." + day + " " + hrs + ":" + min + ":" + sec + "." + msec + "    ";

			byte[] bytes = Encoding.UTF8.GetBytes(datetime + line);
			logfile.Write(bytes, 0, bytes.Length);

			// Close the file
			logfile.Close();

			Console.Write(line);
		}
	}
}
