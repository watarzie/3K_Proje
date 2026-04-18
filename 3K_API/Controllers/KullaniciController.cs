using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.KullaniciIslemleri.Queries;
using _3K.Application.Features.KullaniciIslemleri.Commands;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Kullanıcı Yönetimi API.
    /// Tüm endpoint'ler [Authorize] — sadece giriş yapmış kullanıcılar erişebilir.
    /// Hangi rolün erişeceği, RBAC menü yetki sistemi ile kontrol edilir.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class KullaniciController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KullaniciController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Tüm kullanıcıları listeler.</summary>
        [HttpGet("liste")]
        public async Task<ActionResult> GetAll()
        {
            var result = await _mediator.Send(new KullaniciListeleQuery());
            return result.ToActionResult();
        }

        /// <summary>
        /// Yeni kullanıcı oluşturur.
        /// Eski register endpoint'inin karşılığı — artık sadece admin panelden erişilir.
        /// </summary>
        [HttpPost("olustur")]
        public async Task<ActionResult> Olustur([FromBody] RegisterCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>Kullanıcı bilgilerini günceller (ad, rol).</summary>
        [HttpPut("guncelle")]
        public async Task<ActionResult> Guncelle([FromBody] KullaniciGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>Kullanıcıyı siler.</summary>
        [HttpDelete("{id}/sil")]
        public async Task<ActionResult> Sil(int id)
        {
            var result = await _mediator.Send(new KullaniciSilCommand { Id = id });
            return result.ToActionResult();
        }

        /// <summary>Kullanıcı şifresini değiştirir (Admin yetkisi).</summary>
        [HttpPut("sifre-degistir")]
        public async Task<ActionResult> SifreDegistir([FromBody] KullaniciSifreDegistirCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
