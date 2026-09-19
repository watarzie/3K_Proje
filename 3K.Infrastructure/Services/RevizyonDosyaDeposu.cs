namespace _3K.Infrastructure.Services;

/// <summary>Yalnız ilgili projenin arşivindeki normal dosyalar okunabilir; link/junction izlenmez.</summary>
public static class RevizyonDosyaDeposu
{
    public static string? GuvenliYol(string uploadsRoot, int projeId, string? kayitliYol)
    {
        if (projeId <= 0 || string.IsNullOrWhiteSpace(kayitliYol)) return null;
        try
        {
            var uploads = Path.GetFullPath(uploadsRoot);
            var root = Path.GetFullPath(Path.Combine(uploads, projeId.ToString(System.Globalization.CultureInfo.InvariantCulture), "Revizyonlar"));
            var path = Path.GetFullPath(kayitliYol);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, comparison) || !File.Exists(path)) return null;
            // Arşiv dizininin kendisi veya üstündeki bir junction sınırı aşmamalı.
            for (FileSystemInfo? current = new FileInfo(path); current != null; current = current is FileInfo file ? file.Directory : ((DirectoryInfo)current).Parent)
            {
                if ((current.Attributes & FileAttributes.ReparsePoint) != 0) return null;
            }
            return path;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    public static string IndirmeAdi(string? ad)
    {
        var name = (ad ?? string.Empty).Replace('\\', '/').Split('/').Last();
        name = new string(name.Where(c => !char.IsControl(c)).ToArray());
        return string.IsNullOrWhiteSpace(name) ? "revizyon.xlsx" : name;
    }
}
