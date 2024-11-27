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
        public List<string> _CLUB_LIST = [];

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
            //获取数据库连接字符串
            var bbConfig = _TARGET_CONNECTION_STRING; // 專案指定的 key
            //检查连接字符串是否为空
            if (bbConfig.connectionString == string.Empty)
            {
                AnsiConsole.MarkupLine($"[red]empty connectionString[/]");
                return;
            }
            string queryLastLogin = DbHelper.QUERY_LAST_LOGIN;

            string queryClubs = DbHelper.QUERY_TS_CLUB;
            string queryUnits = DbHelper.QUERY_TS_UNIT;

            using (var connection = new SqlConnection(bbConfig.connectionString))
            {
                try
                {
                    connection.Open();

                    // 紀錄現在時間
                    var nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    AnsiConsole.MarkupLine(
                        $"[yellow]{nowTime}: Connection opened successfully.[/]"
                    );

                    // 我要先算出現在時間減去 N 分鐘的時間
                    var beforeTime = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-dd HH:mm:ss");
                    var queryRangeTime = queryLastLogin.Replace("@StartTime", $"'{beforeTime}'");

                    var lastLoginList = Program.ExecQueryLastLoginTS(queryRangeTime, connection);
                    if (lastLoginList.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[green]{nowTime} 無人登入，不執行驗證!!![/]");
                        return;
                    }

                    // 印出 lastLoginList 的 CLUB_ID
                    lastLoginList.ForEach(lastLogin =>
                    {
                        AnsiConsole.MarkupLine(
                            $"[yellow] 有人進入官網 CLUB_ID: {lastLogin.CLUB_ID}, UPDATE_TIME: {lastLogin.UPDATE_TIME}, IP: {lastLogin.IP}[/]"
                        );

                        var queryClubById = queryClubs.Replace(
                            "@Club_id",
                            $"'{lastLogin.CLUB_ID}'"
                        );
                        var clubList = Program.ExecQueryClubTS(queryClubById, connection);
                        
                        // Club_Id 不重複才處理
                        if(!_CLUB_LIST.Contains(lastLogin.CLUB_ID!))
                        {
                            // 廠商的 keyword 列表
                            var clubKeywordList = new List<string> { "WM", "WE", "IDN" };
                            clubKeywordList.ForEach(keyword =>
                            {
                                // clubList 裡面的 Game_id 欄位必須要有包還 keyword 的關鍵字才行
                                var clubListByKeyword = clubList.FindAll(x => x.Game_id != null && x.Game_id.Contains(keyword));
                                if (clubListByKeyword.Count == 0)
                                {
                                    // 發送 Discord 通知
                                    _notificationHelper.SendDiscordNotification(
                                        $"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, 沒有廠商: {keyword} 的資料"
                                    );

                                    // 發送 TG 通知
                                    _notificationHelper.SendTelegramNotification(
                                        $"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, 沒有廠商: {keyword} 的資料"
                                    );

                                    // 發送 Slack 通知
                                    _notificationHelper.SendSlackNotification(
                                        $"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, 沒有廠商: {keyword} 的資料"
                                    );

                                    _CLUB_LIST.Add(lastLogin.CLUB_ID!);
                                }
                            });
                        }

                        clubList.ForEach(club =>
                        {
                            // 檢查 Game_id 不能有重複的情況
                            var duplicateGameId = clubList
                                .FindAll(x => x.Game_id == club.Game_id)
                                .Count;
                            if (duplicateGameId > 1)
                            {
                                AnsiConsole.MarkupLine(
                                    $"[red]Duplicate Game_id: {club.Game_id}[/]"
                                );
                                AnsiConsole.MarkupLine(
                                    $"[yellow]CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {club.UnitKey}, Flag_id: {club.Flag_id}, Game_id:{club.Game_id}, TuiSui: {club.TuiSui}[/]"
                                );

                                // 發送 Discord 通知
                                _notificationHelper.SendDiscordNotification(
                                    $"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {club.UnitKey}, Flag_id: {club.Flag_id}, Game_id:{club.Game_id}, TuiSui: {club.TuiSui}"
                                );

                                // 發送 TG 通知
                                _notificationHelper.SendTelegramNotification(
                                    $"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {club.UnitKey}, Flag_id: {club.Flag_id}, Game_id:{club.Game_id}, TuiSui: {club.TuiSui}"
                                );

                                // 發送 Slack 通知
                                _notificationHelper.SendSlackNotification(
                                    $"{_TAG}: 個人退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {club.UnitKey}, Flag_id: {club.Flag_id}, Game_id:{club.Game_id}, TuiSui: {club.TuiSui}"
                                );
                            }
                        });

                        if (clubList.Count > 0)
                        {
                            var queryUnitById = queryClubs.Replace(
                                "@Club_id",
                                $"'{clubList[0].UnitKey}'"
                            );

                            var unitList = Program.ExecQueryUnitTS(queryUnitById, connection);

                            unitList.ForEach(unit =>
                            {
                                // 檢查 Game_id 與 Tag_Id 中不能有重複的情況
                                var duplicateGameId = unitList
                                    .FindAll(
                                        x => x.Game_id == unit.Game_id && x.Tag_Id == unit.Tag_Id
                                    )
                                    .Count;
                                if (duplicateGameId > 1)
                                {
                                    AnsiConsole.MarkupLine(
                                        $"[red]Duplicate Game_id: {unit.Game_id} AND Tag_Id: {unit.Tag_Id}[/]"
                                    );
                                    AnsiConsole.MarkupLine(
                                        $"[yellow]UnitKey: {unit.UnitKey}, Tag_Id: {unit.Tag_Id}, Game_id: {unit.Game_id}, TuiSui: {unit.TuiSui}[/]"
                                    );
                                    // 發送 Discord 通知
                                    _notificationHelper.SendDiscordNotification(
                                        $"{_TAG}: 階層退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {unit.UnitKey}, Tag_Id: {unit.Tag_Id}, Game_id: {unit.Game_id}, TuiSui: {unit.TuiSui}"
                                    );

                                    // 發送 TG 通知
                                    _notificationHelper.SendTelegramNotification(
                                        $"{_TAG}: 階層退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {unit.UnitKey}, Tag_Id: {unit.Tag_Id}, Game_id: {unit.Game_id}, TuiSui: {unit.TuiSui}"
                                    );

                                    // 發送 Slack 通知
                                    _notificationHelper.SendSlackNotification(
                                        $"{_TAG}: 階層退水錯誤。CLUB_ID: {lastLogin.CLUB_ID}, UnitKey: {unit.UnitKey}, Tag_Id: {unit.Tag_Id}, Game_id: {unit.Game_id}, TuiSui: {unit.TuiSui}"
                                    );
                                }
                            });
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
    }
}
