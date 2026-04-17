using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드, 손패, 연산자 배치 인터랙션을 담당합니다.
    /// 카드 드로우 결과를 받아 필드/손패에 분배하고
    /// 연산자 슬롯 배치 시 ExpressionEvaluator용 리스트를 재구성합니다.
    /// </summary>
    public class PlayerHandManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public List<CardDataSO> Field { get; private set; } = new();
        public List<CardDataSO> Hand { get; private set; } = new();

        private readonly Dictionary<int, CardDataSO> _placedOperators = new();
        private int _selectedHandIndex = -1;

        // ─── 이벤트 ──────────────────────────────────────────────\
        /// <summary>필드/손패 변경 시 발행 — GameManager가 UI 갱신에 사용</summary>
        public event System.Action OnFieldChanged;

        /// <summary>손패 카드 선택 상태 변경 시 발행 (선택 인덱스, -1=해제)</summary>
        public event System.Action<int> OnHandSelectionChanged;

        /// <summary>슬롯 하이라이트 요청 시 발행 (true=하이라이트, false=해제)</summary>
        public event System.Action<bool> OnSlotHighlightRequested;


        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>라운드 시작 시 모든 상태를 초기화합니다.</summary>
        public void ResetAll()
        {
            Field.Clear();
            Hand.Clear();
            _placedOperators.Clear();
            _selectedHandIndex = -1;
        }

        // ─── 카드 추가 ────────────────────────────────────────────

        /// <summary>
        /// 카드를 필드에 추가합니다.
        /// </summary>
        public void AddNumberToField(CardDataSO card)
        {
            Field.Add(card);
            OnFieldChanged?.Invoke();
        }

        /// <summary>
        /// 연산자 카드를 손패에 추가합니다.
        /// Stage 2+에서 연산자 카드 드로우 시 호출합니다.
        /// </summary>
        public void AddOperatorToHand(CardDataSO card)
        {
            Hand.Add(card);
            OnFieldChanged?.Invoke();
        }

        // ─── 연산자 배치 인터랙션 ─────────────────────────────────

        /// <summary>
        /// 손패 카드 선택 처리.
        /// 같은 카드 재클릭 시 선택을 해제합니다.
        /// </summary>
        public void SelectHandCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= Hand.Count) return;

            if (_selectedHandIndex == handIndex)
            {
                _selectedHandIndex = -1;
                OnHandSelectionChanged?.Invoke(-1);
                OnSlotHighlightRequested?.Invoke(false);
                return;
            }

            _selectedHandIndex = handIndex;
            OnHandSelectionChanged?.Invoke(handIndex);
            OnSlotHighlightRequested?.Invoke(true);
        }

        /// <summary>
        /// 연산자를 필드 슬롯에 배치합니다.
        /// 손패 카드가 선택된 상태에서만 동작합니다.
        /// 이미 배치된 슬롯은 무시합니다.
        /// </summary>
        public bool TryPlaceOperator(int slotIndex, out CardDataSO placedCard)
        {
            placedCard = null;

            if (_selectedHandIndex < 0) return false;
            if (slotIndex < 0 || slotIndex >= Field.Count - 1) return false;
            if (_placedOperators.ContainsKey(slotIndex)) return false;

            placedCard = Hand[_selectedHandIndex];
            _placedOperators[slotIndex] = placedCard;

            Hand.RemoveAt(_selectedHandIndex);
            _selectedHandIndex = -1;

            OnHandSelectionChanged?.Invoke(-1);
            OnSlotHighlightRequested?.Invoke(false);

            RebuildFieldExpression();
            OnFieldChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// _placedOperators를 반영해 Field를 재구성합니다.
        /// 숫자 카드 사이에 배치된 연산자를 끼워넣습니다.
        /// </summary>
        private void RebuildFieldExpression()
        {
            var numberCards = new List<CardDataSO>();
            foreach (var card in Field)
                if (card.cardType == CardType.Number)
                    numberCards.Add(card);

            Field.Clear();
            for (int i = 0; i < numberCards.Count; i++)
            {
                if (i > 0 && _placedOperators.TryGetValue(i - 1, out CardDataSO op))
                    Field.Add(op);

                Field.Add(numberCards[i]);
            }
        }

        // ─── 선택 해제 ────────────────────────────────────────────

        /// <summary>
        /// 손패 선택 상태와 슬롯 하이라이트를 해제합니다.
        /// ExitPlayerTurn 시 GameManager가 호출합니다.
        /// </summary>
        public void ClearSelection()
        {
            _selectedHandIndex = -1;
            OnHandSelectionChanged?.Invoke(-1);
            OnSlotHighlightRequested?.Invoke(false);
        }

        // ─── 읽기 전용 접근 ───────────────────────────────────────

        /// <summary>현재 선택된 손패 인덱스를 반환합니다. -1 = 미선택</summary>
        public int SelectedHandIndex => _selectedHandIndex;
    }
}