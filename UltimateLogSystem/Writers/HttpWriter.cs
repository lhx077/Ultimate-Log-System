using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace UltimateLogSystem.Writers
{
    /// <summary>
    /// HTTP日志输出
    /// </summary>
    public class HttpWriter : ILogWriter
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly Queue<LogEntry> _buffer = new Queue<LogEntry>();
        private readonly int _batchSize;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lockObj = new object();
        private readonly bool _ownsClient;
        private bool _disposed;
        
        public HttpWriter(
            string endpoint,
            HttpClient? httpClient = null,
            int batchSize = 10,
            JsonSerializerOptions? jsonOptions = null)
        {
            _endpoint = endpoint;
            _httpClient = httpClient ?? new HttpClient();
            _ownsClient = httpClient == null;
            _batchSize = batchSize;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                WriteIndented = false
            };
        }
        
        public void Write(LogEntry entry)
        {
            lock (_lockObj)
            {
                _buffer.Enqueue(entry);
                
                if (_buffer.Count >= _batchSize)
                {
                    FlushBuffer();
                }
            }
        }
        
        public void Flush()
        {
            lock (_lockObj)
            {
                if (_buffer.Count > 0)
                {
                    FlushBuffer();
                }
            }
        }
        
        private void FlushBuffer()
        {
            if (_buffer.Count == 0)
                return;
                
            try
            {
                var entries = _buffer.ToArray();
                _buffer.Clear();
                
                var payload = new
                {
                    logs = entries.Select(e => new
                    {
                        timestamp = e.Timestamp,
                        level = e.Level.ToString(),
                        category = e.Category,
                        message = e.Message,
                        exception = e.Exception?.ToString(),
                        properties = e.Properties
                    }).ToArray()
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(payload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");
                
                // 异步发送但不等待结果
                _ = _httpClient.PostAsync(_endpoint, content);
            }
            catch
            {
                // 忽略发送错误
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            try
            {
                Flush();
                
                if (_ownsClient)
                {
                    _httpClient.Dispose();
                }
            }
            catch
            {
                // 忽略释放错误
            }
        }
    }
} 