using mssql_bot.helper;
using Spectre.Console;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Timers;

namespace mssql_bot.command
{
    /// <summary>
    /// 事件處理
    /// </summary>
    public class OnTimedEventByCheckTS
    {
        public System.Timers.Timer? _TIMER;
        public string _YOUR_DISCORD_WEBHOOK_URL = "YOUR_DISCORD_WEBHOOK_URL"; // 設定你的 Discord Webhook URL
        public string _YOUR_TELEGRAM_WEBHOOK_URL = "YOUR_TELEGRAM_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public string _YOUR_SLACK_WEBHOOK_URL = "_YOUR_SLACK_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public DBConfig _TARGET_CONNECTION_STRING = new();
        public string _TAG = "";

        public void OnStart(double interval)
        {
            _TIMER = new System.Timers.Timer(interval);
            _TIMER.Elapsed += OnTimedEvent;
            _TIMER.AutoReset = true;
            _TIMER.Enabled = true;
            AnsiConsole.MarkupLine($"[yellow]Program Start.[/]");

            // 立即觸發 OnTimedEvent
            OnTimedEvent(null, null);
        }

        public void OnStop()
        {
            _TIMER?.Stop();
            _TIMER?.Dispose();
            AnsiConsole.MarkupLine($"[yellow]Program terminated by user.[/]");
        }

        /// <summary>
        /// 用于定期检查数据库中的存储过程和函数，然后与之前备份的数据进行比较。如果有差异，则将这些差异记录到版本控制中，并发送 Discord 通知。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnTimedEvent(Object? source, ElapsedEventArgs? e)
        {
            //获取数据库连接字符串
            var bbConfig = _TARGET_CONNECTION_STRING; // 專案指定的 key
            //检查连接字符串是否为空
            if (bbConfig.connectionString == string.Empty)
            {
                AnsiConsole.MarkupLine($"[red]empty connectionString[/]");
                return;
            }

            string queryClubs = DbHelper.QUERY_TS_CLUB;
            string queryUnits = DbHelper.QUERY_TS_UNIT;

            using (var connection = new SqlConnection(bbConfig.connectionString))
            {
                try
                {
                    connection.Open();

                    // 紀錄現在時間
                    var nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    AnsiConsole.MarkupLine($"[yellow]{nowTime}: Connection opened successfully.[/]");

                    var clubList = Program.ExecQueryClubTS(queryClubs, connection);
                    var unitList = Program.ExecQueryUnitTS(queryUnits, connection);

                    clubList.ForEach(club =>
                    {
                        // 檢查 Game_id 不能有重複的情況
                        var duplicateGameId = clubList.FindAll(x => x.Game_id == club.Game_id).Count;
                        if (duplicateGameId > 1)
                        {
                            AnsiConsole.MarkupLine($"[red]Duplicate Game_id: {club.Game_id}[/]");
                            AnsiConsole.MarkupLine($"[yellow]Club: {club.UnitKey}, {club.Flag_id}, {club.Game_id}, {club.TuiSui}[/]");
                        }
                    });

                    unitList.ForEach(unit =>
                    {
                        // 檢查 Game_id 與 Tag_Id 中不能有重複的情況
                        var duplicateGameId = unitList.FindAll(x => x.Game_id == unit.Game_id && x.Tag_Id == unit.Tag_Id).Count;
                        if (duplicateGameId > 1)
                        {
                            AnsiConsole.MarkupLine($"[red]Duplicate Game_id: {unit.Game_id}[/]");
                            AnsiConsole.MarkupLine($"[yellow]Uint: {unit.UnitKey}, {unit.Tag_Id}, {unit.Game_id}, {unit.TuiSui}[/]");
                        }
                    });

                    AnsiConsole.MarkupLine($"[yellow]{nowTime} 驗證結束!!![/]");
                }
                catch (Exception ex)
                {
                    //异常处理
                    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                }
            }
        }

        /// <summary>
        /// 發送 Discord 通知
        /// </summary>
        /// <param name="message"></param>
        private async void SendDiscordNotification(string message)
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
        private async void SendTelegramNotification(string message)
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
        private async void SendSlackNotification(string message)
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
                    blocks = new[] {
                        new {
                            type = "section",
                            block_id = "section567",
                            text = new {
                                type = "mrkdwn",
                                text = message
                            }
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
