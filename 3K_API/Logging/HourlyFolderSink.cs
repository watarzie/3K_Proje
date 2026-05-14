using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.Globalization;

namespace _3K_API.Logging
{
    /// <summary>
    /// Serilog için özel dosya sink'i.
    /// Logları Ay/Gün alt klasörlerine, saatlik rotasyonla yazar.
    /// 
    /// Klasör yapısı:
    ///   {BasePath}/{yyyy-MM}/{dd}/trace-{HH}.txt
    ///   {BasePath}/{yyyy-MM}/{dd}/service-{HH}.txt
    /// 
    /// Her saat başı yeni dosya oluşturulur, eski saatin dosyası kapatılır.
    /// </summary>
    public class HourlyFolderSink : ILogEventSink, IDisposable
    {
        private readonly string _basePath;
        private readonly string _filePrefix;
        private readonly LogEventLevel _minimumLevel;
        private readonly string _outputTemplate;
        private readonly object _lock = new();
        private StreamWriter? _currentWriter;
        private string _currentFilePath = string.Empty;
        private readonly ITextFormatter _formatter;

        public HourlyFolderSink(
            string basePath,
            string filePrefix,
            LogEventLevel minimumLevel,
            string outputTemplate)
        {
            _basePath = basePath;
            _filePrefix = filePrefix;
            _minimumLevel = minimumLevel;
            _outputTemplate = outputTemplate;

            // Serilog'un MessageTemplateTextFormatter'ını kullan
            _formatter = new Serilog.Formatting.Display.MessageTemplateTextFormatter(outputTemplate);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < _minimumLevel)
                return;

            var now = logEvent.Timestamp.LocalDateTime;

            // Klasör: {basePath}/{yyyy-MM}/{dd}
            var monthFolder = now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var dayFolder = now.ToString("dd", CultureInfo.InvariantCulture);
            var directory = Path.Combine(_basePath, monthFolder, dayFolder);

            // Dosya: {prefix}-{HH}.txt
            var fileName = $"{_filePrefix}-{now:HH}.txt";
            var filePath = Path.Combine(directory, fileName);

            lock (_lock)
            {
                // Saat değişti mi? Yeni dosyaya geç
                if (_currentFilePath != filePath)
                {
                    _currentWriter?.Flush();
                    _currentWriter?.Dispose();

                    Directory.CreateDirectory(directory);

                    _currentWriter = new StreamWriter(
                        new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                        System.Text.Encoding.UTF8)
                    {
                        AutoFlush = true
                    };

                    _currentFilePath = filePath;
                }

                _formatter.Format(logEvent, _currentWriter!);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _currentWriter?.Flush();
                _currentWriter?.Dispose();
                _currentWriter = null;
            }
        }
    }

    /// <summary>
    /// Serilog LoggerConfiguration'a HourlyFolderSink eklemek için extension method.
    /// </summary>
    public static class HourlyFolderSinkExtensions
    {
        public static LoggerConfiguration HourlyFolderFile(
            this Serilog.Configuration.LoggerSinkConfiguration sinkConfig,
            string basePath,
            string filePrefix,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        {
            var sink = new HourlyFolderSink(basePath, filePrefix, minimumLevel, outputTemplate);
            return sinkConfig.Sink(sink, minimumLevel);
        }
    }
}
