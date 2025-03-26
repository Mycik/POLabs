using System.Diagnostics;

class Program
{
    static void Main()
    {
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
        return (0, -1);
    }
    
    private static (int count, int min) CountMultiplesOf17WithLock(int[] array, int threads)
    {
        return (0, -1);
    }
    
    private static (int count, int min) CountMultiplesOf17WithCas(int[] array, int threads)
    {
        return (0, -1);
    }

}