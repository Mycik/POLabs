using System.Diagnostics;
using System.Text;

class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.Unicode;
        
        Console.Write("Введіть 1 - якщо хочете ввести числа вручну, 0 - якщо для лабораторної: ");
        var inputCase = int.Parse(Console.ReadLine()!);

        switch (inputCase)
        {
            case 0: LabsInput(); break;
            case 1: UserInput(); break;
        }
    }

    private static void LabsInput()
    {
        var nValues = new[] { 100, 500, 1000, 2500, 5000, 10000 };
        var sValues = new[] { 4, 8, 16, 32, 64, 128, 256 };

        Console.WriteLine("\n--- Без паралелізації ---");
        Console.WriteLine("Розмірність | Час (мс)");
        Console.WriteLine("------------------------");

        foreach (var nValue in nValues)
        {
            var matrix = GenerateRandomMatrix(nValue);
            var timeSequential = MeasureTime(() => ReflectOverAntiDiagonal(matrix));
            Console.WriteLine($"{nValue,10} | {timeSequential.TotalMilliseconds,8:F3}");
        }

        Console.WriteLine("\n--- З паралелізацією ---");
        Console.WriteLine("Розмірність | Потоки | Час (мс)");
        Console.WriteLine("------------------------------");

        foreach (var sValue in sValues)
        {
            foreach (var nValue in nValues)
            {
                var matrix = GenerateRandomMatrix(nValue);
                var timeParallel = MeasureTime(() => ReflectOverAntiDiagonalParallel(matrix, sValue));
                Console.WriteLine($"{nValue,10} | {sValue,6} | {timeParallel.TotalMilliseconds,8:F3}");
            }
        }
    }

    private static void UserInput()
    {
        Console.Write("Введіть розмірність матриці n: ");
        var n = int.Parse(Console.ReadLine()!);

        var matrix = GenerateRandomMatrix(n);
        Console.WriteLine("\n--- Початкова матриця ---");
        PrintMatrix(matrix);

        Console.WriteLine("\n--- Звичайне відображення ---");
        var matrixCopy = (int[,])matrix.Clone();
        var copy = matrixCopy;
        var timeSequential = MeasureTime(() => ReflectOverAntiDiagonal(copy));
        Console.WriteLine($"Час (звичайна версія): {timeSequential.TotalMilliseconds} мс");

        Console.Write("\nВведіть кількість потоків: ");
        var threadCount = int.Parse(Console.ReadLine()!);

        Console.WriteLine("\n--- Паралельне відображення ---");
        matrixCopy = (int[,])matrix.Clone();
        var timeParallel = MeasureTime(() => ReflectOverAntiDiagonalParallel(matrixCopy, threadCount));
        Console.WriteLine($"Час (паралельна версія з {threadCount} потоками): {timeParallel.TotalMilliseconds} мс");

        Console.WriteLine("\n--- Автоматичний підбір найкращої кількості потоків ---");
        var bestThreads = FindBestThreadCount(matrix);
        Console.WriteLine($"Найкраща кількість потоків: {bestThreads}");
    }

    private static int[,] GenerateRandomMatrix(int n)
    {
        var rand = new Random();
        var matrix = new int[n, n];

        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                matrix[i, j] = rand.Next(10, 100);

        return matrix;
    }

    private static void ReflectOverAntiDiagonal(int[,] matrix)
    {
        var n = matrix.GetLength(0);
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n - i - 1; j++)
            {
                (matrix[i, j], matrix[n - 1 - j, n - 1 - i]) = (matrix[n - 1 - j, n - 1 - i], matrix[i, j]);
            }
    }

    private static void ReflectOverAntiDiagonalParallel(int[,] matrix, int threadCount)
    {
        var n = matrix.GetLength(0);
        Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, i =>
        {
            for (var j = 0; j < n - i - 1; j++)
            {
                var y = j;
                var xi = n - 1 - y;
                var yj = n - 1 - i;

                if (i < xi || (i == xi && y < yj))
                {
                    (matrix[i, y], matrix[xi, yj]) = (matrix[xi, yj], matrix[i, y]);
                }
            }
        });
    }

    private static TimeSpan MeasureTime(Action action)
    {
        var sw = Stopwatch.StartNew();
        action?.Invoke();
        sw.Stop();
        return sw.Elapsed;
    }

    private static int FindBestThreadCount(int[,] matrix)
    {
        var maxThreads = Environment.ProcessorCount * 4;
        var bestThreadCount = 1;
        var bestTime = TimeSpan.MaxValue;

        for (var t = 1; t <= maxThreads; t++)
        {
            var copy = (int[,])matrix.Clone();
            var t1 = t;
            var current = MeasureTime(() => ReflectOverAntiDiagonalParallel(copy, t1));

            Console.WriteLine($"Потоки: {t}, час: {current.TotalMilliseconds} мс");

            if (current >= bestTime) continue;
            bestTime = current;
            bestThreadCount = t;
        }

        return bestThreadCount;
    }

    private static void PrintMatrix(int[,] matrix)
    {
        var n = matrix.GetLength(0);
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
                Console.Write($"{matrix[i, j],4}");
            Console.WriteLine();
        }
    }
}