namespace RWServer
{
    public static class Server
    {
        private static readonly object _sync = new();

        private static int _activeReaders = 0;

        private static int _waitingWriters = 0;
        private static bool _isWriterActive = false;

        private static int _count;

        /// <summary>
        /// Получает текущее значение счетчика
        /// Поддерживает параллельное чтение множеством потоков
        /// Блокируется, если активен писатель или есть ожидающие писатели
        /// </summary>
        public static int GetCount()
        {
            EnterReadLock();
            try
            {
                return _count;
            }
            finally
            {
                ExitReadLock();
            }
        }

        /// <summary>
        /// Добавляет значение к счетчику
        /// Только один писатель в любой момент времени
        /// Блокирует всех читателей на время выполнения операции
        /// </summary>
        public static void AddToCount(int value)
        {
            EnterWriteLock();
            try
            {
                _count += value;
            }
            finally
            {
                ExitWriteLock();
            }
        }

        /// <summary>
        /// Захватывает блокировку для чтения
        /// Множество потоков могут захватить одновременно
        /// Блокируется при наличии активного или ожидающего писателя
        /// </summary>
        private static void EnterReadLock()
        {
            lock (_sync)
            {
                while (_isWriterActive || _waitingWriters > 0)
                {
                    Monitor.Wait(_sync);
                }

                _activeReaders++;
            }
        }

        /// <summary>
        /// Освобождает блокировку чтения
        /// При завершении последнего читателя пробуждает ожидающих писателей
        /// </summary>
        private static void ExitReadLock()
        {
            lock (_sync)
            {
                if (_activeReaders <= 0)
                    throw new SynchronizationLockException("No active readers to exit");

                _activeReaders--;

                if (_activeReaders == 0)
                {
                    Monitor.PulseAll(_sync);
                }
            }
        }

        /// <summary>
        /// Захватывает блокировку для записи
        /// Гарантирует, что только один писатель активен
        /// Блокирует всех читателей пока писатель активен
        /// </summary>
        private static void EnterWriteLock()
        {
            lock (_sync)
            {
                _waitingWriters++;
                try
                {
                    while (_activeReaders > 0 || _isWriterActive)
                    {
                        Monitor.Wait(_sync);
                    }
                }
                finally
                {
                    _waitingWriters--;
                }

                _isWriterActive = true;
            }
        }

        /// <summary>
        /// Освобождает блокировку записи
        /// Пробуждает всех ожидающих потоков
        /// </summary>
        private static void ExitWriteLock()
        {
            lock (_sync)
            {
                if (!_isWriterActive)
                    throw new SynchronizationLockException("No active writer to exit");

                _isWriterActive = false;

                Monitor.PulseAll(_sync);
            }
        }
    }
}
