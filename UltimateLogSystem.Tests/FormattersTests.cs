using System;
using System.Text.Json;
using UltimateLogSystem.Formatters;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class FormattersTests
    {
        [Fact]
        public void TextFormatter_ShouldFormatLogEntry()
        {
            // 准备
            var formatter = new TextFormatter("[{timestamp}] [{level}] {message}");
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Info, null, "测试消息");
            
            // 执行
            var result = formatter.Format(entry);
            
            // 验证
            Assert.Equal("[2023-01-01 12:00:00.000] [Info] 测试消息", result);
        }
        
        [Fact]
        public void TextFormatter_ShouldIncludeCategory()
        {
            // 准备
            var formatter = new TextFormatter("[{timestamp}] [{level}] [{category}] {message}");
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Info, "测试类别", "测试消息");
            
            // 执行
            var result = formatter.Format(entry);
            
            // 验证
            Assert.Equal("[2023-01-01 12:00:00.000] [Info] [测试类别] 测试消息", result);
        }
        
        [Fact]
        public void JsonFormatter_ShouldFormatLogEntry()
        {
            // 准备
            var formatter = new JsonFormatter();
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Info, "测试类别", "测试消息");
            entry.Properties["UserId"] = "123";
            
            // 执行
            var result = formatter.Format(entry);
            
            // 验证
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            
            Assert.Equal("Info", root.GetProperty("Level").GetString());
            Assert.Equal("测试类别", root.GetProperty("Category").GetString());
            Assert.Equal("测试消息", root.GetProperty("Message").GetString());
            Assert.Equal("123", root.GetProperty("Properties").GetProperty("UserId").GetString());
        }
        
        [Fact]
        public void XmlFormatter_ShouldFormatLogEntry()
        {
            // 准备
            var formatter = new XmlFormatter();
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Info, "测试类别", "测试消息");
            entry.Properties["UserId"] = "123";
            
            // 执行
            var result = formatter.Format(entry);
            
            // 验证
            Assert.Contains("<LogEntry>", result);
            Assert.Contains("<Level>Info</Level>", result);
            Assert.Contains("<Category>测试类别</Category>", result);
            Assert.Contains("<Message>测试消息</Message>", result);
            Assert.Contains("<Property Name=\"UserId\">", result);
            Assert.Contains("<Value>123</Value>", result);
        }
        
        [Fact]
        public void TextFormatter_ShouldHandleCustomTemplate()
        {
            // 准备
            var formatter = new TextFormatter("{level} - {message} - {timestamp}");
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            var entry = new LogEntry(timestamp, LogLevel.Error, null, "错误消息");
            
            // 执行
            var result = formatter.Format(entry);
            
            // 验证
            Assert.Equal("Error - 错误消息 - 2023-01-01 12:00:00.000", result);
        }
        
        [Fact]
        public void JsonFormatter_ShouldHandleException()
        {
            // 准备
            var formatter = new JsonFormatter();
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            var exception = new InvalidOperationException("测试异常");
            var entry = new LogEntry(timestamp, LogLevel.Error, null, "错误消息", exception);
            
            // 执行
            var result = formatter.Format(entry);
            
            // 验证
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;
            
            Assert.Equal("Error", root.GetProperty("Level").GetString());
            Assert.Equal("错误消息", root.GetProperty("Message").GetString());
            Assert.Contains("测试异常", root.GetProperty("Exception").GetString());
        }
    }
} 