namespace Smidge
{
    public interface ISmidgeConfig
    {
        string DataFolder { get; }        
        string ServerName { get; }
        string Version { get; }
    }
}