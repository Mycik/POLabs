using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lab5
{
    internal static class Program
    {
        private const int Port = 9000;
        private static readonly string RootDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\site"));

        private static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;
            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Console.WriteLine($"[INFO] Сервер запущено на http://localhost:{Port}");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                var clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private static void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8, false);
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            try
            {
                var requestLine = reader.ReadLine();
                if (string.IsNullOrEmpty(requestLine))
                    return;

                var tokens = requestLine.Split(' ');
                if (tokens.Length < 2 || tokens[0] != "GET")
                {
                    SendResponse(writer, "405 Method Not Allowed", "<h1>405 Method Not Allowed</h1>");
                    return;
                }

                var url = tokens[1];
                if (url == "/") url = "/home.html";

                var filePath = Path.Combine(RootDirectory, url.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    var content = File.ReadAllText(filePath);
                    SendResponse(writer, "200 OK", content);
                }
                else
                {
                    SendResponse(writer, "404 Not Found", "<h1>404 - Page Not Found</h1>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private static void SendResponse(TextWriter writer, string status, string body)
        {
            writer.WriteLine($"HTTP/1.1 {status}");
            writer.WriteLine("Content-Type: text/html; charset=UTF-8");
            writer.WriteLine($"Content-Length: {Encoding.UTF8.GetByteCount(body)}");
            writer.WriteLine("Connection: close");
            writer.WriteLine();
            writer.Write(body);
        }
    }
}