namespace FakeMicro.Interfaces.Services
{
    public interface IConfigService
    {
        string GetValue(string key);
        T GetSection<T>(string key) where T : class, new();
    }
}