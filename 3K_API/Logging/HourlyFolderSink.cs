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
    /// 
    /// Log Retention Policy:
    ///   Belirtilen gün sayısından eski dosya ve boş klasörler otomatik silinir.
    ///   Temizlik her saat değişiminde (dosya rotasyonunda) tetiklenir.
    /// </summary>
    public class HourlyFolderSink : ILogEventSink, IDisposable
    {
        private readonly string _basePath;
        private readonly string _filePrefix;
        private readonly LogEventLevel _minimumLevel;
        private readonly string _outputTemplate;
        private readonly int _retainedDays;
        private readonly object _lock = new();
        private StreamWriter? _currentWriter;
        private string _currentFilePath = string.Empty;
        private readonly ITextFormatter _formatter;
        private DateTime _lastCleanupDate = DateTime.MinValue;

        public HourlyFolderSink(
            string basePath,
            string filePrefix,
            LogEventLevel minimumLevel,
            string outputTemplate,
            int retainedDays = 30)
        {
            _basePath = basePath;
            _filePrefix = filePrefix;
            _minimumLevel = minimumLevel;
            _outputTemplate = outputTemplate;
            _retainedDays = retainedDays;

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

                    // Günde bir kez eski log dosyalarını temizle
                    if (_lastCleanupDate.Date != now.Date)
                    {
                        _lastCleanupDate = now.Date;
                        Task.Run(() => CleanupOldLogs(now));
                    }
                }

                _formatter.Format(logEvent, _currentWriter!);
            }
        }

        /// <summary>
        /// RetainedDays'den eski log dosyalarını ve boş klasörleri siler.
        /// </summary>
        private void CleanupOldLogs(DateTime now)
        {
            try
            {
                if (!Directory.Exists(_basePath))
                    return;

                var cutoffDate = now.AddDays(-_retainedDays);

                // Ay klasörlerini tara: {basePath}/{yyyy-MM}
                foreach (var monthDir in Directory.GetDirectories(_basePath))
                {
                    var monthName = Path.GetFileName(monthDir);
                    // yyyy-MM formatını parse et
                    if (!DateTime.TryParseExact(monthName, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var monthDate))
                        continue;

                    // Tüm ayın sonu cutoff'tan önceyse, tüm klasörü sil
                    var monthEnd = new DateTime(monthDate.Year, monthDate.Month, DateTime.DaysInMonth(monthDate.Year, monthDate.Month));
                    if (monthEnd < cutoffDate.Date)
                    {
                        try
                        {
                            Directory.Delete(monthDir, recursive: true);
                        }
                        catch
                        {
                            // Silme hatası olursa atla (dosya kullanımda olabilir)
                        }
                        continue;
                    }

                    // Gün klasörlerini tara: {basePath}/{yyyy-MM}/{dd}
                    foreach (var dayDir in Directory.GetDirectories(monthDir))
                    {
                        var dayName = Path.GetFileName(dayDir);
                        if (!int.TryParse(dayName, out var day) || day < 1 || day > 31)
                            continue;

                        try
                        {
                            var folderDate = new DateTime(monthDate.Year, monthDate.Month, day);
                            if (folderDate < cutoffDate.Date)
                            {
                                Directory.Delete(dayDir, recursive: true);
                            }
                        }
                        catch
                        {
                            // Geçersiz tarih veya silme hatası — atla
                        }
                    }

                    // Ay klasörü boşsa sil
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(monthDir).Any())
                            Directory.Delete(monthDir);
                    }
                    catch
                    {
                        // Atla
                    }
                }
            }
            catch
            {
                // Temizlik hatası uygulama akışını bozmamalı
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
            string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            int retainedDays = 30)
        {
            var sink = new HourlyFolderSink(basePath, filePrefix, minimumLevel, outputTemplate, retainedDays);
            return sinkConfig.Sink(sink, minimumLevel);
        }
    }
}
