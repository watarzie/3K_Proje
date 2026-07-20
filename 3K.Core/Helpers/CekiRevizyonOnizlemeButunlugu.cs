using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using _3K.Core.Models;

namespace _3K.Core.Helpers
{
    /// <summary>
    /// Talep oluşturulurken gösterilen revizyon ön izlemesi ile onay sırasında
    /// uygulanacak değişikliklerin aynı olduğunu doğrulayan kanonik özeti üretir.
    /// </summary>
    public static class CekiRevizyonOnizlemeButunlugu
    {
        public const int Surum = 1;

        private static readonly JsonSerializerOptions SerializerOptions =
            new(JsonSerializerDefaults.Web);

        public static string HashOlustur(CekiRevizyonOnizlemeSonuc onizleme)
        {
            ArgumentNullException.ThrowIfNull(onizleme);

            var canonical = new
            {
                Surum,
                onizleme.ProjeId,
                onizleme.AnaCekiId,
                onizleme.ToplamIsaretliSatirSayisi,
                onizleme.EklenenSatirSayisi,
                onizleme.GuncellenenSatirSayisi,
                onizleme.SilinecekSatirSayisi,
                onizleme.RiskliSatirSayisi,
                onizleme.EngelliSatirSayisi,
                onizleme.UygulanabilirMi,
                Uyarilar = SiraliMetinler(onizleme.Uyarilar),
                SandikEtkileri = (onizleme.SandikEtkileri ?? new List<CekiRevizyonSandikEtkisi>())
                    .OrderBy(etki => etki.SandikNo, StringComparer.Ordinal)
                    .Select(etki => new
                    {
                        etki.SandikNo,
                        etki.YeniSandikMi,
                        etki.DurumYenidenHesaplanacakMi,
                        etki.BosKalirsaSilinecekMi,
                        etki.EskiDurumId,
                        etki.MevcutIcerikSayisi,
                        etki.MevcutCekiIcerigiSayisi,
                        etki.TamamlanmisCekiIcerigiSayisi,
                        etki.EskiAd,
                        etki.YeniAd,
                        etki.EskiAdIngilizce,
                        etki.YeniAdIngilizce,
                        etki.EskiEn,
                        etki.YeniEn,
                        etki.EskiBoy,
                        etki.YeniBoy,
                        etki.EskiYukseklik,
                        etki.YeniYukseklik,
                        etki.EskiNetKg,
                        etki.YeniNetKg,
                        etki.EskiGrossKg,
                        etki.YeniGrossKg
                    })
                    .ToArray(),
                Satirlar = (onizleme.Satirlar ?? new List<CekiRevizyonOnizlemeSatiri>())
                    .Where(satir => satir != null)
                    .Select((satir, index) => new { Satir = satir, Index = index })
                    .OrderBy(item => item.Satir.ExcelSatirNo)
                    .ThenBy(item => item.Index)
                    .Select(item => new
                    {
                        item.Satir.ExcelSatirNo,
                        item.Satir.CheckKodu,
                        item.Satir.IslemTipi,
                        item.Satir.RiskSeviyesi,
                        item.Satir.UygulanabilirMi,
                        item.Satir.MevcutCekiSatiriId,
                        item.Satir.EskiSiraNo,
                        item.Satir.YeniSiraNo,
                        item.Satir.BarkodNo,
                        item.Satir.PozNo,
                        item.Satir.Tanim,
                        item.Satir.EskiKoliNo,
                        item.Satir.YeniKoliNo,
                        item.Satir.EskiIstenenAdet,
                        item.Satir.YeniIstenenAdet,
                        item.Satir.IslemGormusMu,
                        item.Satir.IslemGorenAdet,
                        GeriAlmaEtkisi = item.Satir.GeriAlmaEtkisi == null
                            ? null
                            : new
                            {
                                item.Satir.GeriAlmaEtkisi.GridDurumuId,
                                item.Satir.GeriAlmaEtkisi.GridGelenAdet,
                                item.Satir.GeriAlmaEtkisi.TrafoSevkAdet,
                                item.Satir.GeriAlmaEtkisi.GridSevkDurumuId,
                                item.Satir.GeriAlmaEtkisi.GridSevkMiktari,
                                item.Satir.GeriAlmaEtkisi.YenidenSevkGerekliAdet,
                                item.Satir.GeriAlmaEtkisi.GridSevkTarihi,
                                item.Satir.GeriAlmaEtkisi.GridAciklama,
                                item.Satir.GeriAlmaEtkisi.GridPersonelId,
                                item.Satir.GeriAlmaEtkisi.UcKDurumuId,
                                item.Satir.GeriAlmaEtkisi.UcKKarsilamaTipiId,
                                item.Satir.GeriAlmaEtkisi.GelenMiktar,
                                item.Satir.GeriAlmaEtkisi.TeslimTarihi,
                                item.Satir.GeriAlmaEtkisi.KaynakHedefProjeNo,
                                item.Satir.GeriAlmaEtkisi.UcKAciklama,
                                item.Satir.GeriAlmaEtkisi.KarsilananMiktar,
                                item.Satir.GeriAlmaEtkisi.StokKarsilanan,
                                item.Satir.GeriAlmaEtkisi.ProjeKarsilanan,
                                item.Satir.GeriAlmaEtkisi.ProjeGonderilen,
                                item.Satir.GeriAlmaEtkisi.TedarikciKarsilanan,
                                item.Satir.GeriAlmaEtkisi.HataliMiktar,
                                item.Satir.GeriAlmaEtkisi.GeriGonderilenMiktar,
                                item.Satir.GeriAlmaEtkisi.GeriGonderilmeSebebiId,
                                item.Satir.GeriAlmaEtkisi.KaynakProjeId,
                                item.Satir.GeriAlmaEtkisi.KaliteDurumId,
                                item.Satir.GeriAlmaEtkisi.SurecDurumId,
                                item.Satir.GeriAlmaEtkisi.PaketleyenId,
                                item.Satir.GeriAlmaEtkisi.KontrolEdenId,
                                item.Satir.GeriAlmaEtkisi.SandikIcerikSayisi,
                                item.Satir.GeriAlmaEtkisi.TahsisMiktari,
                                item.Satir.GeriAlmaEtkisi.KonulanAdet,
                                item.Satir.GeriAlmaEtkisi.EksikAdet,
                                item.Satir.GeriAlmaEtkisi.SandikStokKarsilanan,
                                item.Satir.GeriAlmaEtkisi.SandikProjeKarsilanan,
                                item.Satir.GeriAlmaEtkisi.SandikTedarikciKarsilanan,
                                item.Satir.GeriAlmaEtkisi.StokHareketSayisi,
                                item.Satir.GeriAlmaEtkisi.StoktanKarsilananMiktar,
                                item.Satir.GeriAlmaEtkisi.FazlaTeslimStogaAktarilanMiktar,
                                item.Satir.GeriAlmaEtkisi.DigerStokHareketMiktari,
                                item.Satir.GeriAlmaEtkisi.GelenAktifProjeTransferSayisi,
                                item.Satir.GeriAlmaEtkisi.GelenAktifProjeTransferMiktari,
                                StokHareketleri = (item.Satir.GeriAlmaEtkisi.StokHareketleri ??
                                        new List<CekiRevizyonStokHareketEtkisi>())
                                    .OrderBy(hareket => hareket.StokHareketiId)
                                    .Select(hareket => new
                                    {
                                        hareket.StokHareketiId,
                                        hareket.StokKaydiId,
                                        hareket.IslemTipiId,
                                        hareket.Miktar
                                    })
                                    .ToArray(),
                                GelenAktifProjeTransferleri =
                                    (item.Satir.GeriAlmaEtkisi.GelenAktifProjeTransferleri ??
                                        new List<CekiRevizyonGelenTransferEtkisi>())
                                    .OrderBy(transfer => transfer.ProjeTransferiId)
                                    .Select(transfer => new
                                    {
                                        transfer.ProjeTransferiId,
                                        transfer.KaynakProjeId,
                                        transfer.KaynakCekiSatiriId,
                                        transfer.Miktar
                                    })
                                    .ToArray()
                            },
                        Degisiklikler = SiraliMetinler(item.Satir.Degisiklikler),
                        Engeller = SiraliMetinler(item.Satir.Engeller),
                        Uyarilar = SiraliMetinler(item.Satir.Uyarilar),
                        Sorunlar = (item.Satir.Sorunlar ?? new List<CekiRevizyonSorunu>())
                            .OrderBy(sorun => sorun.Kod, StringComparer.Ordinal)
                            .ThenBy(sorun => sorun.ExcelSatirNo)
                            .ThenBy(sorun => sorun.Mesaj, StringComparer.Ordinal)
                            .Select(sorun => new
                            {
                                sorun.Kod,
                                sorun.Kategori,
                                sorun.Mesaj,
                                sorun.ExcelSatirNo,
                                sorun.CheckKodu,
                                sorun.SiraNo,
                                sorun.BarkodNo,
                                sorun.PozNo,
                                sorun.Tanim,
                                sorun.SandikNo
                            })
                            .ToArray()
                    })
                    .ToArray()
            };

            var canonicalBytes = JsonSerializer.SerializeToUtf8Bytes(canonical, SerializerOptions);
            return Convert.ToHexString(SHA256.HashData(canonicalBytes)).ToLowerInvariant();
        }

        public static bool HashDogrula(CekiRevizyonOnizlemeSonuc onizleme, string? beklenenHash)
        {
            if (string.IsNullOrWhiteSpace(beklenenHash))
                return false;

            var beklenenBytes = Encoding.ASCII.GetBytes(beklenenHash.Trim().ToLowerInvariant());
            var guncelBytes = Encoding.ASCII.GetBytes(HashOlustur(onizleme));

            return beklenenBytes.Length == guncelBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(beklenenBytes, guncelBytes);
        }

        private static string[] SiraliMetinler(IEnumerable<string> metinler)
        {
            return (metinler ?? Array.Empty<string>())
                .Where(metin => !string.IsNullOrWhiteSpace(metin))
                .Select(metin => metin.Trim())
                .OrderBy(metin => metin, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
