using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CrossTalkServer
{
	static class CMD
	{
		/**
		 * Each command i made of 3 bytes, group in a byte[3] array.
		 * 
		 * The first byte is the command set. This is in case we go over 256 commands.
		 * With the command set byte, the total of available commands are 256 * 256 = 65 536
		 * 
		 * The second byte is the command number.
		 * 
		 * The first and second bytes can be viewd as a pair containing the command number.
		 * 
		 * The third byte is an argument byte, should the command require an argument.
		 **/

		static Dictionary<string, Client> clients;
		static Dictionary<int, MocrButton> lButtons;
		static Dictionary<int, MocrButton> tButtons;

		static UdpListener UdpServer;
		static UdpUser UdpClient;

		static List<Loop> loops;

		// Define commands
		public enum CMDS
		{
			StopListeningToLoop,  // 0, 0, Loop

			StartListeningToLoop, // 0, 1, Loop

			StartTalkingToLoop, // 0, 2, Loop

			StopTalkingToLoop, // 0, 3, Loop

			TurnOnLoopListenLight, // 0, 4, Loop
			TurnOffLoopListenLight, // 0, 5, Loop

			TurnOnLoopTalkLight, // 0, 6, Loop
			TurnOffLoopTalkLight, // 0, 7, Loop

			TurnOnLoopTalkFlash, // 0, 8, Loop
			TurnOffLoopTalkFlash, // 0, 9, Loop

			SetClientAudioFormat, // 0, 10, formatCode

			Ping, // 0, 11, 0
			Pong, // 0, 12, 0
		}

		static public void ServerInit(Dictionary<string, Client> clientList, UdpListener server, List<Loop> serverLoops)
		{
			clients = clientList;
			loops = serverLoops;
			UdpServer = server;
		}

		static public void ClientInit(Dictionary<int, MocrButton> listenButtons, Dictionary<int, MocrButton> talkButtons, UdpUser client)
		{
			lButtons = listenButtons;
			tButtons = talkButtons;
			UdpClient = client;
		}

		static public byte[] GetCmdBytes(CMDS command, byte argument)
		{
			byte[] cmd_bytes = BitConverter.GetBytes((int)command);
			byte[] r = new byte[3];

			r[0] = cmd_bytes[0];
			r[1] = cmd_bytes[1];
			r[2] = argument;

			return r;
		}

		static public void ExecuteCmd(byte[] command, string sender)
		{
			if(command.Length != 3)
			{
				throw new ArgumentException("Command has wrong number of bytes");
			}

			int command_number = BitConverter.ToInt16(command, 0);
			if(Enum.IsDefined(typeof(CMDS), command_number))
			{
				RunCommand((CMDS)command_number, command[2], sender);
			}
			else
			{
				throw new Exception("Command not defined");
			}
		}

		static public void RunCommand(CMDS command, byte argument, string sender)
		{
			switch(command)
			{
				case CMDS.StartListeningToLoop:
					if (!clients[sender].GetSources().Contains((int)argument))
					{
						clients[sender].AddSource((int)argument);
						loops[(int)argument].addListenerToLoop(sender);
						UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOnLoopListenLight, argument), clients[sender].endpoint);
						
					}
					break;
				case CMDS.StopListeningToLoop:
					if (clients[sender].GetSources().Contains((int)argument))
					{
						clients[sender].RemoveSource((int)argument);
						loops[(int)argument].removeListenerFromLoop(sender);
						UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOffLoopListenLight, argument), clients[sender].endpoint);
					}
					break;

				case CMDS.StartTalkingToLoop:
					// REMOVE TALKER FROM ALL LOOPS
					{
						for (int i = 0; i < loops.Count; i++)
						{
							if (loops[i].talkers.Contains(sender))
							{
								loops[i].removeTalkerFromLoop(sender);
								clients[sender].destinations.Remove(i);
								UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOffLoopTalkFlash, (byte)i), clients[sender].endpoint);
							}
						}
					}

					if (!clients[sender].destinations.Contains((int)argument))
					{
						clients[sender].destinations.Add((int)argument);
						loops[(int)argument].addTalkerToLoop(sender);
						UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOnLoopTalkFlash, argument), clients[sender].endpoint);

						// ACTIVATE ACTIVE-LIGHT FOR ALL USERS
						foreach(KeyValuePair<string, Client> kvp in clients)
						{
							Client c = kvp.Value;

							if(c.IsConnected() && kvp.Key != sender)
							{
								UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOnLoopTalkLight, argument), c.endpoint);
							}
						}
					}
					break;
				case CMDS.StopTalkingToLoop:
					if (clients[sender].destinations.Contains((int)argument))
					{
						clients[sender].destinations.Remove((int)argument);
						loops[(int)argument].removeTalkerFromLoop(sender);
						UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOffLoopTalkFlash, argument), clients[sender].endpoint);

						// IF LOOP IS EMPTY BLANK TALK BUTTONS
						if (loops[(int)argument].talkers.Count == 0)
						{
							foreach (KeyValuePair<string, Client> kvp in clients)
							{
								Client c = kvp.Value;

								if (c.IsConnected())
								{
									UdpServer.Reply(CMD.GetCmdBytes(CMDS.TurnOffLoopTalkLight, argument), c.endpoint);
								}
							}
						}
					}
					break;

				case CMDS.TurnOnLoopListenLight:
					if(lButtons.ContainsKey((int)argument)) lButtons[(int)argument].Invoke(new Action(() => { lButtons[(int)argument].setLitState(true); }));
					break;
				case CMDS.TurnOffLoopListenLight:
					if (lButtons.ContainsKey((int)argument)) lButtons[(int)argument].Invoke(new Action(() => { lButtons[(int)argument].setLitState(false); }));
					break;

				case CMDS.TurnOnLoopTalkLight:
					if (tButtons.ContainsKey((int)argument)) tButtons[(int)argument].Invoke(new Action(() => { tButtons[(int)argument].setLitState(true); }));
					break;
				case CMDS.TurnOffLoopTalkLight:
					if (tButtons.ContainsKey((int)argument)) tButtons[(int)argument].Invoke(new Action(() => { tButtons[(int)argument].setLitState(false); }));
					break;

				case CMDS.SetClientAudioFormat:
					clients[sender].audioFormat = (int)argument;
					clients[sender].decoder.SetFormat((int)argument);
					clients[sender].encoder.SetFormat((int)argument);
					break;


				case CMDS.Ping:
					UdpServer.Reply(CMD.GetCmdBytes(CMDS.Pong, (byte)0), clients[sender].endpoint);
					break;
			}
		}
	}
}
