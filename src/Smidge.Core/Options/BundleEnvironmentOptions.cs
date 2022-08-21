using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Smidge.Options
{


    /// <summary>
    /// Defines the different bundle options for various configuration profiles such as Debug or Production
    /// </summary>
    public sealed class BundleEnvironmentOptions
    {
        /// <summary>
        /// Creates a new Options Builder
        /// </summary>
        /// <returns></returns>
        public static BundleEnvironmentOptionsBuilder Create()
        {
            var options = new BundleEnvironmentOptions();
            return new BundleEnvironmentOptionsBuilder(options);
        }



        private readonly IDictionary<string, BundleOptions> _profileOptions;


        /// <summary>
        /// Constructor, sets default options
        /// </summary>
        public BundleEnvironmentOptions()
        {
            _profileOptions = new ConcurrentDictionary<string, BundleOptions>();

            DebugOptions = new BundleOptions
            {

                ProcessAsCompositeFile = false,
                CompressResult = false,
                CacheControlOptions = new CacheControlOptions
                {
                    EnableETag = false,
                    CacheControlMaxAge = 0
                }
            };
            ProductionOptions = new BundleOptions();
        }

        /// <summary>
        /// The options for the "debug" profile
        /// </summary>
        public BundleOptions DebugOptions
        {
            get => this[SmidgeOptionsProfile.Debug];
            set => this[SmidgeOptionsProfile.Debug] = value;
        }

        /// <summary>
        /// The options for "production" profile
        /// </summary>
        public BundleOptions ProductionOptions
        {
            get => this[SmidgeOptionsProfile.Production];
            set => this[SmidgeOptionsProfile.Production] = value;
        }



        /// <summary>
        /// Gets or sets the <see cref="BundleOptions"/> for the specified profile.
        /// If the profile has not been previously configured, returns a new <see cref="BundleOptions"/> instance.
        /// </summary>
        /// <param name="profileName">Name of the profile.</param>
        /// <returns></returns>
        public BundleOptions this[string profileName]
        {
            get
            {
                if (!TryGetProfileOptions(profileName, out BundleOptions options))
                {
                    // Initialise a new BundleOptions for the requested profile
                    options = new BundleOptions();
                    _profileOptions.Add(profileName, options);
                }

                return options;
            }
            set => _profileOptions[profileName] = value;
        }


        /// <summary>
        /// Gets the <see cref="BundleOptions"/> for the specified profile, if the profile has previously been configured.
        /// </summary>
        /// <param name="profileName">Name of the profile.</param>
        /// <param name="options">The profile options.</param>
        /// <returns></returns>
        public bool TryGetProfileOptions(string profileName, out BundleOptions options)
        {
            return _profileOptions.TryGetValue(profileName, out options);
        }

    }
}
