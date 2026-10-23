using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SandikController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SandikController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{projeId}/sandiklar")]
        public async Task<ActionResult> GetProjeSandiklari(int projeId)
        {
            var result = await _mediator.Send(new GetProjeSandiklariQuery { ProjeId = projeId });
            return result.ToActionResult();
        }

        [HttpGet("{sandikId}/icerik")]
        public async Task<ActionResult> GetSandikIcerik(int sandikId)
        {
            var result = await _mediator.Send(new GetSandikIcerikQuery { SandikId = sandikId });
            return result.ToActionResult();
        }

        [HttpPut("urun-guncelle")]
        public async Task<ActionResult> UrunGuncelle([FromBody] UrunGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPut("ozellik-guncelle")]
        public async Task<ActionResult> OzellikGuncelle([FromBody] SandikOzellikGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPut("icerik-bilgisi-guncelle")]
        public async Task<ActionResult> IcerikBilgisiGuncelle([FromBody] SandikIcerikBilgisiGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPut("ceki-satiri-guncelle")]
        public async Task<ActionResult> CekiSatiriGuncelle([FromBody] CekiSatiriAnaVeriGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("ceki-satirlari-sil")]
        public async Task<ActionResult> CekiSatirlariSil([FromBody] CekiSatirlariSilCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("manuel-ekle")]
        public async Task<ActionResult> ManuelUrunEkle([FromBody] ManuelUrunEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Projeye yeni boş sandık ekler — çeki dışı ek sandık ihtiyacı için.
        /// </summary>
        [HttpPost("ekle")]
        public async Task<ActionResult> SandikEkle([FromBody] SandikEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Sandık Bölme/Taşıma: Bir sandıktaki ürünlerin bir kısmını başka sandığa taşır.
        /// Örn: 2 nolu sandıktaki 4 ürünün 2'si, 67 nolu sandığa aktarılır.
        /// </summary>
        [HttpPost("urun-tasi")]
        public async Task<ActionResult> SandikUrunTasi([FromBody] SandikUrunTasiCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
        /// <summary>
        /// Sandığı manuel kapatır. Eksik veya hatalı ürün varsa forceClose=false durumunda uyarı döner.
        /// Yetki kontrolü aktif menü bağlamı ve RolYetkileri üzerinden yapılır.
        /// </summary>
        [HttpPost("kapat")]
        public async Task<ActionResult> SandikKapat([FromBody] SandikKapatCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Seçilen birden çok sandığı toplu olarak Hazır (Kapandı) durumuna getirir.
        /// Eksik/hatalı ürün vb. detaylı log döner.
        /// </summary>
        [HttpPost("toplu-kapat")]
        public async Task<ActionResult> TopluSandikKapat([FromBody] TopluSandikKapatCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("lokasyon-guncelle")]
        public async Task<ActionResult> LokasyonGuncelle([FromBody] SandikLokasyonGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Saha/Yedek projelerinde çeki olmadan doğrudan sandığa malzeme ekler.
        /// </summary>
        [HttpPost("saha-yedek-malzeme-ekle")]
        public async Task<ActionResult> SahaYedekMalzemeEkle([FromBody] SahaYedekMalzemeEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Normal projelerdeki kalan > 0 olan ürünleri listeler (Saha/Yedek sandığa ekleme için).
        /// </summary>
        [HttpGet("eksik-urunler-by-proje/{projeId}")]
        public async Task<ActionResult> GetEksikUrunlerByProje(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.SandikIslemleri.Queries.GetEksikUrunlerByProjeQuery { ProjeId = projeId });
            return result.ToActionResult();
        }

        [HttpPost("urunleri-toplu-tasi")]
        public async Task<ActionResult> SandikUrunleriTopluTasi(
            [FromBody] SandikUrunleriTopluTasiCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return result.ToActionResult();
        }

        /// <summary>
        /// Yanlışlıkla sevk edilen sandığın kilidini açar ve sevk öncesi durumuna döndürür.
        /// </summary>
        [HttpPost("kilidi-ac")]
        public async Task<ActionResult> SandikKilidiAc([FromBody] SandikKilidiAcCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Sevkiyat kaydı korunarak açılan sandığın düzeltme modunu kapatır.
        /// Yeni sevkiyat oluşturmaz, mevcut sevkiyat geçmişini değiştirmez.
        /// </summary>
        /// <summary>
        /// Projede sevk sonrası düzeltmeye açık bütün sandıkların düzeltmesini tek işlemde tamamlar.
        /// </summary>
        [HttpPost("sevkiyat-duzeltme-toplu-tamamla")]
        public async Task<ActionResult> SandikSevkiyatDuzeltmeTopluTamamla([FromBody] SandikSevkiyatDuzeltmeTopluTamamlaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpGet("{sandikId}/sevk-sonrasi-duzeltme")]
        public async Task<ActionResult> GetSevkSonrasiDuzeltme(int sandikId)
        {
            var result = await _mediator.Send(new GetSandikDuzeltmeDurumuQuery { SandikId = sandikId });
            return result.ToActionResult();
        }

        [HttpPost("sevkiyat-duzeltme-tamamla")]
        public async Task<ActionResult> SandikSevkiyatDuzeltmeTamamla([FromBody] SandikSevkiyatDuzeltmeTamamlaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Manuel eklenen ürünü siler.
        /// Normal projeler: cekiSatiriId ile, Saha/Yedek: sandikIcerikId ile.
        /// </summary>
        [HttpDelete("manuel-urun-sil")]
        public async Task<ActionResult> ManuelUrunSil([FromQuery] int projeId, [FromQuery] int? cekiSatiriId = null, [FromQuery] int? sandikIcerikId = null)
        {
            var result = await _mediator.Send(new ManuelUrunSilCommand
            {
                CekiSatiriId = cekiSatiriId,
                SandikIcerikId = sandikIcerikId,
                ProjeId = projeId
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Boş sandığı veya sadece işlem görmemiş manuel ürün içeren manuel sandığı siler.
        /// </summary>
        [HttpDelete("sil")]
        public async Task<ActionResult> SandikSil([FromQuery] int sandikId, [FromQuery] int projeId)
        {
            var result = await _mediator.Send(new SandikSilCommand { SandikId = sandikId, ProjeId = projeId });
            return result.ToActionResult();
        }
    }
}
