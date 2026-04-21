namespace _3K.Core.Enums
{
    /// <summary>
    /// LookupGridDurum Id'leriyle birebir eşleşir. 13 kayıt.
    /// </summary>
    public enum GridDurum
    {
        Bekliyor = 1,
        Uretimde = 2,
        StokHazir = 3,
        SevkEdildi = 4,
        KismiSevkEdildi = 5,
        Bekletiliyor = 6,
        IptalEdildi = 7,
        TamGeldi = 8,
        EksikGeldi = 9,
        Gelmedi = 10,
        TrafoSevk = 11,
        Iptal = 12,
        Sipariste = 13
    }
}
