using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Parsers;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class LogExporterTests
    {
        private readonly string _testExportDir = Path.Combine(Path.GetTempPath(), "UltimateLogSystemExportTests");
        
        public LogExporterTests()
        {
            // 清理测试目录
            if (Directory.Exists(_testExportDir))
            {
                Directory.Delete(_testExportDir, true);
            }
            Directory.CreateDirectory(_testExportDir);
        }
        
        private List<LogEntry> CreateTestLogs()
        {
            return new List<LogEntry>
            {
                new LogEntry(new DateTime(2023, 1, 1, 10, 0, 0), LogLevel.Info, "用户", "用户登录"),
                new LogEntry(new DateTime(2023, 1, 1, 11, 0, 0), LogLevel.Warning, "系统", "磁盘空间不足"),
                new LogEntry(new DateTime(2023, 1, 1, 12, 0, 0), LogLevel.Error, "数据库", "连接失败")
            };
        }
        
        [Fact]
        public void ExportToTextFile_ShouldCreateCorrectFile()
        {
            // 准备
            var logs = CreateTestLogs();
            var filePath = Path.Combine(_testExportDir, "export.log");
            
            // 执行
            LogExporter.ExportToTextFile(logs, filePath);
            
            // 验证
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Contains("用户登录", content);
            Assert.Contains("磁盘空间不足", content);
            Assert.Contains("连接失败", content);
        }
        
        [Fact]
        public void ExportToJsonFile_ShouldCreateCorrectFile()
        {
            // 准备
            var logs = CreateTestLogs();
            var filePath = Path.Combine(_testExportDir, "export.json");
            
            // 执行
            LogExporter.ExportToJsonFile(logs, filePath);
            
            // 验证
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Contains("\"Level\": \"Info\"", content);
            Assert.Contains("\"Message\": \"用户登录\"", content);
            Assert.Contains("\"Category\": \"用户\"", content);
            Assert.Contains("\"Level\": \"Warning\"", content);
            Assert.Contains("\"Message\": \"磁盘空间不足\"", content);
            Assert.Contains("\"Level\": \"Error\"", content);
            Assert.Contains("\"Message\": \"连接失败\"", content);
        }
        
        [Fact]
        public void ExportToXmlFile_ShouldCreateCorrectFile()
        {
            // 准备
            var logs = CreateTestLogs();
            var filePath = Path.Combine(_testExportDir, "export.xml");
            
            // 执行
            LogExporter.ExportToXmlFile(logs, filePath);
            
            // 验证
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Contains("<Level>Info</Level>", content);
            Assert.Contains("<Message>用户登录</Message>", content);
            Assert.Contains("<Category>用户</Category>", content);
            Assert.Contains("<Level>Warning</Level>", content);
            Assert.Contains("<Message>磁盘空间不足</Message>", content);
            Assert.Contains("<Level>Error</Level>", content);
            Assert.Contains("<Message>连接失败</Message>", content);
        }
        
        [Fact]
        public void ImportFromTextFile_ShouldReadCorrectEntries()
        {
            // 准备
            var logs = CreateTestLogs();
            var filePath = Path.Combine(_testExportDir, "import.log");
            
            // 先导出
            LogExporter.ExportToTextFile(logs, filePath);
            
            // 执行
            var importedLogs = LogExporter.ImportFromTextFile(filePath).ToList();
            
            // 验证
            Assert.Equal(3, importedLogs.Count);
            Assert.Equal("Info", importedLogs[0].Level.ToString());
            Assert.Equal("用户登录", importedLogs[0].Message);
            Assert.Equal("Warning", importedLogs[1].Level.ToString());
            Assert.Equal("磁盘空间不足", importedLogs[1].Message);
            Assert.Equal("Error", importedLogs[2].Level.ToString());
            Assert.Equal("连接失败", importedLogs[2].Message);
        }
        
        [Fact]
        public void ImportFromJsonFile_ShouldReadCorrectEntries()
        {
            // 准备
            var logs = CreateTestLogs();
            var filePath = Path.Combine(_testExportDir, "import.json");
            
            // 先导出
            LogExporter.ExportToJsonFile(logs, filePath);
            
            // 执行
            var importedLogs = LogExporter.ImportFromJsonFile(filePath).ToList();
            
            // 验证
            Assert.Equal(3, importedLogs.Count);
            Assert.Equal("Info", importedLogs[0].Level.ToString());
            Assert.Equal("用户登录", importedLogs[0].Message);
            Assert.Equal("用户", importedLogs[0].Category);
            Assert.Equal("Warning", importedLogs[1].Level.ToString());
            Assert.Equal("磁盘空间不足", importedLogs[1].Message);
            Assert.Equal("系统", importedLogs[1].Category);
            Assert.Equal("Error", importedLogs[2].Level.ToString());
            Assert.Equal("连接失败", importedLogs[2].Message);
            Assert.Equal("数据库", importedLogs[2].Category);
        }
        
        [Fact]
        public void ImportFromFile_ShouldHandleNonExistentFile()
        {
            // 准备
            var filePath = Path.Combine(_testExportDir, "nonexistent.log");
            
            // 执行
            var textLogs = LogExporter.ImportFromTextFile(filePath).ToList();
            var jsonLogs = LogExporter.ImportFromJsonFile(filePath).ToList();
            
            // 验证
            Assert.Empty(textLogs);
            Assert.Empty(jsonLogs);
        }
        
        [Fact]
        public void ExportToFile_ShouldCreateDirectoryIfNotExists()
        {
            // 准备
            var logs = CreateTestLogs();
            var nestedDir = Path.Combine(_testExportDir, "nested", "dir");
            var filePath = Path.Combine(nestedDir, "export.log");
            
            // 执行
            LogExporter.ExportToTextFile(logs, filePath);
            
            // 验证
            Assert.True(Directory.Exists(nestedDir));
            Assert.True(File.Exists(filePath));
        }
        
        [Fact]
        public void ExportToFile_ShouldOverwriteExistingFile()
        {
            // 准备
            var logs1 = new List<LogEntry>
            {
                new LogEntry(DateTime.Now, LogLevel.Info, null, "原始日志")
            };
            
            var logs2 = new List<LogEntry>
            {
                new LogEntry(DateTime.Now, LogLevel.Info, null, "新日志")
            };
            
            var filePath = Path.Combine(_testExportDir, "overwrite.log");
            
            // 先写入原始日志
            LogExporter.ExportToTextFile(logs1, filePath);
            var originalContent = File.ReadAllText(filePath);
            
            // 执行覆盖
            LogExporter.ExportToTextFile(logs2, filePath);
            var newContent = File.ReadAllText(filePath);
            
            // 验证
            Assert.Contains("原始日志", originalContent);
            Assert.Contains("新日志", newContent);
            Assert.DoesNotContain("原始日志", newContent);
        }
    }
} 