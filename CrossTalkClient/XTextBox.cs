using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public class XTextBox : TextBox
	{
		Color background = Color.FromArgb(32, 32, 32);
		Color foreground = Color.FromArgb(200, 255, 255, 255);
		

		public XTextBox()
		{
			this.DoubleBuffered = true;
			this.BackColor = background;
			this.ForeColor = foreground;
			this.BorderStyle = BorderStyle.FixedSingle;
		}
	}
}
