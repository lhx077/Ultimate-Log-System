using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Parsers;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class LoggerTests
    {
        private readonly string _testLogDir = Path.Combine(Path.GetTempPath(), "UltimateLogSystemTests");
        
        public LoggerTests()
        {
            // 清理测试目录
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
            Directory.CreateDirectory(_testLogDir);
        }
        
        [Fact]
        public void Logger_ShouldWriteToFile()
        {
            // 准备
            var logFilePath = Path.Combine(_testLogDir, "test.log");
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Info)
                .AddFileWriter(logFilePath);
            
            var logger = LoggerFactory.CreateLogger(config);
            
            // 执行
            logger.Info("测试消息");
            logger.Debug("调试消息"); // 不应该被记录
            
            // 释放资源
            LoggerFactory.CloseAll();
            
            // 验证
            Assert.True(File.Exists(logFilePath));
            var content = File.ReadAllText(logFilePath);
            Assert.Contains("测试消息", content);
            Assert.DoesNotContain("调试消息", content);
        }
        
        [Fact]
        public void Logger_ShouldRespectLogLevel()
        {
            // 准备
            var messages = new List<string>();
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Warning)
                .AddAction(entry => messages.Add(entry.Message));
            
            var logger = LoggerFactory.CreateLogger(config);
            
            // 执行
            logger.Trace("跟踪消息");
            logger.Debug("调试消息");
            logger.Info("信息消息");
            logger.Warning("警告消息");
            logger.Error("错误消息");
            logger.Fatal("致命错误消息");
            
            // 释放资源
            LoggerFactory.CloseAll();
            
            // 验证
            Assert.Equal(3, messages.Count);
            Assert.Contains("警告消息", messages);
            Assert.Contains("错误消息", messages);
            Assert.Contains("致命错误消息", messages);
        }
        
        [Fact]
        public void Logger_ShouldHandleExceptions()
        {
            // 准备
            var exceptions = new List<Exception>();
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Error)
                .AddAction(entry => {
                    if (entry.Exception != null)
                    {
                        exceptions.Add(entry.Exception);
                    }
                });
            
            var logger = LoggerFactory.CreateLogger(config);
            
            // 执行
            var testException = new InvalidOperationException("测试异常");
            logger.Error("发生错误", exception: testException);
            
            // 释放资源
            LoggerFactory.CloseAll();
            
            // 验证
            Assert.Single(exceptions);
            Assert.Equal("测试异常", exceptions[0].Message);
        }
        
        [Fact]
        public void Logger_ShouldUseContext()
        {
            // 准备
            var properties = new List<Dictionary<string, object?>>();
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Info)
                .AddAction(entry => {
                    properties.Add(new Dictionary<string, object?>(entry.Properties));
                });
            
            var logger = LoggerFactory.CreateLogger(config);
            
            // 执行
            LogContext.SetProperty("UserId", "123");
            LogContext.SetProperty("SessionId", "abc");
            logger.Info("用户操作");
            
            // 释放资源
            LoggerFactory.CloseAll();
            
            // 验证
            Assert.Single(properties);
            Assert.Equal("123", properties[0]["UserId"]);
            Assert.Equal("abc", properties[0]["SessionId"]);
        }
        
        [Fact]
        public void LogParser_ShouldParseLogFile()
        {
            // 准备
            var logFilePath = Path.Combine(_testLogDir, "parse-test.log");
            var config = new LoggerConfiguration()
                .AddFileWriter(logFilePath);
            
            var logger = LoggerFactory.CreateLogger(config);
            
            // 记录一些日志
            logger.Info("消息1");
            logger.Warning("消息2");
            logger.Error("消息3");
            
            // 释放资源
            LoggerFactory.CloseAll();
            
            // 执行
            var parser = new TextLogParser();
            var entries = parser.ParseFile(logFilePath).ToList();
            
            // 验证
            Assert.Equal(3, entries.Count);
            Assert.Equal("消息1", entries[0].Message);
            Assert.Equal("消息2", entries[1].Message);
            Assert.Equal("消息3", entries[2].Message);
        }
        
        [Fact]
        public void CustomLogLevel_ShouldWork()
        {
            // 准备
            var messages = new List<string>();
            var auditLevel = CustomLogLevel.Create(15, "Audit");
            
            var config = new LoggerConfiguration()
                .SetMinimumLevel(auditLevel)
                .AddAction(entry => messages.Add($"{entry.Level}: {entry.Message}"));
            
            var logger = LoggerFactory.CreateLogger(config);
            
            // 执行
            logger.Log(LogLevel.Debug, "调试消息"); // 不应该被记录
            logger.Log(auditLevel, "审计消息");
            logger.Log(LogLevel.Info, "信息消息");
            
            // 释放资源
            LoggerFactory.CloseAll();
            
            // 验证
            Assert.Equal(2, messages.Count);
            Assert.Contains("Audit: 审计消息", messages);
            Assert.Contains("Info: 信息消息", messages);
        }
    }
} 