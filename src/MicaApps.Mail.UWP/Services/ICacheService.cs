namespace Mail.Services;

public interface ICacheService
{
    public void Set<T>(T cache) where T : class;
    public T? Get<T>()  where T : class;
    public void Remove<T>();
}