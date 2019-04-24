using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CrossTalkServer
{
	public class VerticalMeter : Control
	{
		public bool doubleMeter = false;
		/// <summary>
		/// Show only singe scale even if meter is double
		/// </summary>
		public bool singleScale = false;
		float value1 = 0;
		float value2 = 0;

		int scale1min = 0;
		int scale1max = 100;
		int scale2min = 0;
		int scale2max = 100;
		List<float> manScale1 = new List<float>();
		List<float> manScale2 = new List<float>();

		readonly int scaleType = 0; // 0 = Linear, 1 = Log, 2 = manual
		public int subdivisions { get; set; }

		Font font;

		readonly Pen borderPen = new Pen(Color.FromArgb(255, 32, 32, 32), 3f);
		readonly Pen scalePen = new Pen(Color.FromArgb(240, 255, 255, 255), 1.5f);
		readonly Brush scaleBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255));
		readonly Brush pointerBrush = new SolidBrush(Color.FromArgb(255, 16, 16, 16));

		/// <summary>
		/// 
		/// </summary>
		/// <param name="font"></param>
		/// <param name="scaleType">0: Linear\n1: Log\n2: manual</param>
		public VerticalMeter(Font font, int scaleType)
		{
			this.DoubleBuffered = true;
			this.font = font;
			this.scaleType = scaleType;
			this.subdivisions = 0;
		}

		public void setScale(int min, int max) { setScale1(min, max); setScale2(min, max);}
		
		public void setScale1(int min, int max)
		{
			scale1min = min;
			scale1max = max;
			manScale1.Clear();
			manScale1.Add(min);
			manScale1.Add(max);
		}
		
		public void setScale2(int min, int max)
		{
			scale2min = min;
			scale2max = max;
			manScale2.Clear();
			manScale2.Add(min);
			manScale2.Add(max);
		}

		public void setManScale1(float[] labels)
		{
			manScale1.Clear();
			foreach(float f in labels)
			{
				manScale1.Add(f);
			}
		}

		public void setManScale2(float[] labels)
		{
			manScale2.Clear();
			foreach (float f in labels)
			{
				manScale2.Add(f);
			}
		}

		public void setValue(float value) { setValue1(value); setValue2(value);}
		
		public void setValue1(float value)
		{
			value1 = value;

			this.Invalidate();
		}
		
		public void setValue2(float value)
		{
			value2 = value;

			this.Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			RectangleF Rect = new Rectangle(0, 0, Width, Height);
			Rectangle Rectb = new Rectangle(0, 0, Width, Height);

			// Black background
			g.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 0)), Rect);

			// Peg the arrows
			if (value1 > scale1max) { value1 = scale1max; }
			if (value1 < scale1min) { value1 = scale1min; }
			if (value2 > scale2max) { value2 = scale2max; }
			if (value2 < scale2min) { value2 = scale2min; }

			// Set nan to scaleMinimum
			if (double.IsNaN((double)value1)){ value1 = scale1min; }
			if (double.IsNaN((double)value2)){ value2 = scale2min; }

			if (doubleMeter)
			{
				drawDoubleWhite(g);
				drawDoubleScale(g);
				drawDoublePointer(g);
			}
			else
			{
				drawSingleWhite(g);
				drawSingleScale(g);
				drawSinglePointer(g);
			}
			
			// Border Line
			g.DrawRectangle(borderPen, Rectb);
		}

		private void drawSingleWhite(Graphics g)
		{
			// Arrow-Lane (The white(glowy) bit
			RectangleF Rect = new Rectangle(3, 0, (Width / 3), Height / 2);
			Brush whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), 90);
			g.FillRectangle(whiteGrad, Rect);

			Rect = new Rectangle(3, (Height / 2) - 1, (Width / 3), Height / 2);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(255, 255, 255, 255), Color.FromArgb(200, 255, 255, 255), 90);
			g.FillRectangle(whiteGrad, Rect);

			// Arrow-Lane vertical drop-shadows
			Rect = new Rectangle(2, 0, 3, Height);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 0f);
			g.FillRectangle(whiteGrad, Rect);

			Rect = new Rectangle(2 + (Width / 3), 0, 3, Height);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 180f);
			g.FillRectangle(whiteGrad, Rect);
		}

		private void drawDoubleWhite(Graphics g)
		{
			// Arrow-Lane (The white(glowy) bit
			RectangleF Rect = new Rectangle(3, 0, (Width / 5), Height / 2);
			Brush whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), 90);
			g.FillRectangle(whiteGrad, Rect);

			Rect = new Rectangle(3, (Height / 2) - 1, (Width / 5), Height / 2);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(255, 255, 255, 255), Color.FromArgb(200, 255, 255, 255), 90);
			g.FillRectangle(whiteGrad, Rect);

			// Arrow-Lane vertical drop-shadows
			Rect = new Rectangle(2, 0, 3, Height);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 0f);
			g.FillRectangle(whiteGrad, Rect);

			Rect = new Rectangle(2 + (Width / 5), 0, 3, Height);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 180f);
			g.FillRectangle(whiteGrad, Rect);


			float x = Width - (Width / 5) - 3;
			// Arrow-Lane (The white(glowy) bit
			Rect = new RectangleF(x, 0, (Width / 5), Height / 2);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 255, 255, 255), Color.FromArgb(255, 255, 255, 255), 90);
			g.FillRectangle(whiteGrad, Rect);

			Rect = new RectangleF(x, (Height / 2) - 1, (Width / 5), Height / 2);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(255, 255, 255, 255), Color.FromArgb(200, 255, 255, 255), 90);
			g.FillRectangle(whiteGrad, Rect);

			// Arrow-Lane vertical drop-shadows
			Rect = new RectangleF(x - 1, 0, 3, Height);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 0f);
			g.FillRectangle(whiteGrad, Rect);

			Rect = new RectangleF(x - 1 + (Width / 5), 0, 3, Height);
			whiteGrad = new LinearGradientBrush(Rect, Color.FromArgb(200, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), 180f);
			g.FillRectangle(whiteGrad, Rect);
		}

		private void drawSingleScale(Graphics g)
		{
			// Scale
			float x1 = (Width / 3) + 3;
			float x2 = (Width / 3) + 8;
			float[] scaleData = getScaleData(scale1max, scale1min);
			StringFormat format = new StringFormat();
			format.LineAlignment = StringAlignment.Center;

			if (scaleType == 2)
			{
				// MANUAL SCALE
				foreach(float f in manScale1)
				{
					float y1 = (Height - 10) - (scaleData[1] * f);
					float y2 = y1;

					g.DrawLine(scalePen, x1, y1, x2, y2);

					g.DrawString(Math.Round((f * scaleData[0])).ToString(), font, scaleBrush, x2 + 2, y2, format);
				}
			}
			else
			{
				for (int i = 0; i < scaleData[2] + 1; i++)
				{
					float y1 = (Height - 10) - (scaleData[1] * i);
					float y2 = y1;

					g.DrawLine(scalePen, x1, y1, x2, y2);

					g.DrawString(Math.Round((i * scaleData[0]) + scale1min).ToString(), font, scaleBrush, x2 + 2, y2, format);

					float subSpace = scaleData[1] / subdivisions;
					float xHalf = Math.Abs(x2 - x1) / 2f;
					for (int n = 0; n < subdivisions; n++)
					{
						y1 = (Height - 10) - (scaleData[1] * i) - (subSpace * n);
						y2 = y1;
						g.DrawLine(scalePen, x1, y1, x2 - xHalf, y2);
					}
				}
			}
		}
		
		
		private void drawDoubleScale(Graphics g)
		{
			if (singleScale)
			{
				// Scale 1
				float x1 = (Width / 5) + 3;
				float x2 = (Width / 5) + 6;
				float x3 = Width - (Width / 5) - 3;
				float x4 = Width - (Width / 5) - 6;
				float[] scaleData = getScaleData(scale1max, scale1min);
				StringFormat format = new StringFormat();
				format.LineAlignment = StringAlignment.Center;
				format.Alignment = StringAlignment.Center;

				if (scaleType == 2)
				{
					// MANUAL SCALE
					foreach (float f in manScale1)
					{
						float y1 = (Height - 10) + ((scale1min - f) / scaleData[3]);
						float y2 = y1;

						g.DrawLine(scalePen, x1, y1, x2, y2);
						g.DrawLine(scalePen, x3, y1, x4, y2);

						float xr = (int)Math.Round((Width / 2f) - 10);
						float yr = y2 - 4;
						float wr = 20;
						float hr = 8;
						string prepend = "";
						if (f > 0) prepend = "+";
						if (f < 0) prepend = "-";
						g.DrawString(prepend + Math.Round(Math.Abs(f)).ToString(), font, scaleBrush, new RectangleF(xr, yr, wr, hr), format);
					}
				}
				else
				{
					for (int i = 0; i < scaleData[2] + 1; i++)
					{
						float y1 = (Height - 10) - (scaleData[1] * i);
						float y2 = y1;
						g.DrawLine(scalePen, x1, y1, x2, y2);

						g.DrawString(Math.Round(i * scaleData[0]).ToString(), font, scaleBrush, x2 + 1, y2, format);

						float subSpace = scaleData[1] / subdivisions;
						float xHalf = Math.Abs(x2 - x1) / 2f;
						for (int n = 0; n < subdivisions; n++)
						{
							y1 = (Height - 10) - (scaleData[1] * i) - (subSpace * n);
							y2 = y1;
							g.DrawLine(scalePen, x1, y1, x2 - xHalf, y2);
						}
					}
				}
			}
			else
			{
				// Scale 1
				float x1 = (Width / 5) + 3;
				float x2 = (Width / 5) + 6;
				float[] scaleData = getScaleData(scale1max, scale1min);
				StringFormat format = new StringFormat();
				format.LineAlignment = StringAlignment.Center;

				if (scaleType == 2)
				{
					// MANUAL SCALE
					foreach (float f in manScale1)
					{
						float y1 = (Height - 10) + ((scale1min - f) / scaleData[3]);
						float y2 = y1;

						g.DrawLine(scalePen, x1, y1, x2, y2);

						g.DrawString(Math.Round(f).ToString(), font, scaleBrush, x2 + 2, y2, format);
					}
				}
				else
				{
					for (int i = 0; i < scaleData[2] + 1; i++)
					{
						float y1 = (Height - 10) - (scaleData[1] * i);
						float y2 = y1;
						g.DrawLine(scalePen, x1, y1, x2, y2);

						g.DrawString(Math.Round(i * scaleData[0]).ToString(), font, scaleBrush, x2 + 1, y2, format);

						float subSpace = scaleData[1] / subdivisions;
						float xHalf = Math.Abs(x2 - x1) / 2f;
						for (int n = 0; n < subdivisions; n++)
						{
							y1 = (Height - 10) - (scaleData[1] * i) - (subSpace * n);
							y2 = y1;
							g.DrawLine(scalePen, x1, y1, x2 - xHalf, y2);
						}
					}
				}

				// Scale 2
				x1 = Width - (Width / 5) - 3;
				x2 = Width - (Width / 5) - 6;
				scaleData = getScaleData(scale2max, scale2min);
				format.LineAlignment = StringAlignment.Center;
				format.Alignment = StringAlignment.Far;

				if (scaleType == 2)
				{
					// MANUAL SCALE
					foreach (float f in manScale2)
					{
						float y1 = (Height - 10) + ((scale2min - f) / scaleData[3]);
						float y2 = y1;

						g.DrawLine(scalePen, x1, y1, x2, y2);

						g.DrawString(Math.Round(f).ToString(), font, scaleBrush, x2 + 2, y2, format);
					}
				}
				else
				{
					for (int i = 0; i < scaleData[2] + 1; i++)
					{
						float y1 = (Height - 10) - (scaleData[1] * i);
						float y2 = y1;
						g.DrawLine(scalePen, x1, y1, x2, y2);

						g.DrawString(Math.Round(i * scaleData[0]).ToString(), font, scaleBrush, x2 - 1, y2, format);

						float subSpace = scaleData[1] / subdivisions;
						float xHalf = Math.Abs(x2 - x1) / 2f;
						for (int n = 0; n < subdivisions; n++)
						{
							y1 = (Height - 10) - (scaleData[1] * i) - (subSpace * n);
							y2 = y1;
							g.DrawLine(scalePen, x1, y1, x2 + xHalf, y2);
						}
					}
				}
			}
		}

		private void drawSinglePointer(Graphics g)
		{
			// Pointer
			double scaler = (Height - 20) / (double)Math.Abs(scale1max - scale1min);
			float y = (float)((Height - 10) - (value1 * scaler));
			float x = 3;
			float w = Width / 3;
			float a = 50; // Angle at "pointy" end


			PointF[] points = new PointF[4];
			points[0] = new PointF(x + w, y);
			points[1] = new PointF(x, (float)(y + (w * Math.Sin(Helper.deg2rad(a / 2)))));
			points[2] = new PointF(x, (float)(y - (w * Math.Sin(Helper.deg2rad(a / 2)))));
			points[3] = new PointF(x + w, y);
			g.FillPolygon(pointerBrush, points);

			RectangleF Rect = new RectangleF(0, y - 1, 4, 2);
			g.FillRectangle(pointerBrush, Rect);
		}
		
		
		private void drawDoublePointer(Graphics g)
		{
			// Pointer 1
			double scaler = (Height - 20) / (double)Math.Abs(scale1max - scale1min);
			float y = (float)((Height - 10) - ((value1 - scale1min) * scaler));
			float x = 3;
			float w = Width / 5;
			float a = 50; // Angle at "pointy" end


			PointF[] points = new PointF[4];
			points[0] = new PointF(x + w, y);
			points[1] = new PointF(x, (float)(y + (w * Math.Sin(Helper.deg2rad(a / 2)))));
			points[2] = new PointF(x, (float)(y - (w * Math.Sin(Helper.deg2rad(a / 2)))));
			points[3] = new PointF(x + w, y);
			g.FillPolygon(pointerBrush, points);

			RectangleF Rect = new RectangleF(0, y - 1, 4, 2);
			g.FillRectangle(pointerBrush, Rect);
			
			
			// Pointer 2
			scaler = (Height - 20) / (double)Math.Abs(scale2max - scale2min);
			y = (float)((Height - 10) - ((value2 - scale2min) * scaler));
			x = Width - 3;
			w = Width / 5;
			a = 50; // Angle at "pointy" end


			points = new PointF[4];
			points[0] = new PointF(x - w, y);
			points[1] = new PointF(x, (float)(y + (w * Math.Sin(Helper.deg2rad(a / 2)))));
			points[2] = new PointF(x, (float)(y - (w * Math.Sin(Helper.deg2rad(a / 2)))));
			points[3] = new PointF(x - w, y);
			g.FillPolygon(pointerBrush, points);

			Rect = new RectangleF(Width - 4, y - 1, 4, 2);
			g.FillRectangle(pointerBrush, Rect);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="max">Top of scale</param>
		/// <param name="min">Bottom of scale</param>
		/// <returns>
		/// float[0] - gridStep
		/// float[1] - gridStepPx
		/// float[2] - gridLines
		/// float[3] - xPrPx
		/// </returns>
		private float[] getScaleData(int max, int min)
		{
			float[] ret = new float[4];
			
			int split = max - min;
			float xPrPx = (float)(split / (Height - 20f));
			int maxLabels = (int)Math.Floor((Height - 20) / 25f);
			int numLabels = (int)(Math.Ceiling(maxLabels / 5f) * 5);
			float gridStep = split / (float)numLabels;
			
			int gridLines = (int)Math.Floor(split / gridStep);
			float gridStepPx = (float)(gridStep / xPrPx);

			ret[0] = gridStep;
			ret[1] = gridStepPx;
			ret[2] = gridLines;
			ret[3] = xPrPx;

			return ret;
		}

		public float getValue1()
		{
			return value1;
		}

		public float getValue2()
		{
			return value2;
		}
	}
}
