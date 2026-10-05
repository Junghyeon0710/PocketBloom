using System;
using System.Collections.Generic;

namespace PocketBloom
{
    // 닫기와 보상은 순서가 정해져 있지 않다. 표시 ID별로 원래 요청을 유지한다.
    public sealed class BloomRewardLedger
    {
        sealed class Request
        {
            public string key;
            public Action reward, failed;
            public Func<bool> eligible;
            public bool rewarded, rejected;
        }
        readonly Dictionary<string, Request> requests = new Dictionary<string, Request>();
        Request active;
        public bool IsShowing => active != null;

        public bool Begin(Action reward, Action failed, Func<bool> eligible)
        {
            if (active != null || reward == null || eligible == null || !eligible()) return false;
            var expired = new List<string>();
            foreach (var pair in requests)
                if (pair.Value.rewarded || pair.Value.rejected || !pair.Value.eligible()) expired.Add(pair.Key);
            foreach (var key in expired) requests.Remove(key);
            active = new Request { reward = reward, failed = failed, eligible = eligible };
            return true;
        }
        public bool Displayed(string key)
        {
            if (active == null || string.IsNullOrEmpty(key)) return false;
            if (active.key != null) return active.key == key;
            if (requests.ContainsKey(key)) return false;
            active.key = key; requests.Add(key, active); return true;
        }
        public bool Reward(string key)
        {
            if (string.IsNullOrEmpty(key) || !requests.TryGetValue(key, out var request) ||
                request.rewarded || request.rejected || !request.eligible()) return false;
            request.rewarded = true;
            var grant = request.reward; request.reward = request.failed = null;
            grant(); return true;
        }
        public bool Close(string key)
        {
            if (active == null) return false;
            if (active.key == null && !string.IsNullOrEmpty(key)) Displayed(key);
            if (active.key != key) return false;
            active = null;
            // 닫힘을 보상 실패로 판정하거나 시간 제한으로 보상 콜백을 폐기하지 않는다.
            return true;
        }
        public bool Fail(string key)
        {
            if (active == null || (!string.IsNullOrEmpty(key) && active.key != null && active.key != key)) return false;
            var request = active; active = null; request.rejected = true;
            var fail = request.rewarded ? null : request.failed;
            request.reward = request.failed = null; fail?.Invoke(); return true;
        }
        public void Clear() { active = null; requests.Clear(); }
    }
}
