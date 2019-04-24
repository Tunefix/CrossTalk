using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	class Diode : Control
	{
		public enum DiodeColor { WHITE, GREEN, RED, AMBER }

		private DiodeColor color;

		private Brush dimBrush;
		private Brush litBrush;
		private Pen borderPen = new Pen(Color.FromArgb(255, 16, 16, 16), 1.25f);

		private bool lit;

		public Diode()
		{
			this.DoubleBuffered = true;
			SetColors(DiodeColor.RED);
		}

		public void SetLitState(bool state)
		{
			lit = state;
			this.Invalidate();
		}

		public void SetColors(DiodeColor c)
		{
			color = c;

			switch(color)
			{
				case DiodeColor.WHITE:
					dimBrush = new SolidBrush(Color.FromArgb(255, 100, 100, 100));
					litBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
					break;
				case DiodeColor.GREEN:
					break;
				case DiodeColor.RED:
					dimBrush = new SolidBrush(Color.FromArgb(255, 100, 0, 0));
					litBrush = new SolidBrush(Color.FromArgb(255, 255, 0, 0));
					break;
				case DiodeColor.AMBER:
					dimBrush = new SolidBrush(Color.FromArgb(255, 100, 100, 0));
					litBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 0));
					break;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;


			// Diode color
			if (lit)
			{
				g.FillEllipse(litBrush, 1, 1, this.Width - 2, this.Height - 2);
			}
			else
			{
				g.FillEllipse(dimBrush, 1, 1, this.Width - 2, this.Height - 2);
			}

			// LIGHT SHADE
			//RectangleF mask = new RectangleF(-(this.Width / 2f), -(this.Height / 2f), this.Width * 2, this.Height * 2);
			RectangleF mask = new RectangleF(1f, 1f, this.Width - 2f, this.Width - 2f);
			
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

			// Border
			g.DrawEllipse(borderPen, 1, 1, this.Width - 2, this.Height - 2);
		}
	}
}
