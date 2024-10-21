
namespace Smidge
{

    /// <summary>
    /// An interface that returns the name of an options profile to use for the current request.
    /// </summary>
    public interface ISmidgeProfileStrategy
    {
        string GetCurrentProfileName();
    }
}
