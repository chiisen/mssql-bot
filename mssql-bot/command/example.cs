using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;

public partial class Program
{
    /// <summary>
    /// 範例程式
    /// 命令列引數: example "words" -r 10
    /// </summary>
    public static void Example()
    {
        _ = App.Command(
            "example",
            command =>
            {
                // 第二層 Help 的標題
                command.Description = "example 說明";
                command.HelpOption("-?|-h|-help");

                // 輸入參數說明
                var wordsArgument = command.Argument("[words]", "指定需要輸出的文字。");

                // 輸入指令說明
                var repeatOption = command.Option(
                    "-r|--repeat-count",
                    "指定輸出重複次數",
                    CommandOptionType.SingleValue
                );

                command.OnExecute(() =>
                {
                    var words = wordsArgument.HasValue ? wordsArgument.Value : "world";

                    var count = repeatOption.HasValue() ? repeatOption.Value() : "1";

                    AnsiConsole.MarkupLine($"[yellow]example => words: {words}, count: {count}[/]");

                    return 0;
                });
            }
        );
    }
}
