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

        public static string QUERY_TS_CLUB = @"
        SELECT UnitKey, Flag_id, Game_id, TuiSui FROM dbo.FN_GetClubTuiSuiByClub_id('2005293654')
        ";// @Club_id

        public static string QUERY_TS_UNIT = @"
        SELECT UnitKey, Tag_Id, TuiSui, Game_id FROM dbo.FN_GetAncestorsTuiSuiByUnitKey('131046')
        ";// @UnitKey => UnitHID 暫時無法解析
    }

    /// <summary>
    /// SP 資料
    /// </summary>
    public class SPData
    {
        public string? ROUTINE_NAME { get; set; }
        public string? ROUTINE_DEFINITION { get; set; }
    }

    /// <summary>
    /// TS Club 資料
    /// </summary>
    public class TS_ClubData
    {
        /// <summary>
        /// 
        /// </summary>
        public string? UnitKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? Flag_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? Game_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal TuiSui { get; set; }
    }

    /// <summary>
    /// TS Unit 資料
    /// </summary>
    public class TS_UnitData
    {
        /// <summary>
        /// 
        /// </summary>
        public string? UnitKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? Tag_Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? UnitHID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal TuiSui { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? Game_id { get; set; }
    }
}
