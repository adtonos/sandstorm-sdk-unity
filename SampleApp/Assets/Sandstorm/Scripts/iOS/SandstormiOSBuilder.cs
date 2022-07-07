using System;
using Sandstorm;

namespace Sandstorm.iOS
{
    public class SandstormiOSBuilder : SandstormVastUrlBuilder
    {
        private const string Tag = "SandstormiOSBuilder";

        private const string InvalidLanguage = "INVALID_LANGUAGE";
        private const string InvalidKey = "INVALID_KEY";
        private const string UnknownError = "UNKNOWN_ERROR";
        private const string InvalidBuilderId = "INVALID_BUILDER_ID";



        private readonly int _builderId;

        public SandstormiOSBuilder(int builderId)
        {
            _builderId = builderId;
        }


        public SandstormVastUrlBuilder SetLanguage(string lang)
        {
#if UNITY_IOS && !UNITY_EDITOR
            SandstormSDKiOS.thunder_set_language(builderId: _builderId, language: lang);
#endif
            return this;
        }

        public SandstormVastUrlBuilder SetAdTonosKey(string adtonosKey)
        {
#if UNITY_IOS && !UNITY_EDITOR
            SandstormSDKiOS.thunder_set_ad_tonos_key(builderId: _builderId, key: adtonosKey);
#endif
            return this;
        }

        public SandstormVastUrlBuilder SetAdType(SandstormAdType adType)
        {
            Logs.Log(tag: Tag, () => $"setting ad type: {adType}");
#if UNITY_IOS && !UNITY_EDITOR
            SandstormSDKiOS.thunder_set_ad_type(builderId: _builderId, adType: (int)adType);
#endif
            return this;
        }

        public SandstormVastUrlBuilder SetPrivateIp(string ip)
        {
            Logs.Log(tag: Tag, () => $"Will set private IP?");
#if UNITY_IOS && !UNITY_EDITOR && LIB_PRIVATE_IP
            Logs.Log(tag: Tag, () => $"setting private ip {ip}");
            SandstormSDKiOS.thunder_set_private_ip(builderId: _builderId, ip: ip);
#endif
            return this;
        }

        public string Build()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var result = SandstormSDKiOS.thunder_builder_build(builderId: _builderId);
            if (Equals(InvalidLanguage, result)) {
                throw new SandstormInvalidLanguageException();
            }
            if (Equals(InvalidKey, result)) {
                throw new SandstormInvalidKeyException();
            }
            if (Equals(UnknownError, result)) {
                throw new Exception();
            }
            if (Equals(InvalidBuilderId, result)) {
                throw new Exception();
            }
            return result;
#else
            return null;
#endif
        }
    }
}