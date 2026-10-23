namespace _3K.Core.Constants
{
    public static class SahaAktarimConstants
    {
        public const string SandikBazliAktarimAciklamaPrefix = "Kaynak sandık:";

        /// <summary>
        /// Saha projesinde sevk sonrası eksiklerin toplandığı sandığın numarası. Ana projede aynı numara
        /// transformatör sandığıdır ve hiçbir yolla sandık olarak sahaya aktarılmaz.
        /// </summary>
        public const string EksiklerSandikNo = "1";

        /// <summary>Otomatik aktarımın saha projesinde açtığı eksikler sandığının adı.</summary>
        public const string EksiklerSandikAdi = "Sevk sonrası eksikler";

        /// <summary>Otomatik aktarımın sevk edilmiş sandık eksiklerine yazdığı satır açıklaması ön eki.</summary>
        public const string SevkSonrasiEksikAciklamaPrefix = "Sevk sonrası eksik:";

        /// <summary>Otomatik aktarım başlığının açıklama ön eki; listelerde aktarımı tanıtır.</summary>
        public const string OtomatikAktarimAciklamaPrefix = "Otomatik sahaya aktarım:";
    }
}
