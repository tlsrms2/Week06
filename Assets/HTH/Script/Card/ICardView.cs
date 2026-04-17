using System;
using System.Collections.Generic;

namespace HTH
{
    /// <summary>
    /// 카드 표현 인터페이스.
    /// UI 카드(CardUIView)와 3D 카드(Card3DView) 모두 구현합니다.
    /// </summary>
    public interface ICardView
    {
        /// <summary>카드 데이터</summary>
        CardDataSO Data { get; }

        /// <summary>카드 클릭 시 발행</summary>
        event Action<ICardView> OnClicked;

        /// <summary>카드 데이터를 받아 초기화합니다.</summary>
        void Initialize(CardDataSO data);

        /// <summary>클릭 가능 여부를 설정합니다.</summary>
        void SetInteractable(bool interactable);

        /// <summary>선택 상태를 시각적으로 표시합니다.</summary>
        void SetSelected(bool selected);

        /// <summary>시야 감소 시 블러를 적용합니다.</summary>
        void SetBlurLevel(float normalized);

        /// <summary>OnClicked 이벤트 구독을 전부 해제합니다.</summary>
        void ClearClickListeners();

        /// <summary>
        /// 앞면을 공개합니다.
        /// Y축 180도 회전으로 처리합니다.
        /// </summary>
        void SetFaceUp();

        /// <summary>
        /// 뒷면을 공개합니다.
        /// Y축 0도 회전으로 처리합니다.
        /// </summary>
        void SetFaceDown();
    }
}