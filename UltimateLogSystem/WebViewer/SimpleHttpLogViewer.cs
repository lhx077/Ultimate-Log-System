using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Parsers;
using System.Linq;

namespace UltimateLogSystem.WebViewer
{
    /// <summary>
    /// 简单的HTTP日志查看器，不依赖ASP.NET Core
    /// </summary>
    public class SimpleHttpLogViewer : IDisposable
    {
        private readonly string _logDirectory;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts;
        private Task? _listenerTask;
        private bool _disposed;
        
        /// <summary>
        /// 创建简单的HTTP日志查看器
        /// </summary>
        public SimpleHttpLogViewer(string logDirectory, int port = 5000, string? username = null, string? password = null)
        {
            _logDirectory = logDirectory;
            _port = port;
            _username = username ?? string.Empty;
            _password = password ?? string.Empty;
            _listener = new HttpListener();
            _cts = new CancellationTokenSource();
        }
        
        /// <summary>
        /// 启动Web查看器
        /// </summary>
        public void Start()
        {
            if (_listenerTask != null)
                return;
                
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            
            _listenerTask = Task.Run(() => ListenAsync(_cts.Token));
            
            Console.WriteLine($"日志查看器已启动，请访问: http://localhost:{_port}");
        }
        
        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    
                    // 处理认证
                    if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                    {
                        var authHeader = context.Request.Headers["Authorization"];
                        if (authHeader == null || !authHeader.StartsWith("Basic "))
                        {
                            context.Response.StatusCode = 401;
                            context.Response.AddHeader("WWW-Authenticate", "Basic");
                            context.Response.Close();
                            continue;
                        }
                        
                        var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                        var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                        var credentials = decodedCredentials.Split(':');
                        
                        if (credentials.Length != 2 || credentials[0] != _username || credentials[1] != _password)
                        {
                            context.Response.StatusCode = 401;
                            context.Response.AddHeader("WWW-Authenticate", "Basic");
                            context.Response.Close();
                            continue;
                        }
                    }
                    
                    // 处理请求
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
            }
            catch (HttpListenerException)
            {
                // 监听器已关闭
            }
            catch (OperationCanceledException)
            {
                // 操作已取消
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志查看器出错: {ex.Message}");
            }
        }
        
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                response.ContentEncoding = Encoding.UTF8;
                
                var path = request.Url?.AbsolutePath ?? "/";
                
                if (path == "/")
                {
                    await ShowLogList(response);
                }
                else if (path.StartsWith("/view/"))
                {
                    var fileName = path.Substring("/view/".Length);
                    await ShowLogFile(response, fileName);
                }
                else
                {
                    response.StatusCode = 404;
                    using var writer = new StreamWriter(response.OutputStream);
                    await writer.WriteAsync("页面不存在");
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                using var writer = new StreamWriter(context.Response.OutputStream);
                await writer.WriteAsync($"服务器错误: {ex.Message}");
            }
            finally
            {
                context.Response.Close();
            }
        }
        
        /// <summary>
        /// 显示日志列表
        /// </summary>
        private async Task ShowLogList(HttpListenerResponse response)
        {
            response.ContentType = "text/html; charset=utf-8";
            
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("  <title>终极日志系统 - Web查看器</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("    h1 { color: #333; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("    th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("    tr:hover { background-color: #f5f5f5; }");
            sb.AppendLine("    .refresh-btn { padding: 8px 16px; background-color: #4CAF50; color: white; border: none; cursor: pointer; margin-bottom: 10px; }");
            sb.AppendLine("    .refresh-btn:hover { background-color: #45a049; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("  <script>");
            sb.AppendLine("    function refreshList() {");
            sb.AppendLine("      location.reload();");
            sb.AppendLine("    }");
            sb.AppendLine("  </script>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <h1>终极日志系统 - Web查看器</h1>");
            sb.AppendLine("  <button class=\"refresh-btn\" onclick=\"refreshList()\">刷新列表</button>");
            sb.AppendLine("  <h2>日志文件列表</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th>文件名</th><th>大小</th><th>修改时间</th><th>操作</th></tr>");
            
            if (Directory.Exists(_logDirectory))
            {
                foreach (var file in new DirectoryInfo(_logDirectory).GetFiles().OrderByDescending(f => f.LastWriteTime))
                {
                    sb.AppendLine($"    <tr>");
                    sb.AppendLine($"      <td>{file.Name}</td>");
                    sb.AppendLine($"      <td>{FormatFileSize(file.Length)}</td>");
                    sb.AppendLine($"      <td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td>");
                    sb.AppendLine($"      <td><a href=\"/view/{file.Name}\">查看</a></td>");
                    sb.AppendLine($"    </tr>");
                }
            }
            else
            {
                sb.AppendLine($"    <tr><td colspan=\"4\">日志目录不存在: {_logDirectory}</td></tr>");
            }
            
            sb.AppendLine("  </table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            using var writer = new StreamWriter(response.OutputStream);
            await writer.WriteAsync(sb.ToString());
        }
        
        /// <summary>
        /// 显示日志文件内容
        /// </summary>
        private async Task ShowLogFile(HttpListenerResponse response, string fileName)
        {
            var filePath = Path.Combine(_logDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                response.StatusCode = 404;
                using var streamWriter = new StreamWriter(response.OutputStream);
                await streamWriter.WriteAsync("文件不存在");
                return;
            }
            
            var extension = Path.GetExtension(fileName).ToLower();
            ILogParser? parser = null;
            
            if (extension == ".json")
            {
                parser = new JsonLogParser();
            }
            else
            {
                parser = new TextLogParser();
            }
            
            var entries = parser.ParseFile(filePath).ToList();
            
            response.ContentType = "text/html; charset=utf-8";
            
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("  <title>终极日志系统 - " + fileName + "</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("    h1 { color: #333; }");
            sb.AppendLine("    .log-entry { margin-bottom: 10px; padding: 8px; border-radius: 4px; }");
            sb.AppendLine("    .trace { background-color: #f0f0f0; }");
            sb.AppendLine("    .debug { background-color: #e0f0ff; }");
            sb.AppendLine("    .info { background-color: #e0ffe0; }");
            sb.AppendLine("    .warning { background-color: #fffbe0; }");
            sb.AppendLine("    .error { background-color: #ffe0e0; }");
            sb.AppendLine("    .fatal { background-color: #ffd0d0; }");
            sb.AppendLine("    .timestamp { color: #666; font-size: 0.9em; }");
            sb.AppendLine("    .level { font-weight: bold; }");
            sb.AppendLine("    .category { color: #555; }");
            sb.AppendLine("    .message { margin-top: 5px; }");
            sb.AppendLine("    .exception { margin-top: 5px; color: #c00; font-family: monospace; white-space: pre-wrap; }");
            sb.AppendLine("    .back-link { margin-bottom: 20px; }");
            sb.AppendLine("    .filters { margin-bottom: 20px; padding: 10px; background-color: #f5f5f5; border-radius: 4px; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("  <script>");
            sb.AppendLine("    function applyFilters() {");
            sb.AppendLine("      var level = document.getElementById('level-filter').value;");
            sb.AppendLine("      var text = document.getElementById('text-filter').value.toLowerCase();");
            sb.AppendLine("      var entries = document.getElementsByClassName('log-entry');");
            sb.AppendLine("      for (var i = 0; i < entries.length; i++) {");
            sb.AppendLine("        var entry = entries[i];");
            sb.AppendLine("        var entryLevel = entry.getAttribute('data-level').toLowerCase();");
            sb.AppendLine("        var entryText = entry.textContent.toLowerCase();");
            sb.AppendLine("        var levelMatch = level === 'all' || entryLevel === level.toLowerCase();");
            sb.AppendLine("        var textMatch = text === '' || entryText.includes(text);");
            sb.AppendLine("        entry.style.display = levelMatch && textMatch ? 'block' : 'none';");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            sb.AppendLine("  </script>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"back-link\"><a href=\"/\">返回日志列表</a></div>");
            sb.AppendLine("  <h1>日志文件: " + fileName + "</h1>");
            
            // 添加过滤器
            sb.AppendLine("  <div class=\"filters\">");
            sb.AppendLine("    <h3>过滤器</h3>");
            sb.AppendLine("    <div>");
            sb.AppendLine("      <label for=\"level-filter\">日志级别:</label>");
            sb.AppendLine("      <select id=\"level-filter\" onchange=\"applyFilters()\">");
            sb.AppendLine("        <option value=\"all\">全部</option>");
            sb.AppendLine("        <option value=\"trace\">Trace</option>");
            sb.AppendLine("        <option value=\"debug\">Debug</option>");
            sb.AppendLine("        <option value=\"info\">Info</option>");
            sb.AppendLine("        <option value=\"warning\">Warning</option>");
            sb.AppendLine("        <option value=\"error\">Error</option>");
            sb.AppendLine("        <option value=\"fatal\">Fatal</option>");
            sb.AppendLine("      </select>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style=\"margin-top: 10px;\">");
            sb.AppendLine("      <label for=\"text-filter\">文本搜索:</label>");
            sb.AppendLine("      <input type=\"text\" id=\"text-filter\" onkeyup=\"applyFilters()\" style=\"width: 300px;\" placeholder=\"输入搜索文本...\">");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            
            // 显示日志条目
            foreach (var entry in entries)
            {
                var levelClass = entry.Level.ToString().ToLower();
                sb.AppendLine($"  <div class=\"log-entry {levelClass}\" data-level=\"{entry.Level}\">");
                sb.AppendLine($"    <div class=\"timestamp\">{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}</div>");
                sb.AppendLine($"    <div><span class=\"level\">{entry.Level}</span>");
                
                if (!string.IsNullOrEmpty(entry.Category))
                {
                    sb.AppendLine($" <span class=\"category\">[{entry.Category}]</span>");
                }
                
                sb.AppendLine("</div>");
                sb.AppendLine($"    <div class=\"message\">{HtmlEncode(entry.Message)}</div>");
                
                if (entry.Exception != null)
                {
                    sb.AppendLine($"    <div class=\"exception\">{HtmlEncode(entry.Exception.ToString())}</div>");
                }
                
                // 显示属性
                if (entry.Properties.Count > 0)
                {
                    sb.AppendLine("    <div class=\"properties\" style=\"margin-top: 5px; font-size: 0.9em;\">");
                    sb.AppendLine("      <details>");
                    sb.AppendLine("        <summary>属性</summary>");
                    sb.AppendLine("        <table style=\"margin-top: 5px; width: 100%; border-collapse: collapse;\">");
                    sb.AppendLine("          <tr><th style=\"text-align: left;\">名称</th><th style=\"text-align: left;\">值</th></tr>");
                    
                    foreach (var prop in entry.Properties)
                    {
                        sb.AppendLine($"          <tr><td style=\"padding: 3px;\">{HtmlEncode(prop.Key)}</td><td style=\"padding: 3px;\">{HtmlEncode(prop.Value?.ToString() ?? "null")}</td></tr>");
                    }
                    
                    sb.AppendLine("        </table>");
                    sb.AppendLine("      </details>");
                    sb.AppendLine("    </div>");
                }
                
                sb.AppendLine("  </div>");
            }
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            using var writer = new StreamWriter(response.OutputStream);
            await writer.WriteAsync(sb.ToString());
        }
        
        /// <summary>
        /// 停止Web查看器
        /// </summary>
        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
            Console.WriteLine("日志查看器已停止");
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            Stop();
            _cts.Dispose();
        }
        
        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
        
        /// <summary>
        /// HTML编码
        /// </summary>
        private string HtmlEncode(string text)
        {
            return WebUtility.HtmlEncode(text);
        }
    }
}