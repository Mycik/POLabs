using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lab4Server;

internal static class Server
{
    private const int Port = 9000;
    private const string AccessKey = "KOD_MYCIKA";
    private static readonly ConcurrentDictionary<TcpClient, (int[,], int)> ClientsData = new();

    private static void Main()
    {
        Console.OutputEncoding = Encoding.Unicode;
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Log("Сервер запущено...");

        while (true)
        {
            var client = listener.AcceptTcpClient();
            Log("Клієнт підключився");
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private static void HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        try
        {
            var authLine = reader.ReadLine();
            if (authLine == null || !authLine.StartsWith("AUTH "))
            {
                Log("Клієнт не надіслав ключ авторизації. Відключення.");
                client.Close();
                return;
            }

            var receivedKey = authLine[5..].Trim();
            if (receivedKey != AccessKey)
            {
                Log("Невірний ключ авторизації. Відключення.");
                writer.WriteLine("AUTH_FAIL");
                client.Close();
                return;
            }

            writer.WriteLine("AUTH_OK");
            Log("Клієнт успішно авторизувався.");

            while (true)
            {
                var command = reader.ReadLine();
                if (command == null) break;

                Log($"Отримано команду: {command}");

                switch (command)
                {
                    case "SEND_DATA":
                        var size = int.Parse(reader.ReadLine()!);
                        var threads = int.Parse(reader.ReadLine()!);
                        var matrix = new int[size, size];

                        for (var i = 0; i < size; i++)
                        {
                            var line = reader.ReadLine()?.Split(' ');
                            for (var j = 0; j < size; j++)
                                matrix[i, j] = int.Parse(line![j]);
                        }

                        ClientsData[client] = (matrix, threads);
                        writer.WriteLine("DATA_RECEIVED");
                        Log("Дані клієнта отримані");
                        break;

                    case "START":
                        if (ClientsData.TryGetValue(client, out var data))
                        {
                            Log("Початок обробки даних...");
                            ReflectMatrixParallel(data.Item1, data.Item2);
                            writer.WriteLine("CALCULATION_DONE");
                            Log("Обробка завершена");
                        }
                        else
                        {
                            writer.WriteLine("ERROR_NO_DATA");
                            Log("Помилка: відсутні дані");
                        }
                        break;

                    case "GET_RESULT":
                        if (ClientsData.TryGetValue(client, out var result))
                        {
                            Log("Передача результату клієнту...");
                            var n = result.Item1.GetLength(0);
                            writer.WriteLine(n);
                            for (var i = 0; i < n; i++)
                            {
                                for (var j = 0; j < n; j++)
                                    writer.Write(result.Item1[i, j] + " ");
                                writer.WriteLine();
                            }
                        }
                        else
                        {
                            writer.WriteLine("ERROR_NO_RESULT");
                            Log("Помилка: результат відсутній");
                        }
                        break;

                    case "EXIT":
                        writer.WriteLine("BYE");
                        Log("Клієнт завершив роботу");
                        client.Close();
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Помилка: {ex.Message}");
        }
        finally
        {
            client.Close();
            Log("Клієнт відключився");
        }
    }

    private static void ReflectMatrixParallel(int[,] matrix, int threadCount)
    {
        var n = matrix.GetLength(0);
        Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, i =>
        {
            for (var j = 0; j < n - i - 1; j++)
            {
                (matrix[i, j], matrix[n - 1 - j, n - 1 - i]) = (matrix[n - 1 - j, n - 1 - i], matrix[i, j]);
            }
        });
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}