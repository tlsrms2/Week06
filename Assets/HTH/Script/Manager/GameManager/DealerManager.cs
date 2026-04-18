using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 딜러 턴 진행과 딜러 필드를 담당합니다.
    /// DeckSO.CardEntry(data + suit + suitData)로 카드 한 장을 관리합니다.
    /// Stage 2+에서는 연산자 카드를 손패에 보관하고 AI가 자동 배치합니다.
    /// </summary>
    public class DealerManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public List<DeckSO.CardEntry> Field { get; private set; } = new();
        public List<DeckSO.CardEntry> Hand { get; private set; } = new();

        public bool HasHiddenCard => _hiddenCard != null;
        private DeckSO.CardEntry _hiddenCard;

        // ─── 변환 헬퍼 ───────────────────────────────────────────
        public List<CardDataSO> FieldData => Field.ConvertAll(e => e.data);

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>
        /// 딜러 필드에 카드가 추가될 때 발행.
        /// entry : 추가된 카드
        /// isHidden : 비공개 카드 여부
        /// GameManager.OnDealerCardAdded가 구독해 UI에 카드 1장만 추가합니다.
        /// </summary>
        public event System.Action<DeckSO.CardEntry, bool> OnDealerCardAdded;

        public event System.Action OnDealerCardRevealed;

        /// <summary>딜러 턴이 완전히 종료되면 발행</summary>
        public event System.Action OnDealerTurnEnded;

        // ─── 초기화 ───────────────────────────────────────────────

        public void ResetField()
        {
            Field.Clear();
            Hand.Clear();
            _hiddenCard = null;
        }

        // ─── 초기 딜링 ────────────────────────────────────────────

        /// <summary>딜러 공개 카드를 추가합니다.</summary>
        public void AddOpenCard(DeckSO.CardEntry entry)
        {
            Field.Add(entry);
            OnDealerCardAdded?.Invoke(entry, false); // isHidden = false
        }

        /// <summary>딜러 비공개 카드를 설정합니다. UI에는 뒷면으로 표시됩니다.</summary>
        public void SetHiddenCard(DeckSO.CardEntry entry)
        {
            _hiddenCard = entry;
            OnDealerCardAdded?.Invoke(entry, true); // isHidden = true
        }

        /// <summary>비공개 카드를 공개합니다. 딜러 턴 시작 시 호출합니다.</summary>
        public void RevealHiddenCard()
        {
            if (_hiddenCard == null) return;
            Field.Add(_hiddenCard);
            OnDealerCardAdded?.Invoke(_hiddenCard, false); // 공개 → isHidden = false
            _hiddenCard = null;
        }

        // ─── 딜러 턴 ─────────────────────────────────────────────

        public IEnumerator RunTurn(
            DeckRunner deckRunner,
            IDealerStrategy dealerStrategy,
            StageDataSO stage)
        {
            RevealHiddenCard();
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                if (stage.useOperatorCards && Hand.Count > 0)
                    TryAutoPlaceOperator(stage);

                long total = ExpressionEvaluator.Evaluate(
                    FieldData, stage.useFlexibleAce, stage.bustValue);

                Debug.Log($"[Dealer] total:{total} bustValue:{stage.bustValue}");

                if (total > stage.bustValue)
                {
                    Debug.Log("[Dealer] 버스트 — 턴 종료");
                    break;
                }

                if (!dealerStrategy.ShouldHit(total, stage))
                {
                    Debug.Log("[Dealer] ShouldHit false — 턴 종료");
                    break;
                }

                if (!deckRunner.TryDraw(out DeckSO.CardEntry entry))
                {
                    Debug.Log("[Dealer] 덱 소진 — 턴 종료");
                    break;
                }

                if (entry.data.cardType == CardType.Number)
                {
                    Field.Add(entry);
                    OnDealerCardAdded?.Invoke(entry, false);
                    yield return new WaitForSeconds(0.8f);
                }
                else if (stage.useOperatorCards)
                {
                    Hand.Add(entry);
                    yield return new WaitForSeconds(0.3f);
                }
            }

            if (stage.useOperatorCards && Hand.Count > 0)
                TryAutoPlaceOperator(stage);

            OnDealerTurnEnded?.Invoke();
        }

        // ─── AI 연산자 자동 배치 ──────────────────────────────────

        private void TryAutoPlaceOperator(StageDataSO stage)
        {
            var numberEntries = GetNumberEntries();
            if (numberEntries.Count < 2 || Hand.Count == 0) return;

            int slotIndex = numberEntries.Count - 2;
            if (IsSlotOccupied(slotIndex)) return;

            DeckSO.CardEntry bestOp = SelectBestOperator(numberEntries, stage);
            if (bestOp == null) return;

            Hand.Remove(bestOp);
            InsertOperatorAt(slotIndex, bestOp);

            Debug.Log($"[DealerManager] 연산자 자동 배치 — " +
                      $"slot:{slotIndex} op:{bestOp.data.displayLabel}");
        }

        private DeckSO.CardEntry SelectBestOperator(
            List<DeckSO.CardEntry> numberEntries,
            StageDataSO stage)
        {
            DeckSO.CardEntry bestOp = null;
            long bestScore = long.MinValue;

            foreach (var op in Hand)
            {
                var simField = BuildSimulatedField(numberEntries, op, numberEntries.Count - 2);
                long result = ExpressionEvaluator.Evaluate(
                    simField, stage.useFlexibleAce, stage.bustValue);
                long score = ScoreResult(result, stage);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestOp = op;
                }
            }

            return bestOp;
        }

        private long ScoreResult(long result, StageDataSO stage)
        {
            if (result > stage.bustValue) return long.MinValue;
            return -(stage.bustValue - result);
        }

        private List<CardDataSO> BuildSimulatedField(
            List<DeckSO.CardEntry> numberEntries,
            DeckSO.CardEntry op,
            int slotIndex)
        {
            var sim = new List<CardDataSO>();
            for (int i = 0; i < numberEntries.Count; i++)
            {
                if (i == slotIndex + 1) sim.Add(op.data);
                sim.Add(numberEntries[i].data);
            }
            return sim;
        }

        private List<DeckSO.CardEntry> GetNumberEntries()
        {
            var result = new List<DeckSO.CardEntry>();
            foreach (var entry in Field)
                if (entry.data.cardType == CardType.Number)
                    result.Add(entry);
            return result;
        }

        private bool IsSlotOccupied(int slotIndex)
        {
            int opCount = 0;
            foreach (var entry in Field)
            {
                if (entry.data.cardType == CardType.Operator)
                {
                    if (opCount == slotIndex) return true;
                    opCount++;
                }
            }
            return false;
        }

        private void InsertOperatorAt(int slotIndex, DeckSO.CardEntry op)
        {
            int numSeen = -1;
            int insertPos = Field.Count;

            for (int i = 0; i < Field.Count; i++)
            {
                if (Field[i].data.cardType == CardType.Number)
                {
                    numSeen++;
                    if (numSeen == slotIndex)
                    {
                        insertPos = i + 1;
                        break;
                    }
                }
            }

            Field.Insert(insertPos, op);
        }
    }
}