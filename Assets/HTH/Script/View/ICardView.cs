namespace HTH
{
    // 카드 하나의 표시 인터페이스.
    // 일반 CardView / 뭉개진 BlurredCardView 등으로 교체 가능.
    public interface ICardView
    {
        void SetCard(CardData data);
        void SetInteractable(bool interactable);
        void SetSelected(bool selected);

        // 시야 감소 시 판독 난이도 적용 (0=선명, 1=완전 뭉개짐)
        void SetBlurLevel(float normalized);
    }
}