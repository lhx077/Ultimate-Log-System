using System.Text;
using UltimateLogSystem.Formatters;
using System.IO;
using System.Linq;
using System;

namespace UltimateLogSystem.Writers
{
    /// <summary>
    /// 滚动文件日志输出
    /// </summary>
    public class RollingFileWriter : ILogWriter
    {
        private readonly ILogFormatter _formatter;
        private readonly string _baseFilePath;
        private readonly long _maxFileSize;
        private readonly int _maxRollingFiles;
        private readonly object _lockObj = new object();
        private StreamWriter? _writer;
        private long _currentFileSize;
        private string _currentFilePath;
        
        public RollingFileWriter(
            string filePath,
            ILogFormatter? formatter = null,
            long maxFileSize = 10 * 1024 * 1024, // 默认10MB
            int maxRollingFiles = 5,
            Encoding? encoding = null)
        {
            _formatter = formatter ?? new TextFormatter();
            _baseFilePath = filePath;
            _maxFileSize = maxFileSize;
            _maxRollingFiles = maxRollingFiles;
            _currentFilePath = filePath;
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            _writer = new StreamWriter(filePath, true, encoding ?? Encoding.UTF8)
            {
                AutoFlush = true
            };
            
            FileInfo fileInfo = new FileInfo(filePath);
            _currentFileSize = fileInfo.Exists ? fileInfo.Length : 0;
        }
        
        public void Write(LogEntry entry)
        {
            if (_writer == null)
                return;
                
            string formattedLog = _formatter.Format(entry);
            
            lock (_lockObj)
            {
                // 检查是否需要滚动文件
                long messageSize = Encoding.UTF8.GetByteCount(formattedLog + Environment.NewLine);
                if (_currentFileSize + messageSize > _maxFileSize)
                {
                    RollFiles();
                }
                
                _writer.WriteLine(formattedLog);
                _currentFileSize += messageSize;
            }
        }
        
        private void RollFiles()
        {
            _writer?.Close();
            _writer = null;
            
            // 删除最旧的日志文件
            string oldestFile = GetRollingFilePath(_maxRollingFiles);
            if (File.Exists(oldestFile))
            {
                File.Delete(oldestFile);
            }
            
            // 重命名现有的日志文件
            for (int i = _maxRollingFiles - 1; i >= 1; i--)
            {
                string sourceFile = GetRollingFilePath(i);
                string destFile = GetRollingFilePath(i + 1);
                
                if (File.Exists(sourceFile))
                {
                   MoveFile(sourceFile, destFile, true);
                }
            }
            
            // 重命名当前日志文件
            if (File.Exists(_baseFilePath))
            {
                MoveFile(_baseFilePath, GetRollingFilePath(1), true);
            }
            
            // 创建新的日志文件
            _writer = new StreamWriter(_baseFilePath, false, Encoding.UTF8)
            {
                AutoFlush = true
            };
            _currentFileSize = 0;
        }

        private static void MoveFile(string sourceFile, string destinationFile, bool overwrite)
        {
            if (overwrite && File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            
            File.Move(sourceFile, destinationFile);
        }
        private string GetRollingFilePath(int index)
        {
            string directory = Path.GetDirectoryName(_baseFilePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
            string extension = Path.GetExtension(_baseFilePath);
            
            return Path.Combine(directory, $"{fileNameWithoutExt}.{index}{extension}");
        }
        
        public void Flush()
        {
            lock (_lockObj)
            {
                _writer?.Flush();
            }
        }
        
        public void Dispose()
        {
            lock (_lockObj)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
} 