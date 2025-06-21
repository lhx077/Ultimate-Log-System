using System;
using System.Net;
using System.Net.Mail;

namespace UltimateLogSystem.Handlers
{
    /// <summary>
    /// 邮件通知处理器
    /// </summary>
    public class EmailNotificationHandler : ILogHandler
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _toEmail;
        private readonly string _username;
        private readonly string _password;
        private readonly object _levelToHandle;
        
        public EmailNotificationHandler(
            string smtpServer,
            int smtpPort,
            string fromEmail,
            string toEmail,
            string username,
            string password,
            object? levelToHandle = null)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _fromEmail = fromEmail;
            _toEmail = toEmail;
            _username = username;
            _password = password;
            _levelToHandle = levelToHandle ?? LogLevel.Error;
        }
        
        public void Handle(LogEntry entry)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_username, _password)
                };
                
                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail),
                    Subject = $"日志通知: {entry.Level} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Body = $"时间: {entry.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                           $"级别: {entry.Level}\n" +
                           $"类别: {entry.Category ?? "未指定"}\n" +
                           $"消息: {entry.Message}\n" +
                           $"异常: {entry.Exception?.ToString() ?? "无"}"
                };
                
                message.To.Add(_toEmail);
                client.Send(message);
            }
            catch (Exception)
            {
                // 发送邮件失败，忽略异常
            }
        }
        
        public bool ShouldHandle(LogEntry entry)
        {
            if (_levelToHandle is LogLevel levelEnum)
            {
                return entry.Level is LogLevel entryLevel && (int)entryLevel >= (int)levelEnum;
            }
            
            return entry.Level.Equals(_levelToHandle);
        }
    }
} 