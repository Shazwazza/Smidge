namespace Smidge
{
    public interface ISmidgeConfig
    {
        string DataFolder { get; }
        string Version { get; }
		bool KeepFileExtensions { get; }
    }
}