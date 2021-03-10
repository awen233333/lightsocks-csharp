using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightSocksLocal
{
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket clientSocket = null;
        public Socket serverSocket = null;
    }

    public class LsLocal
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static void StartListening()
        {

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 1086);

            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                Console.WriteLine("listenning to " + ((IPEndPoint)listener.LocalEndPoint).Address.MapToIPv4());
                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            Console.WriteLine("accept to " + ((IPEndPoint)listener.LocalEndPoint).Address.MapToIPv4());

            Socket server = DialRemote();

            Console.WriteLine("connect to " + ((IPEndPoint)server.LocalEndPoint).Address.MapToIPv4());


            // Create the state object.  
            StateObject state = new StateObject();
            state.clientSocket = handler;
            state.serverSocket = server;

            server.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ServerReceive), state);
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ClientReceive), state);
        }

        public static void ServerReceive(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.serverSocket;
                int Ret = handler.EndReceive(ar);
                if (Ret > 0)
                {
                    Console.WriteLine(BitConverter.ToString(state.buffer));
                    state.clientSocket.BeginSend(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ClientSent), state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void ClientSent(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.clientSocket;

                int Ret = handler.EndSend(ar);
                if (Ret > 0)
                {
                    state.serverSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ServerReceive), state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void ClientReceive(IAsyncResult ar)
        {
            try
            {
                String content = String.Empty;

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.clientSocket;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    Console.WriteLine(BitConverter.ToString(state.buffer));
                    state.serverSocket.BeginSend(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ServerSent), state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void ServerSent(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.serverSocket;
                int Ret = handler.EndSend(ar);
                if (Ret > 0)
                {
                    IPAddress clientAddress = ((IPEndPoint)state.clientSocket.LocalEndPoint).Address.MapToIPv4();
                    IPAddress serverAddress = ((IPEndPoint)handler.LocalEndPoint).Address.MapToIPv4();

                    Console.WriteLine(BitConverter.ToString(state.buffer));

                    state.clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ClientReceive), state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static Socket DialRemote()
        {


            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 1088);

            // Create a TCP/IP socket.  
            Socket server = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            server.Connect(remoteEP);
            return server;

        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}