using System;
using System.Collections.Generic;
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
		Dictionary<int, VerticalMeter> meters = new Dictionary<int, VerticalMeter>();
		Dictionary<int, MocrButton> meterListenButtons = new Dictionary<int, MocrButton>();
		int meterWidth = 80;
		int meterHeight = 200;
		int meterSpacing = 10;

		SegDisp loopIntervalDisp;
		SegDisp loopTimeDisp;
		CustomLabel clientList;
		SegDisp outBufferDisp;

		MocrButton NminusOneButton;

		//SegDisp maxBufferDisp;
		//SegDisp minBufferDisp;

		List<Diode> clientDiodeConnected = new List<Diode>();
		List<List<Diode>> clientDiodeListen = new List<List<Diode>>();
		List<List<Diode>> clientDiodeTalk = new List<List<Diode>>();
		List<Label> clientViewName = new List<Label>();
		List<Label> clientViewTalkListen = new List<Label>();
		List<Label> clientViewFormat = new List<Label>();
		List<SegDisp> clientViewInBuffer = new List<SegDisp>();
		List<SegDisp> clientViewInTime = new List<SegDisp>();
		List<TextBox> loopNames = new List<TextBox>();
		List<LedMeter> loopTimeMeters = new List<LedMeter>();
		LineGraph lineGraph;

		private void MakeLayout()
		{
			this.BackgroundImage = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\darknoise.png");
			this.BackColor = Color.FromArgb(72, 72, 72);
			this.ClientSize = new Size(((numLoops / 2) * meterWidth) + (((numLoops / 2) - 1) * meterSpacing) + 400, 550);

			if (Revision != 0)
			{
				this.Text = "CrossTalk Server " + MajorVersion + "." + MinorVersion + "." + Revision;
			}
			else
			{
				this.Text = "CrossTalk Server " + MajorVersion + "." + MinorVersion;
			}

			MakeLoopHeadline();
			MakeLoopMeters();

			MakeServerLoopDurationDisplay();

			//MakeMaxMinBufferDisp();

			//MakeClientList();

			MakeLoopNameBoxes();

			MakeClientView();

			MakeOutputSelector();
			MakeOutputBufferMonitor();

			MakeSettingsButtons();

			MakeLoopTimeStat();
			MakeLoopTimeGraph();

			PictureBox logo = new PictureBox();
			logo.SizeMode = PictureBoxSizeMode.Zoom;
			logo.BackColor = Color.Transparent;
			logo.Location = new Point(570, meterHeight + 200);
			logo.Size = new Size(80, 80);
			logo.Image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\serverLogo.png");
			this.Controls.Add(logo);
		}

		private void MakeLoopHeadline()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Font = buttonFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.setlineOffset(3.0d);
			lbl.Location = new Point(20, 20);
			lbl.Size = new Size((meterWidth * (numLoops / 2)) + (meterSpacing * ((numLoops / 2) - 1)), 28);
			lbl.Text = "LOOP OUTPUT LEVELS";

			this.Controls.Add(lbl);
		}

		private void MakeLoopMeters()
		{
			VerticalMeter meter;
			CustomLabel lbl;

			for(int i = 0; i < numLoops / 2f; i++)
			{
				meter = new VerticalMeter(tinyFont, 2);
				meter.Size = new Size(meterWidth, meterHeight);
				meter.Location = new Point(20 + ((meterWidth + meterSpacing) * i), 55);
				meter.doubleMeter = true;
				meter.singleScale = true;
				meter.setScale(-40, 18);
				meter.setManScale1(new float[] { -40, -36, -30, -24, -18, -12, -6, 0, 6, 12, 18 });
				meter.setManScale2(new float[] { -40, -36, -30, -24, -18, -12, -6, 0, 6, 12, 18 });

				this.Controls.Add(meter);
				meters.Add(i, meter);

				lbl = new CustomLabel();
				lbl.Font = buttonFont;
				lbl.type = CustomLabel.LabelType.ENGRAVED;
				lbl.setlineOffset(3.0);
				lbl.Location = new Point(20 + ((meterWidth + meterSpacing) * i), 90 + meterHeight);
				lbl.Size = new Size(meterWidth, 28);
				lbl.Text = ((i * 2) + 1) + "    " + ((i * 2) + 2);

				this.Controls.Add(lbl);

				int mn = (i * 2);
				meterListenButtons.Add((i * 2), new MocrButton());
				meterListenButtons[(i * 2)].Font = tinyFont;
				meterListenButtons[(i * 2)].buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
				meterListenButtons[(i * 2)].Text = "PFL";
				meterListenButtons[(i * 2)].Location = new Point(20 + ((meterWidth + meterSpacing) * i), 60 + meterHeight);
				meterListenButtons[(i * 2)].Size = new Size((meterWidth / 5) * 2, 25);
				meterListenButtons[(i * 2)].Click += (sender, e) => TogglePFL(mn);

				this.Controls.Add(meterListenButtons[(i * 2)]);


				int mn2 = (i * 2) + 1;
				meterListenButtons.Add((i * 2) + 1, new MocrButton());
				meterListenButtons[(i * 2) + 1].Font = tinyFont;
				meterListenButtons[(i * 2) + 1].buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
				meterListenButtons[(i * 2) + 1].Text = "PFL";
				meterListenButtons[(i * 2) + 1].Location = new Point(20 + ((meterWidth / 5) * 3) + ((meterWidth + meterSpacing) * i), 60 + meterHeight);
				meterListenButtons[(i * 2) + 1].Size = new Size((meterWidth / 5) * 2, 25);
				meterListenButtons[(i * 2) + 1].Click += (sender, e) => TogglePFL(mn2);

				this.Controls.Add(meterListenButtons[(i * 2) + 1]);
			}
		}

		public void MakeServerLoopDurationDisplay()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Font = buttonFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.setlineOffset(3.0);
			lbl.Location = new Point(20, 130 + meterHeight);
			lbl.Size = new Size(80, 28);
			lbl.Text = "LOOPTIME";
			this.Controls.Add(lbl);

			loopIntervalDisp = new SegDisp(3, false, "");
			loopIntervalDisp.Location = new Point(18, meterHeight + 165);
			loopIntervalDisp.Size = new Size(85, 40);
			this.Controls.Add(loopIntervalDisp);

			lbl = new CustomLabel();
			lbl.Font = buttonFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.setlineOffset(3.0);
			lbl.Location = new Point(20, 230 + meterHeight);
			lbl.Size = new Size(80, 28);
			lbl.Text = "LOOPMAX";
			this.Controls.Add(lbl);

			loopTimeDisp = new SegDisp(3, false, "");
			loopTimeDisp.Location = new Point(18, meterHeight + 265);
			loopTimeDisp.Size = new Size(85, 40);
			this.Controls.Add(loopTimeDisp);
		}

		public void MakeClientList()
		{
			clientList = new CustomLabel();
			clientList.Font = CRTfont3;
			clientList.AutoSize = false;
			clientList.bigText = true;
			clientList.type = CustomLabel.LabelType.CRT;
			clientList.Location = new Point(110, 130 + meterHeight);
			clientList.Size = new Size(400, 200);
			clientList.ForeColor = Color.GhostWhite;
			clientList.BackColor = Color.FromArgb(16, 16, 16);
			clientList.setCharWidth(9.0);
			clientList.setlineHeight(14.0);
			clientList.setcharOffset(-1.0);
			clientList.setlineOffset(-3.0);
			clientList.LocationF = new PointF(112, 132 + meterHeight);
			clientList.SizeF = new SizeF(400, 200);
			clientList.Text = "TEST\nTEST\nTESTklasjdflkfsda";

			this.Controls.Add(clientList);
		}

		private void MakeMaxMinBufferDisp()
		{
			/*
			maxBufferDisp = new SegDisp(6, false, "");
			maxBufferDisp.Location = new Point(30, meterHeight + 215);
			maxBufferDisp.Size = new Size(120, 40);

			this.Controls.Add(maxBufferDisp);

			minBufferDisp = new SegDisp(6, false, "");
			minBufferDisp.Location = new Point(30, meterHeight + 265);
			minBufferDisp.Size = new Size(120, 40);

			this.Controls.Add(minBufferDisp);
			*/
		}

		private void MakeClientView()
		{
			/*Dictionary<int, Label> clientViewName = new Dictionary<int, Label>();
			Dictionary<int, Label> clientViewTalkListen = new Dictionary<int, Label>();
			Dictionary<int, SegDisp> clientViewInBuffer = new Dictionary<int, SegDisp>();
			Dictionary<int, SegDisp> clientViewInTime = new Dictionary<int, SegDisp>();*/
			int x = ((numLoops / 2) * meterWidth) + (((numLoops / 2) - 1) * meterSpacing) + 40;

			for (int i = 0; i < 20; i++)
			{
				clientDiodeConnected.Add(new Diode());
				clientDiodeConnected[i].Location = new Point(x - 10, 24 + (18 * i));
				clientDiodeConnected[i].Size = new Size(10, 10);
				this.Controls.Add(clientDiodeConnected[i]);

				clientViewName.Add(new Label());
				clientViewName[i].Location = new Point(x, 20 + (18 * i));
				clientViewName[i].Size = new Size(80, 15);
				clientViewName[i].Font = smallFont;
				clientViewName[i].Text = i.ToString();
				this.Controls.Add(clientViewName[i]);

				clientViewFormat.Add(new Label());
				clientViewFormat[i].Location = new Point(x + 80, 20 + (18 * i));
				clientViewFormat[i].Size = new Size(20, 15);
				clientViewFormat[i].Font = smallFont;
				clientViewFormat[i].Text = i.ToString();
				this.Controls.Add(clientViewFormat[i]);

				clientDiodeListen.Add(new List<Diode>());
				clientDiodeTalk.Add(new List<Diode>());
				for (int d = 0; d < numLoops; d++)
				{
					clientDiodeListen[i].Add(new Diode());
					clientDiodeListen[i][d].Location = new Point(x + 110 + (10 * d), 20 + (18 * i));
					clientDiodeListen[i][d].Size = new Size(8, 8);
					clientDiodeListen[i][d].SetColors(Diode.DiodeColor.AMBER);
					this.Controls.Add(clientDiodeListen[i][d]);

					clientDiodeTalk[i].Add(new Diode());
					clientDiodeTalk[i][d].Location = new Point(x + 110 + (10 * d), 26 + (18 * i));
					clientDiodeTalk[i][d].Size = new Size(8, 8);
					clientDiodeTalk[i][d].SetColors(Diode.DiodeColor.WHITE);
					this.Controls.Add(clientDiodeTalk[i][d]);
				}

				clientViewInBuffer.Add(new SegDisp(4, false, ""));
				clientViewInBuffer[i].Location = new Point(x + 269, 20 + (18 * i));
				clientViewInBuffer[i].Size = new Size(40, 15);
				this.Controls.Add(clientViewInBuffer[i]);

				clientViewInTime.Add(new SegDisp(6, false, ""));
				clientViewInTime[i].Location = new Point(x + 310, 20 + (18 * i));
				clientViewInTime[i].Size = new Size(60, 15);
				this.Controls.Add(clientViewInTime[i]);
			}
		}

		private void MakeLoopNameBoxes()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Font = buttonFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.setlineOffset(3.0);
			lbl.Location = new Point(110, meterHeight + 130);
			lbl.Size = new Size(260, 28);
			lbl.Text = "LOOP NAMES";

			this.Controls.Add(lbl);

			List<string> loopNameList = new List<string>();
			for(int i = 0; i < numLoops; i++)
			{
				loopNameList.Add("Loop " + i.ToString());
			}

			// TRY TO FETCH LOOP NAMES FROM FILE
			string path = AppDomain.CurrentDomain.BaseDirectory + "loops.cfg";
			if (File.Exists(path))
			{
				string[] names = File.ReadAllLines(path);
				int i = 0;
				foreach(string s in names)
				{
					loopNameList[i] = s;
					i++;
				}
			}

			for (int i = 0; i < numLoops; i++)
			{
				int x = 110 + ((i / (numLoops / 2)) * 130);
				int y = meterHeight + 165 + (i * 21) - ((i / (numLoops / 2)) * ((numLoops / 2) * 21));


				Label l = new Label();
				l.Location = new Point(x, y);
				l.Size = new Size(25, 18);
				l.Font = smallFont;
				l.Text = (i+1).ToString() + ":";
				l.TextAlign = ContentAlignment.MiddleRight;
				this.Controls.Add(l);

				loopNames.Add(new TextBox());
				loopNames[i].Location = new Point(x + 25, y);
				loopNames[i].Size = new Size(100, 18);
				loopNames[i].Text = loopNameList[i];
				loopNames[i].Font = smallFont;
				loopNames[i].BorderStyle = BorderStyle.FixedSingle;
				loopNames[i].BackColor = Color.FromArgb(32, 32, 32);
				loopNames[i].ForeColor = Color.FromArgb(200, 255, 255, 255);
				loopNames[i].KeyUp += LoopNameUpdate;
				loopNames[i].TextChanged += LoopNameChange;
				loopNames[i].Tag = i;

				this.Controls.Add(loopNames[i]);
			}
		}

		private void MakeOutputSelector()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Font = buttonFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.setlineOffset(3.0);
			lbl.Location = new Point(380, meterHeight + 130);
			lbl.Size = new Size(300, 28);
			lbl.Text = "SERVER PFL OUTPUT";
			this.Controls.Add(lbl);

			audioOutputSelector = new ComboBox();
			audioOutputSelector.Location = new Point(380, meterHeight + 165);
			audioOutputSelector.Size = new Size(300, 20);
			audioOutputSelector.Font = smallFont;
			audioOutputSelector.SelectedIndexChanged += AudioOutput_SelectedIndexChanged;

			this.Controls.Add(audioOutputSelector);
		}

		private void MakeOutputBufferMonitor()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(380, meterHeight + 200);
			lbl.Size = new Size(175, 28);
			lbl.Font = buttonFont;
			lbl.setCharWidth(8);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "OUT BUFFER";
			this.Controls.Add(lbl);

			outBufferDisp = new SegDisp(4, false, "");
			outBufferDisp.Location = new Point(380, meterHeight + 232);
			outBufferDisp.Size = new Size(85, 35);
			this.Controls.Add(outBufferDisp);

			MocrButton flush = new MocrButton();
			flush.Location = new Point(470, meterHeight + 232);
			flush.Size = new Size(85, 35);
			flush.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			flush.Text = "FLUSH BUFFER";
			flush.Font = smallFont;
			flush.Click += (sender, e) => FlushBuffer(sender, e, pflBuffer);
			this.Controls.Add(flush);
		}

		private void MakeSettingsButtons()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(380, meterHeight + 275);
			lbl.Size = new Size(175, 28);
			lbl.Font = buttonFont;
			lbl.setCharWidth(8);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "OPTIONS";
			this.Controls.Add(lbl);

			NminusOneButton = new MocrButton();
			NminusOneButton.Location = new Point(380, meterHeight + 307);
			NminusOneButton.Size = new Size(45, 35);
			NminusOneButton.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			NminusOneButton.Text = "N-1";
			NminusOneButton.Font = smallFont;
			NminusOneButton.Click += ToggleNminusOne;
			this.Controls.Add(NminusOneButton);
			NminusOneButton.setLitState(NminusOne);
		}

		private void MakeLoopTimeStat()
		{
			int x = ((numLoops / 2) * meterWidth) + (((numLoops / 2) - 1) * meterSpacing) + 40;

			for (int i = 0; i < 51; i++)
			{
				loopTimeMeters.Add(new LedMeter());
				loopTimeMeters[i].Location = new Point(x + (i * 16), meterHeight + 186);
				loopTimeMeters[i].Size = new Size(15, 147);
				loopTimeMeters[i].SetNumberOfLeds(16, 0, 0);
				loopTimeMeters[i].SetScale(0, 100);
				this.Controls.Add(loopTimeMeters[i]);

				Label lbl = new Label();
				lbl.Location = new Point(x + (i * 16), meterHeight + 335);
				lbl.Size = new Size(15, 10);
				lbl.Font = tinyFont;
				lbl.Text = i.ToString();
				lbl.TextAlign = ContentAlignment.MiddleCenter;
				this.Controls.Add(lbl);
			}
		}

		private void MakeLoopTimeGraph()
		{
			lineGraph = new LineGraph();
			lineGraph.Location = new Point(1120, 20);
			lineGraph.Size = new Size(600, 360);
			lineGraph.SetScaleY(0, 20);
			lineGraph.Font = tinyFont;
			this.Controls.Add(lineGraph);
		}
	}
}
