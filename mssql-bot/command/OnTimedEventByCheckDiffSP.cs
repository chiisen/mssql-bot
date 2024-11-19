using mssql_bot.helper;
using mssql_bot.Helper;
using Spectre.Console;
using System.Data.SqlClient;
using System.Text.Json;
using System.Timers;

namespace mssql_bot.command
{
    /// <summary>
    /// 事件處理
    /// </summary>
    public class OnTimedEventByCheckDiffSP
    {
        public System.Timers.Timer? _TIMER;
        public string _YOUR_DISCORD_WEBHOOK_URL = "YOUR_DISCORD_WEBHOOK_URL"; // 設定你的 Discord Webhook URL
        public string _YOUR_TELEGRAM_WEBHOOK_URL = "YOUR_TELEGRAM_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public string _YOUR_SLACK_WEBHOOK_URL = "_YOUR_SLACK_WEBHOOK_URL"; // 設定你的 Telegram Webhook URL
        public DBConfig _TARGET_CONNECTION_STRING = new();
        public string _TARGET_SP_BACKUP = "git 的備份目錄";
        public string _TAG = "";

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

            string queryStoredProcedures = DbHelper.QUERY_STOREDPROCEDURES;
            string queryFunctions = DbHelper.QUERY_FUNCTIONS;

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

                    //获取备份目录
                    var directory = _TARGET_SP_BACKUP; // 專案指定的 key
                    //檢查 directory 是否有設定，沒有就拋出例外
                    if (string.IsNullOrEmpty(directory))
                    {
                        var errorMessage = "Directory is not set.";
                        AnsiConsole.MarkupLine($"[red]{errorMessage}[/]");
                        throw new Exception(errorMessage);
                    }
                    //檢查 directory 字串的最後一個字元是否為目錄分隔符號（在 Windows 上是 \），如果不是，則會自動新增。這樣可以確保 directory 變數的值總是以目錄分隔符號結尾。
                    if (!directory.EndsWith(Path.DirectorySeparatorChar))
                    {
                        directory += Path.DirectorySeparatorChar;
                    }

                    var spPath = $"{directory}stored_procedures.json";
                    var funcPath = $"{directory}functions.json";

                    //读取旧的存储过程和函数数据
                    var comparer = new SPComparer();
                    List<SPData>? oldSP = null;
                    List<SPData>? oldFunc = null;
                    // 檢查 funcPath 檔案是否存在
                    if (File.Exists(spPath))
                    {
                        oldSP = comparer.ReadJsonFile(spPath);
                    }
                    // 檢查 funcPath 檔案是否存在
                    if (File.Exists(funcPath))
                    {
                        oldFunc = comparer.ReadJsonFile(funcPath);
                    }

                    // 在這裡執行資料庫操作
                    var spList = Program.ExecQuerySP(queryStoredProcedures, connection);
                    var funcList = Program.ExecQuerySP(queryFunctions, connection);

                    // 將 spList 和 funcList 儲存為 .json 檔案
                    Program.SaveListAsJson(spList, spPath);
                    Program.SaveListAsJson(funcList, funcPath);

                    AnsiConsole.MarkupLine($"[yellow]{nowTime}: 檔案已成功創建。[/]");
                    AnsiConsole.MarkupLine($"[blue]{spPath}[/]");
                    AnsiConsole.MarkupLine($"[blue]{funcPath}[/]");

                    // 檢查是否有差異
                    if (GitHelper.HasDifferences(directory))
                    {
                        AnsiConsole.MarkupLine($"[red]{nowTime} : 在提交中有差異。[/]");

                        var differences = "";
                        if (oldSP != null) // 檢查是否為 null
                        {
                            var (diffList, delList) = comparer.CompareRoutineNames(oldSP, spList);
                            diffList.ForEach(sp =>
                            {
                                differences += $"+異動 SP: {sp} 、 ";
                            });
                            delList.ForEach(sp =>
                            {
                                differences += $"-刪除 SP: {sp} 、 ";
                            });
                        }
                        else
                        {
                            differences += $"SP: 【第一次建立】 、 ";
                        }
                        if (oldFunc != null) // 檢查是否為 null
                        {
                            var (diffList, delList) = comparer.CompareRoutineNames(oldFunc, funcList);
                            diffList.ForEach(func =>
                            {
                                differences += $"+異動 FN: {func} 、 ";
                            });
                            delList.ForEach(sp =>
                            {
                                differences += $"-刪除 FN: {sp} 、 ";
                            });
                        }
                        else
                        {
                            differences += $"FN: 【第一次建立】 、 ";
                        }

                        // 记录差异并准备提交到 Git
                        GitHelper.AddAndCommit($"{_TAG}: BOT 差異 Commit({differences})", directory);

                        // 發送 Discord 通知
                        _notificationHelper.SendDiscordNotification(
                            $"{_TAG}: 在提交中有差異。({differences})"
                        );

                        // 發送 TG 通知
                        _notificationHelper.SendTelegramNotification(
                            $"{_TAG}: 在提交中有差異。({differences})"
                        );

                        // 發送 Slack 通知
                        _notificationHelper.SendSlackNotification(
                            $"{_TAG}: 在提交中有差異。({differences})"
                        );
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[green]在提交中沒有差異。[/]");
                    }
                }
                catch (Exception ex)
                {
                    //异常处理
                    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                }
            }
        }

        /// <summary>
        /// SP 紀錄比對
        /// </summary>
        public class SPComparer
        {
            /// <summary>
            /// 讀取 SP 紀錄檔案 Json 轉成 List
            /// </summary>
            /// <param name="filePath"></param>
            /// <returns></returns>
            public List<SPData>? ReadJsonFile(string filePath)
            {
                var jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<SPData>>(jsonString);
            }

            /// <summary>
            /// 比對檔案差異或被刪除
            /// </summary>
            /// <param name="previousList"></param>
            /// <param name="currentList"></param>
            /// <returns></returns>
            public (List<string> diffList, List<string> delList) CompareRoutineNames(
                List<SPData> previousList,
                List<SPData> currentList
            )
            {
                var previousDefinitions = previousList
                    .Where(sp => sp.ROUTINE_NAME != null)
                    .ToDictionary(sp => sp.ROUTINE_NAME!, sp => sp.ROUTINE_DEFINITION);

                var currentDefinitions = currentList
                    .Where(sp => sp.ROUTINE_NAME != null)
                    .ToDictionary(sp => sp.ROUTINE_NAME!, sp => sp.ROUTINE_DEFINITION);

                var differences = new List<string>();

                foreach (var current in currentDefinitions)
                {
                    if (previousDefinitions.TryGetValue(current.Key, out var previousDefinition))
                    {
                        if (previousDefinition != current.Value)
                        {
                            differences.Add(current.Key);
                        }
                    }
                    else
                    {
                        differences.Add(current.Key);
                    }
                }

                var deletes = new List<string>();

                // 檢查是不是被刪除了
                foreach (var current in previousDefinitions)
                {
                    if (!currentDefinitions.TryGetValue(current.Key, out var currentDefinition))
                    {
                        deletes.Add(current.Key);
                    }
                }

                return (differences, deletes);
            }
        }
    }
}
