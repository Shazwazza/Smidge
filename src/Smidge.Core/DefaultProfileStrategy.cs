using Smidge.Options;

namespace Smidge
{

    /// <summary>
    /// An implementation of ISmidgeProfileStrategy that will always use the Default profile.
    /// </summary>
    /// <seealso cref="ISmidgeProfileStrategy" />
    public class DefaultProfileStrategy : ISmidgeProfileStrategy
    {
        public string GetCurrentProfileName() => SmidgeOptionsProfile.Default;
    }
}
