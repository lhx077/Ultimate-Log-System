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
        private DateTime _currentDate;
        private static readonly string SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        private bool _useDailyRolling = true;
        
        public RollingFileWriter(
            string filePath,
            ILogFormatter? formatter = null,
            long maxFileSize = 10 * 1024 * 1024, // 默认10MB
            int maxRollingFiles = 5,
            Encoding? encoding = null,
            bool useDailyRolling = true)
        {
            _formatter = formatter ?? new TextFormatter();
            _baseFilePath = filePath;
            _maxFileSize = maxFileSize;
            _maxRollingFiles = maxRollingFiles;
            _useDailyRolling = useDailyRolling;
            _currentDate = DateTime.Today;
            
            // 创建日志目录
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 初始化当前文件路径
            _currentFilePath = GetCurrentFilePath();
            
            // 创建写入器
            _writer = new StreamWriter(_currentFilePath, true, encoding ?? Encoding.UTF8)
            {
                AutoFlush = true
            };
            
            // 获取当前文件大小
            FileInfo fileInfo = new FileInfo(_currentFilePath);
            _currentFileSize = fileInfo.Exists ? fileInfo.Length : 0;
        }
        
        public void Write(LogEntry entry)
        {
            if (_writer == null)
                return;
                
            string formattedLog = _formatter.Format(entry);
            
            lock (_lockObj)
            {
                // 检查是否需要按日期滚动文件
                if (_useDailyRolling && entry.Timestamp.Date != _currentDate)
                {
                    _currentDate = entry.Timestamp.Date;
                    RollFilesForNewDay();
                }
                
                // 检查是否需要按大小滚动文件
                long messageSize = Encoding.UTF8.GetByteCount(formattedLog + Environment.NewLine);
                if (_currentFileSize + messageSize > _maxFileSize)
                {
                    RollFilesForSize();
                }
                
                _writer.WriteLine(formattedLog);
                _currentFileSize += messageSize;
            }
        }
        
        private void RollFilesForNewDay()
        {
            _writer?.Close();
            _writer = null;
            
            // 更新当前文件路径为新的日期格式
            _currentFilePath = GetCurrentFilePath();
            
            // 创建新的日志文件
            _writer = new StreamWriter(_currentFilePath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
            _currentFileSize = 0;
        }
        
        private void RollFilesForSize()
        {
            _writer?.Close();
            _writer = null;
            
            // 获取文件名相关部分
            string directory = Path.GetDirectoryName(_currentFilePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_currentFilePath);
            string extension = Path.GetExtension(_currentFilePath);
            
            // 删除最旧的日志文件
            string oldestFile = Path.Combine(directory, $"{fileNameWithoutExt}.{_maxRollingFiles}{extension}");
            if (File.Exists(oldestFile))
            {
                File.Delete(oldestFile);
            }
            
            // 重命名现有的日志文件
            for (int i = _maxRollingFiles - 1; i >= 1; i--)
            {
                string sourceFile = Path.Combine(directory, $"{fileNameWithoutExt}.{i}{extension}");
                string destFile = Path.Combine(directory, $"{fileNameWithoutExt}.{i + 1}{extension}");
                
                if (File.Exists(sourceFile))
                {
                   MoveFile(sourceFile, destFile, true);
                }
            }
            
            // 重命名当前日志文件
            if (File.Exists(_currentFilePath))
            {
                string firstRollingFile = Path.Combine(directory, $"{fileNameWithoutExt}.1{extension}");
                MoveFile(_currentFilePath, firstRollingFile, true);
            }
            
            // 创建新的日志文件
            _writer = new StreamWriter(_currentFilePath, false, Encoding.UTF8)
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
        
        private string GetCurrentFilePath()
        {
            string directory = Path.GetDirectoryName(_baseFilePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
            string extension = Path.GetExtension(_baseFilePath);
            
            if (_useDailyRolling)
            {
                // 格式: 文件名.YYYY-MM-DD.会话ID.扩展名
                string datePart = _currentDate.ToString("yyyy-MM-dd");
                return Path.Combine(directory, $"{fileNameWithoutExt}.{datePart}.{SessionId}{extension}");
            }
            else
            {
                return _baseFilePath;
            }
        }
        
        private string GetRollingFilePath(int index)
        {
            string directory = Path.GetDirectoryName(_baseFilePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(_baseFilePath);
            string extension = Path.GetExtension(_baseFilePath);
            
            if (_useDailyRolling)
            {
                string datePart = _currentDate.ToString("yyyy-MM-dd");
                string baseFileName = $"{fileNameWithoutExt}.{datePart}.{SessionId}";
                return Path.Combine(directory, $"{baseFileName}.{index}{extension}");
            }
            else
            {
                return Path.Combine(directory, $"{fileNameWithoutExt}.{index}{extension}");
            }
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