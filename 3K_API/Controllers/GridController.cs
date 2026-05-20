using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GridController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GridController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Grid ürün listesi — proje bazında tüm ürünler ve durumları.
        /// </summary>
        [HttpGet("{projeId}/urunler")]
        public async Task<ActionResult> GetGridUrunler(int projeId)
        {
            var result = await _mediator.Send(new GetGridUrunlerQuery { ProjeId = projeId });
            return result.ToActionResult();
        }

        /// <summary>
        /// Grid personeli ürün durumunu günceller.
        /// </summary>
        [HttpPut("durum-guncelle")]
        public async Task<ActionResult> DurumGuncelle([FromBody] GridDurumGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Grid durumunu sıfırlar — ürünü çeki yüklendiğindeki ham durumuna döndürür.
        /// </summary>
        [HttpPut("durum-sifirla")]
        public async Task<ActionResult> DurumSifirla([FromBody] GridDurumSifirlaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Birden fazla ürünü tek seferde SevkEdildi yapar.
        /// </summary>
        [HttpPost("toplu-sevk")]
        public async Task<ActionResult> TopluSevk([FromBody] GridTopluSevkCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Grid tarafında manuel ürün ekler.
        /// </summary>
        [HttpPost("manuel-urun-ekle")]
        public async Task<ActionResult> ManuelUrunEkle([FromBody] GridManuelUrunEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Kalite durumunu günceller (tekli veya toplu).
        /// </summary>
        [HttpPut("kalite-durum")]
        public async Task<ActionResult> KaliteDurumGuncelle([FromBody] KaliteDurumGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Süreç durumunu günceller (tekli veya toplu).
        /// </summary>
        [HttpPut("surec-durum")]
        public async Task<ActionResult> SurecDurumGuncelle([FromBody] SurecDurumGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Seçili ürünleri toplu olarak belirli Grid durumuna getirir (Tam Geldi / Grid Kapandı / İptal).
        /// </summary>
        [HttpPut("toplu-durum-guncelle")]
        public async Task<ActionResult> TopluDurumGuncelle([FromBody] GridTopluDurumGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Seçili ürünlerin Grid durumlarını toplu olarak sıfırlar (geri alır).
        /// </summary>
        [HttpPut("toplu-sifirla")]
        public async Task<ActionResult> TopluSifirla([FromBody] GridTopluSifirlaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
