using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Lab4Client;

internal static class Client
{
    private const int Port = 9000;
    private const string AccessKey = "KOD_MYCIKA";
    private const int MaxMatrixLenght = 50;
    private static string _serverIp = "127.0.0.1";

    private static TcpClient? _client;
    private static StreamReader? _reader;
    private static StreamWriter? _writer;

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        while (true)
        {
            Connect();

            var sessionActive = true;
            while (sessionActive)
            {
                Console.WriteLine();
                Console.WriteLine("Оберіть дію:");
                Console.WriteLine("1. Надіслати нову матрицю");
                Console.WriteLine("2. Завершити поточне з'єднання (EXIT)");
                Console.WriteLine("3. Вийти повністю з програми");

                Console.Write("Ваш вибір: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        if (_client?.Connected ?? false)
                            ProcessMatrix();
                        else
                            Log("Немає з'єднання з сервером. Потрібно перепідключитись.");
                        break;

                    case "2":
                        Disconnect(sendExit: true);
                        sessionActive = false;
                        break;

                    case "3":
                        Disconnect(sendExit: true);
                        Log("Клієнт завершив роботу повністю.");
                        Environment.Exit(0);
                        break;

                    default:
                        Console.WriteLine("Невірний вибір. Спробуйте ще раз.");
                        break;
                }
            }
        }
    }

    private static void Connect()
    {
        while (true)
        {
            try
            {
                Console.Write("Введіть IP сервера (Enter для 127.0.0.1): ");
                var inputIp = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(inputIp))
                    _serverIp = inputIp.Trim();

                _client = new TcpClient();
                var connectWatch = Stopwatch.StartNew();

                Log($"Підключення до сервера {_serverIp}...");
                _client.Connect(_serverIp, Port);
                connectWatch.Stop();
                Log($"Підключення встановлено за {connectWatch.ElapsedMilliseconds} мс");

                var stream = _client.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                _writer.WriteLine($"AUTH {AccessKey}");
                var authResponse = _reader.ReadLine();
                if (authResponse != "AUTH_OK")
                {
                    Log("Помилка авторизації! Сервер закрив з'єднання.");
                    _client.Close();
                    continue;
                }

                Log("Авторизація пройдена успішно.");
                return;
            }
            catch (Exception ex)
            {
                Log($"Не вдалося підключитись: {ex.Message}");
                Console.WriteLine("Спробувати ще раз? (y/n)");
                var retry = Console.ReadLine()?.Trim().ToLower();
                if (retry != "y")
                {
                    Environment.Exit(0);
                }
            }
        }
    }

    private static void Disconnect(bool sendExit = false)
    {
        try
        {
            if (!sendExit || _client?.Connected != true) return;
            _writer!.WriteLine("EXIT");
            Console.WriteLine(_reader!.ReadLine());
        }
        catch
        {
            // ігнорувати помилки при закритті
        }
        finally
        {
            _client?.Close();
            _client = null;
            _reader = null;
            _writer = null;
            Log("Відключення від сервера виконано.");
        }
    }

    private static void ProcessMatrix()
    {
        try
        {
            Console.Write("Введіть розмірність матриці n: ");
            var n = int.Parse(Console.ReadLine()!);
            Console.Write("Введіть кількість потоків: ");
            var threads = int.Parse(Console.ReadLine()!);

            var rand = new Random();
            var matrix = new int[n, n];
            for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                matrix[i, j] = rand.Next(10, 100);

            Console.WriteLine("Початкова матриця:");
            PrintMatrix(matrix);

            var sendDataWatch = Stopwatch.StartNew();
            Log("Передача даних на сервер...");
            _writer!.WriteLine("SEND_DATA");
            _writer.WriteLine(n);
            _writer.WriteLine(threads);
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                    _writer.Write(matrix[i, j] + " ");
                _writer.WriteLine();
            }
            Console.WriteLine(_reader.ReadLine());
            sendDataWatch.Stop();
            Log($"Дані передані за {sendDataWatch.ElapsedMilliseconds} мс");

            var startProcessWatch = Stopwatch.StartNew();
            Log("Надсилання команди старту обробки...");
            _writer.WriteLine("START");
            Console.WriteLine(_reader.ReadLine());
            startProcessWatch.Stop();
            Log($"Команда старту виконана за {startProcessWatch.ElapsedMilliseconds} мс");

            var getResultWatch = Stopwatch.StartNew();
            Log("Запит результату...");
            _writer.WriteLine("GET_RESULT");
            var size = int.Parse(_reader.ReadLine()!);
            var result = new int[size, size];
            for (var i = 0; i < size; i++)
            {
                var line = _reader.ReadLine()?.Split(' ');
                for (var j = 0; j < size; j++)
                    if (line != null)
                        result[i, j] = int.Parse(line[j]);
            }
            getResultWatch.Stop();
            Log($"Результат отримано за {getResultWatch.ElapsedMilliseconds} мс");

            Console.WriteLine("Оброблена матриця:");
            PrintMatrix(result);
        }
        catch (Exception ex)
        {
            Log($"Сталася помилка: {ex.Message}");
            Log("Можливо, з'єднання втрачено. Потрібно перепідключитись.");
            Disconnect();
        }
    }

    private static void PrintMatrix(int[,] matrix)
    {
        var n = matrix.GetLength(0);
        if (n > MaxMatrixLenght) return;
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
                Console.Write($"{matrix[i, j],4}");
            Console.WriteLine();
        }
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}