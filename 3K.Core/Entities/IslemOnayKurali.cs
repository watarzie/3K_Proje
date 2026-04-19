using System.ComponentModel.DataAnnotations;

namespace _3K.Core.Entities
{
    public class IslemOnayKurali : BaseEntity
    {
        public int LookupUcKDurumId { get; set; }
        public virtual LookupUcKDurum LookupUcKDurum { get; set; } = null!;

        public bool OnayGerektirirMi { get; set; }
    }
}
