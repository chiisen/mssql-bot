# mssql-bot
MS-SQL 語法檢查機器人  

# 使用情境
最近在使用 DBA 提供的儲存程序（SP）進行調用時，發生 SP 執行時的例外情況。  
為了解決這個問題，我寫了一個簡單的機器人，用於對 SP 或函數進行簡單的除錯，從而避免在應用程序呼叫 SP 或函數時發生例外。  

# 使用 SQL 語法
目前使用語法如下:  
```sql
SELECT 
    ROUTINE_NAME, 
    ROUTINE_DEFINITION 
FROM 
    INFORMATION_SCHEMA.ROUTINES 
WHERE 
    ROUTINE_TYPE = 'PROCEDURE';
```
與
```sql
SELECT 
    ROUTINE_NAME, 
    ROUTINE_DEFINITION 
FROM 
    INFORMATION_SCHEMA.ROUTINES 
WHERE 
    ROUTINE_TYPE = 'FUNCTION';
```

# 輸出格式
格視為 Jupyter Notebook，VSCode 有支持此格式的套件可使用，並且可以直接開啟 `*.ipynb` 的檔案  
1. stored_procedures.ipynb
2. function.ipynb

# 執行方法
必須本地安裝 Redis
並建立 key 為 `mssql-bot:mssql-bot-connectionString`
內容如下:  
```json
{
    "connectionString": "Data Source=【連線網址】;Initial Catalog=【資料庫名稱】;User ID=【帳號】;Password=【密碼】;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;Pooling=true;Min Pool Size=10;Max Pool Size=150;"
}
```
![執行畫面](./images/MSSQL-BOT.png)
