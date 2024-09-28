using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using mssql_bot;
using mssql_bot.helper;
using Newtonsoft.Json;
using Spectre.Console;

public partial class Program
{
    /// <summary>
    /// 檢查 SP - 目前 case 是 @Output_ErrorCode 型別為 tinyint，卻給負值
    /// 命令列引數: checksp
    /// </summary>
    public static void CheckSP()
    {
        _ = App.Command(
            "checksp",
            command =>
            {
                // 第二層 Help 的標題
                command.Description =
                    "檢查 SP - 目前 case 是 @Output_ErrorCode 型別為 tinyint，卻給負值";
                command.HelpOption("-?|-h|-help");

                command.OnExecute(() =>
                {
                    var bbConfig = RedisHelper.GetValue<DBConfig>(
                        RedisHelper.TARGET_CONNECTION_STRING
                    ); // 專案指定的 key
                    if (bbConfig.connectionString == string.Empty)
                    {
                        AnsiConsole.MarkupLine($"[red]empty connectionString[/]");
                        return 0;
                    }

                    string queryStoredProcedures = DbHelper.QUERY_STOREDPROCEDURES;

                    using (var connection = new SqlConnection(bbConfig.connectionString))
                    {
                        try
                        {
                            connection.Open();
                            AnsiConsole.MarkupLine($"[yellow]Connection opened successfully.[/]");

                            // 在這裡執行資料庫操作

                            var spList = ExecQuery(queryStoredProcedures, connection);

                            var pattern = @"@Output_ErrorCode\s*=\s*-\d+";
                            Matches(spList, pattern);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
                        }
                    }

                    return 0;
                });
            }
        );
    }

    /// <summary>
    /// 存成 Json 檔案
    /// </summary>
    /// <param name="list"></param>
    /// <param name="filePath"></param>
    private static void SaveListAsJson(List<SPData> list, string filePath)
    {
        AnsiConsole.MarkupLine($"[yellow]【存成 Json 檔案】[/]");

        // 將 list 轉換成 JSON 字串
        string json = JsonConvert.SerializeObject(list, Formatting.Indented);

        // 將 JSON 字串寫入檔案
        File.WriteAllText(filePath, json, new System.Text.UTF8Encoding(true));
    }

    /// <summary>
    /// 比對 SP 內容特徵
    /// </summary>
    /// <param name="list"></param>
    /// <param name="pattern"></param>
    private static void Matches(List<SPData> list, string pattern = "")
    {
        AnsiConsole.MarkupLine($"[yellow]【比對 SP 內容特徵】[/]");

        list.ForEach(sp =>
        {
            var procedureName = sp.ROUTINE_NAME;
            var procedureCode = sp.ROUTINE_DEFINITION;

            if (procedureCode != null && procedureName != null)
            {
                // 將 [ 與 ] 替換為中文全形括號，避免 Spectre.Console 解析錯誤
                procedureCode = procedureCode.Replace("[", "【");
                procedureCode = procedureCode.Replace("]", "】");

                MatchCollection matches = Regex.Matches(procedureCode, pattern);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        AnsiConsole.MarkupLine(
                            $"[red]procedureName: {procedureName} => {match.Value}[/]"
                        );
                    }

                    // 列出 SQL 語法寫到 Redis
                    RedisHelper.SetValue(procedureName, procedureCode);

                    var parser = new TSql130Parser(false);
                    TSqlFragment fragment;
                    IList<ParseError> errors;

                    using (TextReader parseReader = new StringReader(procedureCode))
                    {
                        fragment = parser.Parse(parseReader, out errors);
                    }

                    if (errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            AnsiConsole.MarkupLine($"[red]Error: {error.Message}[/]");

                            // 列出 SQL 語法寫到 Redis
                            RedisHelper.SetValue(procedureName, procedureCode);
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(
                            $"[blue]procedureName: {procedureName} => Parsing successful![/]"
                        );

                        // 遍歷語法樹，查找輸入參數和輸出參數
                        var customVisitor = new CustomTSqlFragmentVisitor();
                        fragment.Accept(customVisitor);
                        // 列出輸入參數
                        AnsiConsole.MarkupLine($"[yellow]Input Parameters(列出輸入參數):[/]");
                        if (customVisitor.InputParams.Count > 0)
                        {
                            foreach (var param in customVisitor.InputParams)
                            {
                                AnsiConsole.MarkupLine($"[green]{param}[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]無輸入參數[/]");
                        }

                        // 列出輸出參數及其賦值情況
                        AnsiConsole.MarkupLine(
                            $"[yellow]Output Parameters and their assignments(列出輸出參數及其賦值情況):[/]"
                        );
                        if (customVisitor.OutputParams.Count > 0)
                        {
                            foreach (var param in customVisitor.OutputParams)
                            {
                                if (customVisitor.Assignments.ContainsKey(param))
                                {
                                    AnsiConsole.MarkupLine(
                                        $"[green]{param} = {customVisitor.Assignments[param]}[/]"
                                    );
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[green]{param} is not assigned.[/]");
                                }
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]無輸出參數[/]");
                        }

                        // 列出返回值
                        AnsiConsole.MarkupLine($"[yellow]Return Values(列出返回值):[/]");
                        if (customVisitor.ReturnValues.Count > 0)
                        {
                            foreach (var returnValue in customVisitor.ReturnValues)
                            {
                                AnsiConsole.MarkupLine($"[green]{returnValue}[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]無返回值[/]");
                        }
                    }
                }
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]Procedure code for procedureName({procedureName}) is null.[/]"
                );
            }
        });
    }

    /// <summary>
    /// 執行 SQL 查詢，並將結果回傳
    /// </summary>
    /// <param name="query"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    private static List<SPData> ExecQuery(string query, SqlConnection connection)
    {
        var command = new SqlCommand(query, connection);

        AnsiConsole.MarkupLine($"[yellow]【執行 SQL 查詢，並將結果回傳】[/]");

        AnsiConsole.MarkupLine($"[green]{query}[/]");

        var reader = command.ExecuteReader();

        var list = new List<SPData>();
        while (reader.Read())
        {
            var procedureName = reader["ROUTINE_NAME"].ToString();
            var procedureCode = reader["ROUTINE_DEFINITION"].ToString();

            if (procedureCode != null && procedureName != null)
            {
                list.Add(
                    new SPData { ROUTINE_NAME = procedureName, ROUTINE_DEFINITION = procedureCode }
                );
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]Procedure code for procedureName({procedureName}) is null.[/]"
                );
            }
        }

        reader.Close();
        return list;
    }
}
