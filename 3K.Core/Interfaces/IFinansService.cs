using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IFinansService
    {
        Task<FinansDashboardModel> DashboardAsync(DateTime? baslangic, DateTime? bitis, CancellationToken cancellationToken);
        Task<FinansDashboardModel> DashboardOperasyonAsync(DateTime? baslangic, DateTime? bitis, CancellationToken cancellationToken);
        Task<FinansDashboardModel> DashboardGelirAsync(DateTime? baslangic, DateTime? bitis, CancellationToken cancellationToken);
        Task<FinansDashboardModel> DashboardGiderAsync(DateTime? baslangic, DateTime? bitis, CancellationToken cancellationToken);
        Task<FinansDashboardModel> DashboardNetAsync(DateTime? baslangic, DateTime? bitis, CancellationToken cancellationToken);
        Task<FinansDashboardModel> DashboardDurumTutarlariAsync(DateTime? baslangic, DateTime? bitis, CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansProjeOzetModel>> ProjelerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansProjeSecenekModel>> ProjeSecenekleriAsync(
            string? arama,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansIsKaydiModel>> IsKayitlariAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinansIsKaydiModel>> IsKayitlariSecimAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
        Task<FinansIsKaydiModel?> IsKaydiGetirAsync(int id, CancellationToken cancellationToken);
        Task<FinansIsKaydiModel> IsKaydiOlusturAsync(FinansIsKaydiKaydetModel model, CancellationToken cancellationToken);
        Task<FinansIsKaydiModel?> IsKaydiGuncelleAsync(int id, FinansIsKaydiKaydetModel model, CancellationToken cancellationToken);
        Task<bool> IsKaydiIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
        Task<bool> IsKaydiGeriAlAsync(int id, CancellationToken cancellationToken);
        Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinansAylikIsModel>> AylikOzetAsync(int yil, int ay, CancellationToken cancellationToken);
        Task<FinansAylikSayfaliSonuc> AylikOzetSayfaliAsync(int yil, int ay, FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansOzelIsModel>> OzelIslerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<bool> OzelIsAylikDegerGuncelleAsync(int id, FinansAylikDegerModel model, CancellationToken cancellationToken);

        Task<FinansSayfaliSonuc<FinansSiparisModel>> SiparislerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<FinansSiparisModel?> SiparisGetirAsync(int id, CancellationToken cancellationToken);
        Task<FinansSiparisModel> SiparisOlusturAsync(FinansSiparisOlusturModel model, CancellationToken cancellationToken);
        Task<FinansSiparisModel?> SiparisGuncelleAsync(int id, FinansSiparisGuncelleModel model, CancellationToken cancellationToken);
        Task<bool> SiparisIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
        Task<bool> SiparisGeriAlAsync(int id, CancellationToken cancellationToken);

        Task<FinansSayfaliSonuc<FinansFaturaModel>> FaturalarAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<FinansFaturaModel?> FaturaGetirAsync(int id, CancellationToken cancellationToken);
        Task<FinansFaturaModel> FaturaOlusturAsync(FinansFaturaOlusturModel model, CancellationToken cancellationToken);
        Task<FinansFaturaModel?> FaturaGuncelleAsync(int id, FinansFaturaGuncelleModel model, CancellationToken cancellationToken);
        Task<bool> FaturaIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
        Task<bool> FaturaGeriAlAsync(int id, CancellationToken cancellationToken);

        Task<IReadOnlyList<FinansDuzenliIsModel>> DuzenliIslerAsync(bool sadeceAktif, CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansDuzenliIsModel>> DuzenliIslerSayfaliAsync(bool sadeceAktif, string? arama, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<FinansDuzenliIsModel> DuzenliIsOlusturAsync(FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken);
        Task<FinansDuzenliIsModel?> DuzenliIsGuncelleAsync(int id, FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken);
        Task<FinansDonemOlusturSonucModel> DuzenliIsDonemiOlusturAsync(DateTime referansTarihi, CancellationToken cancellationToken);

        Task<FinansSayfaliSonuc<FinansGiderModel>> GiderlerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<FinansGiderModel> GiderOlusturAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken);
        Task<FinansGiderModel?> GiderGuncelleAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken);
        Task<bool> GiderIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
        Task<bool> GiderGeriAlAsync(int id, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinansGiderKategoriModel>> GiderKategorileriAsync(bool sadeceAktif, CancellationToken cancellationToken);
        Task<FinansGiderKategoriModel> GiderKategoriOlusturAsync(FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken);
        Task<FinansGiderKategoriModel?> GiderKategoriGuncelleAsync(int id, FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinansGiderKalemiModel>> GiderKalemleriAsync(int? kategoriId, bool sadeceAktif, CancellationToken cancellationToken);
        Task<FinansGiderKalemiModel> GiderKalemiOlusturAsync(FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken);
        Task<FinansGiderKalemiModel?> GiderKalemiGuncelleAsync(int id, FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken);
        Task<FinansGiderKalemiModel?> GideriKutuphaneyeKaydetAsync(int giderId, FinansGideriKutuphaneyeKaydetModel model, CancellationToken cancellationToken);

        Task<IReadOnlyList<FinansUrunModel>> UrunlerAsync(bool sadeceAktif, DateTime? tarifeTarihi, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinansUrunSecenekModel>> UrunSecenekleriAsync(CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansUrunModel>> UrunlerSayfaliAsync(bool sadeceAktif, DateTime? tarifeTarihi, string? arama, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<FinansUrunModel> UrunOlusturAsync(FinansUrunKaydetModel model, CancellationToken cancellationToken);
        Task<FinansUrunModel?> UrunGuncelleAsync(int id, FinansUrunKaydetModel model, CancellationToken cancellationToken);
        Task<bool> UrunPasiflestirAsync(int id, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinansFiyatTarifesiModel>> FiyatTarifeleriAsync(int? urunId, int? yil, bool sadeceAktif, CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansFiyatTarifesiModel>> FiyatTarifeleriSayfaliAsync(int? urunId, int? yil, bool sadeceAktif, string? arama, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<FinansFiyatTarifesiModel> FiyatTarifesiOlusturAsync(FinansFiyatTarifesiKaydetModel model, CancellationToken cancellationToken);
        Task<FinansFiyatTarifesiModel?> FiyatTarifesiGuncelleAsync(int id, FinansFiyatTarifesiKaydetModel model, CancellationToken cancellationToken);

        Task<FinansRaporModel> RaporVerisiAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<FinansSayfaliSonuc<FinansDegisiklikModel>> DegisiklikGecmisiAsync(string? varlikTuru, int? varlikId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Üretim modülünün doğrudan kullanacağı daraltılmış finans aktarım kapısı.
    /// </summary>
    public interface IFinansUretimAktarimService
    {
        Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken);
    }

    // Eski/harici entegrasyon adlandırmaları için dar arayüzün okunabilir kısa adı.
    public interface IFinansAktarimService : IFinansUretimAktarimService { }

    public interface IFinansRaporService
    {
        Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
        Task<byte[]> AylikExcelAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
        Task<byte[]> AylikPdfAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
        Task<byte[]> AylikZipAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
    }
}
