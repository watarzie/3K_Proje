using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Common
{
    /// <summary>
    /// Elle ve toplu kapatma/geri açmanın olay kaydı: kaynağı ve elle geri açmanın sebebini taşır. Aynı kaydetmede
    /// <c>SandikDurumOlayInterceptor</c> bu kaydı görür, sandığa ikinci (otomatik) olay yazmaz ve kapanma tarihini
    /// olayın zamanıyla günceller. Durum geçişi olay sayılmıyorsa (<see cref="SandikDurumOlayKurali.OlayTuru"/>) yazılmaz.
    /// </summary>
    public static class SandikDurumOlayYazici
    {
        public static Task EkleAsync(
            IUnitOfWork unitOfWork,
            Sandik sandik,
            int eskiDurumId,
            SandikDurumOlayKaynagi kaynak,
            int? kullaniciId,
            int? sebepId = null,
            string? not = null)
        {
            var tur = SandikDurumOlayKurali.OlayTuru(eskiDurumId, sandik.DurumId);
            if (tur is null)
                return Task.CompletedTask;

            return unitOfWork.GetRepository<SandikDurumOlayi>().AddAsync(new SandikDurumOlayi
            {
                ProjeId = sandik.ProjeId,
                SandikId = sandik.Id,
                SandikNo = sandik.SandikNo,
                Tur = tur.Value,
                Kaynak = kaynak,
                EskiDurumId = eskiDurumId,
                YeniDurumId = sandik.DurumId,
                SebepId = tur == SandikDurumOlayTuru.Acildi ? sebepId : null,
                Not = tur == SandikDurumOlayTuru.Acildi ? SandikDurumOlayKurali.NotNormalize(not) : null,
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now
            });
        }
    }
}
