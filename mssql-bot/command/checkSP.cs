using Microsoft.SqlServer.TransactSql.ScriptDom;
using mssql_bot;
using mssql_bot.helper;
using Newtonsoft.Json;
using Spectre.Console;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using static mssql_bot.helper.RedisHelper;

public partial class Program
{
    /// <summary>
    /// 檢查 SP - 目前 case 是 @Output_ErrorCode 型別為 tinyint，卻給負值
    /// 命令列引數: checksp
    /// </summary>
    private static void CheckSP()
    {
        _ = App.Command(
            "checksp",
            command =>
            {
                // 第二層 Help 的標題
                command.Description = "檢查 SP - 目前 case 是 @Output_ErrorCode 型別為 tinyint，卻給負值";
                command.HelpOption("-?|-h|-help");

                // 輸入參數說明
                var wordsArgument = command.Argument("[words]", "指定需要輸出的文字。");

                command.OnExecute(() =>
                {
                    var words = wordsArgument.HasValue ? $"_{wordsArgument.Value}" : string.Empty;
                    var bbConfig = RedisHelper.GetValue<DBConfig>(
                        RedisKeys.ConnectionString,
                        words
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

                            var spList = ExecQuerySP(queryStoredProcedures, connection);

                            var pattern = @"@Output_ErrorCode\s*=\s*\d+";
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
    public static void SaveListAsJson(List<SPData> list, string filePath)
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

                    // 列出 SQL 語法寫到 Redis (測試中資料很多，先不存)
                    //RedisHelper.SetValue(procedureName, procedureCode);

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

                            // 列出 SQL 語法寫到 Redis (測試中資料很多，先不存)
                            //RedisHelper.SetValue(procedureName, procedureCode);
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
                                    var type = "";
                                    // IntegerLiteral 類表示 T-SQL 語法中的整數字面量。例如，123 或 -456 都是整數字面量。
                                    if (
                                        customVisitor.Assignments[param]
                                        == typeof(IntegerLiteral).ToString()
                                    )
                                    {
                                        type = "數值";
                                        AnsiConsole.MarkupLine($"[green]{param} => {type}[/]");
                                    }
                                    else if (
                                        customVisitor.Assignments[param]
                                        == typeof(FunctionCall).ToString()
                                    )
                                    {
                                        // FunctionCall 類表示 T-SQL 語法中的函數調用。例如，GETDATE() 或 SUM(columnName) 都是函數調用。
                                        type = "函數";
                                        AnsiConsole.MarkupLine($"[green]{param} => {type}[/]");
                                    }
                                    else if (
                                        customVisitor.Assignments[param]
                                        == typeof(GlobalVariableExpression).ToString()
                                    )
                                    {
                                        // GlobalVariableExpression 這個類用於表示 T - SQL 中的全域變數，例如 @@VERSION 或 @@ERROR。這些變數通常用於獲取 SQL Server 的系統資訊或狀態。
                                        type = "全域變數";
                                        AnsiConsole.MarkupLine($"[green]{param} => {type}[/]");
                                    }
                                    else if (
                                        customVisitor.Assignments[param]
                                        == typeof(BinaryExpression).ToString()
                                    )
                                    {
                                        // BinaryExpression 這個類用於表示 T - SQL 中的二元運算，例如加法(+)、減法(-)、乘法(*)、除法(/) 等等。二元運算是指有兩個操作數的運算。
                                        type = "二元運算";
                                        AnsiConsole.MarkupLine($"[green]{param} => {type}[/]");
                                    }
                                    else
                                    {
                                        AnsiConsole.MarkupLine(
                                            $"[green]{param} => {customVisitor.Assignments[param]}[/]"
                                        );
                                    }
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
                                var type = "";
                                // VariableReference 這個類用於表示 T - SQL 中的變數，例如 @MyVariable。變數引用通常用於查找和操作 SQL 腳本中的變數。
                                if (returnValue.ToString() == typeof(VariableReference).ToString())
                                {
                                    type = "腳本變數";
                                    var name = ((VariableReference)returnValue).Name;
                                    AnsiConsole.MarkupLine($"[green]{name} => {type}[/]");
                                }
                                else
                                {
                                    AnsiConsole.MarkupLine($"[green]{returnValue}[/]");
                                }
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
    public static List<SPData> ExecQuerySP(string query, SqlConnection connection)
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
                if (procedureCode.Length == 0)
                {
                    // 沒有內容，可能是權限不足，不需要再處理了
                    throw new Exception($"{procedureName} 權限不足，無法取得程式碼。");
                }
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
