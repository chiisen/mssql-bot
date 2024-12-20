﻿using mssql_bot;
using mssql_bot.command;
using mssql_bot.helper;
using Spectre.Console;
using static mssql_bot.helper.RedisHelper;

public partial class Program
{
    /// <summary>
    /// 查詢 GCP Log
    /// 命令列引數: checklog
    /// </summary>
    private static void CheckLog()
    {
        _ = App.Command(
            "checklog",
            command =>
            {
                // 第二層 Help 的標題
                command.Description = "檢查 GCP Log";
                command.HelpOption("-?|-h|-help");

                // 輸入參數說明
                var tagArgument = command.Argument("[tag]", "指定需要顯示的標籤文字。");

                command.OnExecute(() =>
                {
                    var tag = tagArgument.HasValue ? $"_{tagArgument.Value}" : string.Empty;

                    var timer = new OnTimedEventByCheckLog
                    {
                        _YOUR_DISCORD_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Discord, tag),
                        _YOUR_TELEGRAM_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Telegram, tag),
                        _YOUR_SLACK_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Slack, tag),
                        _TARGET_CONNECTION_STRING = RedisHelper.GetValue<DBConfig>(
                            RedisKeys.ConnectionString,
                            tag
                        ),
                        _TAG = tag,
                        _PS1_PATH = RedisHelper.GetValue(RedisKeys.Ps1, tag),
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
