namespace _3K.Core.Models;

/// <summary>
/// Yedek çeki içe aktarma işleminin kalıcı kayıt özetidir.
/// </summary>
public sealed record YedekCekiImportResult(
    int CekiId,
    int ProjeId,
    string ProjeNo,
    int SatirSayisi,
    int SandikSayisi);
