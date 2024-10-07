using mssql_bot;
using mssql_bot.command;
using mssql_bot.helper;
using Spectre.Console;
using static mssql_bot.helper.RedisHelper;

public partial class Program
{
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

                // 輸入參數說明
                var wordsArgument = command.Argument("[words]", "指定需要輸出的文字。");

                command.OnExecute(() =>
                {
                    var words = wordsArgument.HasValue ? $"_{wordsArgument.Value}" : string.Empty;

                    var timer = new OnTimedEventByCheckDiffSP
                    {
                        _YOUR_DISCORD_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Discord, words),
                        _TARGET_CONNECTION_STRING = RedisHelper.GetValue<DBConfig>(
                            RedisKeys.ConnectionString,
                            words
                        ),
                        _TARGET_SP_BACKUP = RedisHelper.GetValue(RedisKeys.Backup, words),
                        _WORLDS = words
                    };

                    // 設定 Timer，每10分鐘執行一次
                    timer.OnStart(600000);

                    AnsiConsole.MarkupLine($"[yellow]Press 'Esc' to exit the program.[/]");

                    // 監聽 Esc 鍵
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Escape)
                            {
                                timer.OnStop();
                                break;
                            }
                        }
                        // 等待 200 ms
                        Thread.Sleep(200);
                    }

                    return 0;
                });
            }
        );
    }
}
