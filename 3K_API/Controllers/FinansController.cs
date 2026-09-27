using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/finans")]
    [TypeFilter(typeof(FinansAlanErisimFiltresi))]
    public sealed class FinansController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FinansController(IMediator mediator) => _mediator = mediator;

        [HttpGet("panel")]
        public async Task<ActionResult> Panel([FromQuery] DateTime baslangic, [FromQuery] DateTime bitis, CancellationToken ct)
            => (await _mediator.Send(new FinansPanelQuery { Baslangic = baslangic, Bitis = bitis }, ct)).ToActionResult();
        [HttpGet("hareketler")]
        public async Task<ActionResult> Hareketler([FromQuery] FinansFilterRequest request, [FromQuery] string? tur, CancellationToken ct)
            => (await _mediator.Send(new FinansHareketlerQuery { Filtre = request.ToModel(), Tur = tur }, ct)).ToActionResult();
        [HttpGet("genel-arama")]
        public async Task<ActionResult> GenelArama([FromQuery] FinansFilterRequest request, CancellationToken ct)
            => (await _mediator.Send(new FinansGenelAramaQuery { Filtre = request.ToModel() }, ct)).ToActionResult();
        [HttpGet("yaslandirma")]
        public async Task<ActionResult> Yaslandirma([FromQuery] FinansFilterRequest request, [FromQuery] DateTime? referansTarihi, [FromQuery] int minimumGun, CancellationToken ct)
            => (await _mediator.Send(new FinansYaslandirmaQuery { Filtre = request.ToModel(), ReferansTarihi = referansTarihi ?? TurkeyTime.Now.Date, MinimumGun = minimumGun }, ct)).ToActionResult();
        [HttpPost("is-kayitlari/{id:int}/finans-tarihi")]
        public async Task<ActionResult> FinansTarihi(int id, [FromBody] FinansTarihiDegistirModel model, CancellationToken ct)
            => (await _mediator.Send(new FinansTarihiDegistirCommand { Id = id, Model = model }, ct)).ToActionResult();
        [HttpPost("is-kayitlari/{id:int}/fiyatlandirma")]
        public async Task<ActionResult> Fiyatlandirma(int id, [FromBody] FinansFiyatlandirmaModel model, CancellationToken ct)
            => (await _mediator.Send(new FinansFiyatlandirCommand { Id = id, Model = model }, ct)).ToActionResult();
        [HttpGet("sablonlar")]
        public async Task<ActionResult> Sablonlar(CancellationToken ct)
            => (await _mediator.Send(new FinansSablonlarQuery(), ct)).ToActionResult();
        [HttpPost("sablonlar")]
        public async Task<ActionResult> SablonOlustur([FromBody] FinansSablonKaydetModel model, CancellationToken ct)
            => (await _mediator.Send(new FinansSablonKaydetCommand { Model = model }, ct)).ToActionResult();
        [HttpPut("sablonlar/{id:int}")]
        public async Task<ActionResult> SablonGuncelle(int id, [FromBody] FinansSablonKaydetModel model, CancellationToken ct)
            => (await _mediator.Send(new FinansSablonKaydetCommand { Id = id, Model = model }, ct)).ToActionResult();
        [HttpGet("kalici-silme/onizleme")]
        public async Task<ActionResult> SilmeOnizleme([FromQuery] string varlikTuru, [FromQuery] int id, CancellationToken ct)
            => (await _mediator.Send(new FinansKaliciSilOnizlemeQuery { VarlikTuru = varlikTuru, Id = id }, ct)).ToActionResult();
        [HttpPost("kalici-silme")]
        public async Task<ActionResult> KaliciSil([FromBody] FinansKaliciSilModel model, CancellationToken ct)
            => (await _mediator.Send(new FinansKaliciSilCommand { Model = model }, ct)).ToActionResult();
        [HttpGet("belgeler")]
        public async Task<ActionResult> Belgeler([FromQuery] string hedefTuru, [FromQuery] int hedefId, CancellationToken ct)
            => (await _mediator.Send(new FinansBelgelerQuery { HedefTuru = hedefTuru, HedefId = hedefId }, ct)).ToActionResult();
        [HttpPost("belgeler")]
        [RequestSizeLimit(11 * 1024 * 1024)]
        public async Task<ActionResult> BelgeYukle([FromForm] string hedefTuru, [FromForm] int hedefId, [FromForm] IFormFile dosya, CancellationToken ct)
        {
            if (dosya.Length > 10 * 1024 * 1024) return BadRequest(new { message = "PDF en fazla 10 MB olabilir." });
            using var buffer = new MemoryStream();
            await dosya.CopyToAsync(buffer, ct);
            return (await _mediator.Send(new FinansBelgeYukleCommand { Model = new(hedefTuru, hedefId, dosya.FileName, buffer.ToArray()) }, ct)).ToActionResult();
        }
        [HttpGet("belgeler/{id:int}/indir")]
        public async Task<ActionResult> BelgeIndir(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new FinansBelgeIndirQuery { Id = id }, ct);
            return result.ToFileResult();
        }

        [HttpGet("proje-secenekleri")]
        public async Task<ActionResult> ProjeSecenekleri([FromQuery] FinansProjeSecenekRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansProjeSecenekleriQuery
            {
                Arama = request.Arama,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            }, cancellationToken)).ToActionResult();

        [HttpPost("is-kayitlari/secim")]
        public async Task<ActionResult> IsKayitlariSecim([FromBody] FinansIsKayitlariSecimRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKayitlariSecimQuery { Ids = request.Ids }, cancellationToken)).ToActionResult();

        [HttpGet("is-kayitlari/{id:int}")]
        public async Task<ActionResult> IsKaydi(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiGetirQuery { Id = id }, cancellationToken)).ToActionResult();

        [HttpPost("is-kayitlari/{id:int}/iptal")]
        public async Task<ActionResult> IsKaydiIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpGet("aylik-isler")]
        public async Task<ActionResult> AylikIsler(
            [FromQuery] int yil,
            [FromQuery] int ay,
            [FromQuery] FinansFilterRequest request,
            CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansAylikIslerQuery { Yil = yil, Ay = ay, Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpPost("ozel-isler")]
        public async Task<ActionResult> OzelIsOlustur([FromBody] FinansOzelIsKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiOlusturCommand { Model = OzelIsModeli(model) }, cancellationToken)).ToActionResult();

        [HttpPut("ozel-isler/{id:int}/aylik-deger")]
        public async Task<ActionResult> OzelIsAylikDegerGuncelle(int id, [FromBody] FinansAylikDegerModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansOzelIsAylikDegerGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpPost("ozel-isler/{id:int}/iptal")]
        public async Task<ActionResult> OzelIsIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpPost("ozel-isler/{id:int}/geri-al")]
        public async Task<ActionResult> OzelIsGeriAl(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiGeriAlCommand { Id = id }, cancellationToken)).ToActionResult();

        [HttpGet("siparisler")]
        public async Task<ActionResult> Siparisler([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparislerQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("siparisler/{id:int}")]
        public async Task<ActionResult> Siparis(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparisGetirQuery { Id = id }, cancellationToken)).ToActionResult();

        [HttpPost("siparisler")]
        public async Task<ActionResult> SiparisOlustur([FromBody] FinansSiparisOlusturModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparisOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("siparisler/{id:int}")]
        public async Task<ActionResult> SiparisGuncelle(int id, [FromBody] FinansSiparisGuncelleModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparisGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpPost("siparisler/{id:int}/iptal")]
        public async Task<ActionResult> SiparisIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparisIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpGet("faturalar")]
        public async Task<ActionResult> Faturalar([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturalarQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("faturalar/{id:int}")]
        public async Task<ActionResult> Fatura(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaGetirQuery { Id = id }, cancellationToken)).ToActionResult();

        [HttpPost("faturalar")]
        public async Task<ActionResult> FaturaOlustur([FromBody] FinansFaturaOlusturModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("faturalar/{id:int}")]
        public async Task<ActionResult> FaturaGuncelle(int id, [FromBody] FinansFaturaGuncelleModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpPost("faturalar/{id:int}/iptal")]
        public async Task<ActionResult> FaturaIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpGet("duzenli-isler")]
        public async Task<ActionResult> DuzenliIsler(
            [FromQuery] bool sadeceAktif,
            [FromQuery] string? arama,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
            => (await _mediator.Send(new FinansDuzenliIslerQuery
            {
                SadeceAktif = sadeceAktif,
                Arama = arama,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken)).ToActionResult();

        [HttpPost("duzenli-isler")]
        public async Task<ActionResult> DuzenliIsOlustur([FromBody] FinansDuzenliIsUyumKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansDuzenliIsOlusturCommand { Model = DuzenliIsModeli(model) }, cancellationToken)).ToActionResult();

        [HttpPut("duzenli-isler/{id:int}")]
        public async Task<ActionResult> DuzenliIsGuncelle(int id, [FromBody] FinansDuzenliIsUyumKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansDuzenliIsGuncelleCommand { Id = id, Model = DuzenliIsModeli(model) }, cancellationToken)).ToActionResult();

        [HttpGet("giderler")]
        public async Task<ActionResult> Giderler([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderlerQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpPost("giderler")]
        public async Task<ActionResult> GiderOlustur([FromBody] FinansGiderUyumKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderOlusturCommand { Model = GiderModeli(model) }, cancellationToken)).ToActionResult();

        [HttpPut("giderler/{id:int}")]
        public async Task<ActionResult> GiderGuncelle(int id, [FromBody] FinansGiderUyumKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderGuncelleCommand { Id = id, Model = GiderModeli(model) }, cancellationToken)).ToActionResult();

        [HttpPost("giderler/{id:int}/iptal")]
        public async Task<ActionResult> GiderIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpGet("gider-kategorileri")]
        public async Task<ActionResult> GiderKategorileri([FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKategorileriQuery { SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

        [HttpPost("gider-kategorileri")]
        public async Task<ActionResult> GiderKategoriOlustur([FromBody] FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKategoriOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("gider-kategorileri/{id:int}")]
        public async Task<ActionResult> GiderKategoriGuncelle(int id, [FromBody] FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKategoriGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpGet("gider-kalemleri")]
        public async Task<ActionResult> GiderKalemleri([FromQuery] int? kategoriId, [FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKalemleriQuery { KategoriId = kategoriId, SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

        [HttpGet("gider-kutuphanesi/kategoriler")]
        public async Task<ActionResult> GiderKutuphaneKategorileri([FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKutuphaneKategorileriQuery { SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

        [HttpGet("gider-kutuphanesi/kalemler")]
        public async Task<ActionResult> GiderKutuphaneKalemleri([FromQuery] int? kategoriId, [FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKutuphaneKalemleriQuery { KategoriId = kategoriId, SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

        [HttpPost("gider-kalemleri")]
        public async Task<ActionResult> GiderKalemiOlustur([FromBody] FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKalemiOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("gider-kalemleri/{id:int}")]
        public async Task<ActionResult> GiderKalemiGuncelle(int id, [FromBody] FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderKalemiGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpGet("urunler")]
        public async Task<ActionResult> Urunler(
            [FromQuery] bool sadeceAktif,
            [FromQuery] DateTime? tarifeTarihi,
            [FromQuery] string? arama,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
            => (await _mediator.Send(new FinansUrunlerQuery
            {
                SadeceAktif = sadeceAktif,
                TarifeTarihi = tarifeTarihi,
                Arama = arama,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken)).ToActionResult();

        [HttpPost("urunler")]
        public async Task<ActionResult> UrunOlustur([FromBody] FinansUrunKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansUrunOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("urunler/{id:int}")]
        public async Task<ActionResult> UrunGuncelle(int id, [FromBody] FinansUrunKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansUrunGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpDelete("urunler/{id:int}")]
        public async Task<ActionResult> UrunPasiflestir(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansUrunPasiflestirCommand { Id = id }, cancellationToken)).ToActionResult();

        [HttpGet("raporlar/ozet/{tur}/{format}")]
        public async Task<ActionResult> OzetRapor(string tur, string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => await FileResultAsync(new FinansV2RaporQuery { Tur = tur, Format = format, Filtre = request.ToModel() }, cancellationToken);

        [HttpGet("raporlar/isler/{format}")]
        public async Task<ActionResult> IsRaporu(string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => await FileResultAsync(new FinansIsRaporuDosyaQuery { Format = format, Filtre = request.ToModel() }, cancellationToken);

        [HttpGet("raporlar/giderler/{format}")]
        public async Task<ActionResult> GiderRaporu(string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => await FileResultAsync(new FinansGiderRaporuDosyaQuery { Format = format, Filtre = request.ToModel() }, cancellationToken);

        [HttpGet("raporlar/siparis-durumu/{format}")]
        public async Task<ActionResult> SiparisDurumRaporu(string format, [FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => await FileResultAsync(new FinansSiparisDurumRaporuDosyaQuery { Format = format, Filtre = request.ToModel() }, cancellationToken);

        [HttpGet("raporlar/aylik/{format}")]
        public async Task<ActionResult> AylikRapor(
            string format,
            [FromQuery] int yil,
            [FromQuery] int ay,
            [FromQuery] string[]? gruplar,
            CancellationToken cancellationToken)
            => await FileResultAsync(new FinansAylikRaporDosyaQuery
            {
                Format = format,
                Yil = yil,
                Ay = ay,
                Gruplar = gruplar ?? Array.Empty<string>()
            }, cancellationToken);

        [HttpGet("denetim")]
        public async Task<ActionResult> Denetim(
            [FromQuery] string? varlikTuru,
            [FromQuery] int? varlikId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
            => (await _mediator.Send(new FinansDegisiklikGecmisiQuery
            {
                VarlikTuru = varlikTuru,
                VarlikId = varlikId,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken)).ToActionResult();

        private async Task<ActionResult> FileResultAsync(FinansQuery<DosyaIcerigi> query, CancellationToken cancellationToken)
        {
            return (await _mediator.Send(query, cancellationToken)).ToFileResult();
        }

        private static FinansIsKaydiKaydetModel OzelIsModeli(FinansOzelIsKaydetModel model) => new(
            model.ProjeId, null, null, model.Musteri, FinansIsTuru.OzelIs,
            model.IsAdi, model.Aciklama, model.TalepEdenKisi, model.TalepEdenBolum, model.Miktar, model.Birim, 0m,
            null, model.BirimFiyat, model.ParaBirimi, model.KdvOrani,
            model.IsTarihi.Date, model.IsTarihi.Date,
            OzelIsTuru: model.IsTuru,
            HesaplamaYontemi: model.HesaplamaYontemi,
            RaporGrubu: model.RaporGrubu);

        private static FinansDuzenliIsKaydetModel DuzenliIsModeli(FinansDuzenliIsUyumKaydetModel model) => new(
            model.ProjeId, null, null, model.IsAdi, FinansIsTuru.OzelIs,
            model.Musteri, model.Aciklama, model.BaslangicTarihi, model.BitisTarihi,
            model.OlusturmaGunu, model.Miktar, model.Birim, null, model.BirimFiyat,
            model.ParaBirimi, model.KdvOrani, model.Aktif, model.IsTuru,
            model.HesaplamaYontemi, model.RaporGrubu);

        private static FinansGiderKaydetModel GiderModeli(FinansGiderUyumKaydetModel model) => new(
            model.Tarih, model.FinansDonemi ?? new DateTime(model.Tarih.Year, model.Tarih.Month, 1),
            model.KategoriId, model.GiderKalemiId, model.AltKategori, model.FirmaVeyaKisi,
            model.Aciklama, model.Miktar ?? 1m, model.Birim ?? "Tutar", model.BirimFiyat ?? model.Tutar, model.ParaBirimi,
            model.KdvDahil, model.KdvOrani, model.ProjeId, null, model.IsTuru, model.BelgeNo, model.AvansMi, model.MahsupEdilenAvansId, model.FinansTarihi);
    }

    public sealed class FinansFilterRequest
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
        public string? Arama { get; init; }
        public int? ProjeId { get; init; }
        public string? ProjeNo { get; init; }
        public FinansIsTuru? IsTuru { get; init; }
        public FinansIsDurumu? Durum { get; init; }
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
        public string? ParaBirimi { get; init; }
        public bool IptalEdilenleriDahilEt { get; init; }
        public string? PoNumarasi { get; init; }
        public string? TalepEden { get; init; }
        public FinansSiparisDurumu? SiparisDurumu { get; init; }
        public FinansFaturaDurumu? FaturaDurumu { get; init; }
        public bool FaturaBekleyen { get; init; }
        public bool FaturalamaBekleyen { get; init; }
        public string? FaturaNumarasi { get; init; }
        public string? Firma { get; init; }
        public AmbalajSandikCinsi? SandikCinsi { get; init; }
        public int? GiderKategoriId { get; init; }

        public FinansListeFiltre ToModel() => new(
            PageNumber, PageSize, Arama, ProjeId, ProjeNo, IsTuru, Durum,
            Baslangic, Bitis, ParaBirimi, IptalEdilenleriDahilEt, PoNumarasi, TalepEden,
            SiparisDurumu, FaturaDurumu, FaturaBekleyen, FaturalamaBekleyen, FaturaNumarasi, Firma, SandikCinsi, GiderKategoriId);
    }

    public sealed class FinansProjeSecenekRequest
    {
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansIsKayitlariSecimRequest
    {
        public IReadOnlyList<int> Ids { get; init; } = Array.Empty<int>();
    }
}
