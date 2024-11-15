using System.Diagnostics;

namespace mssql_bot.Helper
{
    /// <summary>
    /// PowerShell 執行器
    /// </summary>
    public class PowerShellExecutor
    {
        public static string ExecutePowerShellCommand(string command)
        {
            try
            {
                // 建立一個新的 ProcessStartInfo 物件
                var psi = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 建立並啟動 Process
                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();

                    // 讀取標準輸出和錯誤輸出
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"PowerShell command returned an error: {error}");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occurred while executing PowerShell command: {ex.Message}"
                );
            }
        }

        public static string ExecutePowerShellScript(string scriptPath)
        {
            try
            {
                // 建立一個新的 ProcessStartInfo 物件
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 建立並啟動 Process
                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();

                    // 讀取標準輸出和錯誤輸出
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"PowerShell script returned an error: {error}");
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occurred while executing PowerShell script: {ex.Message}"
                );
            }
        }
    }
}
