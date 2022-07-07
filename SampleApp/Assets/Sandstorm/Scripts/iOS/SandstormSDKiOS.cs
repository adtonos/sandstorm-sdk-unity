#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
#endif

namespace Sandstorm.iOS
{
    public class SandstormSDKiOS : Sandstorm
    {
        private const int ConsentAllowAllOrdinal = 0;
        private const int ConsentNoneOrdinal = 1;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void thunder_initialize();
        
        [DllImport("__Internal")]
        private static extern void thunder_dispose();
        [DllImport("__Internal")]
        private static extern void thunder_start(int consent);
        [DllImport("__Internal")]
        private static extern int thunder_load_latest_consents();
        [DllImport("__Internal")]
        private static extern void thunder_save_consents(int consent);
        [DllImport("__Internal")]
        private static extern int thunder_create_builder();
        
        [DllImport("__Internal")]
        internal static extern void thunder_set_language(int builderId, string language);
        [DllImport("__Internal")]
        internal static extern void thunder_set_ad_tonos_key(int builderId, string key);
        [DllImport("__Internal")]
        internal static extern void thunder_set_ad_type(int builderId, int adType);
        [DllImport("__Internal")]
        private static extern void thunder_set_number_eight_key(string key);

#if LIB_PRIVATE_IP
        [DllImport("__Internal")]
        internal static extern void thunder_set_private_ip(int builderId, string ip);
#endif
        
        [DllImport("__Internal")]
        internal static extern string thunder_builder_build(int builderId);

        [DllImport("__Internal")]
        internal static extern bool thunder_is_started();
#endif

        private bool _isInitailized = false;

        public bool IsStarted()
        {
#if UNITY_IOS && !UNITY_EDITOR
                return  thunder_is_started();
#endif
            return false;
        }

        public bool IsInitialized() => _isInitailized;

        public void Dispose()
        {
#if UNITY_IOS && !UNITY_EDITOR
            thunder_dispose();
            _isInitailized = false;
#endif
        }

        public void Initialize()
        {
#if UNITY_IOS && !UNITY_EDITOR
            thunder_initialize();
            _isInitailized = true;
#endif
        }

        public void Start(AdTonosConsent consents)
        {
#if UNITY_IOS && !UNITY_EDITOR
            thunder_start(MapAdTonosConsentToiOSConsent(consents));
#endif
        }

        public AdTonosConsent? LoadLatestConsents()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return MapiOSConsentToAdTonosConsent(thunder_load_latest_consents());
#endif
            return null;
        }

        public void SaveConsents(AdTonosConsent consents)
        {
#if UNITY_IOS && !UNITY_EDITOR
            thunder_save_consents(MapAdTonosConsentToiOSConsent(consents));
#endif
        }

        public SandstormVastUrlBuilder CreateBuilder()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var id = thunder_create_builder();
            return new SandstormiOSBuilder(id);
        
#else
            return null;
#endif
        }

        public void SetNumberEightKey(string key)
        {
#if UNITY_IOS && !UNITY_EDITOR
            thunder_set_number_eight_key(key);
#endif
        }

        private int MapAdTonosConsentToiOSConsent(AdTonosConsent consent)
        {
            switch (consent)
            {
                case AdTonosConsent.AllowAll:
                    return ConsentAllowAllOrdinal;
                case AdTonosConsent.None:
                    return ConsentNoneOrdinal;
            }

            return ConsentNoneOrdinal;
        }

        private AdTonosConsent? MapiOSConsentToAdTonosConsent(int consent)
        {
            switch (consent)
            {
                case ConsentNoneOrdinal:
                    return AdTonosConsent.None;
                case ConsentAllowAllOrdinal:
                    return AdTonosConsent.AllowAll;
                default:
                    return null;
            }
        }
    }
}