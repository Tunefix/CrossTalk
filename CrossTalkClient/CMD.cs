using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkClient
{
	public partial class Client : Form
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
				  /**
				  * AudioFormats: (All mono)
				  * 0: PCM 32bit float 48kHz
				  * 1: PCM 16bit 48kHz
				  * 2: PCM 16bit 44.1kHz
				  * 3: G722
				  * 4: MPEG Layer 3, 128kbit 48kHz
				  * 5: MPEG Layer 3,  64kbit 24kHz
				  **/
		}

		public byte[] GetCmdBytes(CMDS command, byte argument)
		{
			byte[] cmd_bytes = BitConverter.GetBytes((int)command);
			byte[] r = new byte[3];

			r[0] = cmd_bytes[0];
			r[1] = cmd_bytes[1];
			r[2] = argument;

			return r;
		}

		public void ExecuteCmd(byte[] command, IPEndPoint sender)
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

		public void RunCommand(CMDS command, byte argument, IPEndPoint sender)
		{
			switch(command)
			{
				
				case CMDS.TurnOnLoopListenLight:
					if(listenButtons.Count > (int)argument) listenButtons[(int)argument].Invoke(new Action(() => {
						listenButtons[(int)argument].setLitState(true);
						loopListen[(int)argument] = true;
					}));
					break;
				case CMDS.TurnOffLoopListenLight:
					if (listenButtons.Count > (int)argument) listenButtons[(int)argument].Invoke(new Action(() => {
						listenButtons[(int)argument].setLitState(false);
						loopListen[(int)argument] = false;
					}));
					break;

				case CMDS.TurnOnLoopTalkLight:
					if (talkButtons.Count > (int)argument) talkButtons[(int)argument].Invoke(new Action(() => {
						talkButtons[(int)argument].setLitState(true);
					}));
					break;
				case CMDS.TurnOffLoopTalkLight:
					if (talkButtons.Count > (int)argument) talkButtons[(int)argument].Invoke(new Action(() => {
						talkButtons[(int)argument].setLitState(false);
					}));
					break;

				case CMDS.TurnOnLoopTalkFlash:
					if (talkButtons.Count > (int)argument) talkButtons[(int)argument].Invoke(new Action(() => {
						talkButtons[(int)argument].setTalkState(true);
						loopTalk[(int)argument] = true;
					}));
					break;
				case CMDS.TurnOffLoopTalkFlash:
					if (talkButtons.Count > (int)argument) talkButtons[(int)argument].Invoke(new Action(() => {
						talkButtons[(int)argument].setTalkState(false);
						loopTalk[(int)argument] = false;
					}));
					break;

				case CMDS.Pong:
						// SERVER IS ALIVE!!!
					break;
			}
		}
	}
}
