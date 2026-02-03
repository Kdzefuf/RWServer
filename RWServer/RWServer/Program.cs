using RWServer;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        const int READERS_COUNT = 200;
        const int READERS_OPERATION_COUNT = 100000;
        const int WRITERS_COUNT = 20;
        const int WRITERS_OPERATION_COUNT = 5000;

        Console.WriteLine($"{READERS_COUNT} читателей * {READERS_OPERATION_COUNT} операций + {WRITERS_COUNT} писателей * {WRITERS_OPERATION_COUNT} операций\n");

        var stopwatch = Stopwatch.StartNew();

        var readers = Enumerable.Range(0, READERS_COUNT).Select(reader => Task.Run(() =>
        {
            for (int operation = 0; operation < READERS_OPERATION_COUNT; operation++)
            {
                var _ = Server.GetCount();
                if (operation % 1000 == 0) Thread.Yield();
            }
            Console.WriteLine($"Читатель {reader} завершил работу");
        }));

        var writers = Enumerable.Range(0, WRITERS_COUNT).Select(writer => Task.Run(() =>
        {
            for (int operation = 0; operation < WRITERS_OPERATION_COUNT; operation++)
            {
                Server.AddToCount(1);
                if (operation % 500 == 0) Thread.Yield();
            }
            Console.WriteLine($"Писатель {writer} завершил работу");
        }));

        var allTasks = readers.Concat(writers).ToArray();
        await Task.WhenAll(allTasks)
            .WaitAsync(TimeSpan.FromSeconds(30));

        stopwatch.Stop();

        int finalCount = Server.GetCount();

        Console.WriteLine("\n");
        Console.WriteLine($"Тест завершён за {stopwatch.ElapsedMilliseconds} мс");
        Console.WriteLine($"Финальное значение счётчика: {finalCount}");
        Console.WriteLine($"Средняя производительность: {(10000.0 / stopwatch.Elapsed.TotalSeconds):F0} записей/сек");
    }
}