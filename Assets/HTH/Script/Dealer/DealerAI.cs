namespace HTH
{
    /// <summary>
    /// 표준 블랙잭 딜러 AI.
    /// BustThreshold는 스테이지마다 GameManager가 주입합니다.
    /// Stage 1 : 21 / Stage 2+ : quota값
    /// </summary>
    public class DealerAI : IDealerStrategy
    {
        private long _bustThreshold = 21;

        public long BustThreshold => _bustThreshold;

        /// <summary>딜러 버스트 기준값을 설정합니다. GameManager가 스테이지 진입 시 호출합니다.</summary>
        public void SetBustThreshold(long threshold) => _bustThreshold = threshold;

        /// <summary>현재 합산값이 BustThreshold의 절반 미만이면 히트합니다.</summary>
        public bool ShouldHit(long currentTotal)
            => currentTotal < _bustThreshold * 0.6f;

        /// <summary>승패 결과 설명 문자열을 반환합니다.</summary>
        public string GetResultDescription(long dealerTotal, long playerTotal, long quota)
        {
            if (playerTotal > quota) return "버스트 — 할당량 초과";
            if (dealerTotal > _bustThreshold) return "딜러 버스트 — 플레이어 승리";
            if (playerTotal > dealerTotal) return "플레이어 승리";
            if (playerTotal < dealerTotal) return "딜러 승리";
            return "무승부";
        }
    }
}