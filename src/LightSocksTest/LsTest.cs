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
            // int Ret = await conn.ReceiveAsync(new ArraySegment<byte>(buffer), 0);
            int Ret = await conn.ReceiveAsync(new Memory<byte>(buffer), 0, tokenSource.Token);
            Console.WriteLine(buffer.Length + " " + BitConverter.ToString(buffer));

            int num = await conn.SendAsync(new Memory<byte>(new byte[] { 0x05, 0x00 }), 0, tokenSource.Token);

            int sum = await conn.ReceiveAsync(new Memory<byte>(buffer), 0, tokenSource.Token);

            IPAddress remote = new IPAddress(buffer.Skip(4).Take(4).ToArray());
            Console.WriteLine(remote.MapToIPv4());
            Console.WriteLine(Convert.ToString(buffer[3]));
            Console.WriteLine(BitConverter.ToString(buffer.Skip(4).Take(4).ToArray()));
            byte[] port = { buffer[9], buffer[10] };
            Console.WriteLine(BitConverter.ToUInt16(port, 0));
            Console.WriteLine(BitConverter.ToUInt16(buffer[..sum][^2..]));
            Console.WriteLine(buffer.Length + " " + BitConverter.ToString(buffer));

            //         handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //             new AsyncCallback(ClientReceive), state);
        }
    }
}
