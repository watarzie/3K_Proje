using _3K.Core.Models;

namespace _3K.Application.Features.BildirimIslemleri.DTOs
{
    internal static class BildirimDtoMapper
    {
        public static BildirimDto ToDto(this BildirimSorguKaydi bildirim)
        {
            return new BildirimDto
            {
                Id = bildirim.Id,
                TipId = bildirim.TipId,
                Baslik = bildirim.Baslik,
                Mesaj = bildirim.Mesaj,
                OlusturulmaTarihi = bildirim.OlusturulmaTarihi,
                OkunduMu = bildirim.OkunduMu,
                OkunmaTarihi = bildirim.OkunmaTarihi,
                HedefUrl = bildirim.HedefUrl,
                ReferansTipi = bildirim.ReferansTipi,
                ReferansId = bildirim.ReferansId,
                Metadata = new BildirimMetadataDto
                {
                    ProjeId = bildirim.ProjeId,
                    ProjeNo = bildirim.ProjeNo,
                    OlusturanKullaniciId = bildirim.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = bildirim.OlusturanKullaniciAdi
                }
            };
        }
    }
}
