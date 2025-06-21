using System;
using System.Collections.Generic;  // 提供 Dictionary<,> 和 Queue<>
using System.IO;                   // 提供 StreamWriter
using System.Net.Http;            // 用于 HttpWriter

namespace UltimateLogSystem.Writers
{
    /// <summary>
    /// 日志输出接口
    /// </summary>
    public interface ILogWriter : IDisposable
    {
        /// <summary>
        /// 写入日志
        /// </summary>
        void Write(LogEntry entry);
        
        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        void Flush();
    }
} 