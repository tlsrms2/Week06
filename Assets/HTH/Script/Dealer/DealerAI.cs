namespace HTH
{
    public class DealerAI : IDealerStrategy
    {
        public long BustThreshold => 21;

        public bool ShouldHit(long currentTotal) => currentTotal < 17;

        public string GetResultDescription(long dealerTotal, long playerTotal, long quota)
        {
            if (playerTotal > quota) return "버스트 — 할당량 초과";
            if (dealerTotal > BustThreshold) return "딜러 버스트 — 플레이어 승리";
            if (playerTotal > dealerTotal) return "플레이어 승리";
            if (playerTotal < dealerTotal) return "딜러 승리";
            return "무승부";
        }
    }
}