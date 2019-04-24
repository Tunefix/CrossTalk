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
	class LedMeter : Control
	{
		public enum Direction { VERTICAL, HORIZONTAL}
		public Direction direction = Direction.VERTICAL;

		/// <summary>
		/// Number of leds of each color: green, yellow, red
		/// </summary>
		public Tuple<int, int, int> numberOfLeds = new Tuple<int, int, int>(10,0,0);

		// SCALE
		int min = 0;
		int max = 10;
		int value = 0;

		// CALCULATED NUMBERS
		float heightPrLed = 0;
		float valuePrLed = 0;
		int totalLeds = 0;

		// BRUSHES
		private Brush dimBrush;
		private Brush litBrush;
		private Brush ledBrush;
		private Pen borderPen = new Pen(Color.FromArgb(255, 16, 16, 16), 1.25f);
		private Brush backgroundBrush = new SolidBrush(Color.FromArgb(255, 32, 32, 32));

		private Brush dimGreen = new SolidBrush(Color.FromArgb(255, 0, 100, 0));
		private Brush litGreen = new SolidBrush(Color.FromArgb(255, 0, 200, 0));
		private Brush dimYellow = new SolidBrush(Color.FromArgb(255, 100, 100, 0));
		private Brush litYellow = new SolidBrush(Color.FromArgb(255, 200, 200, 0));
		private Brush dimRed = new SolidBrush(Color.FromArgb(255, 100, 0, 0));
		private Brush litRed = new SolidBrush(Color.FromArgb(255, 200, 0, 0));

		float ledSpacing = 1f;

		public LedMeter()
		{
			this.DoubleBuffered = true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;

			if (heightPrLed == 0) Calculate();

			// BACKGROUND
			g.FillRectangle(backgroundBrush, 1, 1, Width - 2, Height - 2);

			// BORDER
			g.DrawRectangle(borderPen, 1f, 1f, Width - 2f, Height - 2f);

			// LEDS
			for(int i = 0; i < totalLeds; i++)
			{
				if(i < numberOfLeds.Item1)
				{
					dimBrush = dimGreen;
					litBrush = litGreen;
				}
				else if(i < numberOfLeds.Item1 + numberOfLeds.Item2)
				{
					dimBrush = dimYellow;
					litBrush = litYellow;
				}
				else
				{
					dimBrush = dimRed;
					litBrush = litRed;
				}

				ledBrush = value > i * valuePrLed ? litBrush : dimBrush;

				float y = Height - 2f - ((i + 1) * heightPrLed) - (i * ledSpacing);

				RectangleF Rect = new RectangleF(2f, y, Width - 4f, heightPrLed);

				g.FillRectangle(ledBrush, Rect);

				// SHADE
				GraphicsPath Path = new GraphicsPath();
				//Path.AddEllipse(Rect);
				Path.AddRectangle(Rect);

				using (var maskBrush = new PathGradientBrush(Path))
				{
					maskBrush.CenterPoint = new PointF(this.Width / 2f, y + (heightPrLed / 2f));
					maskBrush.CenterColor = Color.FromArgb(0, 0, 0, 0);
					maskBrush.SurroundColors = new[] { Color.FromArgb(160, 0, 0, 0) };
					maskBrush.FocusScales = new PointF(0.2f, 0.2f);

					g.FillRectangle(maskBrush, Rect);
				}
			}
		}

		private void Calculate()
		{
			totalLeds = 0;
			totalLeds += numberOfLeds.Item1;
			totalLeds += numberOfLeds.Item2;
			totalLeds += numberOfLeds.Item3;

			heightPrLed = (Height - 4f - ((totalLeds - 1) * ledSpacing)) / totalLeds;

			int range = max - min;
			valuePrLed = range / totalLeds;
		}

		public void SetValue(int value)
		{
			this.value = value;
			this.Invalidate();
		}

		public void SetNumberOfLeds(int greens, int yellows, int reds)
		{
			numberOfLeds = new Tuple<int, int, int>(greens, yellows, reds);
			heightPrLed = 0;
		}

		public void SetScale(int min, int max)
		{
			this.min = min;
			this.max = max;
		}
	}
}
