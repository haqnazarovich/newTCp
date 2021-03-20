using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static int port;
        static IPAddress serverIp;
        static TcpListener tcpListener;
        static Random random = new Random();
        //static List<TcpClient> tcpClients = new List<TcpClient>();
		static int clientNumber;
		static bool obrabotka = false;

		static void Main(string[] args)
        {
            Console.WriteLine("Введите ip-адрес сервера");
            serverIp = IPAddress.Parse(Console.ReadLine());
            Console.WriteLine("Введите номер порта для сервера");
            port = int.Parse(Console.ReadLine());
			Console.Clear();
			Thread listenThread = new Thread(new ThreadStart(Listen));
			listenThread.Start();
		}

		static void Listen()
		{
			tcpListener = new TcpListener(serverIp, port);
			tcpListener.Start();
			Console.WriteLine("Ваш сервер создан, ожидается подключение клиентов");
			while (true)
			{
				TcpClient tcpClient = tcpListener.AcceptTcpClient();
				if (tcpClient != null)
				{
					//tcpClients.Add(tcpClient);
					clientNumber = clientNumber + 1;
					Client client = new Client(tcpClient);
					Thread clientThread = new Thread(new ThreadStart(client.Process));
					clientThread.Start();
				}
			}
		}


		class Client
		{
			TcpClient tcpClient;

			public Client(TcpClient newtcpClient)
			{
				tcpClient = newtcpClient;
			}

			public void Process()
			{
				NetworkStream Stream = tcpClient.GetStream();
				Console.WriteLine("Клиент с номером потока {0} - подключился", clientNumber - 1);
				string oldMessage = null;
				while (true)
				{
					try
					{
						if ((clientNumber - 1) <= 2)
						{
							string newMessage = GetMessage(Stream);

							if (oldMessage != newMessage && obrabotka != false)
							{
								string attention = "Запрос уже обрабатывается!";
								SendMessage(attention, Stream);
							}
							else
							{
								if (oldMessage == newMessage && obrabotka != false)
								{
									string attention = "Сервер не закончил обработку предыдущего запроса!";
									SendMessage(attention, Stream);
								}
								else
								{
									oldMessage = newMessage;
									ProcessingMessage(oldMessage, Stream);
									oldMessage = null;
								}
							}
						}
						else
						{
							GetMessage(Stream);
							SendMessage("Превышен лимит потоков, попробуйте позже", Stream);
						}
					}
					catch
					{
						Console.WriteLine("Клиент с номером потока {0} - отключился", clientNumber - 1);
						clientNumber = clientNumber - 1;
						break;
					}
				}
			}
			string GetMessage(NetworkStream stream)
			{
				string message = null;
				do
				{
					byte[] data = new byte[256];
					int bytes = stream.Read(data, 0, data.Length);
					message = Encoding.Unicode.GetString(data, 0, bytes);
				}
				while (stream.DataAvailable);

				return message;
			}
			void SendMessage(string message, NetworkStream stream)
			{
				byte[] data = Encoding.Unicode.GetBytes(message);
				stream.Write(data, 0, data.Length);
			}

			async void ProcessingMessage(string message, NetworkStream stream)
			{
				await Task.Run(() =>
				{
					obrabotka = true;
					Thread.Sleep(5000);
					Console.WriteLine("Номер клинта {0}, " + DateTime.Now.ToShortTimeString() + ": " + message, clientNumber - 1);
					string messageWithValue = message + ": " + random.Next(80, 85) / 10;
					SendMessage(messageWithValue, stream);
					obrabotka = false;
				});
			}
		}
	}

	//static void Process()
	//{
	//	Stream = tcpClients[clientNumber - 1].GetStream();
	//	Console.WriteLine("Клиент с номером потока {0} - подключился", clientNumber - 1);
	//	string oldMessage = null;
	//	while (true)
	//	{
	//		try
	//		{
	//			if ((clientNumber - 1) <= 2)
	//			{
	//				string newMessage = GetMessage();

	//				if (oldMessage != newMessage && obrabotka != false)
	//				{
	//					string attention = "Запрос уже обрабатывается!";
	//					SendMessage(attention);
	//				}
	//				else
	//				{
	//					if (oldMessage == newMessage && obrabotka != false)
	//					{
	//						string attention = "Сервер не закончил обработку предыдущего запроса!";
	//						SendMessage(attention);
	//					}
	//					else
	//					{
	//						oldMessage = newMessage;
	//						ProcessingMessage(oldMessage);
	//						oldMessage = null;
	//					}
	//				}
	//			}
	//			else
	//			{
	//				GetMessage();
	//				SendMessage("Превышен лимит потоков, попробуйте позже");
	//			}
	//		}
	//		catch
	//		{
	//			Console.WriteLine("Клиент с номером потока {0} - отключился", clientNumber - 1);
	//			clientNumber = clientNumber - 1;
	//			break;
	//		}
	//	}
	//}

	//static string GetMessage()
	//{
	//	string message = null;
	//	do
	//	{
	//		byte[] data = new byte[256];
	//		int bytes = Stream.Read(data, 0, data.Length);
	//		message = Encoding.Unicode.GetString(data, 0, bytes);
	//	}
	//	while (Stream.DataAvailable);

	//	return message;
	//}
	//static void SendMessage(string message)
	//{
	//	byte[] data = Encoding.Unicode.GetBytes(message);
	//	Stream.Write(data, 0, data.Length);
	//}

	//static async void ProcessingMessage(string message)
	//{
	//	await Task.Run(() =>
	//	{
	//		obrabotka = true;
	//		Thread.Sleep(5000);
	//		Console.WriteLine("Номер клинта {0}, " + DateTime.Now.ToShortTimeString() + ": " + message, clientNumber - 1);
	//		string messageWithValue = message + ": " + random.Next(80, 85) / 10;
	//		SendMessage(messageWithValue);
	//		obrabotka = false;
	//	});
	//}
}