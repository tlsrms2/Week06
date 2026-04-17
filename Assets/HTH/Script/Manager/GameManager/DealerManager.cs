using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 딜러 턴 진행과 딜러 필드를 담당합니다.
    /// Stage 2+에서는 연산자 카드를 손패에 보관하고
    /// DealerAI가 최적 연산자를 자동 배치합니다.
    /// </summary>
    public class DealerManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        /// <summary>공개된 딜러 필드 카드 목록 (숫자 + 배치된 연산자)</summary>
        public List<CardDataSO> Field { get; private set; } = new();

        /// <summary>딜러 손패 — 아직 배치되지 않은 연산자 카드</summary>
        public List<CardDataSO> Hand { get; private set; } = new();

        /// <summary>비공개 카드 보유 여부</summary>
        public bool HasHiddenCard => _hiddenCard != null;

        private CardDataSO _hiddenCard;

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>딜러 필드/손패가 변경될 때마다 발행</summary>
        public event System.Action OnDealerFieldChanged;

        /// <summary>딜러 턴이 완전히 종료되면 발행</summary>
        public event System.Action OnDealerTurnEnded;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>라운드 시작 시 딜러 필드/손패/비공개 카드를 초기화합니다.</summary>
        public void ResetField()
        {
            Field.Clear();
            Hand.Clear();
            _hiddenCard = null;
        }

        // ─── 초기 딜링 ────────────────────────────────────────────

        /// <summary>
        /// 딜러 공개 카드를 추가합니다.
        /// 초기 딜링 시 첫 번째 카드에 사용합니다.
        /// </summary>
        public void AddOpenCard(CardDataSO card)
        {
            Field.Add(card);
            OnDealerFieldChanged?.Invoke();
        }

        /// <summary>
        /// 딜러 비공개 카드를 설정합니다.
        /// 초기 딜링 시 두 번째 카드에 사용합니다.
        /// UI에는 뒷면으로 표시됩니다.
        /// </summary>
        public void SetHiddenCard(CardDataSO card)
        {
            _hiddenCard = card;
            OnDealerFieldChanged?.Invoke();
        }

        /// <summary>
        /// 비공개 카드를 공개합니다.
        /// 플레이어 Stay 후 딜러 턴 시작 시 호출합니다.
        /// </summary>
        public void RevealHiddenCard()
        {
            if (_hiddenCard == null) return;
            Field.Add(_hiddenCard);
            _hiddenCard = null;
            OnDealerFieldChanged?.Invoke();
        }

        // ─── 딜러 턴 ─────────────────────────────────────────────

        /// <summary>
        /// 딜러 턴을 실행합니다.
        /// 비공개 카드를 먼저 공개한 뒤 ShouldHit 조건을 만족하는 동안 드로우합니다.
        /// Stage 2+에서는 드로우한 연산자 카드를 AI가 자동 배치합니다.
        /// </summary>
        public IEnumerator RunTurn(
            DeckRunner deckRunner,
            IDealerStrategy dealerStrategy,
            StageDataSO stage)
        {
            // 비공개 카드 공개
            RevealHiddenCard();
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                // 연산자 자동 배치 시도 (Stage 2+)
                if (stage.useOperatorCards && Hand.Count > 0)
                    TryAutoPlaceOperator(dealerStrategy, stage);

                long total = ExpressionEvaluator.Evaluate(
                    Field,
                    stage.useFlexibleAce,
                    stage.bustValue);

                if (!dealerStrategy.ShouldHit(total, stage)) break;
                if (!deckRunner.TryDraw(out CardDataSO card)) break;

                if (card.cardType == CardType.Number)
                {
                    Field.Add(card);
                    OnDealerFieldChanged?.Invoke();
                    yield return new WaitForSeconds(0.8f);
                }
                else if (stage.useOperatorCards)
                {
                    // 연산자 카드는 손패에 보관
                    Hand.Add(card);
                    OnDealerFieldChanged?.Invoke();
                    yield return new WaitForSeconds(0.3f);
                }
            }

            // 턴 종료 전 남은 연산자 최종 배치
            if (stage.useOperatorCards && Hand.Count > 0)
                TryAutoPlaceOperator(dealerStrategy, stage);

            OnDealerTurnEnded?.Invoke();
        }

        // ─── AI 연산자 자동 배치 ──────────────────────────────────

        /// <summary>
        /// 손패의 연산자 카드 중 목표값에 가장 유리한 것을 자동 배치합니다.
        /// 숫자 카드가 2장 이상일 때만 동작합니다.
        /// 배치 위치는 마지막 숫자 카드 앞 슬롯입니다.
        /// </summary>
        private void TryAutoPlaceOperator(IDealerStrategy dealerStrategy, StageDataSO stage)
        {
            // 숫자 카드 2장 이상이어야 슬롯 존재
            var numberCards = GetNumberCards();
            if (numberCards.Count < 2 || Hand.Count == 0) return;

            // 마지막 슬롯 인덱스
            int slotIndex = numberCards.Count - 2;

            // 이미 해당 슬롯에 연산자가 배치되어 있으면 스킵
            if (IsSlotOccupied(slotIndex)) return;

            // 가장 유리한 연산자 선택
            CardDataSO bestOp = SelectBestOperator(numberCards, stage);
            if (bestOp == null) return;

            // 손패에서 제거 후 필드에 삽입
            Hand.Remove(bestOp);
            InsertOperatorAt(slotIndex, bestOp);
            OnDealerFieldChanged?.Invoke();

            Debug.Log($"[DealerManager] 연산자 자동 배치 — " +
                      $"slot:{slotIndex} op:{bestOp.displayLabel}");
        }

        /// <summary>
        /// 손패의 연산자 중 목표값에 가장 근접한 결과를 내는 것을 선택합니다.
        /// currentValueSet = true  : bustValue 이하 최대값을 목표
        /// currentValueSet = false : bustValue 이상 최소값을 목표
        /// </summary>
        private CardDataSO SelectBestOperator(
            List<CardDataSO> numberCards,
            StageDataSO stage)
        {
            CardDataSO bestOp = null;
            long bestScore = long.MinValue;

            foreach (var op in Hand)
            {
                // 마지막 슬롯에 이 연산자를 배치했을 때의 결과 시뮬레이션
                var simField = BuildSimulatedField(numberCards, op, numberCards.Count - 2);
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

        /// <summary>
        /// 결과값을 점수로 변환합니다.
        /// currentValueSet = true  : bustValue 이하 최대값이 높은 점수
        /// currentValueSet = false : bustValue 이상 최소값이 높은 점수
        /// 버스트 시 최저점 반환합니다.
        /// </summary>
        private long ScoreResult(long result, StageDataSO stage)
        {
            if (stage.currentValueSet)
            {
                // 높아야 하는 스테이지 — 버스트 없이 최대한 높을수록 좋음
                if (result > stage.bustValue) return long.MinValue;
                return result;
            }
            else
            {
                // 낮아야 하는 스테이지 — 버스트 없이 최대한 낮을수록 좋음
                if (result < stage.bustValue) return long.MinValue;
                return -result; // 낮을수록 높은 점수
            }
        }

        /// <summary>
        /// 특정 슬롯에 연산자를 배치했을 때의 필드를 시뮬레이션합니다.
        /// </summary>
        private List<CardDataSO> BuildSimulatedField(
            List<CardDataSO> numberCards,
            CardDataSO op,
            int slotIndex)
        {
            var sim = new List<CardDataSO>();
            for (int i = 0; i < numberCards.Count; i++)
            {
                if (i == slotIndex + 1) sim.Add(op); // 슬롯 위치에 연산자 삽입
                sim.Add(numberCards[i]);
            }
            return sim;
        }

        /// <summary>Field에서 숫자 카드만 추출합니다.</summary>
        private List<CardDataSO> GetNumberCards()
        {
            var result = new List<CardDataSO>();
            foreach (var card in Field)
                if (card.cardType == CardType.Number)
                    result.Add(card);
            return result;
        }

        /// <summary>해당 슬롯에 이미 연산자가 배치되어 있는지 확인합니다.</summary>
        private bool IsSlotOccupied(int slotIndex)
        {
            // Field에서 슬롯 인덱스 번째 연산자 존재 여부 확인
            int opCount = 0;
            foreach (var card in Field)
            {
                if (card.cardType == CardType.Operator)
                {
                    if (opCount == slotIndex) return true;
                    opCount++;
                }
            }
            return false;
        }

        /// <summary>
        /// Field의 특정 슬롯 위치에 연산자 카드를 삽입합니다.
        /// 숫자 카드 기준 slotIndex번째 숫자와 그 다음 숫자 사이에 삽입합니다.
        /// </summary>
        private void InsertOperatorAt(int slotIndex, CardDataSO op)
        {
            // slotIndex번째 숫자 카드 다음 위치 탐색
            int numSeen = -1;
            int insertPos = Field.Count;

            for (int i = 0; i < Field.Count; i++)
            {
                if (Field[i].cardType == CardType.Number)
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