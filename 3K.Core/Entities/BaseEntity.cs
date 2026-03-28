namespace 3K.Core.Entities
{
    public abstract class BaseEntity
{
    public int Id { get; set; }
    // İsteğe bağlı olarak buraya CreatedDate, UpdatedDate gibi ortak alanlar da eklenebilir.
}
}