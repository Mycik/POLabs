using System.Diagnostics;
using System.Text;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.Unicode;
        Console.Write("Введіть розмір масиву: ");
        var n = int.Parse(Console.ReadLine()!);

        Console.Write("Введіть кількість потоків: ");
        var threads = int.Parse(Console.ReadLine()!);

        var array = GenerateRandomArray(n);

        Console.WriteLine("\n--- Послідовна версія ---");
        var time1 = MeasureTime(() =>
        {
            var result = CountMultiplesOf17Sequential(array);
            Console.WriteLine($"Кількість: {result.count}, Мінімум: {result.min}");
        });
        Console.WriteLine($"Час: {time1.TotalMilliseconds} мс");

        Console.WriteLine("\n--- Версія з lock ---");
        var time2 = MeasureTime(() =>
        {
            var result = CountMultiplesOf17WithLock(array, threads);
            Console.WriteLine($"Кількість: {result.count}, Мінімум: {result.min}");
        });
        Console.WriteLine($"Час: {time2.TotalMilliseconds} мс");

        Console.WriteLine("\n--- Версія з CAS (Interlocked) ---");
        var time3 = MeasureTime(() =>
        {
            var result = CountMultiplesOf17WithCas(array, threads);
            Console.WriteLine($"Кількість: {result.count}, Мінімум: {result.min}");
        });
        Console.WriteLine($"Час: {time3.TotalMilliseconds} мс");
    }

    private static int[] GenerateRandomArray(int n)
    {
        var rand = new Random();
        var arr = new int[n];
        for (var i = 0; i < n; i++)
            arr[i] = rand.Next(0, 100_000);
        return arr;
    }

    private static TimeSpan MeasureTime(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed;
    }

    private static (int count, int min) CountMultiplesOf17Sequential(int[] array)
    {
        var count = 0;
        var min = int.MaxValue;

        foreach (var num in array)
        {
            if (num % 17 != 0) continue;
            count++;
            if (num < min)
                min = num;
        }

        return count > 0 ? (count, min) : (0, -1);
    }
    
    private static (int count, int min) CountMultiplesOf17WithLock(int[] array, int threads)
    {
        var count = 0;
        var min = int.MaxValue;
        var locker = new object();
        var n = array.Length;

        Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
        {
            var chunkSize = n / threads;
            var start = t * chunkSize;
            var end = (t == threads - 1) ? n : start + chunkSize;

            for (var i = start; i < end; i++)
            {
                if (array[i] % 17 != 0) continue;
                lock (locker)
                {
                    count++;
                    if (array[i] < min)
                        min = array[i];
                }
            }
        });

        return count > 0 ? (count, min) : (0, -1);
    }
    
    private static (int count, int min) CountMultiplesOf17WithCas(int[] array, int threads)
    {
        var n = array.Length;
        var atomicCount = 0;
        var atomicMin = int.MaxValue;

        Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, t =>
        {
            var chunkSize = n / threads;
            var start = t * chunkSize;
            var end = (t == threads - 1) ? n : start + chunkSize;

            for (var i = start; i < end; i++)
            {
                var val = array[i];
                if (val % 17 != 0) continue;
                // Atomic increment
                Interlocked.Increment(ref atomicCount);

                // Atomic min
                int currentMin;
                do
                {
                    currentMin = atomicMin;
                    if (val >= currentMin) break;
                }
                while (Interlocked.CompareExchange(ref atomicMin, val, currentMin) != currentMin);
            }
        });

        return atomicCount > 0 ? (atomicCount, atomicMin) : (0, -1);
    }

}