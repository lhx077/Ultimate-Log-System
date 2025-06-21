using System;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class LogInterceptorTests
    {
        [Fact]
        public void LogInterceptor_ShouldProcessWhenPredicateTrue()
        {
            // 准备
            bool actionCalled = false;
            var interceptor = new LogInterceptor(
                entry => entry.Message.Contains("敏感"),
                entry => {
                    actionCalled = true;
                    entry.Message = entry.Message.Replace("敏感", "***");
                }
            );
            
            var sensitiveEntry = new LogEntry(DateTime.Now, LogLevel.Info, null, "包含敏感信息");
            var normalEntry = new LogEntry(DateTime.Now, LogLevel.Info, null, "普通信息");
            
            // 执行
            bool processedSensitive = interceptor.Process(sensitiveEntry);
            bool processedNormal = interceptor.Process(normalEntry);
            
            // 验证
            Assert.True(processedSensitive);
            Assert.False(processedNormal);
            Assert.True(actionCalled);
            Assert.Equal("包含***信息", sensitiveEntry.Message);
            Assert.Equal("普通信息", normalEntry.Message);
        }
        
        [Fact]
        public void LogInterceptor_ShouldModifyProperties()
        {
            // 准备
            var interceptor = new LogInterceptor(
                entry => entry.Properties.ContainsKey("Password"),
                entry => {
                    if (entry.Properties.ContainsKey("Password"))
                    {
                        entry.Properties["Password"] = "********";
                    }
                }
            );
            
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, null, "用户登录");
            entry.Properties["Username"] = "admin";
            entry.Properties["Password"] = "secret123";
            
            // 执行
            bool processed = interceptor.Process(entry);
            
            // 验证
            Assert.True(processed);
            Assert.Equal("admin", entry.Properties["Username"]);
            Assert.Equal("********", entry.Properties["Password"]);
        }
        
        [Fact]
        public void LogInterceptor_ShouldHandleMultipleConditions()
        {
            // 准备
            int processCount = 0;
            var interceptor = new LogInterceptor(
                entry => entry.Level.Equals(LogLevel.Error) || 
                         (entry.Category != null && entry.Category.Equals("Security")),
                entry => {
                    processCount++;
                    entry.Properties["Processed"] = true;
                }
            );
            
            var errorEntry = new LogEntry(DateTime.Now, LogLevel.Error, "Normal", "错误消息");
            var securityEntry = new LogEntry(DateTime.Now, LogLevel.Info, "Security", "安全消息");
            var normalEntry = new LogEntry(DateTime.Now, LogLevel.Info, "Normal", "普通消息");
            
            // 执行
            interceptor.Process(errorEntry);
            interceptor.Process(securityEntry);
            interceptor.Process(normalEntry);
            
            // 验证
            Assert.Equal(2, processCount);
            Assert.True((bool)errorEntry.Properties["Processed"]);
            Assert.True((bool)securityEntry.Properties["Processed"]);
            Assert.False(normalEntry.Properties.ContainsKey("Processed"));
        }
    }
} 