using System;
using System.Threading.Tasks;

namespace LightSocksTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            LsTest server = new LsTest("", "127.0.0.1");
            await server.Listen(() => { Console.WriteLine("listen to "); });
        }
    }
}
