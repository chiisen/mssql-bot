using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Timers;
using mssql_bot;
using mssql_bot.helper;
using mssql_bot.Helper;
using Spectre.Console;

public partial class Program
{
    private static System.Timers.Timer? _timer;
    private static string discordWebhookUrl = "YOUR_DISCORD_WEBHOOK_URL"; // 設定你的 Discord Webhook URL

    /// <summary>
    /// 檢查 SP 的異動
    /// 命令列引數: checksp
    /// </summary>
    public static void CheckDiffSP()
    {
        _ = App.Command(
            "checkdiffsp",
            command =>
            {
                // 第二層 Help 的標題
                command.Description = "檢查 SP 的異動";
                command.HelpOption("-?|-h|-help");

                command.OnExecute(() =>
                {
                    discordWebhookUrl = RedisHelper.GetValue(RedisHelper.DISCORD);

                    // 設定 Timer，每10分鐘執行一次
                    _timer = new System.Timers.Timer(600000);
                    _timer.Elapsed += OnTimedEvent;
                    _timer.AutoReset = true;
                    _timer.Enabled = true;

                    AnsiConsole.MarkupLine($"[yellow]Press 'Esc' to exit the program.[/]");

                    // 監聽 Esc 鍵
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Escape)
                            {
                                _timer.Stop();
                                _timer.Dispose();
                                AnsiConsole.MarkupLine($"[yellow]Program terminated by user.[/]");
                                break;
                            }
                        }
                    }

                    return 0;
                });
            }
        );
    }

    private static void OnTimedEvent(Object? source, ElapsedEventArgs e)
    {
        var bbConfig = RedisHelper.GetValue<DBConfig>(RedisHelper.TARGET_CONNECTION_STRING); // 專案指定的 key
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

                var directory = RedisHelper.GetValue(RedisHelper.TARGET_SP_BACKUP); // 專案指定的 key
                var spPath = $"{directory}stored_procedures.json";
                var funcPath = $"{directory}functions.json";

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
                var spList = ExecQuery(queryStoredProcedures, connection);
                var funcList = ExecQuery(queryFunctions, connection);

                // 將 spList 和 funcList 儲存為 .json 檔案
                SaveListAsJson(spList, spPath);
                SaveListAsJson(funcList, funcPath);

                AnsiConsole.MarkupLine($"[yellow]Files have been created successfully.[/]");
                AnsiConsole.MarkupLine($"[blue]{spPath}[/]");
                AnsiConsole.MarkupLine($"[blue]{funcPath}[/]");

                // 檢查是否有差異
                if (GitHelper.HasDifferences(directory))
                {
                    AnsiConsole.MarkupLine($"[red]There are differences in the commit.[/]");
                    GitHelper.AddAndCommit("BOT 差異 Commit", directory);

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

                    // 發送 Discord 通知
                    SendDiscordNotification($"There are differences in the commit.({differences})");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]There are not differences in the commit.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            }
        }
    }

    private static async void SendDiscordNotification(string message)
    {
        using (var client = new HttpClient())
        {
            var content = new StringContent(
                $"{{\"content\": \"{message}\"}}",
                Encoding.UTF8,
                "application/json"
            );
            await client.PostAsync(discordWebhookUrl, content);
        }
    }
}
