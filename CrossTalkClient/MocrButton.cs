﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public class MocrButton : Control
	{
		public enum style {LIGHT, THIN_BORDER_LIGHT, PUSH, DSKY, TINY_PUSH, TINY_LIGHT}
		public style buttonStyle { get; set; }

		private bool pressed = false;
		public bool lit { get; private set; }
		public bool flash { get; private set; }
		private bool flashLit = false;
		private bool talk = false;
		

		readonly private Brush SpacecraftButtonBrush = new SolidBrush(Color.FromArgb(255, 34, 34, 34));
		readonly private Pen SpacecraftButtonBorderPenDark = new Pen(Color.FromArgb(255, 26, 26, 26), 1f);
		readonly private Pen SpacecraftButtonBorderPenLight = new Pen(Color.FromArgb(255, 42, 42, 42), 1f);

		readonly Pen DarkBorderPen = new Pen(Color.FromArgb(255, 16, 16, 16), 2f);
		readonly Pen ControlOuterBorderPen = new Pen(Color.FromArgb(255, 128, 128, 128), 4f);
		readonly Pen ControlInnerBorderPen = new Pen(Color.FromArgb(255, 160, 160, 160), 2f);
		readonly Pen ControlBorderShadowPen = new Pen(Color.FromArgb(55, 0, 0, 0), 1f);
		
		readonly Pen ThinOuterBorderPen = new Pen(Color.FromArgb(255, 128, 128, 128), 2f);
		readonly Pen ThinInnerBorderPen = new Pen(Color.FromArgb(255, 160, 160, 160), 1f);

		readonly Pen ThinSoftBlackPen = new Pen(Color.FromArgb(128, 0, 0, 0), 1f);
		readonly Pen ThinSoftWhitePen = new Pen(Color.FromArgb(128, 255, 255, 255), 1f);

		readonly Brush textBrushPush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
		readonly Brush textBrushLight = new SolidBrush(Color.FromArgb(200, 0, 0, 0));

		readonly Brush backgroundColorBlankDim = new SolidBrush(Color.FromArgb(255, 168, 168, 168));
		readonly Brush backgroundColorBlankLit = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
		readonly Brush backgroundColorRedDim = new SolidBrush(Color.FromArgb(255, 100, 0, 0));
		readonly Brush backgroundColorRedLit = new SolidBrush(Color.FromArgb(255, 255, 0, 0));
		readonly Brush backgroundColorAmberDim = new SolidBrush(Color.FromArgb(255, 100, 100, 0));
		readonly Brush backgroundColorAmberLit = new SolidBrush(Color.FromArgb(255, 255, 200, 32));
		readonly Brush backgroundColorGreenDim = new SolidBrush(Color.FromArgb(255, 0, 100, 0));
		readonly Brush backgroundColorGreenLit = new SolidBrush(Color.FromArgb(255, 0, 255, 0));

		readonly static Color OffWhite = Color.FromArgb(255, 200, 204, 194);
		readonly static Brush WhiteBrush = new SolidBrush(OffWhite);

		readonly static Color Shadow = Color.FromArgb(55, 0, 0, 0);
		readonly static Brush ShadowBrush = new SolidBrush(Shadow);

		readonly static Color SoftText = Color.FromArgb(200, 0, 0, 0);
		readonly static Brush SoftTextBrush = new SolidBrush(SoftText);


		Brush litBrush;
		Brush dimBrush;

		public enum color { BLANK, RED, AMBER, GREEN }
		public color buttonColor { get; private set; }

		public int loop;

		private Timer flashTimer;
		private Timer winkTimer;

		public MocrButton()
		{
			this.DoubleBuffered = true;
			this.buttonStyle = style.PUSH;
			this.buttonColor = color.BLANK;
			this.lit = false;
			this.dimBrush = backgroundColorBlankDim;
			this.litBrush = backgroundColorBlankLit;

			SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);

			flashTimer = new Timer();
			flashTimer.Tick += FlashTimerTick;
			flashTimer.Interval = 125;
		}

		public void setLightColor(color c)
		{
			this.buttonColor = c;
			switch (c)
			{
				case color.BLANK:
					dimBrush = backgroundColorBlankDim;
					litBrush = backgroundColorBlankLit;
					break;
				case color.AMBER:
					dimBrush = backgroundColorAmberDim;
					litBrush = backgroundColorAmberLit;
					break;
				case color.RED:
					dimBrush = backgroundColorRedDim;
					litBrush = backgroundColorRedLit;
					break;
				case color.GREEN:
					dimBrush = backgroundColorGreenDim;
					litBrush = backgroundColorGreenLit;
					break;
			}
			this.Invalidate();
		}
		
		public void setPressedState(bool state)
		{
			this.pressed = state;
			this.Invalidate();
		}
		
		public void setLitState(bool state)
		{
			this.lit = state;
			this.Invalidate();
		}

		public void setTalkState(bool state)
		{
			this.talk = state;
			this.Invalidate();
		}

		public void setFlashingState(bool state)
		{
			this.flash = state;
			

			if(state)
			{
				// START THE FLASH TIMER
				flashTimer.Start();
			}
			else
			{
				flashTimer.Stop();
			}

			this.Invalidate();
		}

		private void FlashTimerTick(object sender, EventArgs e)
		{
			if(flashLit)
			{
				flashLit = false;
				this.Invalidate();
			}
			else
			{
				flashLit = true;
				this.Invalidate();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			if (buttonStyle == style.LIGHT || buttonStyle == style.THIN_BORDER_LIGHT || buttonStyle == style.TINY_LIGHT)
			{
				DrawControlButton(g);
			}
			else if(buttonStyle == style.TINY_PUSH)
			{
				DrawTinyPush(g);
			}
			else
			{
				DrawSpacecraftButton(g);
			}
		}

		private void DrawControlButton(Graphics g)
		{
			// Setup brushes and pens
			Brush brush1;

			brush1 = dimBrush;

			if (this.flash)
			{
				if (this.flashLit)
				{
					brush1 = litBrush;
				}
			}
			else
			{
				if (this.lit)
				{
					brush1 = litBrush;
				}
			}

			if(talk)
			{
				brush1 = backgroundColorRedLit;
			}

			float innerX = 8;
			float innerY = 8;
			float outerBorderWidth = 4;
			Pen outerBorderPen = ControlOuterBorderPen;
			Pen innerBorderPen = ControlInnerBorderPen;
			
			if (buttonStyle == style.THIN_BORDER_LIGHT)
			{
				innerX = innerY = 5;
				outerBorderWidth = 2;
				outerBorderPen = ThinOuterBorderPen;
				innerBorderPen = ThinInnerBorderPen;
			}
			else if(buttonStyle == style.TINY_LIGHT)
			{
				innerX = innerY = 4;
				outerBorderWidth = 2;
				outerBorderPen = ThinOuterBorderPen;
				innerBorderPen = ThinInnerBorderPen;
			}
			
			if (this.pressed)
			{
				innerX += 1;
				innerY += 1;
			}
			

			// Background color
			g.FillRectangle(backgroundColorBlankDim, (innerX / 2f), (innerY / 2f), this.Width - innerX, this.Height - innerY);

			// Button color
			g.FillRectangle(brush1, innerX, innerY, this.Width - ((innerX) * 2), this.Height - ((innerY) * 2));
			
			
			
			// LIGHT SHADE
			RectangleF mask = new RectangleF(-(this.Width / 2f), -(this.Height / 2f), this.Width * 2, this.Height * 2);
			GraphicsPath ellipsePath = new GraphicsPath();
			ellipsePath.AddEllipse(mask);
			//ellipsePath.AddRectangle(mask);

			using (var maskBrush = new PathGradientBrush(ellipsePath))
			{
				maskBrush.CenterPoint = new PointF(this.Width / 2f, this.Height / 2f);
				maskBrush.CenterColor = Color.FromArgb(0, 0, 0, 0);
				maskBrush.SurroundColors = new[] { Color.FromArgb(160, 0, 0, 0) };
				maskBrush.FocusScales = new PointF(0.2f, 0.2f);
	
				g.FillRectangle(maskBrush, mask);
			}
			
			// Draw Text
			StringFormat format = new StringFormat();
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;
			g.DrawString(this.Text, this.Font, textBrushLight, new RectangleF(1, 1, this.Width - 2, this.Height - 2), format);

			// Inner Border
			if (buttonStyle == style.LIGHT || buttonStyle == style.THIN_BORDER_LIGHT)
			{
				g.DrawRectangle(innerBorderPen, innerX, innerY, this.Width - (innerX * 2), this.Height - (innerY * 2));
			}
			
			// Border Shadow
			g.DrawRectangle(ControlBorderShadowPen, outerBorderWidth + 1f, outerBorderWidth + 1f, this.Width - ((outerBorderWidth + 1f) * 2), this.Height - ((outerBorderWidth + 1f) * 2));

			// Border
			g.DrawRectangle(outerBorderPen, outerBorderWidth / 2f, outerBorderWidth / 2f, this.Width - outerBorderWidth, this.Height - outerBorderWidth);
		}

		private void DrawTinyPush(Graphics g)
		{
			// FITTING
			g.FillEllipse(SpacecraftButtonBrush, 2f, 2f, Width - 4f, Height - 4f);
			g.DrawEllipse(ThinInnerBorderPen, 2f, 2f, Width - 4f, Height - 4f);

			// BUTTON SHADOW
			if (this.pressed)
			{
				g.FillEllipse(ShadowBrush, 6f, 6f, Width - 14f, Height - 14f);
			}
			else
			{
				g.FillEllipse(ShadowBrush, 8f, 8f, Width - 14f, Height - 14f);
			}

			// BUTTON
			g.FillEllipse(WhiteBrush, 7f, 7f, Width - 14f, Height - 14f);

			// BEVELS
			if (this.pressed)
			{
				
				g.DrawArc(ThinSoftBlackPen, 7f, 7f, Width - 14f, Height - 14f, 160, 340);
				g.DrawArc(ThinSoftWhitePen, 7f, 7f, Width - 14f, Height - 14f, 340, 160);
			}
			else
			{
				g.DrawArc(ThinSoftWhitePen, 7f, 7f, Width - 14f, Height - 14f, 160, 340);
				g.DrawArc(ThinSoftBlackPen, 7f, 7f, Width - 14f, Height - 14f, 340, 160);
			}

			// TEXT
			g.DrawString(Text, Font, SoftTextBrush, (Width / 2f) - 5f, (Height / 2f) - 6f);
		}

		private void DrawSpacecraftButton(Graphics g)
		{
			Brush brush1;
			Pen pen1;
			Pen pen2;

			if (this.pressed)
			{
				pen1 = SpacecraftButtonBorderPenDark;
				pen2 = SpacecraftButtonBorderPenLight;
				brush1 = SpacecraftButtonBrush;
			}
			else
			{
				pen1 = SpacecraftButtonBorderPenLight;
				pen2 = SpacecraftButtonBorderPenDark;
				brush1 = SpacecraftButtonBrush;
			}
			
			// Draw rounded corners
			float cornerRadius = 3f;
			g.FillEllipse(brush1, new RectangleF(1, 1, cornerRadius, cornerRadius));
			g.FillEllipse(brush1, new RectangleF(this.Width - 2 - cornerRadius, 1, cornerRadius, cornerRadius));
			g.FillEllipse(brush1, new RectangleF(this.Width - 2- cornerRadius, this.Height - 2- cornerRadius, cornerRadius, cornerRadius));
			g.FillEllipse(brush1, new RectangleF(1, this.Height - 2- cornerRadius, cornerRadius, cornerRadius));

			// Draw Cross between corners
			g.FillRectangle(brush1, new RectangleF(1 + (cornerRadius / 2), 1, this.Width - 2 - cornerRadius, this.Height - 2));
			g.FillRectangle(brush1, new RectangleF(1, 1 + (cornerRadius / 2), this.Width - 2 , this.Height - 2 - cornerRadius));

			// Draw corner Borders
			g.DrawArc(pen1, 1, 1, cornerRadius, cornerRadius, 180, 90);
			g.DrawArc(pen1, this.Width - 2 - cornerRadius, 1, cornerRadius, cornerRadius, 270, 45);
			g.DrawArc(pen2, this.Width - 2 - cornerRadius, 1, cornerRadius, cornerRadius, 315, 45);
			g.DrawArc(pen2, this.Width - 2 - cornerRadius, this.Height - 2 - cornerRadius, cornerRadius, cornerRadius, 0, 90);
			g.DrawArc(pen2, 1, this.Height - 2 - cornerRadius, cornerRadius, cornerRadius, 90, 45);
			g.DrawArc(pen1, 1, this.Height - 2 - cornerRadius, cornerRadius, cornerRadius, 135, 45);

			// Draw edge borders
			g.DrawLine(pen1, 1 + (cornerRadius / 2), 1, this.Width - 1 - cornerRadius, 1);
			g.DrawLine(pen2, this.Width - 1, 1 + (cornerRadius / 2), this.Width - 1, this.Height - 1 - cornerRadius);
			g.DrawLine(pen2, 1 + (cornerRadius / 2), this.Height - 1, this.Width - 1 - (cornerRadius / 2), this.Height - 1);
			g.DrawLine(pen1, 1, this.Height - 1 - (cornerRadius / 2), 1, 1 + (cornerRadius / 2));

			// Draw Text
			StringFormat format = new StringFormat();
			format.Alignment = StringAlignment.Center;
			format.LineAlignment = StringAlignment.Center;
			float x = 2;
			float y = 2;
			if (this.pressed) { x = 3; y = 3; }
			g.DrawString(this.Text, this.Font, textBrushPush, new RectangleF(x, y, this.Width - 4, this.Height - 4), format);
		}
		
		protected override void OnMouseDown(MouseEventArgs e)
		{
			this.pressed = true;
			this.Invalidate();
			base.OnMouseDown(e);
		}
		
		protected override void OnMouseUp(MouseEventArgs e)
		{
			this.pressed = false;
			this.Invalidate();
			base.OnMouseUp(e);
		}

		public void PerformClick()
		{
			this.OnClick(new EventArgs());
		}
	}
}
