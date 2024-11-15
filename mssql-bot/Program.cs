using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;

partial class Program
{
    private static string? _currentPath = "";
    private static readonly CommandLineApplication App = new() { Name = "mssql-bot" };

    /// <summary>
    /// 主迴圈
    /// </summary>
    static int Main(string[] args)
    {
        // 指定輸出為 UTF8
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        #region 顯示執行路徑

        var assem = Assembly.GetEntryAssembly();
        if (assem is not null)
        {
            _currentPath = Path.GetDirectoryName(assem.Location);
        }
        AnsiConsole.MarkupLine($"[blue]執行路徑: {_currentPath}[/]");
        AnsiConsole.MarkupLine($"[blue]================[/]");

        #endregion 顯示執行路徑


        #region 【Logger 輸入參數】

        // ! Logger 輸入參數
        var argString = args.Aggregate("", (current, arg) => current + (arg + " "));
        AnsiConsole.MarkupLine($"[blue]輸入參數:mssql-bot {argString}[/]");
        AnsiConsole.MarkupLine($"[blue]================[/]");

        #endregion 【Logger 輸入參數】


        #region 【設定 Help】

        // ! 設定 Help
        App.HelpOption("-?|-h|--help");
        App.OnExecute(() =>
        {
            App.ShowHelp();
            return 0;
        });

        #endregion 【設定 Help】


        #region 【註冊 Command】
        Example();

        CheckSP();

        CheckDiffSP();

        CheckTS();

        CheckLog();
        #endregion 【註冊 Command】


        try
        {
            App.Execute(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]發生錯誤:{ex.Message}[/]");
            return 1;
        }

        return 0;
    }
}
