namespace Mail.Services;

public interface ICacheService
{
    public void AddOrReplaceCache<T>(T cache) where T : class;
    public void RemoveCache<T>();
    public T? GetCache<T>()  where T : class;
}