using System;
using System.Collections.Generic;
using UltimateLogSystem.Handlers;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class HandlersTests
    {
        [Fact]
        public void ActionHandler_ShouldHandleWhenPredicateTrue()
        {
            // 准备
            bool handlerCalled = false;
            var handler = new ActionHandler(
                entry => { handlerCalled = true; },
                entry => entry.Level.Equals(LogLevel.Error)
            );
            
            var errorEntry = new LogEntry(DateTime.Now, LogLevel.Error, null, "错误消息");
            var infoEntry = new LogEntry(DateTime.Now, LogLevel.Info, null, "信息消息");
            
            // 执行
            bool shouldHandleError = handler.ShouldHandle(errorEntry);
            bool shouldHandleInfo = handler.ShouldHandle(infoEntry);
            
            handler.Handle(errorEntry);
            
            // 验证
            Assert.True(shouldHandleError);
            Assert.False(shouldHandleInfo);
            Assert.True(handlerCalled);
        }
        
        [Fact]
        public void ActionHandler_ShouldHandleAllWhenNoPredicateProvided()
        {
            // 准备
            int handleCount = 0;
            var handler = new ActionHandler(entry => { handleCount++; });
            
            var errorEntry = new LogEntry(DateTime.Now, LogLevel.Error, null, "错误消息");
            var infoEntry = new LogEntry(DateTime.Now, LogLevel.Info, null, "信息消息");
            
            // 执行
            bool shouldHandleError = handler.ShouldHandle(errorEntry);
            bool shouldHandleInfo = handler.ShouldHandle(infoEntry);
            
            handler.Handle(errorEntry);
            handler.Handle(infoEntry);
            
            // 验证
            Assert.True(shouldHandleError);
            Assert.True(shouldHandleInfo);
            Assert.Equal(2, handleCount);
        }
        
        [Fact]
        public void ActionHandler_ShouldPassEntryToAction()
        {
            // 准备
            LogEntry? passedEntry = null;
            var handler = new ActionHandler(entry => { passedEntry = entry; });
            
            var testEntry = new LogEntry(DateTime.Now, LogLevel.Info, "测试", "测试消息");
            testEntry.Properties["TestProp"] = "TestValue";
            
            // 执行
            handler.Handle(testEntry);
            
            // 验证
            Assert.NotNull(passedEntry);
            Assert.Equal(testEntry.Message, passedEntry.Message);
            Assert.Equal(testEntry.Level, passedEntry.Level);
            Assert.Equal(testEntry.Category, passedEntry.Category);
            Assert.Equal("TestValue", passedEntry.Properties["TestProp"]);
        }
        
        [Fact]
        public void EmailNotificationHandler_ShouldHandleBasedOnLevel()
        {
            // 准备
            var handler = new EmailNotificationHandler(
                "smtp.example.com", 587,
                "from@example.com", "to@example.com",
                "username", "password",
                LogLevel.Error
            );
            
            var errorEntry = new LogEntry(DateTime.Now, LogLevel.Error, null, "错误消息");
            var fatalEntry = new LogEntry(DateTime.Now, LogLevel.Fatal, null, "致命错误");
            var warningEntry = new LogEntry(DateTime.Now, LogLevel.Warning, null, "警告消息");
            var infoEntry = new LogEntry(DateTime.Now, LogLevel.Info, null, "信息消息");
            
            // 执行
            bool shouldHandleError = handler.ShouldHandle(errorEntry);
            bool shouldHandleFatal = handler.ShouldHandle(fatalEntry);
            bool shouldHandleWarning = handler.ShouldHandle(warningEntry);
            bool shouldHandleInfo = handler.ShouldHandle(infoEntry);
            
            // 验证
            Assert.True(shouldHandleError);
            Assert.True(shouldHandleFatal);
            Assert.False(shouldHandleWarning);
            Assert.False(shouldHandleInfo);
        }
        
        [Fact]
        public void EmailNotificationHandler_ShouldHandleCustomLevel()
        {
            // 准备
            var auditLevel = CustomLogLevel.Create(15, "Audit");
            
            var handler = new EmailNotificationHandler(
                "smtp.example.com", 587,
                "from@example.com", "to@example.com",
                "username", "password",
                auditLevel
            );
            
            var auditEntry = new LogEntry(DateTime.Now, auditLevel, null, "审计消息");
            var infoEntry = new LogEntry(DateTime.Now, LogLevel.Info, null, "信息消息");
            
            // 执行
            bool shouldHandleAudit = handler.ShouldHandle(auditEntry);
            bool shouldHandleInfo = handler.ShouldHandle(infoEntry);
            
            // 验证
            Assert.True(shouldHandleAudit);
            Assert.False(shouldHandleInfo);
        }
    }
} 