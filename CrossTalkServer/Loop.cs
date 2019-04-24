using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace CrossTalkServer
{
	class Loop
	{
		int sampleRate;
		int channels;

		public List<string> listeners = new List<string>();
		public Object listenersLock = new object();

		public List<string> talkers = new List<string>();
		public Object talkersLock = new object();

		public byte[] outgoingSamples;
		public BufferedWaveProvider loopSum;
		public bool pfl = false; // PFL ACTIVE

		public Loop(int sampleRate, int channels)
		{
			this.sampleRate = sampleRate;
			this.channels = channels;
			loopSum = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
			loopSum.ReadFully = true;
			loopSum.DiscardOnBufferOverflow = true;
		}

		public void addListenerToLoop(string listener)
		{
			if(!listeners.Contains(listener))
			{
				listeners.Add(listener);
			}
		}

		public void removeListenerFromLoop(string listener)
		{
			if (listeners.Contains(listener))
			{
				listeners.Remove(listener);
			}
		}

		public void addTalkerToLoop(string talker)
		{
			if (!talkers.Contains(talker))
			{
				talkers.Add(talker);
			}
		}

		public void removeTalkerFromLoop(string talker)
		{
			if (talkers.Contains(talker))
			{
				talkers.Remove(talker);
			}
		}
	}
}
