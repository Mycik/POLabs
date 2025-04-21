namespace Lab3;

public class BufferedThreadPool
{
    private const int QueueCycleTime = 40000;
    private const int MaxCycleSummaryTaskTime = 60000;
    
    private readonly object _lock = new();
    private readonly List<Thread> _workers = new();
    private readonly Queue<ThreadPoolTask> _currentQueue = new();
    private readonly Queue<ThreadPoolTask> _nextQueue = new();

    private bool _running = true;
    private bool _executing;
    private bool _canExecute;

    public BufferedThreadPool(int workerCount)
    {
        for (var i = 0; i < workerCount; i++)
        {
            var thread = new Thread(Worker);
            _workers.Add(thread);
            thread.Start();
        }

        new Thread(ExecutionCycle).Start();
    }

    public void Stop()
    {
        Console.WriteLine("[Shutdown] Завершення пулу задач...");
    
        _running = false;

        lock (_lock)
            Monitor.PulseAll(_lock);

        foreach (var worker in _workers.Where(worker => worker.IsAlive))
            worker.Join();

        Console.WriteLine("[Shutdown] Усі воркери завершені.");
    }

    public bool Enqueue(ThreadPoolTask task)
    {
        lock (_lock)
        {
            var targetQueue = _executing ? _nextQueue : _currentQueue;
            var totalTime = targetQueue.Sum(t => t.ExecutionTimeMs) + task.ExecutionTimeMs;

            if (totalTime > MaxCycleSummaryTaskTime)
            {
                Console.WriteLine(
                    $"[Rejected] Задача {task.GuidIndex} не додана: сумарний час з нею {totalTime} мс перевищує 60000 мс");
                return false;
            }

            targetQueue.Enqueue(task);
            Console.WriteLine(_executing
                ? $"[Enqueued] Задача {task.GuidIndex} додана в наступну чергу на {task.ExecutionTimeMs} мс"
                : $"[Enqueued] Задача {task.GuidIndex} додана в поточну чергу на {task.ExecutionTimeMs} мс");

            Monitor.PulseAll(_lock);
            return true;
        }
    }

    private void ExecutionCycle()
    {
        while (_running)
        {
            Console.WriteLine("[State] Очікування 40 секунд для збору задач...");
            Thread.Sleep(QueueCycleTime);

            lock (_lock)
            {
                if (!_running) return;

                _executing = true;
                _canExecute = true;
                Console.WriteLine("[State] Починається виконання поточної черги.");
                Monitor.PulseAll(_lock);
            }

            bool done;
            do
            {
                Thread.Sleep(500);
                lock (_lock)
                {
                    if (!_running) return;
                    done = _currentQueue.Count == 0;
                }
            } while (!done);

            lock (_lock)
            {
                _canExecute = false;
                Console.WriteLine("[State] Поточна черга виконана. Переходимо до наступної.");
                while (_nextQueue.Count > 0)
                    _currentQueue.Enqueue(_nextQueue.Dequeue());
                _executing = false;
            }
        }
    }


    private void Worker()
    {
        while (_running)
        {
            ThreadPoolTask task = null;

            lock (_lock)
            {
                while (_running && (!_canExecute || _currentQueue.Count == 0))
                    Monitor.Wait(_lock);

                if (!_running) return;

                if (_canExecute && _currentQueue.Count > 0)
                    task = _currentQueue.Dequeue();
            }

            if (task != null)
            {
                Console.WriteLine($"[Executing] Задача {task.GuidIndex} виконується {task.ExecutionTimeMs} мс");
                task.Action?.Invoke();
                Thread.Sleep(task.ExecutionTimeMs);
                Console.WriteLine($"[Done] Задача {task.GuidIndex} завершена.");
            }
        }
    }
}