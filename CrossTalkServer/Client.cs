using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Net;

namespace CrossTalkServer
{
	internal class Client
	{
		public IPEndPoint endpoint;
		private bool isConnected;
		public string guid;
		public BufferedWaveProvider inputStream;
		public BufferedWaveProvider outputStream;
		public int audioFormat = 0;
		public byte[] currentSamples;
		public byte[] outgoingSamples;

		public Decoder decoder;
		public Encoder encoder;

		public DateTime lastPacket;

		/// <summary>
		/// List of loops this client talks to
		/// </summary>
		public List<int> destinations;
		public Object destinationsLock = new Object();

		/// <summary>
		/// List of loops this client listens to
		/// </summary>
		private List<int> sources;
		public Object sourcesLock = new Object();


		public Client(IPEndPoint endPoint, int sampleRate, int channels, string guid)
		{
			this.guid = guid;
			endpoint = endPoint;
			isConnected = true;
			inputStream = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
			inputStream.ReadFully = true;
			inputStream.DiscardOnBufferOverflow = true;

			outputStream = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
			outputStream.ReadFully = true;
			outputStream.DiscardOnBufferOverflow = true;

			destinations = new List<int>();
			sources = new List<int>();

			decoder = new Decoder();
			encoder = new Encoder();
			decoder.SetFormat(0);
			encoder.SetFormat(0);
		}

		public void setCodec(int codecFormat)
		{
			decoder.SetFormat(codecFormat);
			encoder.SetFormat(codecFormat);
		}

		public bool IsConnected()
		{
			return isConnected;
		}

		public void SetConnected(bool connected)
		{
			isConnected = connected;
		}

		public List<int> GetSources()
		{
			return sources;
		}

		public void AddSource(int source)
		{
			lock (sourcesLock)
			{
				if (sources.Contains(source)) return;
				sources.Add(source);
			}
		}

		public void RemoveSource(int source)
		{
			lock(sourcesLock)
			{
				if(sources.Contains(source))
				{
					sources.Remove(source);
				}
			}
		}

		public void AddDestination(int source)
		{
			lock (destinationsLock)
			{
				if (destinations.Contains(source)) return;
				destinations.Add(source);
			}
		}

		public void RemoveDestination(int source)
		{
			lock (destinationsLock)
			{
				if (destinations.Contains(source))
				{
					destinations.Remove(source);
				}
			}
		}
	}
}