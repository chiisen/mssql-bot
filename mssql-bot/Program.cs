using Microsoft.SqlServer.TransactSql.ScriptDom;
using mssql_bot;
using mssql_bot.helper;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.Data.SqlClient;
using System.Text.RegularExpressions;


class Program
{
    /// <summary>
    /// 主迴圈
    /// </summary>
    static void Main()
    {
        var bbConfig = RedisHelper.GetValue<DBConfig>("mssql-bot-connectionString");// 實際 key 為 `mssql-bot:mssql-bot-connectionString`
        if (bbConfig.connectionString == string.Empty)
        {
            AnsiConsole.MarkupLine($"[red]empty connectionString[/]");
            return;
        }

        string queryStoredProcedures =
            @"
            SELECT 
            p.name AS ROUTINE_NAME, 
            m.definition AS ROUTINE_DEFINITION 
        FROM 
            sys.procedures AS p
        JOIN 
            sys.sql_modules AS m ON p.object_id = m.object_id;
        ";

        using (var connection = new SqlConnection(bbConfig.connectionString))
        {
            try
            {
                connection.Open();
                AnsiConsole.MarkupLine($"[yellow]Connection opened successfully.[/]");

                var pattern = @"@Output_ErrorCode\s*=\s*-\d+";

                // 在這裡執行資料庫操作

                var notebookStoredProcedures = ExecQuery(queryStoredProcedures, connection, pattern);


                var destinationFolder = @$"{Environment.CurrentDirectory}\";

                // 將 notebookStoredProcedures 儲存為 .ipynb 檔案
                File.WriteAllText($"{destinationFolder}stored_procedures.ipynb", notebookStoredProcedures.ToString(), new System.Text.UTF8Encoding(true));

                AnsiConsole.MarkupLine($"[yellow]Notebook has been created successfully.[/]");
            }
             catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            }
        }
    }

    /// <summary>
    /// 執行 SQL 查詢，並將結果轉換為 Jupyter Notebook 的 JSON 結構
    /// </summary>
    /// <param name="query"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    private static JObject ExecQuery(string query, SqlConnection connection, string pattern = "")
    {
        var command = new SqlCommand(query, connection);

        AnsiConsole.MarkupLine($"[green]{query}[/]");

        var reader = command.ExecuteReader();

        // 建立 Jupyter Notebook 的 JSON 結構
        var notebook = new JObject(
            new JProperty("cells", new JArray()),
            new JProperty("metadata", new JObject()),
            new JProperty("nbformat", 4),
            new JProperty("nbformat_minor", 5)
        );

        var cells = notebook["cells"] as JArray ?? new JArray();

        while (reader.Read())
        {
            var procedureName = reader["ROUTINE_NAME"].ToString();
            var procedureCode = reader["ROUTINE_DEFINITION"].ToString();
            
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
                        AnsiConsole.MarkupLine($"[red]procedureName: {procedureName} => {match.Value}[/]");
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
                        AnsiConsole.MarkupLine($"[blue]procedureName: {procedureName} => Parsing successful![/]");

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
                        AnsiConsole.MarkupLine($"[yellow]Output Parameters and their assignments(列出輸出參數及其賦值情況):[/]");
                        if (customVisitor.OutputParams.Count > 0)
                        {
                            foreach (var param in customVisitor.OutputParams)
                            {
                                if (customVisitor.Assignments.ContainsKey(param))
                                {
                                    AnsiConsole.MarkupLine($"[green]{param} = {customVisitor.Assignments[param]}[/]");
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
                AnsiConsole.MarkupLine($"[red]Procedure code for procedureName({procedureName}) is null.[/]");
            }

            var cell = new JObject(
                new JProperty("cell_type", "code"),
                new JProperty("metadata", new JObject(new JProperty("language", "sql"))),
                new JProperty("source", new JArray($"-- {procedureName}\n{procedureCode}")),
                new JProperty("outputs", new JArray()),
                new JProperty("execution_count", (JArray?)null)
            );

            cells.Add(cell);
        }

        reader.Close();
        return notebook;
    }
}
