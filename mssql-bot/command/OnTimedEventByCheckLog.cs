using mssql_bot.Helper;
using Spectre.Console;
using System.Timers;

namespace mssql_bot.command
{
    /// <summary>
    /// 事件處理
    /// </summary>
    public class OnTimedEventByCheckLog
    {
        public System.Timers.Timer? _TIMER;
        public string _YOUR_DISCORD_WEBHOOK_URL = "YOUR_DISCORD_WEBHOOK_URL"; // 設定你的 Discord Webhook URL
        public string _YOUR_TELEGRAM_WEBHOOK_URL = "YOUR_TELEGRAM_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public string _YOUR_SLACK_WEBHOOK_URL = "_YOUR_SLACK_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public DBConfig _TARGET_CONNECTION_STRING = new();
        public string _TAG = "";
        public string _PS1_PATH = "YOUR_PS1_PATH"; // 設定你的 PowerShell Script 路徑

        private NotificationHelper _notificationHelper = new();

        public void OnStart(double interval)
        {
            _notificationHelper._YOUR_DISCORD_WEBHOOK_URL = _YOUR_DISCORD_WEBHOOK_URL;
            _notificationHelper._YOUR_TELEGRAM_WEBHOOK_URL = _YOUR_TELEGRAM_WEBHOOK_URL;
            _notificationHelper._YOUR_SLACK_WEBHOOK_URL = _YOUR_SLACK_WEBHOOK_URL;

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
            try
            {
                // 紀錄現在時間
                var nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


                // 執行 PowerShell Core 語法
                var command = _PS1_PATH;
                var result = PowerShellExecutor.ExecutePowerShellScript(command);
                
                // 讀取 _PS1_PATH 的  json 檔案
                // 先找出 _PS1_PATH 的目錄
                var directory = Path.GetDirectoryName(_PS1_PATH);
                // 再找出 directory 目錄下的所有 json 檔案
                if (directory != null)
                {
                    var jsonFiles = Directory.GetFiles(directory, "*.json");
                    // 讀取所有 *.json 檔案
                    foreach (var jsonFile in jsonFiles)
                    {
                        var json = File.ReadAllText(jsonFile);
                        AnsiConsole.MarkupLine($"[green]Read json file: {jsonFile}[/]");
                        AnsiConsole.MarkupLine($"[green]Content: {json}[/]");
                    }
                }

                // 使用 AnsiConsole.Write 來避免標記語法錯誤
                AnsiConsole.Write(new Markup($"[green]PowerShell script output: {result.EscapeMarkup()}[/]"));
                
                AnsiConsole.MarkupLine($"[yellow]{nowTime} 執行結束!!![/]");
            }
            catch (Exception ex)
            {
                //异常处理
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            }
        }
    }
}
