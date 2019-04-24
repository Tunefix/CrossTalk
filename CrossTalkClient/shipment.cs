using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossTalkClient
{
	class shipment
	{
		public DateTime time;
		public int size;

		public shipment(DateTime t, int s)
		{
			time = t;
			size = s;
		}
	}
}
