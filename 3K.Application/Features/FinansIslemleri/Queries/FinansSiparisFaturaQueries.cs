using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>PO listesi: PO'lar alt sekmesi ve fatura girişinde PO seçimi için Faturalar alt sekmesi okur.</summary>
public sealed class FinansSiparislerQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaPo, YetkiKodlari.Finans.SekmePoFaturaFatura];
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>PO açma ekranındaki proje × kategori / düzenli iş seçicisi.</summary>
public sealed class FinansSiparisGruplariQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisGrubuModel>>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.SiparisAcma;
    public FinansSiparisGrupFiltre Filtre { get; init; } = new();
}

public sealed class FinansSiparisGetirQuery : FinansQuery<FinansSiparisModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Kayit;
    public int Id { get; init; }
}

public sealed class FinansFaturalarQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.SekmePoFaturaFatura];
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansFaturaGetirQuery : FinansQuery<FinansFaturaModel>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.Kayit;
    public int Id { get; init; }
}

/// <summary>Vadesi geçen ve uyarı penceresinde vadesi gelen ödenmemiş faturalar (D3 uyarısı); ayrı vade uyarısı izni.</summary>
public sealed class FinansVadeUyarilariQuery : FinansQuery<FinansVadeUyarilariModel>
{
    public override IReadOnlyList<string> Sekmeler => [YetkiKodlari.Finans.VadeUyarisi];
    public int EnFazlaKalem { get; init; } = 20;
}
