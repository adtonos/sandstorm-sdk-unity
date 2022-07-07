using System;
using UnityEngine;

namespace Sandstorm.Android
{
    public class SandstormSDKAndroid : Sandstorm
    {
        private const string Tag = "SandstormSDKAndroid";
        private const string JavaPackageSDK = "com.siroccomobile.adtonos.thundersdk.api.ATThunderSDK";
        private const string JavaPackageConsents = "com.siroccomobile.adtonos.thundersdk.api.AdTonosConsent";
        private const string JavaConsentAllowAll = "AllowAll";
        private const int JavaConsentAllowAllOrdinal = 0;
        private const string JavaConsentNone = "None";
        private const int JavaConsentNoneOrdinal = 1;

        private AndroidJavaObject _sdk;


        private bool _isInitailized;

        public SandstormSDKAndroid()
        {
            _sdk = new AndroidJavaObject(JavaPackageSDK);

        }

        public bool IsStarted()
        {
            return AttachJni<bool>(() => _sdk?.Call<bool>("isStarted") ?? false);
        }

        public bool IsInitialized() => _isInitailized;

        private static AndroidJavaObject GetAndroidContext()
        {
            return new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");
        }

        private static T AttachJni<T>(Func<T> action)
        {
            try
            {
                if (action != null)
                {
                    return action.Invoke();
                }
            }
            catch (Exception e)
            {
                Logs.LogError(tag: Tag, () => $@"Failed with exception {e}");
            }
            return default(T);
        }

        private static void AttachJni(Action action)
        {
            AttachJni<bool>(() =>
           {
               action?.Invoke();
               return true;
           });
        }

        private static void RunOnUiThread(Action action)
        {
            try
            {
                GetAndroidContext().Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Logs.LogError(tag: Tag, () => $@"Failed with exception {e}");
                    }
                }));
            }
            catch (Exception e)
            {
                Logs.LogError(tag: Tag, () => $@"Failed with exception {e}");
            }
        }

        public void Dispose()
        {
            AttachJni(() => _sdk?.Call("dispose"));
            _sdk?.Dispose();
            _sdk = null;
            _isInitailized = false;
        }

        public void Initialize()
        {
            var context = GetAndroidContext();
            RunOnUiThread(() =>
            {
                _sdk?.Call("initialize", context);
                _isInitailized = true;
            });
        }

        public void Start(AdTonosConsent consents)
        {
            RunOnUiThread(() =>
            {
                _sdk.Call("start", GetAndroidContext(), MapToAndroidConsent(consents: consents));
            });
        }

        public void SetNumberEightKey(string key)
        {
            RunOnUiThread(() =>
            {
                _sdk?.Call("setNumberEightKey", key);
            });
        }

        public AdTonosConsent? LoadLatestConsents()
        {

            var consentAndroid = AttachJni<AndroidJavaObject>(() => _sdk?.Call<AndroidJavaObject>("loadLatestConsents"));
            return MapAndroidConsent(consents: consentAndroid);
        }

        public void SaveConsents(AdTonosConsent consents)
        {
            var context = GetAndroidContext();
            AttachJni(() => _sdk?.Call("saveConsents", context, MapToAndroidConsent(consents: consents)));
        }

        public SandstormVastUrlBuilder CreateBuilder()
        {
            var result = AttachJni<AndroidJavaObject>(() => _sdk?.Call<AndroidJavaObject>("createBuilder"));
            return new SandstormAndroidBuilder(javaObject: result);
        }

        private static AdTonosConsent? MapAndroidConsent(AndroidJavaObject consents)
        {
            if (consents == null)
            {
                return null;
            }

            try
            {
                return consents.Get<int>("ordinal") switch
                {
                    JavaConsentAllowAllOrdinal => AdTonosConsent.AllowAll,
                    JavaConsentNoneOrdinal => AdTonosConsent.None,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Logs.LogError(tag: Tag, () => $@"Couldn't map android consent got exception {ex}");
                return null;
            }
        }

        private static AndroidJavaObject MapToAndroidConsent(AdTonosConsent consents)
        {
            AndroidJavaClass androidConsent = new AndroidJavaClass(JavaPackageConsents);
            var consentText = consents switch
            {
                AdTonosConsent.None => JavaConsentNone,
                AdTonosConsent.AllowAll => JavaConsentAllowAll,
                _ => JavaConsentNone
            };
            return androidConsent.GetStatic<AndroidJavaObject>(consentText);
        }
    }
}