using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class LogAnalyzerTests
    {
        private List<LogEntry> CreateTestLogs()
        {
            return new List<LogEntry>
            {
                new LogEntry(new DateTime(2023, 1, 1, 10, 0, 0), LogLevel.Info, "用户", "用户登录"),
                new LogEntry(new DateTime(2023, 1, 1, 11, 0, 0), LogLevel.Warning, "系统", "磁盘空间不足"),
                new LogEntry(new DateTime(2023, 1, 1, 12, 0, 0), LogLevel.Error, "数据库", "连接失败"),
                new LogEntry(new DateTime(2023, 1, 1, 13, 0, 0), LogLevel.Info, "用户", "用户登出"),
                new LogEntry(new DateTime(2023, 1, 1, 14, 0, 0), LogLevel.Error, "数据库", "查询超时"),
                new LogEntry(new DateTime(2023, 1, 1, 15, 0, 0), LogLevel.Info, "系统", "系统启动")
            };
        }
        
        [Fact]
        public void CountByLevel_ShouldReturnCorrectCounts()
        {
            // 准备
            var logs = CreateTestLogs();
            
            // 执行
            var result = LogAnalyzer.CountByLevel(logs);
            
            // 验证
            Assert.Equal(3, result["Info"]);
            Assert.Equal(1, result["Warning"]);
            Assert.Equal(2, result["Error"]);
        }
        
        [Fact]
        public void CountByCategory_ShouldReturnCorrectCounts()
        {
            // 准备
            var logs = CreateTestLogs();
            
            // 执行
            var result = LogAnalyzer.CountByCategory(logs);
            
            // 验证
            Assert.Equal(2, result["用户"]);
            Assert.Equal(2, result["系统"]);
            Assert.Equal(2, result["数据库"]);
        }
        
        [Fact]
        public void CountByTimeInterval_ShouldReturnCorrectCounts()
        {
            // 准备
            var logs = CreateTestLogs();
            
            // 执行
            var result = LogAnalyzer.CountByTimeInterval(logs, TimeSpan.FromHours(2));
            
            // 验证
            Assert.Equal(3, result.Count);
            Assert.Equal(2, result[new DateTime(2023, 1, 1, 10, 0, 0)]); // 10:00 和 11:00
            Assert.Equal(2, result[new DateTime(2023, 1, 1, 12, 0, 0)]); // 12:00 和 13:00
            Assert.Equal(2, result[new DateTime(2023, 1, 1, 14, 0, 0)]); // 14:00 和 15:00
        }
        
        [Fact]
        public void FindByText_ShouldReturnMatchingEntries()
        {
            // 准备
            var logs = CreateTestLogs();
            
            // 执行
            var result = LogAnalyzer.FindByText(logs, "用户").ToList();
            
            // 验证
            Assert.Equal(2, result.Count);
            Assert.Equal("用户登录", result[0].Message);
            Assert.Equal("用户登出", result[1].Message);
        }
        
        [Fact]
        public void FindByTimeRange_ShouldReturnEntriesInRange()
        {
            // 准备
            var logs = CreateTestLogs();
            var startTime = new DateTime(2023, 1, 1, 11, 0, 0);
            var endTime = new DateTime(2023, 1, 1, 13, 0, 0);
            
            // 执行
            var result = LogAnalyzer.FindByTimeRange(logs, startTime, endTime).ToList();
            
            // 验证
            Assert.Equal(3, result.Count);
            Assert.Equal("磁盘空间不足", result[0].Message);
            Assert.Equal("连接失败", result[1].Message);
            Assert.Equal("用户登出", result[2].Message);
        }
        
        [Fact]
        public void FindByLevel_ShouldReturnEntriesWithLevel()
        {
            // 准备
            var logs = CreateTestLogs();
            
            // 执行
            var result = LogAnalyzer.FindByLevel(logs, LogLevel.Error).ToList();
            
            // 验证
            Assert.Equal(2, result.Count);
            Assert.Equal("连接失败", result[0].Message);
            Assert.Equal("查询超时", result[1].Message);
        }
        
        [Fact]
        public void FindByExceptionType_ShouldReturnEntriesWithException()
        {
            // 准备
            var logs = new List<LogEntry>
            {
                new LogEntry(DateTime.Now, LogLevel.Info, null, "正常消息"),
                new LogEntry(DateTime.Now, LogLevel.Error, null, "错误1", new InvalidOperationException()),
                new LogEntry(DateTime.Now, LogLevel.Error, null, "错误2", new ArgumentException()),
                new LogEntry(DateTime.Now, LogLevel.Error, null, "错误3", new InvalidOperationException())
            };
            
            // 执行
            var result = LogAnalyzer.FindByExceptionType(logs, typeof(InvalidOperationException)).ToList();
            
            // 验证
            Assert.Equal(2, result.Count);
            Assert.Equal("错误1", result[0].Message);
            Assert.Equal("错误3", result[1].Message);
        }
        
        [Fact]
        public void GetMostFrequentMessages_ShouldReturnTopMessages()
        {
            // 准备
            var logs = new List<LogEntry>
            {
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息A"),
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息B"),
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息A"),
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息C"),
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息B"),
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息A"),
                new LogEntry(DateTime.Now, LogLevel.Info, null, "消息D")
            };
            
            // 执行
            var result = LogAnalyzer.GetMostFrequentMessages(logs, 2).ToList();
            
            // 验证
            Assert.Equal(2, result.Count);
            Assert.Equal("消息A", result[0].Key);
            Assert.Equal(3, result[0].Value);
            Assert.Equal("消息B", result[1].Key);
            Assert.Equal(2, result[1].Value);
        }
        
        [Fact]
        public void GetHourDistribution_ShouldReturnCorrectDistribution()
        {
            // 准备
            var logs = new List<LogEntry>
            {
                new LogEntry(new DateTime(2023, 1, 1, 9, 0, 0), LogLevel.Info, null, "消息1"),
                new LogEntry(new DateTime(2023, 1, 1, 9, 30, 0), LogLevel.Info, null, "消息2"),
                new LogEntry(new DateTime(2023, 1, 1, 14, 0, 0), LogLevel.Info, null, "消息3"),
                new LogEntry(new DateTime(2023, 1, 1, 18, 0, 0), LogLevel.Info, null, "消息4"),
                new LogEntry(new DateTime(2023, 1, 1, 18, 30, 0), LogLevel.Info, null, "消息5")
            };
            
            // 执行
            var result = LogAnalyzer.GetHourDistribution(logs);
            
            // 验证
            Assert.Equal(24, result.Count); // 应该有24小时的数据
            Assert.Equal(2, result[9]);  // 9点有2条日志
            Assert.Equal(1, result[14]); // 14点有1条日志
            Assert.Equal(2, result[18]); // 18点有2条日志
            Assert.Equal(0, result[0]);  // 0点没有日志
        }
    }
} 