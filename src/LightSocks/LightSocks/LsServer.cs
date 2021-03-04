using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LightSocks
{
	class LsServer
	{
		private IPAddress ListenAddr;
		private Socket _socket;

		public LsServer(string password, string listenAddr)
		{
			ListenAddr = IPAddress.Parse(listenAddr);
		}

		public void Listen() 
		{
			try
			{
				_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				IPEndPoint endPoint = new IPEndPoint(ListenAddr, 8998);
				_socket.Bind(endPoint);
				_socket.Listen();
				while (true)
				{

				}
			}
			catch(Exception ex)
            {
				_socket.Shutdown(SocketShutdown.Both);
				_socket.Close();
				Console.WriteLine(ex.Message);
			}
		}

	}
}
