using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
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
    public sealed class FinansController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FinansController(IMediator mediator) => _mediator = mediator;

        [HttpGet("dashboard")]
        public async Task<ActionResult> Dashboard([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansDashboardQuery { Baslangic = baslangic, Bitis = bitis }, cancellationToken)).ToActionResult();

        [HttpGet("dashboard/gelir")]
        public async Task<ActionResult> GelirOzeti([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGelirOzetiQuery { Baslangic = baslangic, Bitis = bitis }, cancellationToken)).ToActionResult();

        [HttpGet("dashboard/durum-tutarlari")]
        public async Task<ActionResult> DurumTutarOzeti([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansDurumTutarOzetiQuery { Baslangic = baslangic, Bitis = bitis }, cancellationToken)).ToActionResult();

        [HttpGet("dashboard/gider")]
        public async Task<ActionResult> GiderOzeti([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderOzetiQuery { Baslangic = baslangic, Bitis = bitis }, cancellationToken)).ToActionResult();

        [HttpGet("dashboard/net")]
        public async Task<ActionResult> NetOzeti([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansNetOzetiQuery { Baslangic = baslangic, Bitis = bitis }, cancellationToken)).ToActionResult();

        [HttpGet("projeler")]
        public async Task<ActionResult> Projeler([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansProjelerQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("proje-secenekleri")]
        public async Task<ActionResult> ProjeSecenekleri([FromQuery] FinansProjeSecenekRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansProjeSecenekleriQuery
            {
                Arama = request.Arama,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            }, cancellationToken)).ToActionResult();

        [HttpGet("projeler/{projeId:int}")]
        public async Task<ActionResult> ProjeDetay(int projeId, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKayitlariQuery
            {
                Filtre = new FinansListeFiltre(PageSize: 250, ProjeId: projeId, IptalEdilenleriDahilEt: true)
            }, cancellationToken)).ToActionResult();

        [HttpGet("is-kayitlari")]
        public async Task<ActionResult> IsKayitlari([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKayitlariQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpPost("is-kayitlari/secim")]
        public async Task<ActionResult> IsKayitlariSecim([FromBody] FinansIsKayitlariSecimRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKayitlariSecimQuery { Ids = request.Ids }, cancellationToken)).ToActionResult();

        [HttpGet("is-kayitlari/{id:int}")]
        public async Task<ActionResult> IsKaydi(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiGetirQuery { Id = id }, cancellationToken)).ToActionResult();

        [HttpPost("is-kayitlari")]
        public async Task<ActionResult> IsKaydiOlustur([FromBody] FinansIsKaydiKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("is-kayitlari/{id:int}")]
        public async Task<ActionResult> IsKaydiGuncelle(int id, [FromBody] FinansIsKaydiKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpPost("is-kayitlari/{id:int}/iptal")]
        public async Task<ActionResult> IsKaydiIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpPost("is-kayitlari/{id:int}/geri-al")]
        public async Task<ActionResult> IsKaydiGeriAl(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansIsKaydiGeriAlCommand { Id = id }, cancellationToken)).ToActionResult();

        [HttpGet("aylik-isler")]
        public async Task<ActionResult> AylikIsler(
            [FromQuery] int yil,
            [FromQuery] int ay,
            [FromQuery] FinansFilterRequest request,
            CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansAylikIslerQuery { Yil = yil, Ay = ay, Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("aylik-isler-operasyon")]
        public async Task<ActionResult> AylikOperasyonIsler(
            [FromQuery] int yil,
            [FromQuery] int ay,
            [FromQuery] FinansFilterRequest request,
            CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansAylikOperasyonIslerQuery { Yil = yil, Ay = ay, Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("ozel-isler")]
        public async Task<ActionResult> OzelIsler([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansOzelIslerQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

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

        [HttpGet("siparisler-operasyon")]
        public async Task<ActionResult> SiparislerOperasyon([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparisOperasyonQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("faturalama-siparisleri")]
        public async Task<ActionResult> FaturalamaSiparisleri([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturalamaSiparisleriQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

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

        [HttpPost("siparisler/{id:int}/geri-al")]
        public async Task<ActionResult> SiparisGeriAl(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansSiparisGeriAlCommand { Id = id }, cancellationToken)).ToActionResult();

        [HttpGet("faturalar")]
        public async Task<ActionResult> Faturalar([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturalarQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("faturalar-operasyon")]
        public async Task<ActionResult> FaturalarOperasyon([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaOperasyonQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

        [HttpGet("faturalar/{id:int}")]
        public async Task<ActionResult> Fatura(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaGetirQuery { Id = id }, cancellationToken)).ToActionResult();

        [HttpGet("faturalar/{id:int}/operasyon")]
        public async Task<ActionResult> FaturaOperasyonDetay(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaOperasyonDetayQuery { Id = id }, cancellationToken)).ToActionResult();

        [HttpPost("faturalar")]
        public async Task<ActionResult> FaturaOlustur([FromBody] FinansFaturaOlusturModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("faturalar/{id:int}")]
        public async Task<ActionResult> FaturaGuncelle(int id, [FromBody] FinansFaturaGuncelleModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpPost("faturalar/{id:int}/iptal")]
        public async Task<ActionResult> FaturaIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

        [HttpPost("faturalar/{id:int}/geri-al")]
        public async Task<ActionResult> FaturaGeriAl(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFaturaGeriAlCommand { Id = id }, cancellationToken)).ToActionResult();

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

        [HttpPost("duzenli-isler/donem-olustur")]
        public async Task<ActionResult> DuzenliIsDonemOlustur([FromQuery] DateTime? referansTarihi, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansDuzenliIsDonemOlusturCommand { ReferansTarihi = referansTarihi ?? TurkeyTime.Now.Date }, cancellationToken)).ToActionResult();

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

        [HttpPost("giderler/{id:int}/geri-al")]
        public async Task<ActionResult> GiderGeriAl(int id, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGiderGeriAlCommand { Id = id }, cancellationToken)).ToActionResult();

        [HttpPost("giderler/{id:int}/kutuphaneye-kaydet")]
        public async Task<ActionResult> GideriKutuphaneyeKaydet(
            int id,
            [FromBody] FinansGideriKutuphaneyeKaydetModel model,
            CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansGideriKutuphaneyeKaydetCommand { GiderId = id, Model = model }, cancellationToken)).ToActionResult();

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

        [HttpGet("urun-secenekleri")]
        public async Task<ActionResult> UrunSecenekleri(CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansUrunSecenekleriQuery(), cancellationToken)).ToActionResult();

        [HttpGet("urunler-kutuphane")]
        public async Task<ActionResult> UrunlerKutuphanesi(
            [FromQuery] bool sadeceAktif,
            [FromQuery] DateTime? tarifeTarihi,
            [FromQuery] string? arama,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
            => (await _mediator.Send(new FinansUrunKutuphaneQuery
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

        [HttpGet("fiyat-tarifeleri")]
        public async Task<ActionResult> FiyatTarifeleri(
            [FromQuery] int? urunId,
            [FromQuery] int? yil,
            [FromQuery] bool sadeceAktif,
            [FromQuery] string? arama,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
            => (await _mediator.Send(new FinansFiyatTarifeleriQuery
            {
                UrunId = urunId,
                Yil = yil,
                SadeceAktif = sadeceAktif,
                Arama = arama,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, cancellationToken)).ToActionResult();

        [HttpPost("fiyat-tarifeleri")]
        public async Task<ActionResult> FiyatTarifesiOlustur([FromBody] FinansFiyatTarifesiKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFiyatTarifesiOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

        [HttpPut("fiyat-tarifeleri/{id:int}")]
        public async Task<ActionResult> FiyatTarifesiGuncelle(int id, [FromBody] FinansFiyatTarifesiKaydetModel model, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansFiyatTarifesiGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

        [HttpGet("raporlar/veri")]
        public async Task<ActionResult> RaporVerisi([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
            => (await _mediator.Send(new FinansRaporVerisiQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

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

        private async Task<ActionResult> FileResultAsync(FinansQuery<FinansDosyaDto> query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            if (!result.IsSuccess || result.Value is null) return result.ToActionResult();
            return File(result.Value.Icerik, result.Value.IcerikTuru, result.Value.DosyaAdi);
        }

        private static FinansIsKaydiKaydetModel OzelIsModeli(FinansOzelIsKaydetModel model) => new(
            model.ProjeId, null, null, model.Musteri, FinansIsTuru.OzelIs,
            model.IsAdi, model.Aciklama, null, null, model.Miktar, model.Birim, 0m,
            null, model.BirimFiyat, model.ParaBirimi, model.KdvOrani,
            model.IsTarihi.Date, new DateTime(model.IsTarihi.Year, model.IsTarihi.Month, 1),
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
            model.Tarih, new DateTime(model.Tarih.Year, model.Tarih.Month, 1),
            model.KategoriId, null, model.AltKategori, model.FirmaVeyaKisi,
            model.Aciklama, 1m, "Tutar", model.Tutar, model.ParaBirimi,
            model.KdvDahil, model.KdvOrani, model.ProjeId, null, model.IsTuru);
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

        public FinansListeFiltre ToModel() => new(
            PageNumber, PageSize, Arama, ProjeId, ProjeNo, IsTuru, Durum,
            Baslangic, Bitis, ParaBirimi, IptalEdilenleriDahilEt, PoNumarasi, TalepEden,
            SiparisDurumu, FaturaDurumu, FaturaBekleyen, FaturalamaBekleyen);
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
