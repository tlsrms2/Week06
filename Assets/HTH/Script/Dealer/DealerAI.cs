namespace HTH
{
    /// <summary>
    /// 블랙잭 딜러 AI.
    ///
    /// 드로우 판단 기준: 동적 안전 마진(Safe Margin) 방식
    ///   - standThreshold = bustValue - 평균 카드값(5.5)
    ///   - currentTotal < standThreshold 이면 히트
    ///   - 예: bustValue=21 → 15.5 미만이면 히트(≤15 히트, 16 이상 스탠드)
    ///   - 예: bustValue=50 → 44.5 미만이면 히트(≤44 히트, 45 이상 스탠드)
    ///   - 연산자 배치는 DealerManager가 Brute-Force로 담당
    /// </summary>
    public class DealerAI : IDealerStrategy
    {
        private long _bustThreshold = 21;

        // 덱에 남아있는 숫자 카드의 평균값 가정 (1~10 균등 분포 → 5.5)
        private const float AverageCardValue = 5.5f;

        public long BustThreshold => _bustThreshold;

        public void SetBustThreshold(long threshold)
            => _bustThreshold = threshold;

        /// <summary>
        /// 딜러 히트 여부를 판단합니다.
        ///
        /// standThreshold = bustValue - AverageCardValue(5.5)
        /// currentTotal < standThreshold 이면 히트, 이상이면 스탠드.
        ///
        /// bustValue = 21 → standThreshold ≈ 15.5 → 15 이하만 히트
        /// bustValue = 50 → standThreshold ≈ 44.5 → 44 이하만 히트
        /// </summary>
        public bool ShouldHit(long currentTotal, StageDataSO stage = null)
        {
            long bustValue = stage?.bustValue ?? _bustThreshold;

            if (currentTotal > bustValue) return false;

            float standThreshold = bustValue - AverageCardValue;
            bool hit = currentTotal < standThreshold;

            UnityEngine.Debug.Log(
                $"[DealerAI] total:{currentTotal} target:{bustValue} " +
                $"standThreshold:{standThreshold:F1} → {(hit ? "HIT" : "STAND")}");

            return hit;
        }

        public string GetResultDescription(
            long dealerTotal, long playerTotal, long quota)
        {
            if (playerTotal > quota) return "버스트 — 할당량 초과";
            if (dealerTotal > quota) return "딜러 버스트 — 플레이어 승리";
            if (playerTotal > dealerTotal) return "플레이어 승리";
            if (playerTotal < dealerTotal) return "딜러 승리";
            return "무승부";
        }
    }
}
