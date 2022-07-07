using System;
using System.Collections;
using System.Collections.Generic;
using Sandstorm.Unity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Sandstorm
{
    [RequireComponent(typeof(AudioSource))]
    public class SandstormUnityView : MonoBehaviour, SandstormUnity
    {
        private const string Tag = "SandstormUnityView";

        public event OnVastDownloaded VastDownloaded;
        public event OnPlayerDataUpdated DataUpdated;
        public event OnMediaDownloaded MediaDownloaded;

        private AudioSource _audioSource;

        private AudioSource AudioSourceComponent => _audioSource;

        private Dictionary<int, AudioClip> _audioDictionary = new Dictionary<int, AudioClip>();
        private Dictionary<int, Texture2D> _textureDictionary = new Dictionary<int, Texture2D>();

        private int _currentId = SandstormAd.InvalidId;

        private const int UnityInvalidTextureSize = 8;
        private const int BannerMaxHeight = 25;
        private GameObject BannerObject;
        private string BannerClickThroughUrl;
        private SandstormBannerPosition BannerPosition = SandstormBannerPosition.Top;
        private Rect LastSafeArea = Rect.zero;
        private Vector2 LastBannerSize = Vector2.zero;

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            InitComponents();
        }

        private void Update()
        {
            OnUpdate();
        }

        private void OnUpdate()
        {
            if(Screen.safeArea != LastSafeArea) {
                SafeAreaChanged();
            }
            if (AudioSourceComponent == null)
            {
                return;
            }

            var data = CreatePlayerData();
            if (data == null)
            {
                return;
            }

            DataUpdated?.Invoke(sender: this, data: data);
        }


        private SandstormUnityData CreatePlayerData()
        {
            if (AudioSourceComponent == null || AudioSourceComponent.clip == null)
            {
                return null;
            }

            var lengthF = AudioSourceComponent.clip.length;
            var timeF = AudioSourceComponent.time;


            var volume = Mathf.RoundToInt((AudioSourceComponent.volume) * 100f);
            var length = Mathf.RoundToInt(lengthF * 1000f);
            var time = Mathf.RoundToInt(timeF * 1000f);
            var isPlaying = AudioSourceComponent.isPlaying;

            if (timeF >= lengthF || Mathf.Approximately(lengthF, timeF))
            {
                time = length;
            }


            return new SandstormUnityData(audioVolume: volume, audioLengthMs: length, audioTimeMs: time,
                isPlaying: isPlaying, adId: _currentId, isMuted: AudioSettings.Mobile.audioOutputStarted == false || AudioSettings.Mobile.muteState);
        }

        private void InitComponents()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Dispose()
        {
            Clear();
        }

        private IEnumerator TryDownloadVastFile(string url)
        {
            using var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            switch (www.result)
            {
                case UnityWebRequest.Result.Success:
                    var dataOrig = www.downloadHandler.data;
                    var data = new byte[dataOrig.Length];
                    Buffer.BlockCopy(www.downloadHandler.data, 0, data, 0, data.Length);
                    Logs.Log(tag: Tag, () => $"read text length {data?.Length}, {data}");
                    yield return SandstormDownloadResult.CreateWithResult(url: url, result: data);
                    yield break;
                case UnityWebRequest.Result.ConnectionError:
                    yield return SandstormDownloadResult.CreateError(url: url,
                        error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.NetworkError,
                            errorMessage: www.error));
                    yield break;
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    yield return SandstormDownloadResult.CreateError(url: url,
                        error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.ServerError,
                            errorMessage: www.error));
                    yield break;
            }
        }

        public IEnumerator DownloadVastFile(string url)
        {
            if (IsDestroyed())
            {
                yield break;
            }

            var coroutine = SandstormCoroutineWrapperUnity<SandstormDownloadResult>.Create(monoBehaviour: this,
                coroutine: TryDownloadVastFile(url: url));
            yield return coroutine.Coroutine;

            SandstormDownloadResult downloadResult;
            try
            {
                downloadResult = coroutine.Value;
            }
            catch (Exception e)
            {
                Logs.LogError(tag: Tag, () => $@"Failed to download {url} with exception {e}");
                downloadResult = SandstormDownloadResult.CreateError(url: url,
                    error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.NetworkError,
                        errorMessage: $@"Failed to download {url} with exception {e}"));
            }

            if (IsDestroyed())
            {
                yield break;
            }

            VastDownloaded?.Invoke(sender: this, downloadResult: downloadResult);
        }

        public void Play()
        {
            if (AudioSourceComponent != null && gameObject != null)
            {
                DisplayBannerIfExists();
                AudioSourceComponent.Play();
            }
        }

        private float SafeAreaTopOffset() {
            var offset = Screen.safeArea.y / Screen.dpi * 72;
            if(Screen.safeArea.width > Screen.safeArea.height) { // no offset in landscape; unity is reporting wrong safeArea.y in landscape
                offset = 0;
            }
            return offset;
        }

        private void DisplayBannerIfExists() {
            RemoveBanner();
            if (!_textureDictionary.ContainsKey(_currentId)) {
                Logs.Log(tag: Tag, () => $@"No banner to display for ad.id = {_currentId}.");
                return;
            }
            var texture = _textureDictionary[_currentId];
            if (texture == null) {
                Logs.Log(tag: Tag, () => $@"No banner to display for ad.id = {_currentId}.");
                return;
            }
            var sprite = Sprite.Create(texture, new Rect(0.0f,0.0f,texture.width,texture.height), new Vector2(0.5f,0.5f), 100.0f);
            if (sprite == null || texture == null) {
                Logs.LogError(tag: Tag, () => $@"Banner texture not loading.");
                return;         
            }
            
            BannerObject = new GameObject();
            DontDestroyOnLoad(BannerObject);
            BannerObject.name = "SandstormAdBanner";
            Canvas canvas = BannerObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Int16.MaxValue;; // max possible value
            CanvasScaler cs = BannerObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
            cs.physicalUnit = CanvasScaler.Unit.Points;
            GraphicRaycaster gr = BannerObject.AddComponent<GraphicRaycaster>();

            GameObject imgObject = new GameObject("Image");
            Image image = imgObject.AddComponent<Image>();
            image.sprite = sprite;
            imgObject.transform.SetParent(canvas.transform);

            RectTransform rect = imgObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 0);
            if (BannerPosition == SandstormBannerPosition.Top) {
                rect.pivot = new Vector2(0.5f, 1);
                rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, SafeAreaTopOffset(), 0);
            } else {
                rect.pivot = new Vector2(0.5f, 0);
                rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
            }
            
            var aspectRatio = (float)texture.width / (float)texture.height;
            float height = Screen.width / aspectRatio;
            float width = Screen.width;
            if (height > BannerMaxHeight) {
                height = BannerMaxHeight;
                width = height * aspectRatio;
            }
            LastBannerSize = new Vector2(width, height);
            rect.sizeDelta = LastBannerSize;
            BannerObject.transform.position = new Vector3(0, 0, 0);
            Logs.Log(tag: Tag, () => $"Banner displayed for ad.id = {_currentId}.");
            
            // add click action, if clickThroughUrl exists
            var clickThroughUrl = BannerClickThroughUrl;
            if (!String.IsNullOrEmpty(clickThroughUrl)) {
                EventTrigger trigger = imgObject.AddComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener( (eventData) => {
                    Application.OpenURL(clickThroughUrl);
                } );
                trigger.triggers.Add(entry);
            }
        }

        private void SafeAreaChanged() {
            LastSafeArea = Screen.safeArea;
            if (BannerPosition != SandstormBannerPosition.Top) {
                return;
            }
            RectTransform rect = BannerObject?.transform.Find("Image")?.GetComponent<RectTransform>();
            if (rect == null) {
                return;
            }
            rect?.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, SafeAreaTopOffset(), 0);
            rect.sizeDelta = LastBannerSize;
        }

        private void RemoveBanner() {
            if (BannerObject == null) {
                return;
            }
            Destroy(BannerObject);
            Logs.Log(tag: Tag, () => $"Banner removed.");
        }

        public void Pause()
        {
            if (AudioSourceComponent != null && gameObject != null)
            {
                AudioSourceComponent.Pause();
            }
            // hide banner
            BannerObject?.SetActive(false);
        }

        public void Clear()
        {
            if (IsDestroyed())
            {
                return;
            }

            StopAllCoroutines();
            VastDownloaded = null;
            _audioSource = null;
            VastDownloaded = null;
            DataUpdated = null;
            MediaDownloaded = null;
            _audioDictionary?.Clear();
            _textureDictionary?.Clear();
            _currentId = SandstormAd.InvalidId;
            RemoveBanner();
            DestroyImmediate(gameObject);
        }

        private bool IsDestroyed()
        {
            if (this == null)
            {
                return true;
            }
            return gameObject == null && !ReferenceEquals(gameObject, null);
        }

        public void SetCurrentAudioClip(SandstormAd ad)
        {
            if (IsDestroyed())
            {
                return;
            }
            RemoveBanner();
            if (_audioDictionary.ContainsKey(ad.Id))
            {
                _currentId = ad.Id;
                AudioSourceComponent.clip = _audioDictionary[ad.Id];
                BannerClickThroughUrl = ad.ClickThroughUrl;
            }
            else
            {
                Logs.LogError(tag: Tag, () => $@"Couldn't set requested clip {ad.Id} for url {ad.Url}");
                MediaDownloaded?.Invoke(sender: this, ad: ad,
                    error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.InternalError,
                        errorMessage: $@"Can't find requested clip! {ad.Id}"));
            }
        }

        public IEnumerator DownloadAdMedia(SandstormAd ad)
        {
            if (IsDestroyed())
            {
                yield break;
            }

            var coroutineAudio = SandstormCoroutineWrapperUnity<SandstormAudioClipResult>.Create(monoBehaviour: this,
                coroutine: TryDownloadAudioClipAsync(ad: ad));
            var coroutineTexture = SandstormCoroutineWrapperUnity<SandstormTexture2DResult>.Create(monoBehaviour: this,
                coroutine: TryDownloadTextureAsync(ad: ad));

            yield return coroutineAudio.Coroutine;
            yield return coroutineTexture.Coroutine;

            SandstormError errorAudio = null;
            SandstormAudioClipResult resultAudio = null;
            try
            {
                resultAudio = coroutineAudio.Value;
                if (resultAudio.IsSuccess)
                {
                    _audioDictionary[ad.Id] = resultAudio.Clip;
                }
                else
                {
                    errorAudio = resultAudio.Error;
                }
            }
            catch (Exception e)
            {
                Logs.LogError(tag: Tag, () => $@"Failed to download AD audio {ad.Url} with exception {e}");
                errorAudio = SandstormError.CreateError(errorInfo: SandstormErrorInfo.NetworkError,
                    errorMessage: $@"Failed to download AD audio {ad.Id} with exception {e}");
            }

            SandstormError errorTexture = null;
            SandstormTexture2DResult resultTexture = null;
            try
            {
                resultTexture = coroutineTexture.Value;
                if (resultTexture.IsSuccess)
                {
                    Logs.Log(tag: Tag, () => $"Banner image downloaded for url={ad.BannerUrl}.");
                    _textureDictionary[ad.Id] = resultTexture.Texture;
                }
                else
                {
                    errorTexture = resultTexture.Error;
                }
            }
            catch (Exception e)
            {
                Logs.LogError(tag: Tag, () => $@"Failed to download AD texture {ad.BannerUrl} with exception {e}");
                errorTexture = SandstormError.CreateError(errorInfo: SandstormErrorInfo.NetworkError,
                    errorMessage: $@"Failed to download AD texture {ad.Id} with exception {e}");
            }

            if (gameObject == null)
            {
                yield break;
            }
            var error = errorAudio != null ? errorAudio : errorTexture;
            MediaDownloaded?.Invoke(sender: this, ad: ad, error: error);
        }

        public void SetAdBannerPosition(SandstormBannerPosition position) {
            BannerPosition = position;
        }


        private IEnumerator TryDownloadAudioClipAsync(SandstormAd ad)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip(ad.Url, AudioType.UNKNOWN);
            yield return www.SendWebRequest();

            switch (www.result)
            {
                case UnityWebRequest.Result.Success:
                    var adClip = DownloadHandlerAudioClip.GetContent(www);
                    yield return new SandstormAudioClipResult(clip: adClip, error: null);
                    yield break;
                case UnityWebRequest.Result.ConnectionError:
                    yield return new SandstormAudioClipResult(clip: null,
                        error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.NetworkError,
                            errorMessage: www.error));
                    yield break;
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    yield return new SandstormAudioClipResult(clip: null,
                        error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.ServerError,
                            errorMessage: www.error));
                    yield break;
            }
        }

        private IEnumerator TryDownloadTextureAsync(SandstormAd ad)
        {
            var url = ad.BannerUrl;
            if (String.IsNullOrEmpty(url)) {
                yield return new SandstormTexture2DResult(texture: null, error: null);
            }
            using var www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            switch (www.result)
            {
                case UnityWebRequest.Result.Success:
                    var texture = DownloadHandlerTexture.GetContent(www);
                    if (texture.width == UnityInvalidTextureSize && texture.height == UnityInvalidTextureSize) { // detect red question mark image (texture not loaded)
                        yield return new SandstormTexture2DResult(texture: null,
                                                                    error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.BannerTextureInvalid,
                                                                                                    errorMessage: "Banner texture invalid."));
                    }
                    yield return new SandstormTexture2DResult(texture: texture, error: null);
                    yield break;
                case UnityWebRequest.Result.ConnectionError:
                    yield return new SandstormTexture2DResult(texture: null,
                        error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.NetworkError,
                            errorMessage: www.error));
                    yield break;
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    yield return new SandstormTexture2DResult(texture: null,
                        error: SandstormError.CreateError(errorInfo: SandstormErrorInfo.ServerError,
                            errorMessage: www.error));
                    yield break;
            }
        }

        private class SandstormAudioClipResult
        {
            internal SandstormError Error { get; }
            internal AudioClip Clip { get; }

            internal bool IsSuccess => Clip != null && Error == null;

            public SandstormAudioClipResult(SandstormError error, AudioClip clip)
            {
                Error = error;
                Clip = clip;
            }
        }

        private class SandstormTexture2DResult
        {
            internal SandstormError Error { get; }
            internal Texture2D Texture { get; }

            internal bool IsSuccess => Error == null; // Texture is optional

            public SandstormTexture2DResult(SandstormError error, Texture2D texture)
            {
                Error = error;
                Texture = texture;
            }
        }
    }


}