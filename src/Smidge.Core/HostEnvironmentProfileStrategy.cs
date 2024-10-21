using Microsoft.Extensions.Hosting;
using Smidge.Options;

namespace Smidge
{
    /// <summary>
    /// An implementation of ISmidgeProfileStrategy that will use the host environment to determine if the Debug profile should be used.
    /// </summary>
    /// <seealso cref="ISmidgeProfileStrategy" />
    public class HostEnvironmentProfileStrategy : ISmidgeProfileStrategy
    {
        private readonly IHostEnvironment _hostEnvironment;


        public HostEnvironmentProfileStrategy(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }


        private string _profileName;

        public string GetCurrentProfileName() => _profileName ??= GetProfileForEnvironment(_hostEnvironment);
        

        protected virtual string GetProfileForEnvironment(IHostEnvironment hostEnvironment)
        {
            return hostEnvironment.IsDevelopment()
                ? SmidgeOptionsProfile.Debug
                : SmidgeOptionsProfile.Default;
        }
    }
}
