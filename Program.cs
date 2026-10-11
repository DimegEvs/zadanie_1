using WukongBenchAuto;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Консольный режим: --cli для автоматического прогона без окна.
        if (args.Contains("--cli"))
            return CliProgram.Run(args).GetAwaiter().GetResult();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
