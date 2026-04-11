namespace KeyloggerDetection.Evaluation;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        EvaluationOptions options;

        try
        {
            options = EvaluationOptions.Parse(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(EvaluationOptions.UsageText);
            return 1;
        }

        try
        {
            var orchestrator = new EvaluationOrchestrator(options);
            await orchestrator.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Evaluation run failed:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
