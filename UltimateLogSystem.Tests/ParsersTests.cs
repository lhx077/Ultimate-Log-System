using System;
using System.IO;
using System.Linq;
using System.Text;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Parsers;
using Xunit;

namespace UltimateLogSystem.Tests
{
    public class ParsersTests
    {
        private readonly string _testLogDir = Path.Combine(Path.GetTempPath(), "UltimateLogSystemParserTests");
        
        public ParsersTests()
        {
            // 清理测试目录
            if (Directory.Exists(_testLogDir))
            {
                Directory.Delete(_testLogDir, true);
            }
            Directory.CreateDirectory(_testLogDir);
        }
        
        [Fact]
        public void TextLogParser_ShouldParseTextLog()
        {
            // 准备
            var logContent = @"[2023-01-01 12:00:00] [Info] 信息消息
[2023-01-01 12:01:00] [Warning] 警告消息
[2023-01-01 12:02:00] [Error] 错误消息";
            
            var logFile = Path.Combine(_testLogDir, "text.log");
            File.WriteAllText(logFile, logContent);
            
            var parser = new TextLogParser();
            
            // 执行
            var entries = parser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Equal(3, entries.Count);
            Assert.Equal("Info", entries[0].Level.ToString());
            Assert.Equal("信息消息", entries[0].Message);
            Assert.Equal("Warning", entries[1].Level.ToString());
            Assert.Equal("警告消息", entries[1].Message);
            Assert.Equal("Error", entries[2].Level.ToString());
            Assert.Equal("错误消息", entries[2].Message);
        }
        
        [Fact]
        public void JsonLogParser_ShouldParseJsonLog()
        {
            // 准备
            var logContent = @"{""Timestamp"":""2023-01-01T12:00:00"",""Level"":""Info"",""Category"":""测试"",""Message"":""信息消息"",""Exception"":null,""Properties"":{""UserId"":""123""}}
{""Timestamp"":""2023-01-01T12:01:00"",""Level"":""Warning"",""Category"":""测试"",""Message"":""警告消息"",""Exception"":null,""Properties"":{}}
{""Timestamp"":""2023-01-01T12:02:00"",""Level"":""Error"",""Category"":""测试"",""Message"":""错误消息"",""Exception"":""System.Exception: 测试异常"",""Properties"":{}}";
            
            var logFile = Path.Combine(_testLogDir, "json.log");
            File.WriteAllText(logFile, logContent);
            
            var parser = new JsonLogParser();
            
            // 执行
            var entries = parser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Equal(3, entries.Count);
            Assert.Equal("Info", entries[0].Level.ToString());
            Assert.Equal("信息消息", entries[0].Message);
            Assert.Equal("测试", entries[0].Category);
            Assert.Equal("123", entries[0].Properties["UserId"]);
            
            Assert.Equal("Warning", entries[1].Level.ToString());
            Assert.Equal("警告消息", entries[1].Message);
            
            Assert.Equal("Error", entries[2].Level.ToString());
            Assert.Equal("错误消息", entries[2].Message);
        }
        
        [Fact]
        public void TextLogParser_ShouldParseCustomPattern()
        {
            // 准备
            var logContent = @"INFO [2023-01-01] 信息消息
WARN [2023-01-01] 警告消息
ERROR [2023-01-01] 错误消息";
            
            var logFile = Path.Combine(_testLogDir, "custom.log");
            File.WriteAllText(logFile, logContent);
            
            var parser = new TextLogParser(@"(.*) \[(.*)\] (.*)");
            
            // 执行
            var entries = parser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Equal(3, entries.Count);
            Assert.Equal("INFO", entries[0].Level.ToString());
            Assert.Equal("信息消息", entries[0].Message);
            Assert.Equal("WARN", entries[1].Level.ToString());
            Assert.Equal("警告消息", entries[1].Message);
            Assert.Equal("ERROR", entries[2].Level.ToString());
            Assert.Equal("错误消息", entries[2].Message);
        }
        
        [Fact]
        public void Parser_ShouldHandleEmptyFile()
        {
            // 准备
            var logFile = Path.Combine(_testLogDir, "empty.log");
            File.WriteAllText(logFile, "");
            
            var textParser = new TextLogParser();
            var jsonParser = new JsonLogParser();
            
            // 执行
            var textEntries = textParser.ParseFile(logFile).ToList();
            var jsonEntries = jsonParser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Empty(textEntries);
            Assert.Empty(jsonEntries);
        }
        
        [Fact]
        public void Parser_ShouldHandleNonExistentFile()
        {
            // 准备
            var logFile = Path.Combine(_testLogDir, "nonexistent.log");
            
            var textParser = new TextLogParser();
            var jsonParser = new JsonLogParser();
            
            // 执行
            var textEntries = textParser.ParseFile(logFile).ToList();
            var jsonEntries = jsonParser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Empty(textEntries);
            Assert.Empty(jsonEntries);
        }
        
        [Fact]
        public void TextLogParser_ShouldHandleInvalidLines()
        {
            // 准备
            var logContent = @"[2023-01-01 12:00:00] [Info] 信息消息
这是一行无效的日志
[2023-01-01 12:02:00] [Error] 错误消息";
            
            var logFile = Path.Combine(_testLogDir, "invalid.log");
            File.WriteAllText(logFile, logContent);
            
            var parser = new TextLogParser();
            
            // 执行
            var entries = parser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Equal(2, entries.Count); // 应该只解析出两条有效日志
            Assert.Equal("Info", entries[0].Level.ToString());
            Assert.Equal("信息消息", entries[0].Message);
            Assert.Equal("Error", entries[1].Level.ToString());
            Assert.Equal("错误消息", entries[1].Message);
        }
        
        [Fact]
        public void JsonLogParser_ShouldHandleInvalidJson()
        {
            // 准备
            var logContent = @"{""Timestamp"":""2023-01-01T12:00:00"",""Level"":""Info"",""Message"":""信息消息""}
这不是有效的JSON
{""Timestamp"":""2023-01-01T12:02:00"",""Level"":""Error"",""Message"":""错误消息""}";
            
            var logFile = Path.Combine(_testLogDir, "invalid-json.log");
            File.WriteAllText(logFile, logContent);
            
            var parser = new JsonLogParser();
            
            // 执行
            var entries = parser.ParseFile(logFile).ToList();
            
            // 验证
            Assert.Equal(2, entries.Count); // 应该只解析出两条有效日志
            Assert.Equal("Info", entries[0].Level.ToString());
            Assert.Equal("信息消息", entries[0].Message);
            Assert.Equal("Error", entries[1].Level.ToString());
            Assert.Equal("错误消息", entries[1].Message);
        }
        
        [Fact]
        public void TextLogParser_ShouldParseStringContent()
        {
            // 准备
            var logContent = @"[2023-01-01 12:00:00] [Info] 信息消息
[2023-01-01 12:01:00] [Warning] 警告消息";
            
            var parser = new TextLogParser();
            
            // 执行
            var entries = parser.Parse(logContent).ToList();
            
            // 验证
            Assert.Equal(2, entries.Count);
            Assert.Equal("Info", entries[0].Level.ToString());
            Assert.Equal("信息消息", entries[0].Message);
            Assert.Equal("Warning", entries[1].Level.ToString());
            Assert.Equal("警告消息", entries[1].Message);
        }
    }
} 