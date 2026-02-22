using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using System.Text;
using HtmlAgilityPack;
using DietWorker.DTO;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;

namespace DietWorker.Services
{
    public class EmailService
    {
        private readonly EmailServiceOptions _options;

        public EmailService(IOptions<EmailServiceOptions> options)
        {
            _options = options.Value;
        }

        public async Task<string?> GetTodayMenusFromEmailAsync(string senderEmail)
        {
            using var client = new ImapClient();
            await client.ConnectAsync(
                _options.Imap.Host,
                _options.Imap.Port,
                _options.Imap.UseSsl);
            await client.AuthenticateAsync(
                _options.Username,
                _options.Password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            var query = SearchQuery.DeliveredAfter(DateTime.Today)
                                   .And(SearchQuery.FromContains(senderEmail));

            var uids = await inbox.SearchAsync(query);

            if (uids.Count == 0)
            {
                await client.DisconnectAsync(true);
                return null;
            }

            var messages = new List<MimeMessage>();

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                messages.Add(message);
            }

            var sb = new StringBuilder();
            int menuIndex = 1;

            foreach (var message in messages.OrderBy(m => m.Date))
            {
                string? body = message.TextBody;

                if (body == null && message.HtmlBody != null)
                {
                    body = ExtractPlainTextFromHtml(message.HtmlBody);
                }

                if (!string.IsNullOrWhiteSpace(body))
                {
                    sb.AppendLine($"===== MENU {menuIndex} =====");
                    sb.AppendLine(body.Trim());
                    sb.AppendLine();
                    menuIndex++;
                }
            }

            await client.DisconnectAsync(true);

            return sb.ToString();
        }

        private string ExtractPlainTextFromHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText;
        }

        public async Task SendSelectedDishesAsync(string recipientEmail, List<MealRecommendationDTO> recommendations)
        {
            if (recommendations == null || recommendations.Count == 0)
                return;

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Diet Assistant", _options.Username));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = "🍽️ Menu consigliato di oggi";

            var sb = new StringBuilder();

            sb.AppendLine("Ciao!");
            sb.AppendLine();
            sb.AppendLine("Ecco il menu consigliato per oggi:");
            sb.AppendLine();

            int index = 1;

            foreach (var rec in recommendations)
            {
                sb.AppendLine($"--- Piatto {index} ---");
                sb.AppendLine($"🍽️ {rec.Piatto_scelto}");
                sb.AppendLine($"Tipologia: {rec.Tipologia_piatto}");
                sb.AppendLine($"Livello equilibrio: {rec.Livello_equilibrio}/10");
                sb.AppendLine("Motivazione:");
                sb.AppendLine(rec.Motivazione);
                sb.AppendLine();
                index++;
            }

            sb.AppendLine("Buon appetito 😄");

            var bodyBuilder = new BodyBuilder
            {
                TextBody = sb.ToString()
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.Smtp.Host,
                _options.Smtp.Port,
                MailKit.Security.SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _options.Username,
                _options.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

    }
}
