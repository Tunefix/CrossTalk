using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace CrossTalkClient
{
	public partial class Client : Form
	{
		static int numLoops = 16;
		int MajorVersion = 0;
		int MinorVersion = 5;
		int Revision = 0;

		int mainServerInterval = 100;
		int minimumServerInterval = 100;

		private LowLevelKeyboardListener _listener;


		public Client()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			this.BackgroundImage = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\darknoise.png");
			this.BackColor = Color.FromArgb(72, 72, 72);
			this.ClientSize = new Size(794, 417);
			this.ClientSize = new Size(994, 417);
			if (Revision != 0)
			{
				this.Text = "CrossTalk Client " + MajorVersion + "." + MinorVersion + "." + Revision;
			}
			else
			{
				this.Text = "CrossTalk Client " + MajorVersion + "." + MinorVersion;
			}


			InitClient();
		}

		private void InitClient()
		{
			_listener = new LowLevelKeyboardListener();
			_listener.OnKeyPressed += _listener_OnKeyPressed;
			_listener.OnKeyUp += _listener_OnKeyUp;

			_listener.HookKeyboard();



			LoadData();
			CreateFonts();
			MakeLayout();
			CodecsInit();
			InitAudio();

			this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + "Resources\\CrossTalkClient.ico");

			Thread serverMainThread = new Thread(ClientMain);
			serverMainThread.Start();
		}

		private void InitAudio()
		{
			FormatSelectorInit();
			GetAudioOutputs();
			GetAudioInputs();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (connected) disconnectFromServer();
			_listener.UnHookKeyboard();
			Environment.Exit(0);
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			//Environment.Exit(Environment.ExitCode);
		}

		private void ClientMain()
		{
			DateTime start;
			DateTime end;
			TimeSpan duration;
			double sleepTime;
			while (true)
			{
				start = DateTime.UtcNow;

				// DO STUFF
				FetchAudioFromInputBuffer();
				UpdateBufferDisp();

				GC.Collect(2, GCCollectionMode.Optimized, true);

				end = DateTime.UtcNow;

				duration = end - start;
				sleepTime = mainServerInterval - duration.TotalMilliseconds;
				if (sleepTime < 0) sleepTime = 0;

				// CHECK FOR SLOW EXECUTION
				if (duration.TotalMilliseconds > mainServerInterval)
				{
					// NOTIFY OF SLOW EXECUTION
					Console.WriteLine("=== SLOW EXECUTION ===");
					Console.WriteLine("ServerMainLoop used " + Math.Round(duration.TotalMilliseconds, 2) + " on the last run.");

					// TRY TO REMEDY THE SITUATION BY RUNNING SLOWER, BUT HANDLING MORE DATA EACH TIME
					mainServerInterval++;
					Console.WriteLine("Increasing mainServerInterval to " + mainServerInterval + " ms.");
				}
				else if (mainServerInterval > minimumServerInterval && duration.TotalMilliseconds < (mainServerInterval / 2f))
				{
					// NOTIFY OF FAST EXECUTION
					Console.WriteLine("=== FAST EXECUTION ===");
					Console.WriteLine("ServerMainLoop used " + Math.Round(duration.TotalMilliseconds, 2) + " on the last run.");

					// INCREASE SPEED TO GET LOWER LATENCY
					mainServerInterval--;
					Console.WriteLine("Decreasing mainServerInterval to " + mainServerInterval + " ms.");
				}

				// SLEEP THE REMAINDER OF THE INTERVAL
				Thread.Sleep((int)Math.Floor(sleepTime));
			}
		}

		private void _listener_OnKeyPressed(object sender, KeyPressedArgs e)
		{
			//Console.WriteLine(e.KeyPressed.KeyValue);

			if (PTTkey != null && PTTkey == e.KeyPressed.KeyValue)
			{
				PTTopen = true;
				transmitPTTButton.setLightColor(MocrButton.color.GREEN);
			}

			if (askingForPTTkey)
			{
				askingForPTTkey = false;
				askForPTTKeyBox.Visible = false;
				PTTkey = e.KeyPressed.KeyValue;
			}


		}

		private void _listener_OnKeyUp(object sender, KeyPressedArgs e)
		{
			if (PTTkey != null && PTTkey == e.KeyPressed.KeyValue)
			{
				PTTopen = false;
				transmitPTTButton.setLightColor(MocrButton.color.BLANK);
			}
		}
	}
}
