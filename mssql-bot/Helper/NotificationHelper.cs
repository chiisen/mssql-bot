using Spectre.Console;
using System.Text;
using System.Text.Json;

namespace mssql_bot.Helper
{
    /// <summary>
    /// 通知管理類別
    /// </summary>
    public class NotificationHelper
    {
        public string _YOUR_DISCORD_WEBHOOK_URL = "YOUR_DISCORD_WEBHOOK_URL"; // 設定你的 Discord Webhook URL
        public string _YOUR_TELEGRAM_WEBHOOK_URL = "YOUR_TELEGRAM_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public string _YOUR_SLACK_WEBHOOK_URL = "_YOUR_SLACK_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL

        /// <summary>
        /// 發送 Discord 通知
        /// </summary>
        /// <param name="message"></param>
        public async void SendDiscordNotification(string message)
        {
            if (string.IsNullOrEmpty(_YOUR_DISCORD_WEBHOOK_URL))
            {
                AnsiConsole.MarkupLine($"[yellow]Null _YOUR_DISCORD_WEBHOOK_URL.[/]");
                return;
            }

            using (var client = new HttpClient())
            {
                var content = new StringContent(
                    $"{{\"content\": \"{message}\"}}",
                    Encoding.UTF8,
                    "application/json"
                );
                await client.PostAsync(_YOUR_DISCORD_WEBHOOK_URL, content);
            }
        }

        /// <summary>
        /// 發送 TG 通知
        /// </summary>
        /// <param name="message"></param>
        public async void SendTelegramNotification(string message)
        {
            if (string.IsNullOrEmpty(_YOUR_TELEGRAM_WEBHOOK_URL))
            {
                AnsiConsole.MarkupLine($"[yellow]Null _YOUR_TELEGRAM_WEBHOOK_URL.[/]");
                return;
            }

            using (var client = new HttpClient())
            {
                // 將訊息編碼成 URL 格式
                var encodedMessage = Uri.EscapeDataString(message);
                var url = $"{_YOUR_TELEGRAM_WEBHOOK_URL}{encodedMessage}";

                // 發送 GET 請求
                var response = await client.GetAsync(url);

                // 檢查回應狀態碼
                if (response.IsSuccessStatusCode)
                {
                    AnsiConsole.MarkupLine($"[green]Telegram notification sent successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to send Telegram notification.[/]");
                }
            }
        }

        /// <summary>
        /// 發送 Slack 通知
        /// </summary>
        /// <param name="message"></param>
        public async void SendSlackNotification(string message)
        {
            if (string.IsNullOrEmpty(_YOUR_SLACK_WEBHOOK_URL))
            {
                AnsiConsole.MarkupLine($"[yellow]Null _YOUR_SLACK_WEBHOOK_URL.[/]");
                return;
            }

            using (var client = new HttpClient())
            {
                var payload = new
                {
                    text = "訊息😋",
                    blocks = new[]
                    {
                        new
                        {
                            type = "section",
                            block_id = "section567",
                            text = new { type = "mrkdwn", text = message }
                        }
                    }
                };

                var body = JsonSerializer.Serialize(payload);

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                await client.PostAsync(_YOUR_SLACK_WEBHOOK_URL, content);
            }
        }
    }
}
