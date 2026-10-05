using UnityEngine;

namespace PocketBloom
{
    [CreateAssetMenu(menuName = "Pocket Bloom/Ad Configuration")]
    public sealed class BloomAdConfig : ScriptableObject
    {
        [Tooltip("광고 계정과 실제 동의 흐름 검증 후에만 켭니다.")]
        public bool enableAds;
        public string androidAppKey = "", androidRewardedId = "", androidInterstitialId = "";
        public string privacyPolicyUrl = "";
        public int interstitialEveryRuns = 3;
        public float interstitialCooldown = 180;
    }
}
