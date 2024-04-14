
public enum LogType
{
    Debug,
    Info,
    Warning,
    Error
}

internal class Log
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

    public Log(string path)
    {
        _logPath = path;
        Clear();
    }

    public void Add(LogType type, string logSource, string message)
    {
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
                Console.WriteLine(entry);
                File.AppendAllTextAsync(_logPath, $"{entry}\n");
            }
        }
    }

    public void Clear()
    {
        File.WriteAllTextAsync(_logPath, "");
        _log.Clear();
    }

    public int Count()
    {
        return _log.Count;
    }

}