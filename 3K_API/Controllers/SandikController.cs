using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.DTOs;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Entities;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SandikController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SandikController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// İş akışı 3: Projeye ait sandıkları listele
        /// </summary>
        [HttpGet("proje/{projeId}")]
        public async Task<ActionResult<IEnumerable<Sandik>>> GetProjeSandiklari(int projeId)
        {
            var result = await _mediator.Send(new GetProjeSandiklariQuery { ProjeId = projeId });
            return Ok(result);
        }

        /// <summary>
        /// İş akışı 3: Sandık içeriğini getir
        /// </summary>
        [HttpGet("{sandikId}/icerik")]
        public async Task<ActionResult<SandikDetayDto>> GetSandikIcerik(int sandikId)
        {
            try
            {
                var result = await _mediator.Send(new GetSandikIcerikQuery { SandikId = sandikId });
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// İş akışı 3, 7, 8: Ürün güncelle (adet, paketleyen, kontrol, açıklama)
        /// </summary>
        [HttpPut("urun-guncelle")]
        public async Task<ActionResult> UrunGuncelle([FromBody] UrunGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok(new { message = "Ürün güncellendi." }) : NotFound(new { message = "Ürün bulunamadı." });
        }

        /// <summary>
        /// İş akışı 4: Fiili sandık değiştir
        /// </summary>
        [HttpPut("degistir")]
        public async Task<ActionResult> SandikDegistir([FromBody] FiiliSandikDegistirCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok(new { message = "Sandık değiştirildi." }) : NotFound(new { message = "İşlem başarısız." });
        }

        /// <summary>
        /// İş akışı 9: Ekrandan sisteme "manuel" ürün satırı eklenmesi.
        /// </summary>
        [HttpPost("manuel-ekle")]
        public async Task<ActionResult> ManuelUrunEkle([FromBody] ManuelUrunEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok(new { message = "Manuel ürün eklendi." }) : BadRequest(new { message = "Eklerken hata oluştu." });
        }

        /// <summary>
        /// İş akışı 9: Ürün satırının durumunu pasif / iptal olarak işaretle.
        /// </summary>
        [HttpPost("iptal")]
        public async Task<ActionResult> UrunIptal([FromBody] UrunIptalCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok(new { message = "Ürün iptal edildi." }) : NotFound(new { message = "Ürün bulunamadı." });
        }

        /// <summary>
        /// İş akışı 10: Bekleyen eksik ürünü mevcut bir stok ile karşıla.
        /// </summary>
        [HttpPost("stoktan-karsila")]
        public async Task<ActionResult> StoktanKarsila([FromBody] StoktanKarsilaCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return result ? Ok(new { message = "Ürün stoktan başarıyla karşılandı." }) : BadRequest(new { message = "İşlem başarısız." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// İş akışı 11: Bekleyen eksik ürünü farklı bir Birim/Tesis transferi (FB) ile karşıla.
        /// </summary>
        [HttpPost("fbden-karsila")]
        public async Task<ActionResult> FBDenKarsila([FromBody] FBDenKarsilaCommand command)
        {
            var result = await _mediator.Send(command);
            return result ? Ok(new { message = "Ürün FB'den karşılandı." }) : BadRequest(new { message = "İşlem başarısız." });
        }
    }
}
