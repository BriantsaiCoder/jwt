using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;

namespace JwtAuthApi.Benchmarks;

class Program
{
  static void Main(string[] args)
  {
    var config = DefaultConfig.Instance
        .AddExporter(HtmlExporter.Default)
        .AddExporter(MarkdownExporter.GitHub)
        .AddExporter(CsvExporter.Default);

    // 如果有參數，使用 BenchmarkSwitcher 讓使用者選擇要執行的基準測試
    if (args.Length > 0)
    {
      BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
    else
    {
      // 預設執行所有基準測試
      Console.WriteLine("執行所有基準測試...");
      Console.WriteLine("若要選擇特定基準測試，請使用參數執行（例如：--filter *Token*）");
      Console.WriteLine();

      BenchmarkRunner.Run<TokenGenerationBenchmarks>(config);
      BenchmarkRunner.Run<PasswordHashingBenchmarks>(config);
      BenchmarkRunner.Run<RefreshTokenRotationBenchmarks>(config);
    }
  }
}
