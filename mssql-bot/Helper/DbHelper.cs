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

        public static string QUERY_LAST_LOGIN = @"
        SELECT TOP (1000) 
        l.CLUB_ID,
        l.UPDATE_TIME,
        l.IP,
        c.Club_Ename,
        c.Club_Cname,
        c.Franchiser_id,
        c.Password,
        c.Max_XinYong,
        c.Now_XinYong,
        c.Datetime,
        c.Active,
        c.PanZu,
        c.MAC,
        c.JieSuan_Time,
        c.Login,
        c.OnlineTime,
        c.ChongZhi,
        c.VIP_Flag,
        c.Login_Game_Id,
        c.Login_Server_Id,
        c.TingYong_XinYong,
        c.Lock,
        c.Open_Server_id,
        c.DongJie_Flag,
        c.msrepl_tran_version,
        c.Logout_Xinyong,
        c.Login_EGame,
        c.Login_Room,
        c.UidKey,
        c.FKey,
        c.LimitLevel,
        c.PlayerReturnTime,
        c.Test_Flag,
        c.UnitKey
    FROM 
        HKNetGame_HJ.dbo.T_AloneLogin_Club_LastLogin l
    INNER JOIN 
        HKNetGame_HJ.dbo.T_Club c ON l.CLUB_ID = c.Club_id
    WHERE 
        l.UPDATE_TIME >= @StartTime
        ";

        public static string QUERY_TS_CLUB = @"
        SELECT UnitKey, Flag_id, Game_id, TuiSui FROM dbo.FN_GetClubTuiSuiByClub_id(@Club_id)
        ";

        public static string QUERY_TS_UNIT = @"
        SELECT UnitKey, Tag_Id, TuiSui, Game_id FROM dbo.FN_GetAncestorsTuiSuiByUnitKey(@UnitKey)
        ";// UnitHID 暫時無法解析
    }

    /// <summary>
    /// SP 資料
    /// </summary>
    public class SPData
    {
        /// <summary>
        /// 名稱
        /// </summary>
        public string? ROUTINE_NAME { get; set; }
        /// <summary>
        /// 內容
        /// </summary>
        public string? ROUTINE_DEFINITION { get; set; }
    }

    /// <summary>
    /// 最後登入時間
    /// </summary>

    public class LastLoginData
    {
        /// <summary>
        /// 
        /// </summary>
        public string? CLUB_ID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? UPDATE_TIME { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string? IP { get; set; }
        /// <summary>
        /// 線別
        /// </summary>
        public string? PanZu { get; set; }
        /// <summary>
        /// 玩家帳號名稱
        /// </summary>
        public string? Club_Ename { get; set; }
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
