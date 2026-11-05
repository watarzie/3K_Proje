using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;

/// <summary>Plan kaydında sandık yükünün kullanıcı seçimleri; ölçü ve ağırlık istemciden alınmaz.</summary>
public sealed record PlanSandikYukuDto(
    int SandikId,
    bool Dahil,
    bool DondurulebilirMi,
    bool UsteKonabilirMi,
    bool AltaKalabilirMi,
    decimal? UstYukSiniriKg,
    PlanYukIzni ElleDegisenIzinler)
{
    public PlanSandikYukuKaydi Model() =>
        new(SandikId, Dahil, DondurulebilirMi, UsteKonabilirMi, AltaKalabilirMi, UstYukSiniriKg, ElleDegisenIzinler);
}

/// <summary>Elle eklenen yükün bir biriminin seçimleri.</summary>
public sealed record PlanManuelYukBirimiDto(
    int BirimNo,
    bool Dahil,
    bool DondurulebilirMi,
    bool UsteKonabilirMi,
    bool AltaKalabilirMi,
    decimal? UstYukSiniriKg,
    PlanYukIzni ElleDegisenIzinler)
{
    public PlanManuelYukBirimi Model() =>
        new(BirimNo, Dahil, DondurulebilirMi, UsteKonabilirMi, AltaKalabilirMi, UstYukSiniriKg, ElleDegisenIzinler);
}

/// <summary>Elle eklenen yük (isterler 4): ortak bilgiler, adet ve birim seçimleri.</summary>
public sealed record PlanManuelYukDto(
    string Kod,
    string? Aciklama,
    int YukTuruId,
    decimal? BoyMm,
    decimal? EnMm,
    decimal? YukseklikMm,
    decimal? BrutKg,
    int Adet,
    int? ProjeId,
    int? LokasyonId,
    string? OzelTasimaNotu,
    IReadOnlyList<PlanManuelYukBirimiDto>? Birimler)
{
    public PlanManuelYukKaydi Model() =>
        new(Kod.Trim(), string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim(), YukTuruId, BoyMm, EnMm, YukseklikMm,
            BrutKg, Adet, ProjeId, LokasyonId, string.IsNullOrWhiteSpace(OzelTasimaNotu) ? null : OzelTasimaNotu.Trim(),
            (Birimler ?? []).Select(b => b.Model()).ToList());
}
