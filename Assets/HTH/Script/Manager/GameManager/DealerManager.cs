using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 딜러 턴 진행과 딜러 필드를 담당합니다.
    ///
    /// 이벤트 구분
    /// ├── OnDealerCardAdded    : 새 카드가 필드에 추가될 때 → UI 카드 생성
    /// ├── OnDealerCardRevealed : 비공개 카드 공개 요청 → FieldManager가 애니메이션 처리
    ///                           Action 콜백으로 완료 시점을 전달
    /// └── OnDealerTurnEnded   : 딜러 턴 종료 시
    /// </summary>
    public class DealerManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public List<DeckSO.CardEntry> Field { get; private set; } = new();
        public List<DeckSO.CardEntry> Hand  { get; private set; } = new();

        public bool HasHiddenCard => _hiddenCard != null;
        private DeckSO.CardEntry _hiddenCard;

        // ─── 변환 헬퍼 ───────────────────────────────────────────
        public List<CardDataSO> FieldData => Field.ConvertAll(e => e.data);

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>새 카드가 필드에 추가될 때 발행 (entry, isHidden)</summary>
        public event System.Action<DeckSO.CardEntry, bool> OnDealerCardAdded;

        /// <summary>
        /// 비공개 카드 공개 요청.
        /// Action 콜백 = 뒤집기 완료 후 호출할 함수 (RunTurn 재개용)
        /// </summary>
        public event System.Action<System.Action> OnDealerCardRevealed;

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

        public void AddOpenCard(DeckSO.CardEntry entry)
        {
            Field.Add(entry);
            OnDealerCardAdded?.Invoke(entry, false);
        }

        public void SetHiddenCard(DeckSO.CardEntry entry)
        {
            _hiddenCard = entry;
            OnDealerCardAdded?.Invoke(entry, true);
        }

        /// <summary>
        /// 비공개 카드 공개 요청을 발행합니다.
        /// onComplete : 뒤집기 애니메이션 완료 후 호출될 콜백
        /// </summary>
        public void RevealHiddenCard(System.Action onComplete = null)
        {
            if (_hiddenCard == null)
            {
                onComplete?.Invoke();
                return;
            }

            Field.Add(_hiddenCard);
            _hiddenCard = null;

            OnDealerCardRevealed?.Invoke(onComplete);
        }

        // ─── 딜러 턴 ─────────────────────────────────────────────

        /// <summary>
        /// 딜러 턴을 실행합니다.
        /// 비공개 카드 뒤집기 애니메이션 완료 후 카드 드로우를 시작합니다.
        ///
        /// 드로우 루프 흐름 (스펙 반영)
        ///  1) 손패의 연산자를 Brute-Force로 최적 슬롯에 먼저 배치
        ///  2) 현재 점수 계산 → 버스트 시 종료
        ///  3) ShouldHit (동적 standThreshold) → false 면 스탠드
        ///  4) 카드 드로우 → 숫자면 필드, 연산자면 손패
        ///  5) 1로 돌아가 반복
        /// </summary>
        public IEnumerator RunTurn(
            DeckRunner deckRunner,
            IDealerStrategy dealerStrategy,
            StageDataSO stage)
        {
            // 비공개 카드 뒤집기 — 완료 콜백으로 코루틴 재개
            bool revealed = false;
            RevealHiddenCard(onComplete: () => revealed = true);

            yield return new WaitUntil(() => revealed);
            yield return new WaitForSeconds(0.3f);

            // 딜러 카드 드로우 루프
            while (true)
            {
                // 1. 손패 연산자 최적 배치
                if (stage.useOperatorCards && Hand.Count > 0)
                    TryAutoPlaceOperator(stage);

                // 2. 현재 점수 계산
                long total = ExpressionEvaluator.Evaluate(
                    FieldData, stage.useFlexibleAce, stage.bustValue);

                Debug.Log($"[Dealer] total:{total} bustValue:{stage.bustValue}");

                if (total > stage.bustValue)
                {
                    Debug.Log("[Dealer] 버스트 — 턴 종료");
                    break;
                }

                // 3. 스탠드 판단
                if (!dealerStrategy.ShouldHit(total, stage))
                {
                    Debug.Log("[Dealer] ShouldHit false — 턴 종료");
                    break;
                }

                // 4. 카드 드로우
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

            // 턴 종료 직전 손패에 남은 연산자 마지막으로 한 번 더 배치 시도
            if (stage.useOperatorCards && Hand.Count > 0)
                TryAutoPlaceOperator(stage);

            OnDealerTurnEnded?.Invoke();
        }

        // ─── AI 연산자 자동 배치 (Brute-Force 전탐색) ────────────────

        /// <summary>
        /// 손패에 있는 연산자를 모든 슬롯 × 모든 연산자 조합으로 전탐색하여
        /// 최적 위치에 배치합니다.
        ///
        /// 판단 기준 (우선순위):
        /// 1순위: 배치 후 결과가 bustValue 이하이면서 bustValue에 가장 가까운 경우
        /// 2순위: 모든 배치가 버스트라면, 결과가 가장 작은 위치
        ///        (다음 턴에 ÷ 또는 − 카드로 구제받을 확률 극대화)
        ///
        /// 슬롯 정의: 숫자 카드 N장 → N-1개의 빈 슬롯(이미 연산자가 없는 슬롯만 대상)
        /// </summary>
        private void TryAutoPlaceOperator(StageDataSO stage)
        {
            var numberEntries = GetNumberEntries();
            if (numberEntries.Count < 2 || Hand.Count == 0) return;

            int slotCount = numberEntries.Count - 1; // 숫자 N개 → 슬롯 N-1개

            DeckSO.CardEntry bestOp   = null;
            int              bestSlot = -1;
            long             bestScore = long.MinValue; // 非버스트: 클수록 좋음(목표 근접)

            DeckSO.CardEntry fallbackOp   = null;
            int              fallbackSlot = -1;
            long             fallbackMin  = long.MaxValue; // 全버스트 fallback: 작을수록 좋음

            bool anyNonBust = false;

            foreach (var op in Hand)
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (IsSlotOccupied(slot)) continue;

                    var simField = BuildSimulatedField(numberEntries, op, slot);
                    long result  = ExpressionEvaluator.Evaluate(
                        simField, stage.useFlexibleAce, stage.bustValue);

                    Debug.Log($"[DealerManager] 시뮬 op:{op.data.displayLabel} " +
                              $"slot:{slot} → {result} (bust>{stage.bustValue})");

                    if (result <= stage.bustValue)
                    {
                        anyNonBust = true;
                        long score = -(stage.bustValue - result); // diff 작을수록 score 높음
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestOp    = op;
                            bestSlot  = slot;
                        }
                    }
                    else
                    {
                        // 全버스트 fallback: 가장 결과값이 작은 배치 선택
                        if (result < fallbackMin)
                        {
                            fallbackMin  = result;
                            fallbackOp   = op;
                            fallbackSlot = slot;
                        }
                    }
                }
            }

            DeckSO.CardEntry chosenOp   = anyNonBust ? bestOp   : fallbackOp;
            int              chosenSlot = anyNonBust ? bestSlot : fallbackSlot;

            if (chosenOp == null) return;

            Hand.Remove(chosenOp);
            InsertOperatorAt(chosenSlot, chosenOp);

            Debug.Log($"[DealerManager] 최적 연산자 배치 완료 — " +
                      $"op:{chosenOp.data.displayLabel} slot:{chosenSlot} " +
                      $"(NonBust선택:{anyNonBust})");
        }

        // ─── 시뮬레이션 헬퍼 ─────────────────────────────────────

        /// <summary>
        /// 숫자 카드 리스트에서 slotIndex번째 숫자 뒤에 연산자를 끼운 가상 필드를 만듭니다.
        /// 예) [3, 7, 5] 에서 slot=1 에 op(×) → [3, 7, ×, 5]
        /// </summary>
        private List<CardDataSO> BuildSimulatedField(
            List<DeckSO.CardEntry> numberEntries,
            DeckSO.CardEntry op,
            int slotIndex)
        {
            var sim = new List<CardDataSO>();
            for (int i = 0; i < numberEntries.Count; i++)
            {
                sim.Add(numberEntries[i].data);
                if (i == slotIndex)
                    sim.Add(op.data);
            }
            return sim;
        }

        // ─── 유틸리티 ────────────────────────────────────────────

        private List<DeckSO.CardEntry> GetNumberEntries()
        {
            var result = new List<DeckSO.CardEntry>();
            foreach (var entry in Field)
                if (entry.data.cardType == CardType.Number)
                    result.Add(entry);
            return result;
        }

        /// <summary>
        /// 숫자[slotIndex]와 숫자[slotIndex+1] 사이에 이미 연산자가 있는지 확인합니다.
        /// </summary>
        private bool IsSlotOccupied(int slotIndex)
        {
            int numSeen = -1;
            foreach (var entry in Field)
            {
                if (entry.data.cardType == CardType.Number)
                {
                    numSeen++;
                    if (numSeen == slotIndex) continue;
                }
                else if (entry.data.cardType == CardType.Operator && numSeen == slotIndex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 숫자[slotIndex] 바로 뒤에 연산자를 Field 리스트에 삽입합니다.
        /// </summary>
        private void InsertOperatorAt(int slotIndex, DeckSO.CardEntry op)
        {
            int numSeen  = -1;
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
