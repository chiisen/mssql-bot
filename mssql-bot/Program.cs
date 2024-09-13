using mssql_bot.helper;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Drawing;
using System.Text.RegularExpressions;
using Console = Colorful.Console;
using System.Text.RegularExpressions;
class Program
{
    /// <summary>
    /// DB 設定
    /// </summary>
    public class DBConfig
    {
        public string? connectionString { get; set; }
    }

    static void Main()
    {
        var bbConfig = RedisHelper.GetValue<DBConfig>("mssql-bot-connectionString");// 實際 key 為 `mssql-bot:mssql-bot-connectionString`
        if (bbConfig.connectionString == string.Empty)
        {
            Console.WriteLine($"empty connectionString", Color.Red);
            return;
        }

        string queryStoredProcedures =
            @"
            SELECT 
                ROUTINE_NAME, 
                ROUTINE_DEFINITION 
            FROM 
                INFORMATION_SCHEMA.ROUTINES 
            WHERE 
                ROUTINE_TYPE = 'PROCEDURE';
        ";

        string queryFunction =
            @"
            SELECT 
                ROUTINE_NAME, 
                ROUTINE_DEFINITION 
            FROM 
                INFORMATION_SCHEMA.ROUTINES 
            WHERE 
                ROUTINE_TYPE = 'FUNCTION';
        ";

        using (SqlConnection connection = new SqlConnection(bbConfig.connectionString))
        {
            try
            {
                connection.Open();
                Console.WriteLine("Connection opened successfully.", Color.Yellow);

                string pattern = @"@Output_ErrorCode\s*=\s*-\d+";

                // 在這裡執行資料庫操作

                JObject notebookStoredProcedures = ExecQuery(queryStoredProcedures, connection, pattern);

                JObject notebookFunction = ExecQuery(queryFunction, connection, pattern);


                var destinationFolder = @$"{Environment.CurrentDirectory}\";

                // 將 notebookStoredProcedures 儲存為 .ipynb 檔案
                File.WriteAllText($"{destinationFolder}stored_procedures.ipynb", notebookStoredProcedures.ToString(), new System.Text.UTF8Encoding(true));
                File.WriteAllText($"{destinationFolder}function.ipynb", notebookFunction.ToString(), new System.Text.UTF8Encoding(true));

                Console.WriteLine("Notebook has been created successfully.", Color.Yellow);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}", Color.Red);
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

        Console.WriteLine(query, Color.Green);

        var reader = command.ExecuteReader();

        // 建立 Jupyter Notebook 的 JSON 結構
        var notebook = new JObject(
            new JProperty("cells", new JArray()),
            new JProperty("metadata", new JObject()),
            new JProperty("nbformat", 4),
            new JProperty("nbformat_minor", 5)
        );

        JArray cells = notebook["cells"] as JArray ?? new JArray();

        while (reader.Read())
        {
            var procedureName = reader["ROUTINE_NAME"].ToString();
            var procedureCode = reader["ROUTINE_DEFINITION"].ToString();

            // 检查 procedureCode 是否为 null
            if (procedureCode != null)
            {
                MatchCollection matches = Regex.Matches(procedureCode, pattern);

                foreach (Match match in matches)
                {
                    Console.WriteLine($"procedureName: {procedureName} => \r\n{match.Value}\r\n", Color.Yellow);
                }
            }
            else
            {
                // 处理 procedureCode 为 null 的情况
                Console.WriteLine($"Procedure code for procedureName({procedureName}) is null.", Color.Red);
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
