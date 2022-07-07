// ReSharper disable once RedundantUsingDirective
using Sandstorm.Android;
// ReSharper disable once RedundantUsingDirective
using Sandstorm.iOS;
using Sandstorm.Unity;

namespace Sandstorm
{
    public class SandstormUnityContext : SandstormContext
    {
        public Sandstorm CreateSandstorm()
        {
#if UNITY_EDITOR
            return new SandstormSDKUnity();
#elif UNITY_ANDROID
            return new SandstormSDKAndroid();
#elif UNITY_IOS
            return new SandstormSDKiOS();
#else
            return new SandstormSDKUnity();
#endif
        }

        public SandstormUnityAssetsLoader CreateAssetsLoader() => new SandstormAssetsLoader();

        public SandstormLogger CreateSandstormLogger() => new SandstormUnityLogger();
    }
}