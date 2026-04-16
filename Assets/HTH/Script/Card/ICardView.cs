namespace HTH
{
    /// <summary>
    /// 카드 하나의 표현을 담당하는 인터페이스.
    /// UI Text / 3D Mesh / 스프라이트 등 어떤 방식으로든 구현 가능합니다.
    ///
    /// 교체 방법:
    /// CardUIView  → 현재 (UI Text 기반)
    /// Card3DView  → 추후 (3D Mesh 오브젝트)
    /// </summary>
    public interface ICardView
    {
        /// <summary>
        /// 카드 데이터를 받아 표현을 초기화합니다.
        /// 생성 직후 반드시 호출해야 합니다.
        /// </summary>
        void Initialize(CardDataSO data);

        /// <summary>
        /// 카드를 클릭/선택 가능 상태로 설정합니다.
        /// false면 클릭 이벤트가 발생하지 않습니다.
        /// </summary>
        void SetInteractable(bool interactable);

        /// <summary>
        /// 카드 선택 상태를 시각적으로 표시합니다.
        /// 손패에서 카드를 선택했을 때 하이라이트 용도로 사용합니다.
        /// </summary>
        void SetSelected(bool selected);

        /// <summary>
        /// 시야 감소 시 카드 판독 난이도를 적용합니다.
        /// 0.0 = 선명, 1.0 = 완전히 뭉개짐
        /// </summary>
        void SetBlurLevel(float normalized);

        /// <summary>
        /// OnClicked 이벤트의 모든 구독을 해제하고 버튼 리스너를 초기화합니다.
        /// 손패 카드 제거 후 인덱스 재정렬 시 HandUIManager.RebuildCallbacks에서 호출합니다.
        /// 기존 구독을 해제하지 않으면 카드 하나 클릭 시 콜백이 중복 호출되는 버그가 발생합니다.
        /// </summary>
        void ClearClickListeners();

        /// <summary>카드가 보유한 데이터를 반환합니다.</summary>
        CardDataSO Data { get; }

        /// <summary>
        /// 카드 클릭 이벤트.
        /// 구독자가 없어도 안전하게 null 체크 후 호출합니다.
        /// </summary>
        event System.Action<ICardView> OnClicked;
    }
}