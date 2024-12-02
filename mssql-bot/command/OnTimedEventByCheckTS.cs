using mssql_bot.helper;
using mssql_bot.Helper;
using Spectre.Console;
using System.Data.SqlClient;
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
        public List<string> _CLUB_LIST = new();

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
            var bbConfig = _TARGET_CONNECTION_STRING; // 專案指定的 key
            if (string.IsNullOrEmpty(bbConfig.connectionString))
            {
                AnsiConsole.MarkupLine($"[red]empty connectionString[/]");
                return;
            }

            using (var connection = new SqlConnection(bbConfig.connectionString))
            {
                try
                {
                    connection.Open();
                    var nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    AnsiConsole.MarkupLine($"[yellow]{nowTime}: Connection opened successfully.[/]");

                    var beforeTime = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-dd HH:mm:ss");
                    var queryRangeTime = DbHelper.QUERY_LAST_LOGIN.Replace("@StartTime", $"'{beforeTime}'");

                    var lastLoginList = Program.ExecQueryLastLoginTS(queryRangeTime, connection);
                    if (lastLoginList.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[green]{nowTime} 無人登入，不執行驗證!!![/]");
                        return;
                    }

                    lastLoginList.ForEach(lastLogin =>
                    {
                        AnsiConsole.MarkupLine($"[yellow] 有人進入官網 CLUB_ID: {lastLogin.CLUB_ID}, CLUB_ENAME: {lastLogin.Club_Ename}, PanZu: {lastLogin.PanZu}, UPDATE_TIME: {lastLogin.UPDATE_TIME}, IP: {lastLogin.IP}[/]");

                        var queryClubById = DbHelper.QUERY_TS_CLUB.Replace("@Club_id", $"'{lastLogin.CLUB_ID}'");
                        var clubList = Program.ExecQueryClubTS(queryClubById, connection);

                        if (!_CLUB_LIST.Contains(lastLogin.CLUB_ID!))
                        {
                            var clubKeywordList = GetClubKeywordList(lastLogin.PanZu!);
                            clubKeywordList.ForEach(keyword =>
                            {
                                var clubListByKeyword = clubList.FindAll(x => x.Game_id != null && x.Game_id.Contains(keyword));
                                if (clubListByKeyword.Count == 0)
                                {
                                    SendNotifications($"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, CLUB_ENAME: {lastLogin.Club_Ename}, PanZu: {lastLogin.PanZu}, 沒有廠商: {keyword} 的資料(IP: {lastLogin.IP})");
                                    _CLUB_LIST.Add(lastLogin.CLUB_ID!);
                                }
                            });
                        }

                        clubList.ForEach(club =>
                        {
                            var duplicateGameId = clubList.FindAll(x => x.Game_id == club.Game_id).Count;
                            if (duplicateGameId > 1)
                            {
                                AnsiConsole.MarkupLine($"[red]Duplicate Game_id: {club.Game_id}[/]");
                                AnsiConsole.MarkupLine($"[yellow]CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {club.UnitKey}, Flag_id: {club.Flag_id}, Game_id:{club.Game_id}, TuiSui: {club.TuiSui}[/]");
                                SendNotifications($"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, CLUB_ENAME: {lastLogin.Club_Ename}, PanZu: {lastLogin.PanZu}, UnitKey: {club.UnitKey}, Flag_id: {club.Flag_id}, Game_id:{club.Game_id}, TuiSui: {club.TuiSui}(IP: {lastLogin.IP})");
                            }
                        });

                        if (clubList.Count > 0)
                        {
                            var queryUnitById = DbHelper.QUERY_TS_UNIT.Replace("@UnitKey", $"'{clubList[0].UnitKey}'");
                            var unitList = Program.ExecQueryUnitTS(queryUnitById, connection);

                            unitList.ForEach(unit =>
                            {
                                var duplicateGameId = unitList.FindAll(x => x.Game_id == unit.Game_id && x.Tag_Id == unit.Tag_Id).Count;
                                if (duplicateGameId > 1)
                                {
                                    AnsiConsole.MarkupLine($"[red]Duplicate Game_id: {unit.Game_id} AND Tag_Id: {unit.Tag_Id}[/]");
                                    AnsiConsole.MarkupLine($"[yellow]UnitKey: {unit.UnitKey}, Tag_Id: {unit.Tag_Id}, Game_id: {unit.Game_id}, TuiSui: {unit.TuiSui}[/]");
                                    SendNotifications($"{_TAG}: 階層退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, CLUB_ENAME: {lastLogin.Club_Ename}, PanZu: {lastLogin.PanZu}, UnitKey: {unit.UnitKey}, Tag_Id: {unit.Tag_Id}, Game_id: {unit.Game_id}, TuiSui: {unit.TuiSui}(IP: {lastLogin.IP})");
                                }
                            });
                        }
                    });

                    AnsiConsole.MarkupLine($"[yellow]{nowTime} 驗證結束!!![/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                }
            }
        }

        /// <summary>
        /// 依據線別 panZu 取得對應的廠商關鍵字
        /// </summary>
        /// <param name="panZu"></param>
        /// <returns></returns>
        private List<string> GetClubKeywordList(string panZu)
        {
            var clubKeywordList = new List<string>
            {
                "WM",
                "WE",
                "IDN",
                panZu switch
                {
                    "XF" => "RCG3",
                    "J" => "RCG2",
                    _ => "RCG"
                }
            };
            return clubKeywordList;
        }

        /// <summary>
        /// telegram, discord, slack 通知
        /// </summary>
        /// <param name="message"></param>
        private void SendNotifications(string message)
        {
            _notificationHelper.SendDiscordNotification(message);
            _notificationHelper.SendTelegramNotification(message);
            _notificationHelper.SendSlackNotification(message);
        }
    }
}
