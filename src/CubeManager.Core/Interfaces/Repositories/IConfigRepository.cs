namespace CubeManager.Core.Interfaces.Repositories;

public interface IConfigRepository
{
    Task<string?> GetAsync(string key);
    Task<int> GetIntAsync(string key, int defaultValue);
    Task SetAsync(string key, string value);
}
