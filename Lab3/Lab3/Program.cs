using System.Text;

namespace Lab3;

internal static class Program
{
    private static readonly Random Rng = new();

    private static void Main()
    {
        Console.OutputEncoding = Encoding.Unicode;
        
        const int threadCount = 4;
        const int taskTimeMin = 6000;
        const int taskTimeMax = 14000;
        
        Console.Write("Введіть мінімальний час сліпу потоків: ");
        var threadsSleepMin = int.Parse(Console.ReadLine()!);
        
        Console.Write("Введіть максимальний час сліпу потоків: ");
        var threadsSleepMax = int.Parse(Console.ReadLine()!);
        
        var pool = new BufferedThreadPool(threadCount);

        for (var i = 0; i < 3; i++)
        {
            new Thread(() =>
            {
                while (true)
                {
                    var time = Rng.Next(taskTimeMin, taskTimeMax);
                    var task = new ThreadPoolTask
                    {
                        ExecutionTimeMs = time,
                        Action = () =>
                            Console.WriteLine(
                                $"[Task] Виконується задача потоку #{Environment.CurrentManagedThreadId} ({time} мс)")
                    };

                    var accepted = pool.Enqueue(task);
                    if (!accepted)
                        Console.WriteLine(
                            $"[Rejected] Задача {task.GuidIndex} відхилена: перевищення ліміту 60 секунд.");
                    Thread.Sleep(Rng.Next(threadsSleepMin, threadsSleepMax));
                }
            }).Start();
        }

        Console.ReadLine();
        pool.Stop();
    }
}