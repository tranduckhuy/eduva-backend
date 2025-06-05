namespace Eduva.Domain.Common
{
    public interface IEntity<TKey>
    {
        TKey ID { get; set; }
    }
}
