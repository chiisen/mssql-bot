using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Timers;
using mssql_bot.helper;
using mssql_bot.Helper;
using Spectre.Console;

namespace mssql_bot.command
{
    /// <summary>
    /// 事件處理
    /// </summary>
    public class OnTimedEventByCheckDiffSP
    {
        public System.Timers.Timer? _TIMER;
        public string _YOUR_DISCORD_WEBHOOK_URL = "YOUR_DISCORD_WEBHOOK_URL"; // 設定你的 Discord Webhook URL
        public DBConfig _TARGET_CONNECTION_STRING = new DBConfig();
        public string _TARGET_SP_BACKUP = "git 的備份目錄";
        public string _WORLDS = "";

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

            string queryStoredProcedures = DbHelper.QUERY_STOREDPROCEDURES;
            string queryFunctions = DbHelper.QUERY_FUNCTIONS;

            using (var connection = new SqlConnection(bbConfig.connectionString))
            {
                try
                {
                    connection.Open();
                    AnsiConsole.MarkupLine($"[yellow]Connection opened successfully.[/]");

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
                    var spList = Program.ExecQuery(queryStoredProcedures, connection);
                    var funcList = Program.ExecQuery(queryFunctions, connection);

                    // 將 spList 和 funcList 儲存為 .json 檔案
                    Program.SaveListAsJson(spList, spPath);
                    Program.SaveListAsJson(funcList, funcPath);

                    AnsiConsole.MarkupLine($"[yellow]Files have been created successfully.[/]");
                    AnsiConsole.MarkupLine($"[blue]{spPath}[/]");
                    AnsiConsole.MarkupLine($"[blue]{funcPath}[/]");

                    // 檢查是否有差異
                    if (GitHelper.HasDifferences(directory))
                    {
                        AnsiConsole.MarkupLine($"[red]There are differences in the commit.[/]");

                        var differences = "";
                        if (oldSP != null) // 檢查是否為 null
                        {
                            comparer
                                .CompareRoutineNames(oldSP, spList)
                                .ForEach(sp =>
                                {
                                    differences += $"SP: {sp} 、 ";
                                });
                        }
                        else
                        {
                            differences += $"SP: 【第一次建立】 、 ";
                        }
                        if (oldFunc != null) // 檢查是否為 null
                        {
                            comparer
                                .CompareRoutineNames(oldFunc, funcList)
                                .ForEach(func =>
                                {
                                    differences += $"FN: {func} 、 ";
                                });
                        }
                        else
                        {
                            differences += $"FN: 【第一次建立】 、 ";
                        }

                        // 记录差异并准备提交到 Git
                        GitHelper.AddAndCommit($"BOT 差異 Commit({differences})", directory);

                        // 發送 Discord 通知
                        SendDiscordNotification(
                            $"{_WORLDS}: There are differences in the commit.({differences})"
                        );
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(
                            $"[green]There are not differences in the commit.[/]"
                        );
                    }
                }
                catch (Exception ex)
                {
                    //异常处理
                    AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                }
            }
        }

        private async void SendDiscordNotification(string message)
        {
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
    }
}
