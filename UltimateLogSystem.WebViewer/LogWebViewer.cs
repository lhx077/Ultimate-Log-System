using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using UltimateLogSystem.Parsers;
using UltimateLogSystem.Formatters;

namespace UltimateLogSystem.WebViewer
{
    /// <summary>
    /// 日志Web查看器
    /// </summary>
    public class LogWebViewer
    {
        private readonly string _logDirectory;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private WebApplication? _webApp;
        
        /// <summary>
        /// 创建日志Web查看器
        /// </summary>
        /// <param name="logDirectory">日志目录</param>
        /// <param name="port">端口号</param>
        /// <param name="username">用户名（可选）</param>
        /// <param name="password">密码（可选）</param>
        public LogWebViewer(string logDirectory, int port = 5000, string? username = null, string? password = null)
        {
            _logDirectory = logDirectory;
            _port = port;
            _username = username ?? string.Empty;
            _password = password ?? string.Empty;
        }
        
        /// <summary>
        /// 启动Web查看器
        /// </summary>
        public void Start()
        {
            var builder = WebApplication.CreateBuilder();
            
            // 添加服务
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<ILogParser>(new TextLogParser());
            builder.Services.AddSingleton<ILogParser>(new JsonLogParser());
            
            // 配置Kestrel
            builder.WebHost.UseUrls($"http://localhost:{_port}");
            
            // 创建应用
            var app = builder.Build();
            
            // 配置中间件
            app.UseSwagger();
            app.UseSwaggerUI();
            
            // 认证中间件
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                app.Use(async (context, next) =>
                {
                    string? authHeader = context.Request.Headers["Authorization"];
                    if (authHeader == null || !authHeader.StartsWith("Basic "))
                    {
                        context.Response.StatusCode = 401;
                        context.Response.Headers.Add("WWW-Authenticate", "Basic");
                        return;
                    }
                    
                    var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                    var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                    var credentials = decodedCredentials.Split(':');
                    
                    if (credentials.Length != 2 || credentials[0] != _username || credentials[1] != _password)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.Headers.Add("WWW-Authenticate", "Basic");
                        return;
                    }
                    
                    await next();
                });
            }
            
            // 路由配置
            app.MapGet("/", async (HttpContext context) =>
            {
                await ShowLogList(context);
            });
            
            app.MapGet("/view/{fileName}", async (HttpContext context, string fileName) =>
            {
                await ShowLogFile(context, fileName);
            });
            
            app.MapGet("/api/logs", async (HttpContext context) =>
            {
                await GetLogsApi(context);
            });
            
            app.MapGet("/api/stats", async (HttpContext context) =>
            {
                await GetStatsApi(context);
            });
            
            // 启动应用
            app.Start();
            _webApp = app;
            
            Console.WriteLine($"日志查看器已启动，请访问: http://localhost:{_port}");
        }
        
        /// <summary>
        /// 显示日志列表
        /// </summary>
        private async Task ShowLogList(HttpContext context)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            
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
                    sb.AppendLine($"      <td><a href=\"/view/{file.Name}\">查看</a> | <a href=\"/api/stats?file={file.Name}\" target=\"_blank\">统计</a></td>");
                    sb.AppendLine($"    </tr>");
                }
            }
            else
            {
                sb.AppendLine($"    <tr><td colspan=\"4\">日志目录不存在: {_logDirectory}</td></tr>");
            }
            
            sb.AppendLine("  </table>");
            
            // 添加统计图表区域
            sb.AppendLine("  <div id=\"stats-container\" style=\"margin-top: 30px; display: none;\">");
            sb.AppendLine("    <h2>日志统计</h2>");
            sb.AppendLine("    <div id=\"level-chart\" style=\"height: 300px;\"></div>");
            sb.AppendLine("    <div id=\"hour-chart\" style=\"height: 300px; margin-top: 20px;\"></div>");
            sb.AppendLine("  </div>");
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            await context.Response.WriteAsync(sb.ToString());
        }
        
        /// <summary>
        /// 显示日志文件内容
        /// </summary>
        private async Task ShowLogFile(HttpContext context, string fileName)
        {
            var filePath = Path.Combine(_logDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("文件不存在");
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
            
            context.Response.ContentType = "text/html; charset=utf-8";
            
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
            sb.AppendLine("    .pagination { margin-top: 20px; }");
            sb.AppendLine("    .pagination a, .pagination span { display: inline-block; padding: 8px 12px; margin-right: 5px; border: 1px solid #ddd; }");
            sb.AppendLine("    .pagination a { text-decoration: none; color: #333; }");
            sb.AppendLine("    .pagination a:hover { background-color: #f5f5f5; }");
            sb.AppendLine("    .pagination .current { background-color: #4CAF50; color: white; border-color: #4CAF50; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("  <script>");
            sb.AppendLine("    function applyFilters() {");
            sb.AppendLine("      var level = document.getElementById('level-filter').value;");
            sb.AppendLine("      var text = document.getElementById('text-filter').value.toLowerCase();");
            sb.AppendLine("      var category = document.getElementById('category-filter').value.toLowerCase();");
            sb.AppendLine("      var entries = document.getElementsByClassName('log-entry');");
            sb.AppendLine("      var count = 0;");
            sb.AppendLine("      for (var i = 0; i < entries.length; i++) {");
            sb.AppendLine("        var entry = entries[i];");
            sb.AppendLine("        var entryLevel = entry.getAttribute('data-level').toLowerCase();");
            sb.AppendLine("        var entryText = entry.textContent.toLowerCase();");
            sb.AppendLine("        var entryCategory = (entry.getAttribute('data-category') || '').toLowerCase();");
            sb.AppendLine("        var levelMatch = level === 'all' || entryLevel === level.toLowerCase();");
            sb.AppendLine("        var textMatch = text === '' || entryText.includes(text);");
            sb.AppendLine("        var categoryMatch = category === '' || entryCategory.includes(category);");
            sb.AppendLine("        var visible = levelMatch && textMatch && categoryMatch;");
            sb.AppendLine("        entry.style.display = visible ? 'block' : 'none';");
            sb.AppendLine("        if (visible) count++;");
            sb.AppendLine("      }");
            sb.AppendLine("      document.getElementById('filter-count').textContent = count;");
            sb.AppendLine("      document.getElementById('filter-total').textContent = entries.length;");
            sb.AppendLine("    }");
            sb.AppendLine("    function clearFilters() {");
            sb.AppendLine("      document.getElementById('level-filter').value = 'all';");
            sb.AppendLine("      document.getElementById('text-filter').value = '';");
            sb.AppendLine("      document.getElementById('category-filter').value = '';");
            sb.AppendLine("      applyFilters();");
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
            
            // 添加自定义日志级别
            var customLevels = entries
                .Select(e => e.Level.ToString())
                .Where(l => !Enum.TryParse<LogLevel>(l, true, out _))
                .Distinct();
                
            foreach (var level in customLevels)
            {
                sb.AppendLine($"        <option value=\"{level.ToLower()}\">{level}</option>");
            }
            
            sb.AppendLine("      </select>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style=\"margin-top: 10px;\">");
            sb.AppendLine("      <label for=\"category-filter\">类别:</label>");
            sb.AppendLine("      <input type=\"text\" id=\"category-filter\" onkeyup=\"applyFilters()\" style=\"width: 200px;\" placeholder=\"输入类别...\">");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style=\"margin-top: 10px;\">");
            sb.AppendLine("      <label for=\"text-filter\">文本搜索:</label>");
            sb.AppendLine("      <input type=\"text\" id=\"text-filter\" onkeyup=\"applyFilters()\" style=\"width: 300px;\" placeholder=\"输入搜索文本...\">");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style=\"margin-top: 10px;\">");
            sb.AppendLine("      <button onclick=\"clearFilters()\">清除过滤器</button>");
            sb.AppendLine("      <span style=\"margin-left: 20px;\">显示 <span id=\"filter-count\">" + entries.Count + "</span> / <span id=\"filter-total\">" + entries.Count + "</span> 条日志</span>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            
            // 分页参数
            int pageSize = 100;
            int pageNumber = 1;
            if (context.Request.Query.TryGetValue("page", out var pageStr) && int.TryParse(pageStr, out int page))
            {
                pageNumber = Math.Max(1, page);
            }
            
            int totalPages = (int)Math.Ceiling(entries.Count / (double)pageSize);
            int startIndex = (pageNumber - 1) * pageSize;
            var pageEntries = entries.Skip(startIndex).Take(pageSize);
            
            // 显示分页
            if (totalPages > 1)
            {
                sb.AppendLine("  <div class=\"pagination\">");
                sb.AppendLine("    <span>页码:</span>");
                
                // 上一页
                if (pageNumber > 1)
                {
                    sb.AppendLine($"    <a href=\"/view/{fileName}?page={pageNumber - 1}\">上一页</a>");
                }
                
                // 页码
                int startPage = Math.Max(1, pageNumber - 2);
                int endPage = Math.Min(totalPages, startPage + 4);
                
                for (int i = startPage; i <= endPage; i++)
                {
                    if (i == pageNumber)
                    {
                        sb.AppendLine($"    <span class=\"current\">{i}</span>");
                    }
                    else
                    {
                        sb.AppendLine($"    <a href=\"/view/{fileName}?page={i}\">{i}</a>");
                    }
                }
                
                // 下一页
                if (pageNumber < totalPages)
                {
                    sb.AppendLine($"    <a href=\"/view/{fileName}?page={pageNumber + 1}\">下一页</a>");
                }
                
                sb.AppendLine("  </div>");
            }
            
            // 显示日志条目
            foreach (var entry in pageEntries)
            {
                var levelClass = entry.Level.ToString().ToLower();
                sb.AppendLine($"  <div class=\"log-entry {levelClass}\" data-level=\"{entry.Level}\" data-category=\"{entry.Category ?? ""}\">");
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
            
            // 显示底部分页
            if (totalPages > 1)
            {
                sb.AppendLine("  <div class=\"pagination\" style=\"margin-top: 20px;\">");
                sb.AppendLine("    <span>页码:</span>");
                
                // 上一页
                if (pageNumber > 1)
                {
                    sb.AppendLine($"    <a href=\"/view/{fileName}?page={pageNumber - 1}\">上一页</a>");
                }
                
                // 页码
                int startPage = Math.Max(1, pageNumber - 2);
                int endPage = Math.Min(totalPages, startPage + 4);
                
                for (int i = startPage; i <= endPage; i++)
                {
                    if (i == pageNumber)
                    {
                        sb.AppendLine($"    <span class=\"current\">{i}</span>");
                    }
                    else
                    {
                        sb.AppendLine($"    <a href=\"/view/{fileName}?page={i}\">{i}</a>");
                    }
                }
                
                // 下一页
                if (pageNumber < totalPages)
                {
                    sb.AppendLine($"    <a href=\"/view/{fileName}?page={pageNumber + 1}\">下一页</a>");
                }
                
                sb.AppendLine("  </div>");
            }
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            await context.Response.WriteAsync(sb.ToString());
        }
        
        /// <summary>
        /// 处理API请求：获取日志列表
        /// </summary>
        private async Task GetLogsApi(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            
            var files = new List<object>();
            
            if (Directory.Exists(_logDirectory))
            {
                foreach (var file in new DirectoryInfo(_logDirectory).GetFiles().OrderByDescending(f => f.LastWriteTime))
                {
                    files.Add(new
                    {
                        name = file.Name,
                        size = file.Length,
                        sizeFormatted = FormatFileSize(file.Length),
                        lastModified = file.LastWriteTime,
                        lastModifiedFormatted = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }
            
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { files }));
        }
        
        /// <summary>
        /// 处理API请求：获取日志统计信息
        /// </summary>
        private async Task GetStatsApi(HttpContext context)
        {
            string? fileName = context.Request.Query["file"];
            
            if (string.IsNullOrEmpty(fileName))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("缺少文件参数");
                return;
            }
            
            var filePath = Path.Combine(_logDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("文件不存在");
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
            var levelStats = LogAnalyzer.CountByLevel(entries);
            var hourStats = LogAnalyzer.GetHourDistribution(entries);
            var categoryStats = entries
                .GroupBy(e => e.Category ?? "未分类")
                .ToDictionary(g => g.Key, g => g.Count());
                
            // 获取最常见的错误消息
            var errorMessages = entries
                .Where(e => e.Level.ToString().Equals("Error", StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.Message)
                .Select(g => new { Message = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();
            
            context.Response.ContentType = "application/json; charset=utf-8";
            
            var result = new
            {
                fileName = fileName,
                fileSize = new FileInfo(filePath).Length,
                fileSizeFormatted = FormatFileSize(new FileInfo(filePath).Length),
                lastModified = File.GetLastWriteTime(filePath),
                totalEntries = entries.Count,
                oldestEntry = entries.Any() ? entries.Min(e => e.Timestamp) : (DateTime?)null,
                newestEntry = entries.Any() ? entries.Max(e => e.Timestamp) : (DateTime?)null,
                byLevel = levelStats,
                byHour = hourStats,
                byCategory = categoryStats,
                topErrorMessages = errorMessages
            };
            
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        
        /// <summary>
        /// 停止Web查看器
        /// </summary>
        public async Task StopAsync()
        {
            if (_webApp != null)
            {
                await _webApp.StopAsync();
                await _webApp.DisposeAsync();
                _webApp = null;
                
                Console.WriteLine("日志查看器已停止");
            }
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
            return System.Net.WebUtility.HtmlEncode(text);
        }
    }
    
    /// <summary>
    /// 日志配置Web查看器扩展
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// 添加Web查看器
        /// </summary>
        public static LoggerConfiguration AddWebViewer(
            this LoggerConfiguration configuration,
            string logDirectory,
            int port = 5000,
            string? username = null,
            string? password = null)
        {
            var viewer = new LogWebViewer(logDirectory, port, username, password);
            viewer.Start();
            
            return configuration;
        }
    }
}