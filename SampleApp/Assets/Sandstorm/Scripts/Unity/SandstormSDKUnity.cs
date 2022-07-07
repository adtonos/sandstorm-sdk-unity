using UnityEngine;

namespace Sandstorm.Unity
{
    public class SandstormSDKUnity : Sandstorm
    {
        private const string AdTonosKeyConsents = "AdTonosKeyConsents";

        private bool _isInitialized = false;
        private bool _isStarted = false;

        public void Dispose()
        {
            _isInitialized = false;
            _isStarted = false;
        }

        public void Initialize()
        {
            _isInitialized = true;
        }

        public void Start(AdTonosConsent consents)
        {
            _isStarted = true;
        }

        public void SetNumberEightKey(string key) {

        }

        public AdTonosConsent? LoadLatestConsents()
        {
            var result = PlayerPrefs.GetInt(AdTonosKeyConsents, -1);
            switch (result)
            {
                case 0:
                    return AdTonosConsent.AllowAll;
                case 1:
                    return AdTonosConsent.None;
                default:
                    return null;
            }
        }

        public void SaveConsents(AdTonosConsent consents)
        {
            PlayerPrefs.SetInt(AdTonosKeyConsents, consents == AdTonosConsent.AllowAll ? 0 : 1);
            PlayerPrefs.Save();
        }

        public SandstormVastUrlBuilder CreateBuilder()
        {
            return new SandstormBuilderUnityInternal();
        }

        public bool IsStarted() => _isStarted;

        public bool IsInitialized() => _isInitialized;
    }

    internal class SandstormBuilderUnityInternal : SandstormVastUrlBuilder
    {

        private const string Replace = "XXXXX";
        private static string Link = $"https://play.adtonos.com/xml/{Replace}/vast.xml";

        private string AdTonosKey { get; set; }

        public SandstormVastUrlBuilder SetLanguage(string lang)
        {
            return this;
        }

        public SandstormVastUrlBuilder SetAdTonosKey(string adtonosKey)
        {
            AdTonosKey = adtonosKey;
            return this;
        }

        public SandstormVastUrlBuilder SetAdType(SandstormAdType adType)
        {
            return this;
        }

        public SandstormVastUrlBuilder SetPrivateIp(string ip)
        {
            return this;
        }

        public string Build()
        {
            if (string.IsNullOrEmpty(AdTonosKey))
            {
                throw new SandstormInvalidKeyException();
            }

            return Link.Replace(Replace, AdTonosKey);
        }


    }
}