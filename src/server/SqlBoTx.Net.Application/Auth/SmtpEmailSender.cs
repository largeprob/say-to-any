using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqlBoTx.Net.Application.Auth
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendVerificationCodeAsync(string email, string code, TimeSpan expiresIn)
        {
            var smtp = _options.Smtp;
            if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromEmail))
            {
                _logger.LogWarning("未配置 SMTP，注册验证码 {Code} 已生成给 {Email}，{Minutes} 分钟内有效", code, email, (int)expiresIn.TotalMinutes);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(smtp.FromEmail, string.IsNullOrWhiteSpace(smtp.FromName) ? "Say To Any" : smtp.FromName),
                Subject = "Say To Any 安全验证码",
                Body = BuildHtmlBody(email, code, expiresIn),
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(email);
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                BuildPlainTextBody(code, expiresIn),
                Encoding.UTF8,
                MediaTypeNames.Text.Plain));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                BuildHtmlBody(email, code, expiresIn),
                Encoding.UTF8,
                MediaTypeNames.Text.Html));

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.EnableSsl
            };


            if (!string.IsNullOrWhiteSpace(smtp.UserName))
            {
                client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);
            }

            await client.SendMailAsync(message);
        }

        private static string BuildPlainTextBody(string code, TimeSpan expiresIn)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(expiresIn.TotalMinutes));
            return $"""
                   Say To Any 安全验证码

                   你的验证码是：{code}

                   该验证码将在 {minutes} 分钟后过期。请勿将验证码转发或告知他人。

                   如果这不是你的操作，请忽略本邮件。
                   """;
        }

        private static string BuildHtmlBody(string email, string code, TimeSpan expiresIn)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(expiresIn.TotalMinutes));
            var safeEmail = WebUtility.HtmlEncode(email);
            var safeCode = WebUtility.HtmlEncode(code);

            return $"""
                   <!doctype html>
                   <html lang="zh-CN">
                   <head>
                     <meta charset="utf-8">
                     <meta name="viewport" content="width=device-width, initial-scale=1.0">
                     <title>Say To Any 安全验证码</title>
                   </head>
                   <body style="margin:0;padding:0;background:#f5f5f5;color:#1f1f1f;font-family:'Segoe UI',Arial,'Microsoft YaHei',sans-serif;">
                     <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                       你的 Say To Any 验证码是 {safeCode}，{minutes} 分钟内有效。
                     </div>
                     <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f5f5f5;margin:0;padding:32px 16px;">
                       <tr>
                         <td align="center">
                           <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border:1px solid #e5e5e5;">
                             <tr>
                               <td style="padding:28px 32px 18px 32px;border-top:4px solid #0078d4;">
                                 <div style="font-size:18px;font-weight:600;letter-spacing:0;color:#1f1f1f;">Say To Any</div>
                               </td>
                             </tr>
                             <tr>
                               <td style="padding:0 32px 28px 32px;">
                                 <h1 style="margin:0 0 16px 0;font-size:24px;line-height:32px;font-weight:600;color:#1f1f1f;">验证你的邮箱</h1>
                                 <p style="margin:0 0 20px 0;font-size:15px;line-height:24px;color:#4b5563;">
                                   我们收到了使用 <strong style="font-weight:600;color:#1f1f1f;">{safeEmail}</strong> 注册 Say To Any 的请求。请在注册页面输入下面的安全验证码。
                                 </p>
                                 <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:24px 0;background:#f8fafc;border:1px solid #e5e7eb;">
                                   <tr>
                                     <td align="center" style="padding:26px 16px;">
                                       <div style="font-size:13px;line-height:18px;color:#6b7280;margin-bottom:10px;">安全验证码</div>
                                       <div style="font-size:34px;line-height:42px;font-weight:700;letter-spacing:8px;color:#0078d4;font-family:'Segoe UI',Arial,sans-serif;">{safeCode}</div>
                                     </td>
                                   </tr>
                                 </table>
                                 <p style="margin:0 0 12px 0;font-size:14px;line-height:22px;color:#4b5563;">
                                   该验证码将在 <strong style="font-weight:600;color:#1f1f1f;">{minutes} 分钟</strong> 后过期。为了保护你的账号安全，请勿将验证码转发或告知他人。
                                 </p>
                                 <p style="margin:0;font-size:14px;line-height:22px;color:#6b7280;">
                                   如果这不是你的操作，可以安全忽略本邮件。
                                 </p>
                               </td>
                             </tr>
                             <tr>
                               <td style="padding:18px 32px 28px 32px;border-top:1px solid #eeeeee;">
                                 <p style="margin:0;font-size:12px;line-height:18px;color:#8a8f98;">
                                   此邮件由系统自动发送，请勿直接回复。
                                 </p>
                               </td>
                             </tr>
                           </table>
                         </td>
                       </tr>
                     </table>
                   </body>
                   </html>
                   """;
        }
    }
}
