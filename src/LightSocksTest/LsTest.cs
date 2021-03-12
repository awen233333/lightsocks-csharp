using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections;
using System.Linq;

namespace LightSocksTest
{
    public class LsTest
    {
        private IPAddress _listenAddr;
        private string _password;
        private byte[] buffer = new byte[128];
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public LsTest(string password, string listenAddr)
        {
            _listenAddr = IPAddress.Parse(listenAddr);
            _password = password;
        }
        public async Task Listen(Action didListen)
        {
            TcpListener server = null;
            try
            {
                server = new TcpListener(_listenAddr, 1088);

                server.Start();

                didListen();

                Console.Write("Waiting for a connection... ");
                while (true)
                {
                    Socket client = await server.AcceptSocketAsync();

                    var task = Task.Run(() => { HandleConn(client); });
                    Console.WriteLine("connected");
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }


        public async void HandleConn(Socket conn)
        {
            try
            {
                // int Ret = await conn.ReceiveAsync(new ArraySegment<byte>(buffer), 0);
                int Ret = await conn.ReceiveAsync(new Memory<byte>(buffer), SocketFlags.None, tokenSource.Token);
                if (Ret! > 0 && buffer[0] != 5)
                {
                    conn.Close();
                    return;
                }

                int num = await conn.SendAsync(new byte[] { 0x05, 0x00 }, SocketFlags.None, tokenSource.Token);

                int sum = await conn.ReceiveAsync(new Memory<byte>(buffer), SocketFlags.None, tokenSource.Token);

                byte[] dstAddress = null;
                switch (buffer[3])
                {
                    case (byte)0x01:
                        dstAddress = buffer[4..8];
                        break;
                    case 0x03:
                        dstAddress = buffer[..(sum - 1)][5..-2];
                        break;
                    case 0x04:
                        dstAddress = buffer[4..8];
                        break;
                }
                IPAddress dstAddr = new IPAddress(dstAddress);
                byte[] port = { buffer[9], buffer[10] };
                Socket dstServer = null;
                try
                {
                    // BitConverter.ToString(dstAddress), BitConverter.ToUInt16(port)
                    dstServer = new Socket(dstAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await dstServer.ConnectAsync(dstAddr, BitConverter.ToUInt16(port));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    dstServer.Close();
                    dstServer = null;
                }
                if (dstServer != null)
                {
                    StateObject state = new StateObject();
                    state.clientSocket = conn;
                    state.serverSocket = dstServer;
                    await conn.SendAsync(new byte[] { 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, SocketFlags.None, tokenSource.Token);

                    // var task = Task.Run(() => { OnProxyData(state); });
                    await Forward(state);
                    // conn.BeginReceive(state.up_buffer, 0, StateObject.BufferSize, 0,
                    //     new AsyncCallback(ClientReceive), state);
                    // dstServer.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    //     new AsyncCallback(ServerReceive), state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        private async Task Forward(StateObject state)
        {
            try
            {
                Console.WriteLine("hehe333");
                await Task.WhenAll(OnProxyData(state),OnClientData(state));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public async Task OnProxyData(StateObject state)
        {
            try
            {
                Socket dst = state.serverSocket;
                Socket conn = state.clientSocket;
                while (true)
                {
                    int len = await dst.ReceiveAsync(new Memory<byte>(state.buffer), 0, tokenSource.Token);
                    if (len == 0) break;
                    int len2 = await conn.SendAsync(state.buffer, 0, tokenSource.Token);
                    if (len2 == 0) break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task OnClientData(StateObject state)
        {
            try
            {
                Socket dst = state.serverSocket;
                Socket conn = state.clientSocket;
                while (true)
                {
                    int len = await conn.ReceiveAsync(new Memory<byte>(state.up_buffer), 0, tokenSource.Token);
                    if (len == 0) break;
                    int len2 = await dst.SendAsync(state.up_buffer, 0, tokenSource.Token);
                    if (len2 == 0) break;
                }
                dst.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void ClientReceive(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handle = state.clientSocket;
                int readSize = handle.EndReceive(ar);
                if (readSize > 0)
                {
                    state.serverSocket.Send(state.up_buffer);
                    handle.BeginReceive(state.up_buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ClientReceive), state);
                    // state.serverSocket.BeginSend(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ServerSend), state);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public void ServerReceive(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handle = state.serverSocket;
                int readSize = handle.EndReceive(ar);
                if (readSize > 0)
                {
                    state.clientSocket.Send(state.buffer);
                    handle.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ServerReceive), state);
                    // state.clientSocket.BeginSend(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ServerSend), state);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // public void ServerSend(IAsyncResult ar)
        // {
        //     try
        //     {
        //         StateObject state = (StateObject)ar.AsyncState;
        //         Socket server = state.serverSocket;
        //         int writeSize = server.EndSend(ar);
        //         if (writeSize > 0)
        //         {
        //             state.clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //                     new AsyncCallback(ClientReceive), state);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine(ex.Message);
        //     }
        // }
        // public void ClientSend(IAsyncResult ar)
        // {
        //     try
        //     {
        //         StateObject state = (StateObject)ar.AsyncState;
        //         Socket handle = state.clientSocket;
        //         int writeSize = handle.EndSend(ar);
        //         if (writeSize > 0)
        //         {
        //             state.serverSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //                     new AsyncCallback(ClientReceive), state);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine(ex.Message);
        //     }
        // }

    }

    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 4096;

        // Receive buffer.  
        public byte[] up_buffer = new byte[BufferSize];
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket clientSocket = null;
        public Socket serverSocket = null;
    }
}
