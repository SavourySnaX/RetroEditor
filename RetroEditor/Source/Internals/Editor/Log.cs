
using System.Collections.Concurrent;
using RetroEditor.Plugins;

internal interface ILogger
{
    void Add(LogType type, string logSource, string message);
}

internal class Log : ILogger
{
    private class LogEntry
    {
        public LogType type;
        public string logSource;
        public string message;
        public string time;

        public LogEntry(LogType type, string logSource, string message)
        {
            this.type = type;
            this.logSource = logSource;
            this.message = message;
            this.time = DateTime.Now.ToString("HH:mm:ss");
        }

        public override string ToString()
        {
            return $"{time} [{type}] <{logSource}> - {message}";
        }
    }

    private List<LogEntry> _log = new List<LogEntry>();
    private Dictionary<string, List<LogEntry>> _logPerSource = new Dictionary<string, List<LogEntry>>();
    private string _logPath;

    private ConcurrentQueue<string> logWriteQueue = new ConcurrentQueue<string>();
    private Task taskWriter;
    public Log(string path)
    {
        _logPath = path;
        taskWriter = Task.CompletedTask;
        if (File.Exists(_logPath))
        {
            File.Delete(_logPath);
        }
        Clear();
    }

    // Can be called from multiple threads
    public void Add(LogType type, string logSource, string message)
    {
#if !DEBUG
        if (type == LogType.Debug)
        {
            return;
        }
#endif
        var split = message.Split('\n');
        lock (_log)
        {
            foreach (var line in split)
            {
                var logEntry = new LogEntry(type, logSource, line.Trim());
                string entry = logEntry.ToString();
                _log.Add(logEntry);
                _logPerSource.TryAdd(logSource, new List<LogEntry>());
                _logPerSource[logSource].Add(logEntry);

                logWriteQueue.Enqueue(entry);
            }
            KickWriter();
        }
    }

    void KickWriter()
    {
        if (taskWriter.IsCompleted)
        {
            taskWriter = Task.Run(async () =>
            {
                while (logWriteQueue.TryDequeue(out var logLine))
                {
                    await File.AppendAllTextAsync(_logPath, logLine + Environment.NewLine);
                }
            });
        }
    }

    public void Clear()
    {
        logWriteQueue.Enqueue("_LOG CLEARED_");
        lock(_log)
        {
            _log.Clear();
            KickWriter();
        }
    }

    public int Count()
    {
        lock(_log)
        {
            return _log.Count;
        }
    }

    internal ReadOnlySpan<char> Entry(int index)
    {
        lock(_log)
        {
            return _log[index].ToString().AsSpan();
        }
    }

    internal IEnumerable<string> Sources()
    {
        return _logPerSource.Keys;
    }

    internal int Count(string source)
    {
        return _logPerSource.ContainsKey(source) ? _logPerSource[source].Count : 0;
    }

    internal ReadOnlySpan<char> Entry(string source, int index)
    {
        return _logPerSource[source][index].ToString().AsSpan();
    }
}