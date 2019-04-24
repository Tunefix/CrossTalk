using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	public partial class Server : Form
	{
		static private List<PrivateFontCollection> _fontCollections;
		public Font font;
		public Font fontB;
		public Font buttonFont;
		public Font smallFont;
		public Font smallFontB;
		public Font tinyFont;
		public Font tinyFontB;
		public Font CRTfont3;

		private void CreateFonts()
		{
			// GET FONTS
			font = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 12, FontStyle.Regular);
			fontB = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 12, FontStyle.Bold);

			buttonFont = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 10, FontStyle.Regular);
			smallFont = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 8, FontStyle.Regular);
			smallFontB = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 8, FontStyle.Bold);
			tinyFont = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 7, FontStyle.Regular);
			tinyFontB = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 7, FontStyle.Bold);

			CRTfont3 = GetCustomFont(GetBytesFromFile(AppDomain.CurrentDomain.BaseDirectory + "Resources\\consola.ttf"), 12, FontStyle.Regular);
		}

		static public Font GetCustomFont(byte[] fontData, float size, FontStyle style)
		{
			if (_fontCollections == null) _fontCollections = new List<PrivateFontCollection>();
			PrivateFontCollection fontCol = new PrivateFontCollection();
			IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);

			Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
			fontCol.AddMemoryFont(fontPtr, fontData.Length);
			Marshal.FreeCoTaskMem(fontPtr); //<-- It works!
			_fontCollections.Add(fontCol);
			return new Font(fontCol.Families[0], size, style);
		}

		public static byte[] GetBytesFromFile(string fullFilePath)
		{
			// this method is limited to 2^32 byte files (4.2 GB)

			FileStream fs = File.OpenRead(fullFilePath);
			try
			{
				byte[] bytes = new byte[fs.Length];
				fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
				fs.Close();
				return bytes;
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message + "\n" + e.StackTrace);
				fs.Close();
				return null;
			}
		}
	}
}
