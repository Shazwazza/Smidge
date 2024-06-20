using Smidge.Options;

namespace Smidge.Tests.Helpers
{
    public class FakeProfileStrategy : ISmidgeProfileStrategy
    {
        public static readonly ISmidgeProfileStrategy DebugProfileStrategy = new FakeProfileStrategy(SmidgeOptionsProfile.Debug);
        public static readonly ISmidgeProfileStrategy DefaultProfileStrategy = new FakeProfileStrategy(SmidgeOptionsProfile.Default);


        public FakeProfileStrategy()
        {
            ProfileName = SmidgeOptionsProfile.Default;
        }

        public FakeProfileStrategy(string profileName)
        {
            ProfileName = profileName;
        }


        public string ProfileName { get; set; }


        public string GetCurrentProfileName() => ProfileName;

        
    }
}
