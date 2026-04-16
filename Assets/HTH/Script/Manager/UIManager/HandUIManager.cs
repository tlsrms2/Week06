using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 손패 카드 UI를 담당합니다.
    /// 카드 추가/제거/하이라이트/콜백 재등록을 처리합니다.
    /// </summary>
    public class HandUIManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Tooltip("손패 컨테이너")]
        [SerializeField] private RectTransform _handContainer;

        // ─── 의존성 ───────────────────────────────────────────────
        private CardCreateManager _cardCreate;

        // ─── 카드 뷰 추적 ────────────────────────────────────────
        private readonly List<ICardView> _handCards = new();

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>손패 카드 클릭 시 발행 (손패 인덱스)</summary>
        public event Action<int> OnHandCardClicked;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>CardViewFactory를 주입합니다.</summary>
        public void Initialize(CardCreateManager cardCreate)
        {
            _cardCreate = cardCreate;
        }

        // ─── 손패 UI ─────────────────────────────────────────────

        /// <summary>
        /// 손패 UI를 갱신합니다.
        /// GameUIManager.RefreshPlayerArea에서 호출합니다.
        /// </summary>
        public void RefreshHand(List<CardDataSO> hand)
        {
            ClearHand();
            for (int i = 0; i < hand.Count; i++)
                AddOperatorToHand(hand[i], i);
        }

        /// <summary>손패의 모든 카드 UI를 제거합니다.</summary>
        public void ClearHand()
        {
            foreach (var v in _handCards)
                if (v is MonoBehaviour mb && mb != null)
                    Destroy(mb.gameObject);
            _handCards.Clear();
        }

        /// <summary>손패에 연산자 카드 UI를 추가합니다.</summary>
        private void AddOperatorToHand(CardDataSO card, int index)
        {
            int captured = index;
            var view = _cardCreate.CreateHandCard(_handContainer, card);
            view.SetInteractable(true);
            view.OnClicked += _ => OnHandCardClicked?.Invoke(captured);
            _handCards.Add(view);
        }

        /// <summary>
        /// 손패에서 카드를 제거하고 인덱스를 재정렬합니다.
        /// </summary>
        public void RemoveFromHand(int index)
        {
            if (index < 0 || index >= _handCards.Count) return;

            var view = _handCards[index];
            if (view is MonoBehaviour mb && mb != null)
                Destroy(mb.gameObject);

            _handCards.RemoveAt(index);
            RebuildCallbacks();
        }

        /// <summary>손패 카드 버튼 콜백을 현재 인덱스 기준으로 재등록합니다.</summary>
        private void RebuildCallbacks()
        {
            for (int i = 0; i < _handCards.Count; i++)
            {
                int captured = i;
                _handCards[i].ClearClickListeners();
                _handCards[i].OnClicked += _ => OnHandCardClicked?.Invoke(captured);
            }
        }

        /// <summary>
        /// 손패 카드 선택 상태를 갱신합니다.
        /// index = -1 이면 전체 선택 해제합니다.
        /// </summary>
        public void HighlightHandCard(int index)
        {
            for (int i = 0; i < _handCards.Count; i++)
                _handCards[i].SetSelected(i == index);
        }

        /// <summary>
        /// 시야 비율에 따라 손패 카드 블러를 갱신합니다.
        /// </summary>
        public void ApplyBlur(float visionRatio)
        {
            float blur = visionRatio > 0.2f
                ? 0f
                : Mathf.InverseLerp(0.2f, 0f, visionRatio);

            foreach (var v in _handCards) v.SetBlurLevel(blur);
        }
    }
}