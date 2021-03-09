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
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

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

            // Socket remoteServer = DialRemote();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.clientSocket = handler;
            
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static Socket DialRemote()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry("host.contoso.com");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 9999);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            client.Connect(remoteEP);
            return client;
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.clientSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.  
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
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
    // class LsLocal
    // {
    //     public IPAddress ListenAddr;
    //     public IPAddress RemoteAddr;
    //     private string _ip = string.Empty;
    //     private int _port = 0;
    //     private Socket _socket = null;
    //     private byte[] buffer = new byte[1024 * 1024 * 2];

    //     public LsLocal(string password, string listenAddr, string remoteAddr)
    //     {
    //         ListenAddr = IPAddress.Parse(listenAddr);
    //         RemoteAddr = IPAddress.Parse(remoteAddr);
    //     }

    //     public void Listen()
    //     {
    //         try
    //         {
    //             //1.0 实例化套接字(IP4寻址地址,流式传输,TCP协议)
    //             _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    //             //2.0 创建IP对象
    //             IPAddress address = IPAddress.Parse(_ip);
    //             //3.0 创建网络端口包括ip和端口
    //             IPEndPoint endPoint = new IPEndPoint(address, _port);
    //             //4.0 建立连接
    //             _socket.Connect(endPoint);
    //             Console.WriteLine("连接服务器成功");
    //             //5.0 接收数据
    //             int length = _socket.Receive(buffer);
    //             Console.WriteLine("接收服务器{0},消息:{1}", _socket.RemoteEndPoint.ToString(), Encoding.UTF8.GetString(buffer, 0, length));
    //             //6.0 像服务器发送消息
    //             for (int i = 0; i < 10; i++)
    //             {
    //                 Thread.Sleep(2000);
    //                 string sendMessage = string.Format("客户端发送的消息,当前时间{0}", DateTime.Now.ToString());
    //                 _socket.Send(Encoding.UTF8.GetBytes(sendMessage));
    //                 Console.WriteLine("像服务发送的消息:{0}", sendMessage);
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             _socket.Shutdown(SocketShutdown.Both);
    //             _socket.Close();
    //             Console.WriteLine(ex.Message);
    //         }
    //         Console.WriteLine("发送消息结束");
    //         Console.ReadKey();
    //     }
    // }
}