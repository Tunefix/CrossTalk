using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public partial class Client : Form
	{
		MocrButton connectButton;
		MocrButton loopbackButton;
		MocrButton transmitOpenButton;
		MocrButton transmitPTTButton;
		MocrButton inputGainPlus;
		MocrButton inputGainMinus;
		MocrButton outputGainPlus;
		MocrButton outputGainMinus;
		TextBox serverAddr;
		XCombo audioFormatSelector;
		XCombo audioInputSelector;
		XCombo audioOutputSelector;
		VerticalMeter meter;
		SegDisp inBufferDisp;
		SegDisp outBufferDisp;
		SegDisp bandwidthDisp;
		SegDisp inputGainDisp;
		SegDisp outputGainDisp;
		List<MocrButton> listenButtons = new List<MocrButton>();
		List<MocrButton> talkButtons = new List<MocrButton>();
		List<MocrButton> inputModeButtons = new List<MocrButton>();
		List<MocrButton> outputModeButtons = new List<MocrButton>();
		EventIndicator outputPlayingIndicator;
		EventIndicator outputPausedIndicator;
		EventIndicator outputStoppedIndicator;


		private void MakeLayout()
		{
			MakeInputSelector();
			MakeOutputSelector();
			MakeFormatSelector();
			MakeInOutMeter();

			MakeServerDetails();

			MakeBufferReadouts();

			MakePlayoutLights();

			MakeBandwithMeter();

			MakeLoopbackButton();

			FillListenTalkLists();
			MakeLoopControls();

			MakeTransmitControls();

			PictureBox logo = new PictureBox();
			logo.SizeMode = PictureBoxSizeMode.Zoom;
			logo.BackColor = Color.Transparent;
			logo.Location = new Point(20, 317);
			logo.Size = new Size(80, 80);
			logo.Image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\clientLogo.png");
			this.Controls.Add(logo);
		}

		private void MakeInputSelector()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(120, 20);
			lbl.Size = new Size(317, 28);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "SELECT INPUT SOURCE";

			this.Invoke(new Action(() => this.Controls.Add(lbl)));


			audioInputSelector = new XCombo();
			audioInputSelector.Location = new Point(120, 53);
			audioInputSelector.Size = new Size(317, 28);
			audioInputSelector.Font = smallFont;
			audioInputSelector.SelectedIndexChanged += AudioInput_SelectedIndexChanged;

			this.Controls.Add(audioInputSelector);


			// INPUT MODE
			lbl = new CustomLabel();
			lbl.Location = new Point(120, 80);
			lbl.Size = new Size(50, 20);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.NORMAL;
			lbl.Text = "MODE:";
			this.Controls.Add(lbl);

			inputModeButtons.Add(new MocrButton());
			inputModeButtons[0].Font = tinyFont;
			inputModeButtons[0].buttonStyle = MocrButton.style.TINY_LIGHT;
			inputModeButtons[0].setLightColor(MocrButton.color.BLANK);
			inputModeButtons[0].Text = "LR";
			inputModeButtons[0].Location = new Point(170, 77);
			inputModeButtons[0].Size = new Size(32, 20);
			inputModeButtons[0].Click += (sender, e) => SetInputMode(0);
			this.Controls.Add(inputModeButtons[0]);

			inputModeButtons.Add(new MocrButton());
			inputModeButtons[1].Font = tinyFont;
			inputModeButtons[1].buttonStyle = MocrButton.style.TINY_LIGHT;
			inputModeButtons[1].setLightColor(MocrButton.color.BLANK);
			inputModeButtons[1].Text = "LL";
			inputModeButtons[1].Location = new Point(202, 77);
			inputModeButtons[1].Size = new Size(32, 20);
			inputModeButtons[1].Click += (sender, e) => SetInputMode(1);
			this.Controls.Add(inputModeButtons[1]);

			inputModeButtons.Add(new MocrButton());
			inputModeButtons[2].Font = tinyFont;
			inputModeButtons[2].buttonStyle = MocrButton.style.TINY_LIGHT;
			inputModeButtons[2].setLightColor(MocrButton.color.BLANK);
			inputModeButtons[2].Text = "RR";
			inputModeButtons[2].Location = new Point(234, 77);
			inputModeButtons[2].Size = new Size(32, 20);
			inputModeButtons[2].Click += (sender, e) => SetInputMode(2);
			this.Controls.Add(inputModeButtons[2]);

			inputModeButtons.Add(new MocrButton());
			inputModeButtons[3].Font = tinyFont;
			inputModeButtons[3].buttonStyle = MocrButton.style.TINY_LIGHT;
			inputModeButtons[3].setLightColor(MocrButton.color.BLANK);
			inputModeButtons[3].Text = "RL";
			inputModeButtons[3].Location = new Point(266, 77);
			inputModeButtons[3].Size = new Size(32, 20);
			inputModeButtons[3].Click += (sender, e) => SetInputMode(3);
			this.Controls.Add(inputModeButtons[3]);


			// INPUT GAIN
			lbl = new CustomLabel();
			lbl.Location = new Point(303, 80);
			lbl.Size = new Size(50, 20);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.NORMAL;
			lbl.Text = "GAIN:";
			this.Controls.Add(lbl);

			inputGainMinus = new MocrButton();
			inputGainMinus.Location = new Point(353, 77);
			inputGainMinus.Size = new Size(25, 20);
			inputGainMinus.buttonStyle = MocrButton.style.DSKY;
			inputGainMinus.Text = "-";
			inputGainMinus.Click += (s, e) => MoveInputGain(-1);
			inputGainMinus.DoubleClick += (s, e) => MoveInputGain(-1);
			this.Controls.Add(inputGainMinus);

			inputGainDisp = new SegDisp(2, true, "");
			inputGainDisp.Location = new Point(380, 77);
			inputGainDisp.Size = new Size(30, 20);
			inputGainDisp.setValue(inputGain.ToString());
			this.Controls.Add(inputGainDisp);

			inputGainPlus = new MocrButton();
			inputGainPlus.Location = new Point(412, 77);
			inputGainPlus.Size = new Size(25, 20);
			inputGainPlus.buttonStyle = MocrButton.style.DSKY;
			inputGainPlus.Text = "+";
			inputGainPlus.Click += (s, e) => MoveInputGain(1);
			inputGainPlus.DoubleClick += (s, e) => MoveInputGain(1);
			this.Controls.Add(inputGainPlus);
		}

		private void MakeOutputSelector()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(457, 20);
			lbl.Size = new Size(317, 28);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "SELECT OUTPUT SOURCE";
			this.Controls.Add(lbl);

			audioOutputSelector = new XCombo();
			audioOutputSelector.Location = new Point(457, 53);
			audioOutputSelector.Size = new Size(317, 28);
			audioOutputSelector.Font = smallFont;
			audioOutputSelector.SelectedIndexChanged += AudioOutput_SelectedIndexChanged;
			this.Controls.Add(audioOutputSelector);

			// OUTPUT MODE
			lbl = new CustomLabel();
			lbl.Location = new Point(457, 80);
			lbl.Size = new Size(50, 20);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.NORMAL;
			lbl.Text = "MODE:";
			this.Controls.Add(lbl);

			outputModeButtons.Add(new MocrButton());
			outputModeButtons[0].Font = tinyFont;
			outputModeButtons[0].buttonStyle = MocrButton.style.TINY_LIGHT;
			outputModeButtons[0].setLightColor(MocrButton.color.BLANK);
			outputModeButtons[0].Text = "LR";
			outputModeButtons[0].Location = new Point(507, 77);
			outputModeButtons[0].Size = new Size(32, 20);
			outputModeButtons[0].Click += (sender, e) => SetOutputMode(0);
			this.Controls.Add(outputModeButtons[0]);

			outputModeButtons.Add(new MocrButton());
			outputModeButtons[1].Font = tinyFont;
			outputModeButtons[1].buttonStyle = MocrButton.style.TINY_LIGHT;
			outputModeButtons[1].setLightColor(MocrButton.color.BLANK);
			outputModeButtons[1].Text = "L";
			outputModeButtons[1].Location = new Point(539, 77);
			outputModeButtons[1].Size = new Size(32, 20);
			outputModeButtons[1].Click += (sender, e) => SetOutputMode(1);
			this.Controls.Add(outputModeButtons[1]);

			outputModeButtons.Add(new MocrButton());
			outputModeButtons[2].Font = tinyFont;
			outputModeButtons[2].buttonStyle = MocrButton.style.TINY_LIGHT;
			outputModeButtons[2].setLightColor(MocrButton.color.BLANK);
			outputModeButtons[2].Text = "R";
			outputModeButtons[2].Location = new Point(571, 77);
			outputModeButtons[2].Size = new Size(32, 20);
			outputModeButtons[2].Click += (sender, e) => SetOutputMode(2);
			this.Controls.Add(outputModeButtons[2]);


			// OUTPUT GAIN
			lbl = new CustomLabel();
			lbl.Location = new Point(640, 80);
			lbl.Size = new Size(50, 20);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.NORMAL;
			lbl.Text = "GAIN:";
			this.Controls.Add(lbl);

			outputGainMinus = new MocrButton();
			outputGainMinus.Location = new Point(690, 77);
			outputGainMinus.Size = new Size(25, 20);
			outputGainMinus.buttonStyle = MocrButton.style.DSKY;
			outputGainMinus.Text = "-";
			outputGainMinus.Click += (s, e) => MoveOutputGain(-1);
			outputGainMinus.DoubleClick += (s, e) => MoveOutputGain(-1);
			this.Controls.Add(outputGainMinus);

			outputGainDisp = new SegDisp(2, true, "");
			outputGainDisp.Location = new Point(717, 77);
			outputGainDisp.Size = new Size(30, 20);
			outputGainDisp.setValue(outputGain.ToString());
			this.Controls.Add(outputGainDisp);

			outputGainPlus = new MocrButton();
			outputGainPlus.Location = new Point(749, 77);
			outputGainPlus.Size = new Size(25, 20);
			outputGainPlus.buttonStyle = MocrButton.style.DSKY;
			outputGainPlus.Text = "+";
			outputGainPlus.Click += (s, e) => MoveOutputGain(1);
			outputGainPlus.DoubleClick += (s, e) => MoveOutputGain(1);
			this.Controls.Add(outputGainPlus);
		}

		private void MakeFormatSelector()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(120, 101);
			lbl.Size = new Size(317, 28);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "SELECT AUDIO FORMAT";

			this.Controls.Add(lbl);


			audioFormatSelector = new XCombo();
			audioFormatSelector.Location = new Point(120, 134);
			audioFormatSelector.Size = new Size(317, 28);
			audioFormatSelector.Font = smallFont;
			audioFormatSelector.SelectedIndexChanged += AudioFormat_SelectedIndexChanged;

			this.Controls.Add(audioFormatSelector);
		}

		private void MakeInOutMeter()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(20, 20);
			lbl.Size = new Size(80, 28);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "IN   OUT";

			this.Controls.Add(lbl);

			meter = new VerticalMeter(tinyFontB, 2);
			meter.Size = new Size(80, 248);
			meter.Location = new Point(20, 53);
			meter.doubleMeter = true;
			meter.singleScale = true;
			meter.setScale(-40, 18);
			meter.setManScale1(new float[] { -40, -36, -30, -24, -18, -12, -6, 0, 6, 12, 18 });
			meter.setManScale2(new float[] { -40, -36, -30, -24, -18, -12, -6, 0, 6, 12, 18 });

			this.Controls.Add(meter);
		}

		private void MakeServerDetails()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(457, 101);
			lbl.Size = new Size(317, 28);
			lbl.Font = smallFont;
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "SERVER ADDRESS";

			this.Controls.Add(lbl);

			serverAddr = new XTextBox();
			serverAddr.Location = new Point(457, 134);
			serverAddr.Size = new Size(232, 23);
			serverAddr.Font = font;
			serverAddr.AutoSize = false;
			serverAddr.TextAlign = HorizontalAlignment.Left;
			serverAddr.Text = Server_IP;
			this.Controls.Add(serverAddr);

			connectButton = new MocrButton();
			connectButton.Location = new Point(694, 134);
			connectButton.Size = new Size(80, 23);
			connectButton.Font = smallFontB;
			connectButton.Text = "CONNECT";
			connectButton.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			connectButton.Click += ConnectToServer;

			this.Controls.Add(connectButton);
		}

		private void MakeLoopControls()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(120, 177);
			lbl.Size = new Size(654, 28);
			lbl.Font = tinyFont;
			lbl.setCharWidth(6);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "LOOP CONTROL (YELLOW = LISTEN, WHITE FLSH = TALKING TO THIS LOOP, WHITE STDY = SOMEONE HAS ENABLED TALK)";
			this.Controls.Add(lbl);

			for (int i = 0; i < numLoops; i++)
			{
				int ln = i;
				listenButtons.Add(new MocrButton());
				listenButtons[i].Location = new Point(120 + (82 * i) - ((i/8) * 82 * 8) , 208 + ((i/8) * 98));
				listenButtons[i].Size = new Size(80, 45);
				listenButtons[i].Font = smallFontB;
				listenButtons[i].setLightColor(MocrButton.color.AMBER);
				listenButtons[i].Text = "LOOP " + (i+1);
				listenButtons[i].buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
				listenButtons[i].Click += (sender, e) => toggleLoopListen(sender, e, ln);
				this.Controls.Add(listenButtons[i]);

				int tn = i;
				talkButtons.Add(new MocrButton());
				talkButtons[i].Location = new Point(120 + (82 * i) - ((i / 8) * 82 * 8), 254 + ((i / 8) * 98));
				talkButtons[i].Size = new Size(80, 45);
				talkButtons[i].Font = smallFontB;
				talkButtons[i].setLightColor(MocrButton.color.BLANK);
				talkButtons[i].Text = "LOOP " + (i + 1);
				talkButtons[i].buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
				talkButtons[i].Click += (sender, e) => toggleLoopTalk(sender, e, tn);
				this.Controls.Add(talkButtons[i]);
				
			}
		}

		private void FillListenTalkLists()
		{
			for(int i = 0; i < numLoops; i++)
			{
				loopListen.Add(i, false);
				loopTalk.Add(i, false);
			}
		}

		private void MakeBufferReadouts()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(794, 20);
			lbl.Size = new Size(85, 28);
			lbl.Font = tinyFont;
			lbl.setCharWidth(8);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "IN BUFFER";
			this.Controls.Add(lbl);

			inBufferDisp = new SegDisp(4, false, "");
			inBufferDisp.Location = new Point(794, 52);
			inBufferDisp.Size = new Size(85, 35);
			this.Controls.Add(inBufferDisp);

			lbl = new CustomLabel();
			lbl.Location = new Point(899, 20);
			lbl.Size = new Size(85, 28);
			lbl.Font = tinyFont;
			lbl.setCharWidth(8);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "OUT BUFFER";
			this.Controls.Add(lbl);

			outBufferDisp = new SegDisp(4, false, "");
			outBufferDisp.Location = new Point(899, 52);
			outBufferDisp.Size = new Size(85, 35);
			this.Controls.Add(outBufferDisp);

			MocrButton flush = new MocrButton();
			flush.Location = new Point(794, 90);
			flush.Size = new Size(85, 35);
			flush.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			flush.Text = "FLUSH BUFFER";
			flush.Font = smallFont;
			flush.Click += (sender, e) => FlushBuffer(sender, e, inputBuffer);
			this.Controls.Add(flush);

			flush = new MocrButton();
			flush.Location = new Point(899, 90);
			flush.Size = new Size(85, 35);
			flush.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			flush.Text = "FLUSH BUFFER";
			flush.Font = smallFont;
			flush.Click += (sender, e) => FlushBuffer(sender, e, outputBuffer);
			this.Controls.Add(flush);
		}

		private void MakePlayoutLights()
		{
			outputPlayingIndicator = new EventIndicator();
			outputPlayingIndicator.Location = new Point(899, 130);
			outputPlayingIndicator.Size = new Size(85, 20);
			outputPlayingIndicator.Font = tinyFont;
			outputPlayingIndicator.small = true;
			outputPlayingIndicator.upperText = "PLAYING";
			outputPlayingIndicator.upperOnColor = EventIndicator.color.GREEN_LIT;
			outputPlayingIndicator.upperOffColor = EventIndicator.color.GREEN;
			outputPlayingIndicator.turnOffUpper();
			this.Controls.Add(outputPlayingIndicator);

			outputPausedIndicator = new EventIndicator();
			outputPausedIndicator.Location = new Point(899, 149);
			outputPausedIndicator.Size = new Size(85, 20);
			outputPausedIndicator.Font = tinyFont;
			outputPausedIndicator.small = true;
			outputPausedIndicator.upperText = "PAUSED";
			outputPausedIndicator.upperOnColor = EventIndicator.color.AMBER_LIT;
			outputPausedIndicator.upperOffColor = EventIndicator.color.AMBER;
			outputPausedIndicator.turnOffUpper();
			this.Controls.Add(outputPausedIndicator);

			outputStoppedIndicator = new EventIndicator();
			outputStoppedIndicator.Location = new Point(899, 168);
			outputStoppedIndicator.Size = new Size(85, 20);
			outputStoppedIndicator.Font = tinyFont;
			outputStoppedIndicator.small = true;
			outputStoppedIndicator.upperText = "STOPPED";
			outputStoppedIndicator.upperOnColor = EventIndicator.color.RED_LIT;
			outputStoppedIndicator.upperOffColor = EventIndicator.color.RED;
			outputStoppedIndicator.turnOffUpper();
			this.Controls.Add(outputStoppedIndicator);
		}

		private void MakeBandwithMeter()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(794, 177);
			lbl.Size = new Size(85, 28);
			lbl.Font = tinyFont;
			lbl.setCharWidth(8);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "KBPS OUT";
			this.Controls.Add(lbl);

			bandwidthDisp = new SegDisp(4, false, "");
			bandwidthDisp.Location = new Point(794, 205);
			bandwidthDisp.Size = new Size(85, 35);
			this.Controls.Add(bandwidthDisp);
		}

		private void MakeLoopbackButton()
		{
			loopbackButton = new MocrButton();
			loopbackButton.Location = new Point(794, 130);
			loopbackButton.Size = new Size(85, 35);
			loopbackButton.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			loopbackButton.Text = "LOOPBACK";
			loopbackButton.Font = smallFont;
			loopbackButton.setLightColor(MocrButton.color.BLANK);
			loopbackButton.Click += ToggleLoopback;
			this.Controls.Add(loopbackButton);

			ToolTip toolTip1 = new ToolTip();
			toolTip1.ShowAlways = true;
			toolTip1.AutomaticDelay = 1000;
			toolTip1.UseFading = true;
			toolTip1.ToolTipTitle = "LOOPBACK";
			toolTip1.SetToolTip(loopbackButton, "If loopback is active no data is transmitted to,"
				+ "\nor received from the server. Instead the audio"
				+ "\nis run through the encoder and straight into"
				+ "\nthe decoder. This is usefull for testing the"
				+ "\nlocal inputs and outputs, and for evaluating"
				+ "\nthe different codecs without a server connection.");
		}

		private void MakeTransmitControls()
		{
			CustomLabel lbl = new CustomLabel();
			lbl.Location = new Point(794, 250);
			lbl.Size = new Size(85, 28);
			lbl.Font = tinyFont;
			lbl.setCharWidth(8);
			lbl.type = CustomLabel.LabelType.ENGRAVED;
			lbl.Text = "TRANSMIT";
			this.Controls.Add(lbl);

			transmitOpenButton = new MocrButton();
			transmitOpenButton.Location = new Point(794, 278);
			transmitOpenButton.Size = new Size(85, 20);
			transmitOpenButton.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			transmitOpenButton.Text = "OPEN";
			transmitOpenButton.Font = tinyFont;
			transmitOpenButton.setLightColor(MocrButton.color.BLANK);
			transmitOpenButton.setLitState(true);
			transmitOpenButton.Click += (a, b) => SetTransmitMode(a, b, 0);
			this.Controls.Add(transmitOpenButton);

			transmitPTTButton = new MocrButton();
			transmitPTTButton.Location = new Point(794, 298);
			transmitPTTButton.Size = new Size(85, 20);
			transmitPTTButton.buttonStyle = MocrButton.style.THIN_BORDER_LIGHT;
			transmitPTTButton.Text = "PTT";
			transmitPTTButton.Font = tinyFont;
			transmitPTTButton.setLightColor(MocrButton.color.BLANK);
			transmitPTTButton.Click += (a, c) => SetTransmitMode(a, c, 1);
			this.Controls.Add(transmitPTTButton);

			askForPTTKeyBox = new Label();
			askForPTTKeyBox.Text = "Press the key you wish to use for PTT(Push - To - Talk)";
			askForPTTKeyBox.Size = new Size(300, 200);
			askForPTTKeyBox.Location = new Point((this.ClientSize.Width - 300) / 2, (this.ClientSize.Height - 200) / 2);
			askForPTTKeyBox.Font = font;
			askForPTTKeyBox.Visible = false;
			askForPTTKeyBox.TextAlign = ContentAlignment.MiddleCenter;
			askForPTTKeyBox.BackColor = Color.FromArgb(96, 96, 96);
			this.Controls.Add(askForPTTKeyBox);

			MocrButton btn = new MocrButton();
			btn.Text = "SET KEY";
			btn.Font = tinyFont;
			btn.buttonStyle = MocrButton.style.DSKY;
			btn.Size = new Size(65, 20);
			btn.Location = new Point(880, 298);
			btn.Click += AskForPTTKey;
			this.Controls.Add(btn);
		}
	}
}
