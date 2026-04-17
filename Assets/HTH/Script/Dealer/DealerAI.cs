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

        /// <summary>
        /// 딜러 히트 여부를 판단합니다.
        /// currentValueSet = true  : 임계값의 60% 미만이면 히트 (높게 쌓아야 함)
        /// currentValueSet = false : 임계값의 140% 초과면 히트 (낮게 내려야 함)
        /// StageDataSO 없는 경우 기본 블랙잭 룰 (17 미만 히트) 적용
        /// </summary>
        public bool ShouldHit(long currentTotal, StageDataSO stage = null)
        {
            if (stage == null || stage.stageIndex == 1)
                return currentTotal < 17;

            if (stage.currentValueSet)
                // 높아야 함 → bustValue의 80% 미만이면 더 드로우
                return currentTotal < stage.bustValue * 0.8f;
            else
                // 낮아야 함 → bustValue의 60% 초과면 더 드로우 (낮추려고 뺄셈/나눗셈 사용)
                return currentTotal > stage.bustValue;
        }

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