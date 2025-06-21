# 终极日志系统 API 参考

## 目录

1. [核心类](#核心类)
2. [日志级别](#日志级别)
3. [配置](#配置)
4. [日志记录](#日志记录)
5. [格式化器](#格式化器)
6. [输出目标](#输出目标)
7. [处理器和拦截器](#处理器和拦截器)
8. [上下文和扩展](#上下文和扩展)
9. [分析和导出](#分析和导出)
10. [Web查看器](#web查看器)

## 核心类

### LogEntry

日志条目，表示单条日志记录。

```csharp
public class LogEntry
{
    public DateTime Timestamp { get; }
    public object Level { get; }
    public string? Category { get; }
    public string Message { get; }
    public Exception? Exception { get; }
    public ConcurrentDictionary<string, object?> Properties { get; }
    
    public LogEntry(DateTime timestamp, object level, string? category, string message, Exception? exception = null);
}
```

### Logger

日志记录器，提供各种日志记录方法。

```csharp
public class Logger : IDisposable
{
    public Logger(LoggerConfiguration configuration);
    
    public void Log(object level, string message, string? category = null, Exception? exception = null);
    public void Trace(string message, string? category = null, Exception? exception = null);
    public void Debug(string message, string? category = null, Exception? exception = null);
    public void Info(string message, string? category = null, Exception? exception = null);
    public void Warning(string message, string? category = null, Exception? exception = null);
    public void Error(string message, string? category = null, Exception? exception = null);
    public void Fatal(string message, string? category = null, Exception? exception = null);
    public void Flush();
    public void Dispose();
}
```

### LoggerFactory

日志记录器工厂，用于创建和管理日志记录器。

```csharp
public static class LoggerFactory
{
    public static Logger CreateLogger(LoggerConfiguration configuration, string? name = null);
    public static Logger GetLogger(string? name = null);
    public static void CloseAll();
}
```

## 日志级别

### LogLevel

预定义的日志级别枚举。

```csharp
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
    None = 6
}
```

### CustomLogLevel

自定义日志级别，用于创建预定义级别之外的日志级别。

```csharp
public class CustomLogLevel
{
    public int Value { get; }
    public string Name { get; }
    
    public static CustomLogLevel Create(int value, string name);
}
```

## 配置

### LoggerConfiguration

日志配置类，用于配置日志系统。

```csharp
public class LoggerConfiguration
{
    public object MinimumLevel { get; set; }
    public List<ILogWriter> Writers { get; }
    public List<ILogHandler> Handlers { get; }
    public string? DefaultCategory { get; set; }
    
    public LoggerConfiguration SetMinimumLevel(object level);
    public LoggerConfiguration AddConsoleWriter(ILogFormatter? formatter = null);
    public LoggerConfiguration AddFileWriter(string filePath, ILogFormatter? formatter = null, long maxFileSize = 10 * 1024 * 1024, int maxRollingFiles = 5);
    public LoggerConfiguration AddWriter(ILogWriter writer);
    public LoggerConfiguration AddHandler(ILogHandler handler);
    public LoggerConfiguration AddEmailNotification(string smtpServer, int smtpPort, string fromEmail, string toEmail, string username, string password, object levelToHandle = LogLevel.Error);
    public LoggerConfiguration AddAction(Action<LogEntry> action, Func<LogEntry, bool>? predicate = null);
    public LoggerConfiguration SetDefaultCategory(string category);
}
```

## 日志记录

### ILogWriter

日志输出接口，定义日志写入方法。

```csharp
public interface ILogWriter : IDisposable
{
    void Write(LogEntry entry);
    void Flush();
}
```

### ColorConsoleWriter

彩色控制台输出，支持不同级别使用不同颜色。

```csharp
public class ColorConsoleWriter : ILogWriter
{
    public ColorConsoleWriter(ILogFormatter formatter);
    
    public void Write(LogEntry entry);
    public void Flush();
    public void Dispose();
}
```

### RollingFileWriter

滚动文件输出，支持按大小自动滚动更新日志文件。

```csharp
public class RollingFileWriter : ILogWriter
{
    public RollingFileWriter(string filePath, ILogFormatter? formatter = null, long maxFileSize = 10 * 1024 * 1024, int maxRollingFiles = 5, Encoding? encoding = null);
    
    public void Write(LogEntry entry);
    public void Flush();
    public void Dispose();
}
```

### DatabaseWriter

数据库输出，将日志写入数据库表。

```csharp
public class DatabaseWriter : ILogWriter
{
    public DatabaseWriter(DbConnection connection, string tableName = "Logs", bool ownsConnection = false, int batchSize = 100);
    
    public void Write(LogEntry entry);
    public void Flush();
    public void Dispose();
}
```

### HttpWriter

HTTP输出，将日志通过HTTP请求发送到指定端点。

```csharp
public class HttpWriter : ILogWriter
{
    public HttpWriter(string endpoint, HttpClient? httpClient = null, int batchSize = 10, JsonSerializerOptions? jsonOptions = null);
    
    public void Write(LogEntry entry);
    public void Flush();
    public void Dispose();
}
```

### CustomWriter

自定义输出，允许通过委托自定义日志输出行为。

```csharp
public class CustomWriter : ILogWriter
{
    public CustomWriter(Action<LogEntry> writeAction, Action? flushAction = null, Action? disposeAction = null);
    
    public void Write(LogEntry entry);
    public void Flush();
    public void Dispose();
}
```

## 格式化器

### ILogFormatter

日志格式化器接口，定义日志格式化方法。

```csharp
public interface ILogFormatter
{
    string Format(LogEntry entry);
}
```

### TextFormatter

文本格式化器，将日志格式化为文本。

```csharp
public class TextFormatter : ILogFormatter
{
    public TextFormatter(string template = "[{timestamp}] [{level}] {message}");
    
    public string Format(LogEntry entry);
}
```

### JsonFormatter

JSON格式化器，将日志格式化为JSON。

```csharp
public class JsonFormatter : ILogFormatter
{
    public JsonFormatter(JsonSerializerOptions? options = null);
    
    public string Format(LogEntry entry);
}
```

### XmlFormatter

XML格式化器，将日志格式化为XML。

```csharp
public class XmlFormatter : ILogFormatter
{
    public string Format(LogEntry entry);
}
```

## 处理器和拦截器

### ILogHandler

日志处理器接口，定义日志处理方法。

```csharp
public interface ILogHandler
{
    void Handle(LogEntry entry);
    bool ShouldHandle(LogEntry entry);
}
```

### ActionHandler

回调处理器，通过委托处理日志。

```csharp
public class ActionHandler : ILogHandler
{
    public ActionHandler(Action<LogEntry> action, Func<LogEntry, bool>? predicate = null);
    
    public void Handle(LogEntry entry);
    public bool ShouldHandle(LogEntry entry);
}
```

### EmailNotificationHandler

邮件通知处理器，通过邮件发送日志通知。

```csharp
public class EmailNotificationHandler : ILogHandler
{
    public EmailNotificationHandler(string smtpServer, int smtpPort, string fromEmail, string toEmail, string username, string password, object levelToHandle = LogLevel.Error);
    
    public void Handle(LogEntry entry);
    public bool ShouldHandle(LogEntry entry);
}
```

### LogInterceptor

日志拦截器，用于拦截和修改日志条目。

```csharp
public class LogInterceptor
{
    public LogInterceptor(Func<LogEntry, bool> predicate, Action<LogEntry> action);
    
    public bool Process(LogEntry entry);
}
```

## 解析器

### ILogParser

日志解析器接口，定义日志解析方法。

```csharp
public interface ILogParser
{
    IEnumerable<LogEntry> Parse(string logContent);
    IEnumerable<LogEntry> ParseFile(string filePath);
}
```

### TextLogParser

文本日志解析器，解析文本格式的日志。

```csharp
public class TextLogParser : ILogParser
{
    public TextLogParser(string pattern = @"\[(.*?)\] \[(.*?)\] (.*)");
    
    public IEnumerable<LogEntry> Parse(string logContent);
    public IEnumerable<LogEntry> ParseFile(string filePath);
}
```

### JsonLogParser

JSON日志解析器，解析JSON格式的日志。

```csharp
public class JsonLogParser : ILogParser
{
    public JsonLogParser(JsonSerializerOptions? options = null);
    
    public IEnumerable<LogEntry> Parse(string logContent);
    public IEnumerable<LogEntry> ParseFile(string filePath);
}
```

## 上下文和扩展

### LogContext

日志上下文，用于存储和管理日志上下文属性。

```csharp
public static class LogContext
{
    public static Dictionary<string, object?> Properties { get; }
    
    public static void SetProperty(string key, object? value);
    public static object? GetProperty(string key);
    public static void ClearProperties();
    public static void EnrichLogEntry(LogEntry entry);
}
```

### LoggerExtensions

日志记录器扩展方法，提供额外的日志记录功能。

```csharp
public static class LoggerExtensions
{
    public static void WithContext(this Logger logger, Action action, Dictionary<string, object?> contextProperties);
    public static T WithContext<T>(this Logger logger, Func<T> func, Dictionary<string, object?> contextProperties);
    public static void LogWithProperties(this Logger logger, object level, string message, Dictionary<string, object?> properties, string? category = null, Exception? exception = null);
    public static void LogStructured(this Logger logger, object level, string messageTemplate, object values, string? category = null, Exception? exception = null);
    public static T Time<T>(this Logger logger, Func<T> action, string operationName, string? category = null);
    public static void Time(this Logger logger, Action action, string operationName, string? category = null);
    public static void SafeExecute(this Logger logger, Action action, string operationName, string? category = null);
    public static T? SafeExecute<T>(this Logger logger, Func<T> func, string operationName, string? category = null, T? defaultValue = default);
}
```

## 分析和导出

### LogAnalyzer

日志分析工具，提供日志分析功能。

```csharp
public static class LogAnalyzer
{
    public static Dictionary<string, int> CountByLevel(IEnumerable<LogEntry> entries);
    public static Dictionary<string, int> CountByCategory(IEnumerable<LogEntry> entries);
    public static Dictionary<DateTime, int> CountByTimeInterval(IEnumerable<LogEntry> entries, TimeSpan interval);
    public static IEnumerable<LogEntry> FindByText(IEnumerable<LogEntry> entries, string text, bool ignoreCase = true);
    public static IEnumerable<LogEntry> FindByTimeRange(IEnumerable<LogEntry> entries, DateTime start, DateTime end);
    public static IEnumerable<LogEntry> FindByLevel(IEnumerable<LogEntry> entries, object level);
    public static IEnumerable<LogEntry> FindByExceptionType(IEnumerable<LogEntry> entries, Type exceptionType);
    public static IEnumerable<KeyValuePair<string, int>> GetMostFrequentMessages(IEnumerable<LogEntry> entries, int topCount = 10);
    public static Dictionary<int, int> GetHourDistribution(IEnumerable<LogEntry> entries);
}
```

### LogExporter

日志导出工具，提供日志导出功能。

```csharp
public static class LogExporter
{
    public static void ExportToFile(IEnumerable<LogEntry> entries, string filePath, ILogFormatter formatter);
    public static void ExportToTextFile(IEnumerable<LogEntry> entries, string filePath, string template = "[{timestamp}] [{level}] {message}");
    public static void ExportToJsonFile(IEnumerable<LogEntry> entries, string filePath);
    public static void ExportToXmlFile(IEnumerable<LogEntry> entries, string filePath);
    public static IEnumerable<LogEntry> ImportFromFile(string filePath, ILogParser parser);
    public static IEnumerable<LogEntry> ImportFromTextFile(string filePath, string pattern = @"\[(.*?)\] \[(.*?)\] (.*)");
    public static IEnumerable<LogEntry> ImportFromJsonFile(string filePath);
}
```

## Web查看器

### LogWebViewer

Web日志查看器，提供Web界面查看日志。

```csharp
public class LogWebViewer
{
    public LogWebViewer(string logDirectory, int port = 5000, string? username = null, string? password = null);
    
    public void Start();
    public Task StopAsync();
}
```

### LoggerConfigurationExtensions

日志配置Web查看器扩展。

```csharp
public static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration AddWebViewer(this LoggerConfiguration configuration, string logDirectory, int port = 5000, string? username = null, string? password = null);
}
```
