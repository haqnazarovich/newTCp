using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Server
{
	class Program
	{
		static int port;
		static IPAddress serverIp;
		static TcpListener tcpListener;
		static string path;
		static List<Client> Clients = new List<Client>();
		static int ThreadNumber;

		static void Main(string[] args)
		{
			Console.WriteLine("Введите ip-адрес сервера");
			serverIp = IPAddress.Parse(Console.ReadLine());
			Console.WriteLine("Введите номер порта для сервера");
			port = int.Parse(Console.ReadLine());
			Console.Clear();

			path = Directory.GetCurrentDirectory() + @"\EventLog.txt";
			File.WriteAllText(path, "Event log\n");

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
					Clients.Add(new Client(tcpClient, ThreadNumber++, Clients.Count));
					Thread clientThread = new Thread(new ThreadStart(Clients.Last().Process));
					clientThread.Start();
				}
			}
		}


		class Client
		{
			TcpClient tcpClient;
			int ThreadNumber;
			Random random = new Random();
			bool obrabotka = false;
			int ClientNumber;

			public Client(TcpClient newtcpClient, int threadNumber, int clientNumber)
			{
				tcpClient = newtcpClient;
				ThreadNumber = threadNumber;
				ClientNumber = clientNumber;
			}

			public void Process()
			{
				NetworkStream Stream = tcpClient.GetStream();
				Console.WriteLine("Клиент с номером потока {0} - подключился", ThreadNumber);
				File.AppendAllText(path, "Клиент с номером потока " + ThreadNumber + " - подключился\n");
				string oldMessage = null;
				while (true)
				{
					try
					{
						if (ClientNumber <= 2)
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
					catch (Exception ex)
					{
						Console.WriteLine("Клиент с номером потока {0} - отключился", ThreadNumber);
						File.AppendAllText(path, "Клиент с номером потока " + ThreadNumber + " - отключился\n");
						Clients.Remove(this);
						return;
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
				try
				{
					stream.Write(data, 0, data.Length);
				}
				catch (Exception ex)
				{
					return;
				}
			}

			async void ProcessingMessage(string message, NetworkStream stream)
			{
				await Task.Run(() =>
				{
					obrabotka = true;
					Thread.Sleep(5000);
					Console.WriteLine("Номер клиeнта {0}, " + DateTime.Now.ToShortTimeString() + ": " + message, ThreadNumber);
					File.AppendAllText(path, "Номер клиeнта " + ThreadNumber + " " + DateTime.Now.ToShortTimeString() + ": " + message + "\n");
					string messageWithValue = message + ": " + random.Next(80, 85) / 10.0;
					SendMessage(messageWithValue, stream);
					obrabotka = false;
				});
			}
		}
	}
}