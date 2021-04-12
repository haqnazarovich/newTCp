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
		static string[] Answers = new[] { "зачет ", "удовлетворительно ", "три ", "хорошо " };
		static List<Tuple<Client, string>> ClientWithDublet = new List<Tuple<Client, string>>();
		static List<string> MessagesInProcces = new List<string>();


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
			public int ClientNumber;
			public NetworkStream Stream;
			TcpClient tcpClient;
			int ThreadNumber;
			Random random = new Random();
			bool serverIsBusy = false;

			public Client(TcpClient newtcpClient, int threadNumber, int clientNumber)
			{
				tcpClient = newtcpClient;
				ThreadNumber = threadNumber;
				ClientNumber = clientNumber;
			}

			public void Process()
			{
				Stream = tcpClient.GetStream();
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
							if (MessagesInProcces.Any(m => m == newMessage))
							{
								string attention = "Данный запрос уже находится в обработке!";
								SendMessage(attention, Stream);
								if (ClientWithDublet.All(c => c.Item2 != newMessage))
									ClientWithDublet.Add(new Tuple<Client, string>(this, newMessage));
							}
							else
							{
								if (serverIsBusy)
								{
									string attention = "Запрос уже обрабатывается!";
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
						for (int i = 0; i < Clients.Count; i++)
						{
							Clients[i].ClientNumber = i;
						}
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
					serverIsBusy = true;
					MessagesInProcces.Add(message);

					Thread.Sleep(5000);
					var answer = Answers[random.Next(3)];
					string fullAnswer = DateTime.Now.ToShortTimeString() + ": " + answer + message;
					Console.WriteLine("Номер клиeнта {0}, " + fullAnswer, ThreadNumber);
					SendMessage(fullAnswer, stream);

					var clients = ClientWithDublet.Where(c => c.Item2 == message);
					var clientNumbers = clients.Select(c => c.Item1);
					foreach (var clientWithDuplet in clientNumbers)
					{
						var client = Clients.FirstOrDefault(c => c.ClientNumber == clientWithDuplet.ClientNumber);
						if (client == null)
							continue;

						Console.WriteLine("Номер клиeнта {0}, " + fullAnswer, client.ThreadNumber);
					}

					clients.Distinct();
					foreach (var clientWithDuplet in clients.ToList())
					{
						SendMessage(fullAnswer, clientWithDuplet.Item1.Stream);
						ClientWithDublet.Remove(clientWithDuplet);
					}

					MessagesInProcces.Remove(message);
					serverIsBusy = false;
				});
			}
		}
	}
}