using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드, 손패, 연산자 배치 인터랙션을 담당합니다.
    ///
    /// 손패(Hand)는 연산자 카드 최대 1장만 보유합니다.
    /// 버리기는 선택 여부와 관계없이 Hand에 카드가 있으면 실행됩니다.
    /// 슬롯 배치는 Hand[0]을 직접 사용합니다.
    /// </summary>
    public class PlayerHandManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public List<DeckSO.CardEntry> Field { get; private set; } = new();
        public List<DeckSO.CardEntry> Hand { get; private set; } = new();

        private readonly Dictionary<int, DeckSO.CardEntry> _placedOperators = new();

        // ─── 선택 상태 ────────────────────────────────────────────
        // 손패는 0장 또는 1장이므로 선택 = Hand[0] 선택 여부
        private bool _isHandSelected = false;

        // ─── 이벤트 ──────────────────────────────────────────────
        public event System.Action OnFieldChanged;
        public event System.Action OnHandChanged;
        public event System.Action<int> OnHandSelectionChanged;
        public event System.Action<bool> OnSlotHighlightRequested;

        // ─── 변환 헬퍼 ───────────────────────────────────────────
        public List<CardDataSO> FieldData => Field.ConvertAll(e => e.data);
        public List<CardDataSO> HandData => Hand.ConvertAll(e => e.data);

        // ─── 읽기 전용 ────────────────────────────────────────────
        /// <summary>손패에 카드가 있는지 여부</summary>
        public bool HasHandCard => Hand.Count > 0;

        /// <summary>새 연산자 카드를 받을 수 있는지 여부</summary>
        public bool CanReceiveOperatorCard
        {
            get
            {
                if (HasHandCard) return false;

                int numberCount = 0;
                foreach (var entry in Field)
                    if (entry.data.cardType == CardType.Number)
                        numberCount++;

                int availableSlotCount = numberCount - 1 - _placedOperators.Count;
                return availableSlotCount > 0;
            }
        }

        /// <summary>현재 선택 인덱스 (-1 = 미선택)</summary>
        public int SelectedHandIndex => _isHandSelected ? 0 : -1;

        // ─── 초기화 ───────────────────────────────────────────────

        public void ResetAll()
        {
            Field.Clear();
            Hand.Clear();
            _placedOperators.Clear();
            _isHandSelected = false;
        }

        // ─── 카드 추가 ────────────────────────────────────────────

        public void AddNumberToField(DeckSO.CardEntry entry)
        {
            Field.Add(entry);
            OnFieldChanged?.Invoke();
        }

        /// <summary>
        /// 연산자 카드를 손패에 추가합니다.
        /// 손패는 1장만 허용합니다.
        /// </summary>
        public void AddOperatorToHand(DeckSO.CardEntry entry)
        {
            if (Hand.Count >= 1)
            {
                Debug.LogWarning("[PHM] 손패에 이미 연산자 카드가 있습니다.");
                return;
            }

            Hand.Add(entry);
            OnHandChanged?.Invoke();
        }

        // ─── 연산자 선택 ──────────────────────────────────────────

        /// <summary>
        /// 손패 카드 선택 처리.
        /// 재클릭 시 선택 해제.
        /// 손패가 없으면 무시합니다.
        /// </summary>
        public void SelectHandCard(int handIndex)
        {
            // 손패가 없으면 무시
            if (Hand.Count == 0) return;

            // 재클릭 시 선택 해제
            if (_isHandSelected)
            {
                _isHandSelected = false;
                OnHandSelectionChanged?.Invoke(-1);
                OnSlotHighlightRequested?.Invoke(false);
                return;
            }

            // 선택
            _isHandSelected = true;
            OnHandSelectionChanged?.Invoke(0);
            OnSlotHighlightRequested?.Invoke(true);
        }

        // ─── 연산자 배치 ──────────────────────────────────────────

        /// <summary>
        /// 연산자를 필드 슬롯에 배치합니다.
        /// 선택 여부와 관계없이 Hand[0]을 사용합니다.
        /// </summary>
        public bool TryPlaceOperator(int slotIndex, out DeckSO.CardEntry placedEntry)
        {
            placedEntry = null;

            // 손패 없으면 불가
            if (Hand.Count == 0) return false;

            int numberCount = 0;
            foreach (var entry in Field)
                if (entry.data.cardType == CardType.Number) numberCount++;

            if (slotIndex < 0 || slotIndex >= numberCount - 1) return false;
            if (_placedOperators.ContainsKey(slotIndex)) return false;

            // Hand[0] 직접 사용
            placedEntry = Hand[0];
            _placedOperators[slotIndex] = placedEntry;

            Hand.RemoveAt(0);
            _isHandSelected = false;

            OnHandSelectionChanged?.Invoke(-1);
            OnSlotHighlightRequested?.Invoke(false);

            RebuildFieldExpression();

            return true;
        }

        private void RebuildFieldExpression()
        {
            var numberEntries = new List<DeckSO.CardEntry>();
            foreach (var entry in Field)
                if (entry.data.cardType == CardType.Number)
                    numberEntries.Add(entry);

            Field.Clear();
            for (int i = 0; i < numberEntries.Count; i++)
            {
                Field.Add(numberEntries[i]);

                if (i < numberEntries.Count - 1 &&
                    _placedOperators.TryGetValue(i, out DeckSO.CardEntry op))
                    Field.Add(op);
            }

            var log = new System.Text.StringBuilder("[PHM] RebuildField — ");
            foreach (var entry in Field)
                log.Append($"{entry.data.displayLabel} ");
            Debug.Log(log.ToString());
        }

        // ─── 버리기 ──────────────────────────────────────────────

        /// <summary>
        /// 손패 연산자 카드를 버립니다.
        /// 선택 여부와 관계없이 Hand에 카드가 있으면 버립니다.
        /// </summary>
        public void DiscardLastOperator()
        {
            if (Hand.Count == 0)
            {
                Debug.LogWarning("[PHM] 버릴 카드가 없습니다.");
                return;
            }

            Hand.RemoveAt(0);
            _isHandSelected = false;

            OnHandSelectionChanged?.Invoke(-1);
            OnSlotHighlightRequested?.Invoke(false);
            OnHandChanged?.Invoke();
        }

        // ─── 선택 해제 ────────────────────────────────────────────

        public void ClearSelection()
        {
            _isHandSelected = false;
            OnHandSelectionChanged?.Invoke(-1);
            OnSlotHighlightRequested?.Invoke(false);
        }
    }
}
