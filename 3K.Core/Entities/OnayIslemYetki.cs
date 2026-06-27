namespace _3K.Core.Entities
{
    public class OnayIslemYetki : BaseEntity
    {
        public string IslemKodu { get; set; } = string.Empty;
        public int RolId { get; set; }

        public virtual Rol Rol { get; set; } = null!;
    }
}
