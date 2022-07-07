using System;
using UnityEngine;

namespace Sandstorm.Android
{
    public class SandstormAndroidBuilder : SandstormVastUrlBuilder
    {
        private const string Tag = "SandstormAndroidBuilder";
        private const string JavaPackageAdType = "com.siroccomobile.adtonos.thundersdk.api.VastAdType";
        private const string JavaAdTypeRegular = "regular";
        private const string JavaAdTypeBanner = "bannerAd";
        private AndroidJavaObject _javaObject;

        public SandstormAndroidBuilder(AndroidJavaObject javaObject)
        {
            _javaObject = javaObject;
        }

        public SandstormVastUrlBuilder SetLanguage(string lang)
        {
            Logs.Log(tag: Tag, () => $@"Setting language {_javaObject} {lang}");
            try
            {
                _javaObject?.Call("setLanguage", lang);
            }
            catch (Exception ex)
            {
                Logs.LogError(tag: Tag, () => $@"SetLanguage Failed with exception exception = {ex} ");

            }
            return this;
        }

        public SandstormVastUrlBuilder SetAdTonosKey(string adtonosKey)
        {
            _javaObject?.Call("setAdTonosKey", adtonosKey);
            return this;
        }

        public SandstormVastUrlBuilder SetAdType(SandstormAdType adType)
        {
            Logs.Log(tag: Tag, () => $"setting ad type: {adType}");
            _javaObject?.Call("setAdType", MapToAndroidAdType(adType));
            return this;
        }

        public SandstormVastUrlBuilder SetPrivateIp(string ip)
        {
#if LIB_PRIVATE_IP
            _javaObject?.Call("setAdTonosKey", ip);
#endif
            return this;
        }

        public string Build()
        {
            try
            {
                return _javaObject?.Call<string>("build");
            }
            catch (AndroidJavaException ex)
            {
                if (ex.Message.Contains("ThunderInvalidKeyException"))
                {
                    throw new SandstormInvalidKeyException();
                }
                else if (ex.Message.Contains("ThunderInvalidLanguageException"))
                {
                    throw new SandstormInvalidLanguageException();
                }

                Logs.LogError(tag: Tag, () => $@"Failed with exception exception = {ex} ");
                throw ex;
            }
        }

        private static AndroidJavaObject MapToAndroidAdType(SandstormAdType adType)
        {
            AndroidJavaClass androidAdType = new AndroidJavaClass(JavaPackageAdType);
            var adTypeText = adType switch
            {
                SandstormAdType.Regular => JavaAdTypeRegular,
                SandstormAdType.BannerAd => JavaAdTypeBanner,
                _ => JavaAdTypeRegular
            };
            return androidAdType.GetStatic<AndroidJavaObject>(adTypeText);
        }
    }
}