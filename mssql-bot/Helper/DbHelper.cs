using System.Text.Json;

namespace mssql_bot.helper
{
    /// <summary>
    ///
    /// </summary>
    public class DbHelper
    {
        public static string QUERY_STOREDPROCEDURES =
            @"
            SELECT 
            p.name AS ROUTINE_NAME, 
            m.definition AS ROUTINE_DEFINITION 
        FROM 
            sys.procedures AS p
        JOIN 
            sys.sql_modules AS m ON p.object_id = m.object_id
            ORDER BY ROUTINE_NAME ASC;
        ";

        public static string QUERY_FUNCTIONS =
            @"
            SELECT 
            f.name AS ROUTINE_NAME, 
            m.definition AS ROUTINE_DEFINITION 
        FROM 
            sys.objects AS f
        JOIN 
            sys.sql_modules AS m ON f.object_id = m.object_id
        WHERE 
            f.type IN ('FN', 'IF', 'TF')
            ORDER BY ROUTINE_NAME ASC;
        ";
    }

    /// <summary>
    /// SP ¸ê®Æ
    /// </summary>
    public class SPData
    {
        public string? ROUTINE_NAME { get; set; }
        public string? ROUTINE_DEFINITION { get; set; }
    }

    public class SPComparer
    {
        public List<SPData>? ReadJsonFile(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<SPData>>(jsonString);
        }

        public List<string> CompareRoutineNames(List<SPData> previousList, List<SPData> currentList)
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

            return differences;
        }
    }
}
