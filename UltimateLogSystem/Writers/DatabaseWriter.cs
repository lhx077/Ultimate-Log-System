using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Collections.Generic;
using System.Data;

namespace UltimateLogSystem.Writers
{
    /// <summary>
    /// 数据库日志输出
    /// </summary>
    public class DatabaseWriter : ILogWriter
    {
        private readonly DbConnection _connection;
        private readonly string _tableName;
        private readonly bool _ownsConnection;
        private readonly Queue<LogEntry> _buffer = new Queue<LogEntry>();
        private readonly int _batchSize;
        private readonly object _lockObj = new object();
        private bool _disposed;
        
        public DatabaseWriter(
            DbConnection connection,
            string tableName = "Logs",
            bool ownsConnection = false,
            int batchSize = 100)
        {
            _connection = connection;
            _tableName = tableName;
            _ownsConnection = ownsConnection;
            _batchSize = batchSize;
            
            EnsureTableExists();
        }
        
        private void EnsureTableExists()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
            
            using var cmd = _connection.CreateCommand();
            
            // 这里使用通用SQL语法，可能需要根据具体数据库调整
            cmd.CommandText = $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{_tableName}')
                BEGIN
                    CREATE TABLE {_tableName} (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Timestamp DATETIME NOT NULL,
                        Level NVARCHAR(50) NOT NULL,
                        Category NVARCHAR(255) NULL,
                        Message NVARCHAR(MAX) NOT NULL,
                        Exception NVARCHAR(MAX) NULL,
                        Properties NVARCHAR(MAX) NULL
                    )
                END";
            
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // 表可能已存在或语法不兼容，忽略异常
            }
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
            if (_connection.State != ConnectionState.Open)
            {
                try
                {
                    _connection.Open();
                }
                catch
                {
                    return; // 无法打开连接，直接返回
                }
            }
            
            using var transaction = _connection.BeginTransaction();
            
            try
            {
                while (_buffer.Count > 0)
                {
                    var entry = _buffer.Dequeue();
                    
                    using var cmd = _connection.CreateCommand();
                    cmd.Transaction = transaction;
                    
                    cmd.CommandText = $@"
                        INSERT INTO {_tableName} (Timestamp, Level, Category, Message, Exception, Properties)
                        VALUES (@Timestamp, @Level, @Category, @Message, @Exception, @Properties)";
                    
                    var timestampParam = cmd.CreateParameter();
                    timestampParam.ParameterName = "@Timestamp";
                    timestampParam.Value = entry.Timestamp;
                    cmd.Parameters.Add(timestampParam);
                    
                    var levelParam = cmd.CreateParameter();
                    levelParam.ParameterName = "@Level";
                    levelParam.Value = entry.Level.ToString();
                    cmd.Parameters.Add(levelParam);
                    
                    var categoryParam = cmd.CreateParameter();
                    categoryParam.ParameterName = "@Category";
                    categoryParam.Value = entry.Category as object ?? DBNull.Value;
                    cmd.Parameters.Add(categoryParam);
                    
                    var messageParam = cmd.CreateParameter();
                    messageParam.ParameterName = "@Message";
                    messageParam.Value = entry.Message;
                    cmd.Parameters.Add(messageParam);
                    
                    var exceptionParam = cmd.CreateParameter();
                    exceptionParam.ParameterName = "@Exception";
                    exceptionParam.Value = entry.Exception?.ToString() as object ?? DBNull.Value;
                    cmd.Parameters.Add(exceptionParam);
                    
                    var propertiesParam = cmd.CreateParameter();
                    propertiesParam.ParameterName = "@Properties";
                    propertiesParam.Value = entry.Properties.Count > 0
                        ? JsonSerializer.Serialize(entry.Properties) as object
                        : DBNull.Value;
                    cmd.Parameters.Add(propertiesParam);
                    
                    cmd.ExecuteNonQuery();
                }
                
                transaction.Commit();
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // 回滚失败，忽略
                }
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
                
                if (_ownsConnection)
                {
                    _connection.Dispose();
                }
            }
            catch
            {
                // 忽略释放错误
            }
        }
    }
} 