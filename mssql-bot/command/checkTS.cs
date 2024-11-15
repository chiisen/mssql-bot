using mssql_bot;
using mssql_bot.command;
using mssql_bot.helper;
using Spectre.Console;
using System.Data.SqlClient;
using static mssql_bot.helper.RedisHelper;

public partial class Program
{
    /// <summary>
    /// 檢查 TS 的異動
    /// 命令列引數: checkts
    /// </summary>
    private static void CheckTS()
    {
        _ = App.Command(
            "checkts",
            command =>
            {
                // 第二層 Help 的標題
                command.Description = "檢查 TS 的異動";
                command.HelpOption("-?|-h|-help");

                // 輸入參數說明
                var tagArgument = command.Argument("[tag]", "指定需要顯示的標籤文字。");

                command.OnExecute(() =>
                {
                    var tag = tagArgument.HasValue ? $"_{tagArgument.Value}" : string.Empty;

                    var timer = new OnTimedEventByCheckTS
                    {
                        _YOUR_DISCORD_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Discord, tag),
                        _YOUR_TELEGRAM_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Telegram, tag),
                        _YOUR_SLACK_WEBHOOK_URL = RedisHelper.GetValue(RedisKeys.Slack, tag),
                        _TARGET_CONNECTION_STRING = RedisHelper.GetValue<DBConfig>(
                            RedisKeys.ConnectionString,
                            tag
                        ),
                        _TAG = tag
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

    /// <summary>
    /// 執行 SQL 查詢，並將結果回傳
    /// </summary>
    /// <param name="query"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    public static List<LastLoginData> ExecQueryLastLoginTS(string query, SqlConnection connection)
    {
        var command = new SqlCommand(query, connection);

        AnsiConsole.MarkupLine($"[yellow]【執行 SQL 查詢，並將結果回傳】[/]");

        AnsiConsole.MarkupLine($"[green]{query}[/]");

        var reader = command.ExecuteReader();

        var list = new List<LastLoginData>();
        while (reader.Read())
        {
            var club_id = reader["CLUB_ID"].ToString();
            var update_time = Convert.ToDateTime(reader["UPDATE_TIME"]).ToString("yyyy-MM-dd HH:mm:ss");
            var ip = reader["IP"].ToString();

            if (club_id != null && update_time != null && ip != null )
            {
                if (club_id.Length == 0)
                {
                    // 沒有內容，可能是權限不足，不需要再處理了
                    throw new Exception($"{club_id} 權限不足，無法取得程式碼。");
                }
                list.Add(
                    new LastLoginData
                    {
                        CLUB_ID = club_id,
                        UPDATE_TIME = update_time,
                        IP = ip
                    }
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]CLUB_ID ({club_id}) is null.[/]"
                );
            }
        }

        reader.Close();
        return list;
    }

    /// <summary>
    /// 執行 SQL 查詢，並將結果回傳
    /// </summary>
    /// <param name="query"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    public static List<TS_ClubData> ExecQueryClubTS(string query, SqlConnection connection)
    {
        var command = new SqlCommand(query, connection);

        AnsiConsole.MarkupLine($"[yellow]【執行 SQL 查詢，並將結果回傳】[/]");

        AnsiConsole.MarkupLine($"[green]{query}[/]");

        var reader = command.ExecuteReader();

        var list = new List<TS_ClubData>();
        while (reader.Read())
        {
            var unitKey = reader["UnitKey"].ToString();
            var flag_id = reader["Flag_id"].ToString();
            var game_id = reader["Game_id"].ToString();
            var tuiSui = reader["TuiSui"].ToString();

            if (unitKey != null && flag_id != null && game_id != null && tuiSui != null)
            {
                if (unitKey.Length == 0)
                {
                    // 沒有內容，可能是權限不足，不需要再處理了
                    throw new Exception($"{unitKey} 權限不足，無法取得程式碼。");
                }
                list.Add(
                    new TS_ClubData
                    {
                        UnitKey = unitKey,
                        Flag_id = flag_id,
                        Game_id = game_id,
                        TuiSui = decimal.Parse(tuiSui!)
                    }
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]unitKey ({unitKey}), flag_id ({flag_id}), game_id ({game_id}), tuiSui ({tuiSui}) is null.[/]"
                );
            }
        }

        reader.Close();
        return list;
    }

    /// <summary>
    /// 執行 SQL 查詢，並將結果回傳
    /// </summary>
    /// <param name="query"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    public static List<TS_UnitData> ExecQueryUnitTS(string query, SqlConnection connection)
    {
        var command = new SqlCommand(query, connection);

        AnsiConsole.MarkupLine($"[yellow]【執行 SQL 查詢，並將結果回傳】[/]");

        AnsiConsole.MarkupLine($"[green]{query}[/]");

        var reader = command.ExecuteReader();

        var list = new List<TS_UnitData>();
        while (reader.Read())
        {
            var unitKey = reader["UnitKey"].ToString();
            var tag_Id = reader["Tag_Id"].ToString();
            var tuiSui = reader["TuiSui"].ToString();
            var game_id = reader["Game_id"].ToString();

            if (unitKey != null && tag_Id != null && game_id != null && tuiSui != null)
            {
                if (unitKey.Length == 0)
                {
                    // 沒有內容，可能是權限不足，不需要再處理了
                    throw new Exception($"{unitKey} 權限不足，無法取得程式碼。");
                }
                list.Add(
                    new TS_UnitData
                    {
                        UnitKey = unitKey,
                        Tag_Id = tag_Id,
                        Game_id = game_id,
                        TuiSui = decimal.Parse(tuiSui!)
                    }
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]unitKey ({unitKey}), tag_Id ({tag_Id}), game_id ({game_id}), TuiSui ({tuiSui}) is null.[/]"
                );
            }
        }

        reader.Close();
        return list;
    }
}
