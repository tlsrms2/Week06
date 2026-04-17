using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 플레이어 필드, 손패, 연산자 배치 인터랙션을 담당합니다.
    /// DeckSO.CardEntry(data + suit + suitData)로 카드 한 장을 관리합니다.
    /// </summary>
    public class PlayerHandManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public List<DeckSO.CardEntry> Field { get; private set; } = new();
        public List<DeckSO.CardEntry> Hand { get; private set; } = new();

        private readonly Dictionary<int, DeckSO.CardEntry> _placedOperators = new();
        private int _selectedHandIndex = -1;

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>필드에 카드가 추가될 때 발행</summary>
        public event System.Action OnFieldChanged;

        /// <summary>손패에 카드가 추가될 때 발행 — GameManager.OnPlayerHandChanged가 구독</summary>
        public event System.Action OnHandChanged;

        /// <summary>손패 카드 선택 상태 변경 시 발행 (-1=해제)</summary>
        public event System.Action<int> OnHandSelectionChanged;

        /// <summary>슬롯 하이라이트 요청 시 발행</summary>
        public event System.Action<bool> OnSlotHighlightRequested;

        // ─── 변환 헬퍼 ───────────────────────────────────────────
        /// <summary>BlackjackManager / ExpressionEvaluator용 CardDataSO 목록</summary>
        public List<CardDataSO> FieldData => Field.ConvertAll(e => e.data);

        /// <summary>BlackjackManager / ExpressionEvaluator용 손패 CardDataSO 목록</summary>
        public List<CardDataSO> HandData => Hand.ConvertAll(e => e.data);

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

        /// <summary>숫자 카드를 필드에 추가합니다.</summary>
        public void AddNumberToField(DeckSO.CardEntry entry)
        {
            Field.Add(entry);
            OnFieldChanged?.Invoke();
        }

        /// <summary>
        /// 연산자 카드를 손패에 추가합니다.
        /// OnHandChanged를 발행해 GameManager가 UI에 카드 1장만 추가하도록 합니다.
        /// </summary>
        public void AddOperatorToHand(DeckSO.CardEntry entry)
        {
            Hand.Add(entry);
            OnHandChanged?.Invoke(); // ← OnFieldChanged가 아닌 OnHandChanged 발행
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
        /// slotIndex는 숫자 카드 기준 인덱스입니다.
        /// (슬롯0 = 첫번째와 두번째 숫자 사이)
        /// </summary>
        public bool TryPlaceOperator(int slotIndex, out DeckSO.CardEntry placedEntry)
        {
            placedEntry = null;

            if (_selectedHandIndex < 0) return false;

            int numberCount = 0;
            foreach (var entry in Field)
                if (entry.data.cardType == CardType.Number) numberCount++;

            if (slotIndex < 0 || slotIndex >= numberCount - 1) return false;
            if (_placedOperators.ContainsKey(slotIndex)) return false;

            placedEntry = Hand[_selectedHandIndex];
            _placedOperators[slotIndex] = placedEntry;

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
        /// 숫자 카드 사이에 연산자를 끼워넣습니다.
        /// </summary>
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
            UnityEngine.Debug.Log(log.ToString());
        }

        // ─── 선택 해제 ────────────────────────────────────────────

        /// <summary>손패 선택 상태와 슬롯 하이라이트를 해제합니다.</summary>
        public void ClearSelection()
        {
            _selectedHandIndex = -1;
            OnHandSelectionChanged?.Invoke(-1);
            OnSlotHighlightRequested?.Invoke(false);
        }

        // ─── 읽기 전용 접근 ───────────────────────────────────────

        /// <summary>현재 선택된 손패 인덱스. -1 = 미선택</summary>
        public int SelectedHandIndex => _selectedHandIndex;
    }
}