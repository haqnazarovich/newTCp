using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

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
		static string path;
		static StringBuilder stringbuilder = new StringBuilder();
		static Thread SendThread;
		static Thread GetThread;

		static void Main(string[] args)
		{
			Console.WriteLine("Введите ip-адрес сервера");
			ServerIp = IPAddress.Parse(Console.ReadLine());
			Console.WriteLine("Введите порт сервера");
			Port = int.Parse(Console.ReadLine());
			Console.Clear();

			path = Directory.GetCurrentDirectory() + @"\EventLog.txt";
			File.WriteAllText(path, "Event log\n");

			Client = new TcpClient(ServerIp.ToString(), Port);
			Stream = Client.GetStream();

			try
			{
				SendThread = new Thread(new ThreadStart(SendMessage));
				SendThread.Start();
				GetThread = new Thread(new ThreadStart(GetMessageFromServer));
				GetThread.Start();
			}
			catch (Exception ex)
			{
				Exit();
			}
		}

		public static void SendMessage()
		{
			while (true)
			{
				string message = Requests[random.Next(3)];
				if (message == "exit")
					Exit();
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
				if (message.Contains("8"))
					File.AppendAllText(path, DateTime.Now.ToShortTimeString() + " Сервер: " + message + "\n");
			}
			while (true);
		}

		public static void Exit()
		{
			SendThread.Abort();
			GetThread.Abort();
			Environment.Exit(0);
		}
	}
}
