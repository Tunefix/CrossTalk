using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	class LineGraph : Control
	{
		List<List<double>> lines = new List<List<double>>();

		private Pen borderPen = new Pen(Color.FromArgb(255, 16, 16, 16), 1.25f);
		private Pen gridPen = new Pen(Color.FromArgb(255, 24, 24, 24), 1.0f);
		private Brush backgroundBrush = new SolidBrush(Color.FromArgb(255, 32, 32, 32));
		private Brush fontBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

		private List<Pen> linePens = new List<Pen>();
		private float multiply = 10;
		StringFormat scaleformat = new StringFormat();


		// LAYOUT OPTIONS
		int margin = 5;
		int maxY = 500;
		int minY = 0;
		double PxPrY;
		int usableHeight;
		int usableWidth;


		public LineGraph()
		{
			this.DoubleBuffered = true;
			Calculate();

			// ADD PENS
			linePens.Add(new Pen(Color.FromArgb(255, 200, 0, 0), 1.25f));
			linePens.Add(new Pen(Color.FromArgb(255, 0, 200, 0), 1.25f));
			linePens.Add(new Pen(Color.FromArgb(255, 0, 100, 200), 1.25f));
			linePens.Add(new Pen(Color.FromArgb(255, 200, 200, 0), 1.25f));
			linePens.Add(new Pen(Color.FromArgb(255, 200, 0, 200), 1.25f));
			linePens.Add(new Pen(Color.FromArgb(255, 0, 200, 200), 1.25f));
			linePens.Add(new Pen(Color.FromArgb(255, 200, 200, 200), 1.25f));


			scaleformat.Alignment = StringAlignment.Far;
			scaleformat.LineAlignment = StringAlignment.Center;
		}

		public void SetData(List<List<float>> data)
		{
			List<List<double>> linesTmp = new List<List<double>>();
			foreach (List<float> list in data)
			{
				linesTmp.Add(list.ConvertAll(x => (double)x));
			}
			lines = linesTmp;
			this.Invalidate();
		}

		public void SetData(List<List<int>> data)
		{
			List<List<double>> linesTmp = new List<List<double>>();
			foreach (List<int> list in data)
			{
				linesTmp.Add(list.ConvertAll(x => (double)x));
			}
			lines = linesTmp;
			this.Invalidate();
		}

		public void SetData(List<List<double>> data)
		{
			lines = data;
			this.Invalidate();
		}

		public void SetScaleY(int min, int max)
		{
			minY = min;
			maxY = max;
			Calculate();
		}

		private void Calculate()
		{
			usableHeight = Height - (2 * margin);
			usableWidth = Width - (2 * margin);

			PxPrY = usableHeight / (maxY - minY);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			// BACKGROUND
			g.FillRectangle(backgroundBrush, 1, 1, Width - 2, Height - 2);

			// BORDER
			g.DrawRectangle(borderPen, 1f, 1f, Width - 2f, Height - 2f);

			// SCALE
			DrawScale(g);

			// LEGEND
			DrawLegend(g);

			DrawLines(lines, g);
		}

		private void DrawLegend(Graphics g)
		{
			g.DrawLine(linePens[0], new PointF(10f, 10f), new PointF(20f, 10f));
			g.DrawLine(linePens[1], new PointF(10f, 20f), new PointF(20f, 20f));
			g.DrawLine(linePens[2], new PointF(10f, 30f), new PointF(20f, 30f));
			g.DrawLine(linePens[3], new PointF(10f, 40f), new PointF(20f, 40f));

			g.DrawString("Fetch Audio", Font, fontBrush, new PointF(25f, 5f));
			g.DrawString("Mix Audio", Font, fontBrush, new PointF(25f, 15f));
			g.DrawString("Send Audio", Font, fontBrush, new PointF(25f, 25f));
			g.DrawString("Update Meters", Font, fontBrush, new PointF(25f, 35f));
		}

		private void DrawScale(Graphics g)
		{
			float y;
			float x = 18f;
			

			for(int i = 0; i < 30; i++)
			{
				y = usableHeight - (i * multiply) + margin;

				g.DrawString(i.ToString(), Font, fontBrush, x, y, scaleformat);
				g.DrawLine(gridPen, x + 3f, y, usableWidth, y);
			}
		}

		private void DrawLines(List<List<double>> data, Graphics g)
		{
			int startX = margin;
			int offset = 0;
			int curPos = 0;
			PointF? p1 = null;
			PointF? p2;
			float x;
			float y;
			int line = 0;

			foreach(List<double> list in data)
			{
				curPos = 0;
				startX = margin;
				offset = 0;
				p1 = null;

				if (list.Count > usableWidth)
				{
					offset = list.Count - usableWidth;
				}

				if(list.Count < usableWidth)
				{
					startX += usableWidth - list.Count;
				}

				for(int i = offset; i < list.Count; i++)
				{
					y = usableHeight - ((float)list[i] * multiply) + margin;
					x = startX + curPos;
					p2 = new PointF(x, y);

					if(p1 !=null)
					{
						// Draw line
						g.DrawLine(linePens[line], (PointF)p1, (PointF)p2);
					}

					p1 = p2;
					curPos++;
				}

				line++;
				if (line > linePens.Count) line = 0;
			}
		}
	}
}
