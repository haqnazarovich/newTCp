using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class Programm
    {
        static int Port;
		static IPAddress ServerIp;
		static TcpClient Client;
		static NetworkStream Stream;
		static Random random = new Random();
		static string[] Requests = new[] { "зачет", "удовлетворительно", "3", "хорошо" };

		static void Main(string[] args)
		{
			Console.WriteLine("Введите ip-адрес сервера");
			ServerIp = IPAddress.Parse(Console.ReadLine());
			Console.WriteLine("Введите порт сервера");
			Port = int.Parse(Console.ReadLine());
			Console.Clear();

			Client = new TcpClient(ServerIp.ToString(), Port);
			Stream = Client.GetStream();

			Thread SendThread = new Thread(new ThreadStart(SendMessage));
			SendThread.Start();
			Thread GetThread = new Thread(new ThreadStart(GetMessageFromServer));
			GetThread.Start();
		}

		public static void SendMessage()
		{
			while (true)
			{
				string message = Requests[random.Next(3)];
				byte[] data = Encoding.Unicode.GetBytes(message);
				Stream.Write(data);
				Console.WriteLine("Клиент: " + message);
				Thread.Sleep(2300);
			}
		}

		public static void GetMessageFromServer()
		{
			byte[] data = new byte[256];
			do
			{
				int bytes = Stream.Read(data, 0, data.Length);
				string message = Encoding.Unicode.GetString(data, 0, bytes);
				Console.WriteLine("Сервер: " + message);
			}
			while (true);
		}
	}
}
