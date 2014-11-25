namespace Smidge
{
    public interface ISmidgeConfig
    {
        string DataFolder { get; }
        bool IsDebug { get; }
        string ServerName { get; }
        string Version { get; }
    }
}