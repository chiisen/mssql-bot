using Newtonsoft.Json;
using Spectre.Console;
using StackExchange.Redis;
using System.Reflection;

namespace mssql_bot.helper
{
    /// <summary>
    ///
    /// </summary>
    public class RedisHelper
    {
        public enum RedisKeys
        {
            /// <summary>
            /// DB 連線字串，實際 key 為 `mssql-bot:mssql-bot-connectionString`
            /// </summary>
            ConnectionString,

            /// <summary>
            /// 本地備份目錄，實際 key 為 `mssql-bot:mssql-bot-backup`
            /// </summary>
            Backup,

            /// <summary>
            /// Discord Webhook URL，實際 key 為 `mssql-bot:mssql-bot-discord`
            /// </summary>
            Discord,

            /// <summary>
            /// Telegram Webhook URL，實際 key 為 `mssql-bot:mssql-bot-telegram`
            /// </summary>
            Telegram,
        }

        public static Dictionary<RedisKeys, string> RedisKeyDictionary = new Dictionary<
            RedisKeys,
            string
        >
        {
            { RedisKeys.ConnectionString, "mssql-bot-connectionString" },
            { RedisKeys.Backup, "mssql-bot-backup" },
            { RedisKeys.Discord, "mssql-bot-discord" },
            { RedisKeys.Telegram, "mssql-bot-telegram" },
        };

        protected static IDatabase? Redis { get; set; }
        protected static IDatabase? OtherRedis { get; set; }
        public static bool IsInitialized { get; private set; }

        private static void LazyInitializer(int db = 0)
        {
            if (IsInitialized is true)
            {
                return;
            }

            const string connectionString =
                "127.0.0.1:6379, abortConnect=false, connectRetry=5, connectTimeout=5000, syncTimeout=5000";
            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;
            options.ReconnectRetryPolicy = new ExponentialRetry(3000);

            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
            Redis = connectionMultiplexer.GetDatabase(db);

            IsInitialized = true;
        }

        public static string GetProjectName()
        {
            // 取得目前執行的 assembly
            var asm = Assembly.GetExecutingAssembly();

            // 取得 assembly 的名稱
            return asm?.GetName()?.Name ?? string.Empty;
        }

        /// <summary>
        /// 用 key 取得 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static T GetValue<T>(string? cacheKey, int db = 0)
        {
            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }
            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var data = Redis!.StringGet($"{assemblyName}:{cacheKey}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? JsonConvert.DeserializeObject<T>(data!) : default;
            return info!;
        }

        /// <summary>
        /// 用 key 取得 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static T GetValue<T>(RedisKeys enumKey, int db = 0)
        {
            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var key = RedisKeyDictionary[enumKey];

            var data = Redis!.StringGet($"{assemblyName}:{key}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? JsonConvert.DeserializeObject<T>(data!) : default;
            return info!;
        }

        /// <summary>
        /// 用 key 取得 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static T GetValue<T>(RedisKeys enumKey, string words, int db = 0)
        {
            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var key = RedisKeyDictionary[enumKey];
            var newKey = $"{key}{words}";

            var data = Redis!.StringGet($"{assemblyName}:{newKey}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? JsonConvert.DeserializeObject<T>(data!) : default;
            return info!;
        }

        /// <summary>
        /// 用 key 取得 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string GetValue(string? cacheKey, int db = 0)
        {
            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }

            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var data = Redis!.StringGet($"{assemblyName}:{cacheKey}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? data.ToString() : string.Empty;
            return info!;
        }

        /// <summary>
        /// 用 key 取得 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string GetValue(RedisKeys enumKey, int db = 0)
        {
            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var key = RedisKeyDictionary[enumKey];

            var data = Redis!.StringGet($"{assemblyName}:{key}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? data.ToString() : string.Empty;
            return info!;
        }

        /// <summary>
        /// 用 key 取得 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string GetValue(RedisKeys enumKey, string words, int db = 0)
        {
            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var key = RedisKeyDictionary[enumKey];
            var newKey = $"{key}{words}";

            var data = Redis!.StringGet($"{assemblyName}:{newKey}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? data.ToString() : string.Empty;
            return info!;
        }

        /// <summary>
        /// 用 key 設定 value，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="db"></param>
        public static void SetValue<T>(string? cacheKey, T value, int db = 0)
        {
            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }

            LazyInitializer(db);

            var assemblyName = GetProjectName();

            var key = $"{assemblyName}:{cacheKey}";

            if (value?.GetType() == typeof(string))
            {
                Redis!.StringSet($"{key}", value.ToString());
                return;
            }
            var data = JsonConvert.SerializeObject(value);

            Redis!.StringSet($"{key}", data);
        }

        /// <summary>
        /// 刪除本地 Redis 的 Key，目前 Redis 的 Key 值會加上專案的名稱，例如: mssql-bot:add-customer
        /// </summary>
        /// <param name="connectionString">Redis連線字串</param>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        public static void KeyDelete(string? cacheKey, int db = 0)
        {
            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }

            var assemblyName = GetProjectName();

            var key = $"{assemblyName}:{cacheKey}";

            OtherRedis!.KeyDelete(key);
        }

        /// <summary>
        /// 刪除指定 Redis 的 Key(不一定是本地，所以需要連線字串)
        /// </summary>
        /// <param name="connectionString">Redis連線字串</param>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        public static void OtherKeyDelete(string? connectionString, string? cacheKey, int db = 0)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }

            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;
            options.ReconnectRetryPolicy = new ExponentialRetry(3000);

            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
            OtherRedis = connectionMultiplexer.GetDatabase(db);
            OtherRedis!.KeyDelete(cacheKey);
        }

        /// <summary>
        /// 指定 Redis 的 key 取得 value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static T GetOtherValue<T>(string? connectionString, string? cacheKey, int db = 0)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }

            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;
            options.ReconnectRetryPolicy = new ExponentialRetry(3000);

            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
            OtherRedis = connectionMultiplexer.GetDatabase(db);

            var data = OtherRedis!.StringGet($"{cacheKey}");
            if (data == RedisValue.EmptyString)
            {
                AnsiConsole.MarkupLine($"[red]empty data[/]");
                return default!;
            }

            var info = data.HasValue ? JsonConvert.DeserializeObject<T>(data!) : default;
            return info!;
        }

        /// <summary>
        /// 用指定 Redis 的 key 設定 value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="value"></param>
        /// <param name="db"></param>
        public static void SetOtherValue<T>(
            string? connectionString,
            string? cacheKey,
            T value,
            int db = 0
        )
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (cacheKey == null)
            {
                throw new ArgumentNullException(nameof(cacheKey));
            }

            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;
            options.ReconnectRetryPolicy = new ExponentialRetry(3000);

            IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
            OtherRedis = connectionMultiplexer.GetDatabase(db);

            if (value?.GetType() == typeof(string))
            {
                OtherRedis!.StringSet($"{cacheKey}", value.ToString());
                return;
            }
            var data = JsonConvert.SerializeObject(value);

            Redis!.StringSet($"{cacheKey}", data);
        }
    }
}
