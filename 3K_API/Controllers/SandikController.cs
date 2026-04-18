using MediatR;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPut("degistir")]
        public async Task<ActionResult> SandikDegistir([FromBody] FiiliSandikDegistirCommand command)
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

        [HttpPost("iptal")]
        public async Task<ActionResult> UrunIptal([FromBody] UrunIptalCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("stoktan-karsila")]
        public async Task<ActionResult> StoktanKarsila([FromBody] StoktanKarsilaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("fbden-karsila")]
        public async Task<ActionResult> FBDenKarsila([FromBody] FBDenKarsilaCommand command)
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
        /// 3K personeli ürün teslim alır — kümülatif (parça parça gelebilir).
        /// </summary>
        [HttpPut("teslim-al")]
        public async Task<ActionResult> TeslimAl([FromBody] UcKTeslimAlCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Birden fazla ürünü tek seferde teslim alır.
        /// </summary>
        [HttpPost("toplu-teslim-al")]
        public async Task<ActionResult> TopluTeslimAl([FromBody] UcKTopluTeslimAlCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Grid sevk etti ama 3K tarafında eksik/gelmemiş ürünler raporu.
        /// </summary>
        [HttpGet("{projeId}/eksik-urunler")]
        public async Task<ActionResult> EksikUrunler(int projeId)
        {
            var result = await _mediator.Send(new GetEksikUrunlerQuery { ProjeId = projeId });
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
        /// Ayrıca sadece Admin kapatabilir (Command tarafında rolü belirtilir).
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
    }
}
