using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using System;

namespace Cari_kayıt_Programı
{
    public static class LogManager
    {
        static LogManager()
        {
            // Serilog yapılandırması
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext() // LogContext ile özel alanları destekler
                .Enrich.WithProperty("Application", "Cari Kayıt Programı") // Uygulama adı
                .WriteTo.File(
                    new JsonFormatter(),
                    path: ConfigManager.LogFilePath,
                    retainedFileCountLimit: null // Dosya birikimini sınırlandırmaz
                )
                .CreateLogger();
        }

        public static void LogInformation(string message, string className, string methodName, string? stackTrace = null, string? user = null, string? additionalInfo = null)
        {
            using (LogContext.PushProperty("User", user ?? "Unknown"))
            using (LogContext.PushProperty("AdditionalInfo", additionalInfo))
            using (LogContext.PushProperty("StackTrace", stackTrace))
            using (LogContext.PushProperty("MethodName", methodName))
            using (LogContext.PushProperty("ClassName", className))
            {
                Log.Information(message);
            }
        }

        public static void LogWarning(string message, string className, string methodName, string? stackTrace = null, string? user = null, string additionalInfo = null)
        {
            using (LogContext.PushProperty("User", user ?? "Unknown"))
            using (LogContext.PushProperty("AdditionalInfo", additionalInfo))
            using (LogContext.PushProperty("StackTrace", stackTrace))
            using (LogContext.PushProperty("MethodName", methodName))
            using (LogContext.PushProperty("ClassName", className))
            {
                Log.Warning(message);
            }
        }

        public static void LogError(Exception ex, string className, string methodName, string? stackTrace = null, string? user = null, string? additionalInfo = null)
        {
            using (LogContext.PushProperty("User", user ?? "Unknown"))
            using (LogContext.PushProperty("AdditionalInfo", additionalInfo))
            using (LogContext.PushProperty("StackTrace", stackTrace))
            using (LogContext.PushProperty("MethodName", methodName))
            using (LogContext.PushProperty("ClassName", className))
            {
                Log.Error(ex, ex.Message);
            }
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}