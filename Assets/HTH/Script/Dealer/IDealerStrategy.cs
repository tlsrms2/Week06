namespace HTH
{
    public interface IDealerStrategy
    {
        bool ShouldHit(long currentTotal);  // int ¡æ long
        string GetResultDescription(long dealerTotal, long playerTotal, long quota);
        long BustThreshold { get; }         // int ¡æ long
    }
}