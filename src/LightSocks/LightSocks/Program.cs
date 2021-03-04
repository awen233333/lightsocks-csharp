using System;

namespace LightSocks
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			LsLocal lsLocal = new LsLocal("","127.0.0.1", "127.0.0.1");
			lsLocal.Listen();
		}
	}
}
