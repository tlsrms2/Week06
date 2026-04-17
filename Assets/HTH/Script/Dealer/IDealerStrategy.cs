namespace HTH
{
    public interface IDealerStrategy
    {
        bool ShouldHit(long currentTotal, StageDataSO stage = null);
        string GetResultDescription(long dealerTotal, long playerTotal, long quota);
        long BustThreshold { get; }
        void SetBustThreshold(long threshold);
    }
}