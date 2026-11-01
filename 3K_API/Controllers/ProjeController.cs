using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Enums;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProjeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int? projeTipiId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isSevkEdilen = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _mediator.Send(new ProjeListeleQuery
            {
                ProjeTipiId = projeTipiId,
                SearchTerm = searchTerm,
                IsSevkEdilen = isSevkEdilen,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Dropdown'lar için hafif proje listesi — Include yok, sadece Id/ProjeNo/Musteri.
        /// </summary>
        [HttpGet("dropdown")]
        public async Task<ActionResult> GetDropdown(
            [FromQuery] int? projeTipiId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isSevkEdilen = null,
            [FromQuery] int take = 50,
            [FromQuery] List<int>? includeIds = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new ProjeDropdownQuery
            {
                ProjeTipiId = projeTipiId,
                SearchTerm = searchTerm,
                IsSevkEdilen = isSevkEdilen,
                Take = take,
                IncludeIds = includeIds ?? []
            }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ProjeOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("eksiklerden-saha-olustur")]
        public async Task<ActionResult> EksiklerdenSahaOlustur([FromBody] EksiklerdenSahaProjesiOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("sandiklardan-saha-olustur")]
        public async Task<ActionResult> SandiklardanSahaOlustur([FromBody] SandiklardanSahaProjesiOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("saha-aktarim-geri-al")]
        public async Task<ActionResult> SahaAktarimGeriAl([FromBody] SahaAktarimGeriAlCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("saha-sandik-aktarimlari-geri-al")]
        public async Task<ActionResult> SahaSandikAktarimlariGeriAl(
            [FromBody] SahaSandikAktarimlariGeriAlCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpGet("{id}/saha-aktarimlari")]
        public async Task<ActionResult> GetSahaAktarimlari(int id)
        {
            var result = await _mediator.Send(new GetSahaAktarimlariQuery { ProjeId = id });
            return result.ToActionResult();
        }

        /// <summary>
        /// Sevk edilmiş normal projenin kalanlarının otomatik sahaya aktarım önizlemesi (veri değiştirmez).
        /// </summary>
        [HttpGet("{id}/otomatik-saha-aktarim/onizle")]
        public async Task<ActionResult> OtomatikSahaAktarimOnizle(
            int id,
            [FromQuery] int? hedefSahaProjeId = null,
            [FromQuery] bool yeniProje = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new OtomatikSahaAktarimOnizleQuery
            {
                ProjeId = id,
                HedefSahaProjeId = hedefSahaProjeId,
                YeniProje = yeniProje
            }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("{id}/otomatik-saha-aktarim")]
        public async Task<ActionResult> OtomatikSahayaAktar(
            int id,
            [FromBody] OtomatikSahayaAktarCommand command,
            CancellationToken cancellationToken)
        {
            command.ProjeId = id;
            var result = await _mediator.Send(command, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// Sahadan sahaya taşıma önizlemesi: satırlar, hedef sandık numarası ve engeller (veri değiştirmez).
        /// </summary>
        [HttpPost("sahadan-sahaya-tasima/onizle")]
        public async Task<ActionResult> SahadanSahayaTasimaOnizle(
            [FromBody] SahadanSahayaTasimaOnizleQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("sahadan-sahaya-tasima")]
        public async Task<ActionResult> SahadanSahayaTasi(
            [FromBody] SahadanSahayaTasiCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// Bir saha aktarımının (otomatik ya da elle) bütün satırlarını hepsi-ya-hiçbiri geri alır.
        /// </summary>
        [HttpPost("saha-aktarimi-geri-al")]
        public async Task<ActionResult> SahaAktarimiGeriAl(
            [FromBody] SahaAktarimiGeriAlCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// Sandık Kapat/Aç
        /// </summary>
        [HttpPut("sandik-kapat")]
        public async Task<ActionResult> SandikKapat([FromBody] SandikKapatCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
        /// <summary>
        /// Projeyi kilitler (Sevk Edildi durumuna çeker) — Sadece yetkililer
        /// </summary>
        [HttpPost("{id}/sevk-et")]
        public async Task<ActionResult> SevkEt(int id, [FromBody] SevkEtRequest? request = null)
        {
            var result = await _mediator.Send(new ProjeSevkEtCommand
            {
                ProjeId = id,
                SevkTarihi = request?.SevkTarihi,
                SandikIds = request?.SandikIds,
                Aciklama = request?.Aciklama,
                AracPlaka = request?.AracPlaka,
                Sofor = request?.Sofor,
                DorsePlaka = request?.DorsePlaka
            });
            return result.ToActionResult();
        }

        [HttpGet("{id}/sevkiyatlar")]
        public async Task<ActionResult> GetSevkiyatlar(int id)
        {
            var result = await _mediator.Send(new GetProjeSevkiyatlariQuery { ProjeId = id });
            return result.ToActionResult();
        }

        /// <summary>
        /// Proje kilidini açar (Devam durumuna çeker)
        /// </summary>
        [HttpPost("{id}/kilidi-ac")]
        public async Task<ActionResult> KilidiAc(int id, [FromBody] KilitAcRequest? request = null)
        {
            var result = await _mediator.Send(new ProjeKilidiAcCommand
            {
                ProjeId = id,
                ProjeNo = request?.ProjeNo,
                KilitAcmaTipiId = request?.KilitAcmaTipiId ?? (int)_3K.Core.Enums.SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc,
                Aciklama = request?.Aciklama
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Projenin Planlanan Sevk Tarihini günceller
        /// </summary>
        [HttpPut("sevk-tarihi-guncelle")]
        public async Task<ActionResult> SevkTarihiGuncelle([FromBody] ProjeSevkTarihiGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
        /// <summary>
        /// Projeye fotoğraf veya gerçekleşen çeki bağlantısı ekler (Tur boşsa fotoğraf; proje-fotograf-baglantisi W);
        /// projenin güncel bağlantı listesini döner.
        /// </summary>
        [HttpPost("{id:int}/fotograf-baglantilari")]
        public async Task<ActionResult> FotografBaglantisiEkle(int id, [FromBody] FotografBaglantisiRequest request)
        {
            var result = await _mediator.Send(new ProjeFotografBaglantisiKaydetCommand
            {
                ProjeId = id, Tur = request.Tur, Adres = request.Adres, Aciklama = request.Aciklama
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Projenin bir bağlantısının adresini/açıklamasını değiştirir; tür değişmez (proje-fotograf-baglantisi W).
        /// </summary>
        [HttpPut("{id:int}/fotograf-baglantilari/{baglantiId:int}")]
        public async Task<ActionResult> FotografBaglantisiGuncelle(int id, int baglantiId, [FromBody] FotografBaglantisiRequest request)
        {
            var result = await _mediator.Send(new ProjeFotografBaglantisiKaydetCommand
            {
                ProjeId = id, BaglantiId = baglantiId, Adres = request.Adres, Aciklama = request.Aciklama
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Projenin bir fotoğraf bağlantısını siler (proje-fotograf-baglantisi W).
        /// </summary>
        [HttpDelete("{id:int}/fotograf-baglantilari/{baglantiId:int}")]
        public async Task<ActionResult> FotografBaglantisiSil(int id, int baglantiId)
        {
            var result = await _mediator.Send(new ProjeFotografBaglantisiSilCommand { ProjeId = id, BaglantiId = baglantiId });
            return result.ToActionResult();
        }

        /// <summary>
        /// Projeyi ve tüm alt verilerini (sandık, ürün, çeki vb.) siler
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new ProjeSilCommand { ProjeId = id });
            return result.ToActionResult();
        }
    }

    public record SevkEtRequest
    {
        public DateTime? SevkTarihi { get; init; }
        public List<int>? SandikIds { get; init; }
        public string? Aciklama { get; init; }
        public string? AracPlaka { get; init; }
        public string? Sofor { get; init; }
        public string? DorsePlaka { get; init; }
    }

    public record FotografBaglantisiRequest
    {
        /// <summary>Yalnız eklemede: 1 fotoğraf (varsayılan), 2 gerçekleşen çeki.</summary>
        public ProjeBaglantiTuru? Tur { get; init; }
        public string? Adres { get; init; }
        public string? Aciklama { get; init; }
    }

    public record KilitAcRequest
    {
        public int? KilitAcmaTipiId { get; init; }
        public string? ProjeNo { get; init; }
        public string? Aciklama { get; init; }
    }
}
