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
    }
    
    /// <summary>
    /// SP ¸ê®Æ
    /// </summary>
    public class SPData
    {
        public string? ROUTINE_NAME { get; set; }
        public string? ROUTINE_DEFINITION { get; set; }
    }
}
