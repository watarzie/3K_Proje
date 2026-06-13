namespace _3K.Core.Models
{
    public class CekiRevizyonSonuc
    {
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public int AnaCekiId { get; set; }
        public int RevizyonCekiId { get; set; }
        public int EklenenSatirSayisi { get; set; }
        public int GuncellenenSatirSayisi { get; set; }
        public int SilinenSatirSayisi { get; set; }
        public int AtlananSatirSayisi { get; set; }
        public int IslenenRevizyonSatiriSayisi => EklenenSatirSayisi + GuncellenenSatirSayisi + SilinenSatirSayisi;
        public string Mesaj { get; set; } = string.Empty;
        public List<string> Uyarilar { get; set; } = new();
    }
}
