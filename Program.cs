using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class Program
    {
        private static TcpListener server;
        private static List<TcpClient> clients = new List<TcpClient>();
        private static Dictionary<TcpClient, string> clientNames = new Dictionary<TcpClient, string>();
        private static StreamWriter logWriter;

        static async Task Main(string[] args)
        {
            int port = 11000;
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"Сервер запущен на порту {port}...");

            string logFileName = DateTime.Now.ToString("dd.MM.yyyy_HH-mm-ss") + ".txt";
            logWriter = new StreamWriter(logFileName, true);
            logWriter.WriteLine($"Запуск сервера: {DateTime.Now}");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                clients.Add(client);
                _ = HandleClient(client);
            }
        }

        private static async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                string userName = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(userName))
                {
                    clientNames[client] = userName;
                    Console.WriteLine($"Пользователь {userName} подключился.");
                    await BroadcastMessage($"Пользователь {userName} подключился.");
                }

                while (true)
                {
                    string message = await reader.ReadLineAsync();
                    if (message == null) break;

                    Console.WriteLine($"{userName}: {message}");
                    await BroadcastMessage($"{userName}: {message}");
                    LogToFile($"{userName}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                clients.Remove(client);
                if (clientNames.ContainsKey(client))
                {
                    string userName = clientNames[client];
                    clientNames.Remove(client);
                    await BroadcastMessage($"Пользователь {userName} отключился.");
                    Console.WriteLine($"Пользователь {userName} отключился.");
                }
                client.Close();
            }
        }

        private static async Task BroadcastMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);

            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(data, 0, data.Length);
                }
                catch
                {
                }
            }
        }

        private static void LogToFile(string message)
        {
            logWriter.WriteLine($"{DateTime.Now}: {message}");
            logWriter.Flush();
        }
    }
}
