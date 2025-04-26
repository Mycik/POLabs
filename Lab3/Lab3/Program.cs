using System.Text;

namespace Lab3;

internal static class Program
{
    private static readonly Random Rng = new();
    private static bool _generatorsRunning = true;
    private static readonly List<Thread> Generators = new();
    private static volatile bool _allowGeneration = true;

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

        BufferedThreadPool pool = new(threadCount);

        for (var i = 0; i < 3; i++)
        {
            var thread = new Thread(() =>
            {
                while (_generatorsRunning)
                {
                    if (_allowGeneration)
                    {
                        var time = Rng.Next(taskTimeMin, taskTimeMax);
                        var task = new ThreadPoolTask
                        {
                            ExecutionTimeMs = time,
                            Action = () => Console.WriteLine($"[Task] Виконується задача потоку #{Environment.CurrentManagedThreadId} ({time} мс)")
                        };

                        var accepted = pool.Enqueue(task);
                        if (!accepted)
                            Console.WriteLine($"[Rejected] Задача {task.GuidIndex} відхилена: перевищення ліміту 60 секунд.");
                    }

                    Thread.Sleep(Rng.Next(threadsSleepMin, threadsSleepMax));
                }

                Console.WriteLine($"[Generator] Потік #{Environment.CurrentManagedThreadId} завершено.");
            });

            Generators.Add(thread);
            thread.Start();
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Натисніть:");
            Console.WriteLine("1 - Поставити на паузу");
            Console.WriteLine("2 - Продовжити роботу");
            Console.WriteLine("3 - Повністю завершити");

            var key = Console.ReadLine();

            if (key == "1")
            {
                pool.Pause();
                _allowGeneration = false;
            }
            else if (key == "2")
            {
                pool.Resume();
                _allowGeneration = true;
            }
            else if (key == "3")
            {
                _generatorsRunning = false;

                foreach (var gen in Generators)
                    gen.Join();

                pool.Stop();
                Console.WriteLine("Програму завершено коректно.");
                break;
            }
        }
    }
}