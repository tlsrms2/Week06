namespace HTH
{
    /// <summary>
    /// 블랙잭 딜러 AI.
    ///
    /// 1단계 : bustValue의 80% 미만이면 히트
    /// 2단계 : 사칙연산 스테이지에서도 동일 기준 적용
    ///   - bustValue가 달라도 80% 미만이면 히트
    ///   - 연산자 배치는 DealerManager가 담당
    /// </summary>
    public class DealerAI : IDealerStrategy
    {
        private long _bustThreshold = 21;
        public long BustThreshold => _bustThreshold;

        public void SetBustThreshold(long threshold)
            => _bustThreshold = threshold;

        /// <summary>
        /// 딜러 히트 여부를 판단합니다.
        /// bustValue의 80% 미만이면 히트합니다.
        /// bustValue 초과 시 이미 버스트이므로 히트 불필요.
        ///
        /// bustValue = 21 → 16 미만이면 히트
        /// bustValue = 50 → 40 미만이면 히트
        /// bustValue = 10 → 8  미만이면 히트
        /// </summary>
        public bool ShouldHit(long currentTotal, StageDataSO stage = null)
        {
            long bustValue = stage?.bustValue ?? _bustThreshold;

            if (currentTotal > bustValue) return false;
            return currentTotal < (long)(bustValue * 0.8f);
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
