using System;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace PocketBloom
{
    // SDK 어댑터. 동의 제공자가 완료를 알리기 전에는 SDK를 초기화하지 않는다.
    // 에디터용 가짜 광고나 보상 지급을 실제 광고 성공으로 취급하지 않는다.
    public sealed class BloomAds : MonoBehaviour
    {
        public BloomAdConfig config;
        LevelPlayRewardedAd rewarded;
        LevelPlayInterstitialAd interstitial;
        readonly BloomRewardLedger rewards = new BloomRewardLedger();
        bool initialized, subscribed, showing;
        float lastInterstitial = -999, sessionStarted, lastRewarded = -999;
        public bool Configured => config && config.enableAds && !string.IsNullOrWhiteSpace(config.androidAppKey)
            && !string.IsNullOrWhiteSpace(config.androidRewardedId) && !string.IsNullOrWhiteSpace(config.androidInterstitialId);
        public bool RewardedReady => initialized && !showing && rewarded != null && rewarded.IsAdReady();
        void Awake() => sessionStarted = Time.realtimeSinceStartup;

        // 실제 CMP/연령 확인 제공자가 호출할 진입점. 기본 배포판에서는 호출하지 않는다.
        public void InitializeAfterPrivacy(bool gdprConsent, bool optOutOfSale, bool childDirected)
        {
            if (!Configured || subscribed || Application.platform != RuntimePlatform.Android) return;
            LevelPlayPrivacySettings.SetGDPRConsent(gdprConsent);
            LevelPlayPrivacySettings.SetCCPA(optOutOfSale);
            LevelPlayPrivacySettings.SetCOPPA(childDirected);
            LevelPlay.OnInitSuccess += OnInitialized; LevelPlay.OnInitFailed += OnInitFailed;
            subscribed = true; LevelPlay.Init(config.androidAppKey);
        }
        void OnInitialized(LevelPlayConfiguration _)
        {
            if (initialized) return;
            CancelInvoke(nameof(RetryInitialize));
            initialized = true;
            rewarded = new LevelPlayRewardedAd(config.androidRewardedId);
            rewarded.OnAdDisplayed += OnRewardDisplayed;
            rewarded.OnAdRewarded += OnRewarded; rewarded.OnAdClosed += OnRewardClosed;
            rewarded.OnAdDisplayFailed += OnRewardFailed; rewarded.OnAdLoadFailed += OnRewardLoadFailed;
            interstitial = new LevelPlayInterstitialAd(config.androidInterstitialId);
            interstitial.OnAdClosed += OnInterstitialClosed; interstitial.OnAdDisplayFailed += OnInterstitialFailed;
            interstitial.OnAdLoadFailed += OnInterstitialLoadFailed;
            rewarded.LoadAd(); interstitial.LoadAd();
        }
        void OnInitFailed(LevelPlayInitError error)
        {
            Debug.LogWarning("광고 초기화 실패: " + error.ErrorMessage);
            CancelInvoke(nameof(RetryInitialize)); Invoke(nameof(RetryInitialize), 30);
        }
        void RetryInitialize()
        {
            if (!initialized && Configured && subscribed) LevelPlay.Init(config.androidAppKey);
        }
        public bool ShowRewarded(Action reward, Action failed, Func<bool> eligible)
        {
            if (!RewardedReady || !rewards.Begin(reward, failed, eligible)) { failed?.Invoke(); return false; }
            showing = true; AudioListener.pause = true;
            try { rewarded.ShowAd(); return true; }
            catch (Exception error)
            {
                Debug.LogWarning("광고 표시 실패: " + error.Message);
                rewards.Fail(null); showing = false; AudioListener.pause = false;
                Invoke(nameof(ReloadRewarded), 30); return false;
            }
        }
        static string RewardKey(LevelPlayAdInfo info) => info == null || (string.IsNullOrEmpty(info.AuctionId) && string.IsNullOrEmpty(info.AdId))
            ? null : info.AuctionId + ":" + info.AdId;
        void OnRewardDisplayed(LevelPlayAdInfo info) => rewards.Displayed(RewardKey(info));
        void OnRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            rewards.Reward(RewardKey(info));
        }
        void OnRewardClosed(LevelPlayAdInfo info)
        {
            if (!rewards.Close(RewardKey(info))) return;
            AudioListener.pause = false; showing = false; lastRewarded = Time.realtimeSinceStartup;
            rewarded?.LoadAd();
        }
        void OnRewardFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            if (!rewards.Fail(RewardKey(info))) return;
            AudioListener.pause = false; showing = false; Invoke(nameof(ReloadRewarded), 30);
        }
        void OnRewardLoadFailed(LevelPlayAdError _) { CancelInvoke(nameof(ReloadRewarded)); Invoke(nameof(ReloadRewarded), 30); }
        void ReloadRewarded() => rewarded?.LoadAd();
        public bool TryInterstitial(int completedRuns)
        {
            float now = Time.realtimeSinceStartup;
            if (!initialized || showing || interstitial == null || !interstitial.IsAdReady() || completedRuns < 3 ||
                completedRuns % Math.Max(3, config.interstitialEveryRuns) != 0 || now - sessionStarted < 180 ||
                now - lastInterstitial < Mathf.Max(180, config.interstitialCooldown) || now - lastRewarded < 180) return false;
            showing = true; lastInterstitial = now; AudioListener.pause = true; interstitial.ShowAd(); return true;
        }
        void OnInterstitialClosed(LevelPlayAdInfo _) { showing = false; AudioListener.pause = false; interstitial?.LoadAd(); }
        void OnInterstitialFailed(LevelPlayAdInfo _, LevelPlayAdError error) { showing = false; AudioListener.pause = false; Invoke(nameof(ReloadInterstitial), 30); }
        void OnInterstitialLoadFailed(LevelPlayAdError _) { CancelInvoke(nameof(ReloadInterstitial)); Invoke(nameof(ReloadInterstitial), 30); }
        void ReloadInterstitial() => interstitial?.LoadAd();
        void OnDestroy()
        {
            CancelInvoke(); rewards.Clear(); AudioListener.pause = false;
            if (subscribed) { LevelPlay.OnInitSuccess -= OnInitialized; LevelPlay.OnInitFailed -= OnInitFailed; }
            if (rewarded != null)
            {
                rewarded.OnAdDisplayed -= OnRewardDisplayed;
                rewarded.OnAdRewarded -= OnRewarded; rewarded.OnAdClosed -= OnRewardClosed;
                rewarded.OnAdDisplayFailed -= OnRewardFailed; rewarded.OnAdLoadFailed -= OnRewardLoadFailed; rewarded.DestroyAd();
            }
            if (interstitial != null)
            {
                interstitial.OnAdClosed -= OnInterstitialClosed; interstitial.OnAdDisplayFailed -= OnInterstitialFailed;
                interstitial.OnAdLoadFailed -= OnInterstitialLoadFailed; interstitial.DestroyAd();
            }
        }
    }
}
