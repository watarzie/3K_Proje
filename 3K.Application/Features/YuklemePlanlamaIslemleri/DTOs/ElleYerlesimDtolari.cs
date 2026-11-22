using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;

/// <summary>Elle yerleşimde yükün konumu (ekipman koordinatları, mm); ölçü ve ağırlık istemciden alınmaz.</summary>
public sealed record ElleKonumDto(string YukAnahtari, int XMm, int YMm, int ZMm, bool Donduruldu)
{
    public ElleKonumModeli Model() => new(YukAnahtari, XMm, YMm, ZMm, Donduruldu);
}

/// <summary>Elle yerleşimde bir araç: ekipman konfigürasyonu ve yükleri (boş olabilir: "aynı tipten araç ekle").</summary>
public sealed record ElleAracDto(int KonfigurasyonId, List<ElleKonumDto>? Konumlar)
{
    public ElleAracModeli Model() => new(KonfigurasyonId, (Konumlar ?? []).Select(k => k.Model()).ToList());
}
