using Elastic.Apm.SerilogEnricher;
using Serilog;


namespace Franz.Common.Logging.Tracing;

public static class TraceHelper
{
  public static void LogConsole()
  {
    Log.Logger = new LoggerConfiguration()
       .Enrich.WithEnvironmentUserName()  // Adds the current environment's user name
       .Enrich.WithElasticApmCorrelationInfo()
       .WriteTo.Console(outputTemplate: "[{ElasticApmTraceId} {ElasticApmTransactionId} {Level:u3}] {Message:lj} {NewLine}{Exception}")
       .CreateLogger();
  }

  public static void LogTofile()
  {
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()  // Ensures that context-specific properties are included in the logs
        .Enrich.WithMachineName()  // Adds the machine name to each log entry
        .Enrich.WithEnvironmentUserName()  // Adds the current environment's user name
        .Enrich.WithElasticApmCorrelationInfo()  // Adds Elastic APM correlation info for tracing
        .Enrich.WithProperty("Application", "YourApplicationName")  // Adds a custom property for the application name
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {MachineName} {EnvironmentUserName} {Application} {ElasticApmTraceId} {ElasticApmTransactionId}] {Message:lj}{NewLine}{Exception}")  // Console sink with detailed output template
        .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {MachineName} {EnvironmentUserName} {Application} {ElasticApmTraceId} {ElasticApmTransactionId}] {Message:lj}{NewLine}{Exception}")  // File sink
        .CreateLogger();
  }
}
