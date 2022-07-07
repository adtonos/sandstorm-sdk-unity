using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sandstorm;
using UnityEngine.UI;

public class SceneManager: MonoBehaviour, SandstormCallback
{
    [SerializeField]
    private GameObject _prefabSandstormView;

    [SerializeField] Button playBtn;
    [SerializeField] Button requestBtn;
    [SerializeField] Text isLoadedText;
    [SerializeField] Dropdown adTypeDropdown;
    [SerializeField] Dropdown bannerPositionDropdown;

    private bool  _isAdsLoaded;
    private bool IsAdsLoaded {
        get{return _isAdsLoaded;}
        set{
            _isAdsLoaded = value; 
            playBtn.interactable = value;
            requestBtn.interactable = !value;
            isLoadedText.text = value.ToString();
        }
    }

    void Awake() 
    {
        playBtn.interactable = false;
        ATSandstormSDK.Initialize(context: new SandstormUnityContext(), dispatcher: FindObjectOfType<SandstormDispatcherUnity>());
        ATSandstormSDK.SetNumberEightKey("U71E94V86CT9ZXY98ABNMFLQ0Y9B"); // only for SandstormSDK which we're using; not needed if using SandstormLiteSDK
        ATSandstormSDK.Start(AdTonosConsent.AllowAll);
    }

    public void OnRequestAdPressed() {
        StartCoroutine(RequestAd());
    }

    public void OnPlayPressed() {
        if (!IsAdsLoaded) {
            return;
        }
        if (ATSandstormSDK.PlayAd() == false) {
            // Handle error or retry request later.
            ATSandstormSDK.Clear();
            IsAdsLoaded = false;
            Debug.Log("Can't play ads");
        }
    }

    public void OnBannerPositionValueChanged() {
        // banner position should be set before playing ad; changing it during playback won't affect currently playing ad
        var position = bannerPositionDropdown.value == 0 ? SandstormBannerPosition.Top : SandstormBannerPosition.Bottom;
        ATSandstormSDK.SetAdBannerPosition(position);
        Debug.Log("Banner position set to:" + position);
    }

    IEnumerator RequestAd()
    {
        Debug.Log("RequestAd");
        yield return new WaitUntil(() => ATSandstormSDK.IsStarted());
        ATSandstormSDK.AddCallback(this);
        IsAdsLoaded = false;

        var builder = ATSandstormSDK.CreateBuilder();
        builder.SetAdTonosKey("KT267qyGPudAugiSt");

        var adType = adTypeDropdown.value == 0 ? SandstormAdType.Regular : SandstormAdType.BannerAd;
        builder.SetAdType(adType);

        var requestResult = ATSandstormSDK.RequestForAds(builder: builder, view: CreateSandstormUnity());
        if (requestResult == SandstormAdRequestResult.Success) {
            Debug.Log("RequestAd success");
        }
        else {
            Debug.Log("RequestAd error");
            Debug.Log(requestResult);
        }

        yield return null;
    }

    private SandstormUnityView CreateSandstormUnity()
    {
        var gm = Instantiate(_prefabSandstormView, Vector3.zero, Quaternion.identity);
        return gm.GetComponent<SandstormUnityView>();
    }

    

    public void OnVastAdsLoaded()
    {
        Debug.Log("VastAdsLoaded");
        IsAdsLoaded = true;
    }

    public void OnVastError(SandstormError vastError)
    {
        Debug.Log($"OnVastError {vastError}");
        IsAdsLoaded = false;
    }

    public void OnVastAdsAvailabilityExpired()
    {
        Debug.Log("OnVastAdsAvailabilityExpired");
    }

    public void OnVastAdsStarted()
    {
        Debug.Log("OnVastAdsStarted");
        playBtn.interactable = false;
    }

    public void OnVastAdPaused()
    {
        Debug.Log("OnVastAdPaused");
        playBtn.interactable = true;
    }

    public void OnVastAdPlayStarted()
    {
        Debug.Log("OnVastAdPlayStarted");
    }

    public void OnVastAdsEnded()
    {
        Debug.Log("OnVastAdsEnded");
        playBtn.interactable = true;
        IsAdsLoaded = false;
    }

    public void OnStarted()
    {
        Debug.Log("OnStarted");
    }
}
