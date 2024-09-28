using System.Diagnostics;

namespace mssql_bot.Helper
{
    public class GitHelper
    {
        public static bool HasDifferences(string directory)
        {
            // 假設這個方法會檢查指定目錄中的 Git 狀態，並返回是否有差異
            // 這裡可以使用 git status 或其他方式來檢查
            // 這是一個簡單的範例，實際實作可能需要更複雜的邏輯
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = directory
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !string.IsNullOrEmpty(output);
        }

        public static void AddAndCommit(string commitMessage, string workingDirectory)
        {
            try
            {
                // 執行 git add .
                ExecuteGitCommand("add .", workingDirectory);

                // 執行 git commit
                ExecuteGitCommand($"commit -m \"{commitMessage}\"", workingDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private static void ExecuteGitCommand(string command, string workingDirectory)
        {
            // 建立 ProcessStartInfo 物件
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory // 設定工作目錄
            };

            // 建立 Process 物件
            using (var process = new Process { StartInfo = processStartInfo })
            {
                // 啟動 Process
                process.Start();

                // 讀取輸出
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // 等待 Process 結束
                process.WaitForExit();

                // 輸出結果
                Console.WriteLine("Output: " + output);
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Error: " + error);
                }
            }
        }
    }
}
