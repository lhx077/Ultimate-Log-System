using System;
using System.Threading.Tasks;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class LogContextTests
    {
        [Fact]
        public void LogContext_ShouldSetAndGetProperty()
        {
            // 准备
            LogContext.ClearProperties();
            
            // 执行
            LogContext.SetProperty("TestKey", "TestValue");
            var value = LogContext.GetProperty("TestKey");
            
            // 验证
            Assert.Equal("TestValue", value);
        }
        
        [Fact]
        public void LogContext_ShouldReturnNullForNonExistentProperty()
        {
            // 准备
            LogContext.ClearProperties();
            
            // 执行
            var value = LogContext.GetProperty("NonExistentKey");
            
            // 验证
            Assert.Null(value);
        }
        
        [Fact]
        public void LogContext_ShouldClearProperties()
        {
            // 准备
            LogContext.ClearProperties();
            LogContext.SetProperty("Key1", "Value1");
            LogContext.SetProperty("Key2", "Value2");
            
            // 执行
            LogContext.ClearProperties();
            
            // 验证
            Assert.Empty(LogContext.Properties);
            Assert.Null(LogContext.GetProperty("Key1"));
            Assert.Null(LogContext.GetProperty("Key2"));
        }
        
        [Fact]
        public void LogContext_ShouldEnrichLogEntry()
        {
            // 准备
            LogContext.ClearProperties();
            LogContext.SetProperty("UserId", "123");
            LogContext.SetProperty("SessionId", "abc");
            
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, null, "测试消息");
            
            // 执行
            LogContext.EnrichLogEntry(entry);
            
            // 验证
            Assert.Equal("123", entry.Properties["UserId"]);
            Assert.Equal("abc", entry.Properties["SessionId"]);
        }
        
        [Fact]
        public void LogContext_ShouldNotOverwriteExistingProperties()
        {
            // 准备
            LogContext.ClearProperties();
            LogContext.SetProperty("UserId", "123");
            
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, null, "测试消息");
            entry.Properties["UserId"] = "456"; // 已有属性
            
            // 执行
            LogContext.EnrichLogEntry(entry);
            
            // 验证
            Assert.Equal("456", entry.Properties["UserId"]); // 不应被覆盖
        }
        
        [Fact]
        public async Task LogContext_ShouldBeThreadLocal()
        {
            // 准备
            LogContext.ClearProperties();
            LogContext.SetProperty("MainThread", "MainValue");
            
            string? backgroundValue = null;
            
            // 执行
            var task = Task.Run(() => {
                // 后台线程应该有独立的上下文
                backgroundValue = LogContext.GetProperty("MainThread") as string;
                LogContext.SetProperty("BackgroundThread", "BackgroundValue");
            });
            
            await task;
            
            // 验证
            Assert.Null(backgroundValue); // 后台线程不应看到主线程的属性
            Assert.Equal("MainValue", LogContext.GetProperty("MainThread")); // 主线程应保留自己的属性
            Assert.Null(LogContext.GetProperty("BackgroundThread")); // 主线程不应看到后台线程的属性
        }
        
        [Fact]
        public void LogContext_ShouldHandleNullValues()
        {
            // 准备
            LogContext.ClearProperties();
            
            // 执行
            LogContext.SetProperty("NullKey", null);
            var value = LogContext.GetProperty("NullKey");
            
            // 验证
            Assert.Null(value);
            Assert.True(LogContext.Properties.ContainsKey("NullKey"));
        }
        
        [Fact]
        public void LogContext_ShouldHandleComplexObjects()
        {
            // 准备
            LogContext.ClearProperties();
            var complexObject = new { Id = 123, Name = "Test" };
            
            // 执行
            LogContext.SetProperty("ComplexKey", complexObject);
            var value = LogContext.GetProperty("ComplexKey");
            
            // 验证
            Assert.Equal(complexObject, value);
        }
    }
} 