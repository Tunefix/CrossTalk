using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	public partial class Server : Form
	{
		int MajorVersion = 0;
		int MinorVersion = 5;
		int Revision = 3;

		private Logging Logger;

		public Server()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			Logger = new Logging();

			AppDomain.CurrentDomain.FirstChanceException += (a, eventArgs) =>
			{
				Logger.WriteLine(eventArgs.Exception.ToString()
					+ "\n" + eventArgs.Exception.StackTrace
					+ "\n" + eventArgs.Exception.TargetSite);

			};
			this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + "Resources\\CrossTalkServer.ico");

			CreateFonts();
			MakeLayout();
			GetAudioOutputs();
			ServerInit();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			SaveLoopNames();
			output.Stop();
			output.Dispose();
			Environment.Exit(0);
		}
	}
}
