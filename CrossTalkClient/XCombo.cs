using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public class XCombo : ComboBox
	{
		Color background = Color.FromArgb(32, 32, 32);
		Color foreground = Color.FromArgb(200, 255, 255, 255);

		public XCombo()
		{
			this.DoubleBuffered = true;
		}
	}
}
