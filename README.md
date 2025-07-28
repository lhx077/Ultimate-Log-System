# 终极日志系统

这是一个功能全面的.NET日志系统，支持多种日志级别、多种输出格式、多种输出目标，以及丰富的扩展功能。

- [使用指南](使用指南.md)
- [快速参考](快速参考.md)
- [API参考](API_Reference.md)

## 主要特性

- **多级别日志**：支持Trace、Debug、Info、Warning、Error、Fatal等预定义级别，也支持自定义日志级别
- **实时日志记录**：通过后台线程处理，不阻塞主线程
- **时间戳**：每条日志都附带精确时间戳
- **滚动文件更新**：支持按大小自动滚动更新日志文件
- **日期滚动日志**：支持按日期创建新文件，同一天多次启动应用时自动创建不同的日志文件
- **自定义头信息**：可以为日志添加自定义类别和属性
- **多格式支持**：支持文本、JSON、XML等多种格式
- **内置解析器**：支持解析各种格式的日志文件
- **彩色输出**：控制台输出支持不同颜色
- **事件响应**：支持根据日志级别或内容触发不同操作
- **上下文支持**：支持日志上下文，方便跟踪请求
- **结构化日志**：支持记录结构化数据
- **性能监控**：内置操作计时功能
- **导入导出**：支持导入导出各种格式的日志

## 快速开始

### 安装

```bash
dotnet add package UltimateLogSystem
```

### 基本用法

```csharp
// 创建日志配置
var config = new LoggerConfiguration()
    .SetMinimumLevel(LogLevel.Info)
    .AddConsoleWriter()
    .AddFileWriter("logs/app.log");

// 创建日志记录器
var logger = LoggerFactory.CreateLogger(config);

// 记录日志
logger.Info("应用程序启动成功");
logger.Warning("发现潜在问题");
logger.Error("发生错误", exception: ex);

// 关闭日志系统
LoggerFactory.CloseAll();
```

### 高级用法

```csharp
// 创建更复杂的配置
var config = new LoggerConfiguration()
    .SetMinimumLevel(LogLevel.Trace)
    .SetDefaultCategory("MyApp")
    .AddConsoleWriter(new TextFormatter("[{timestamp}] [{level}] {message}"))
    .AddFileWriter("logs/app.log", maxFileSize: 5 * 1024 * 1024, maxRollingFiles: 10)
    .AddFileWriter("logs/app.json", new JsonFormatter())
    .AddEmailNotification(
        "smtp.example.com", 587,
        "alerts@example.com", "admin@example.com",
        "username", "password",
        LogLevel.Error);

// 使用上下文属性
LogContext.SetProperty("UserId", "12345");
logger.Info("用户登录成功");

// 结构化日志
logger.LogStructured(LogLevel.Info, "用户 {UserName} 执行了 {Action}", 
    new { UserName = "张三", Action = "登录" });

// 性能监控
logger.Time(() => {
    // 执行耗时操作
}, "数据库查询");
```

## 扩展

### 自定义输出目标

```csharp
public class MyCustomWriter : ILogWriter
{
    public void Write(LogEntry entry)
    {
        // 自定义日志处理逻辑
    }
    
    public void Flush() { }
    
    public void Dispose() { }
}

// 使用自定义输出
config.AddWriter(new MyCustomWriter());
```

### 自定义格式化器

```csharp
public class MyFormatter : ILogFormatter
{
    public string Format(LogEntry entry)
    {
        // 自定义格式化逻辑
        return $"自定义格式: {entry.Message}";
    }
}

// 使用自定义格式化器
config.AddConsoleWriter(new MyFormatter());
```

## 许可证

Apache 2.0 