using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 딜러 턴 진행과 딜러 필드를 담당합니다.
    /// GameManager가 코루틴을 시작하고 완료 콜백을 받습니다.
    /// </summary>
    public class DealerManager : MonoBehaviour
    {
        // ─── 상태 ─────────────────────────────────────────────────
        public List<CardDataSO> Field { get; private set; } = new();

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>딜러가 카드를 드로우할 때마다 발행</summary>
        public event System.Action OnDealerFieldChanged;

        /// <summary>딜러 턴이 완전히 종료되면 발행</summary>
        public event System.Action OnDealerTurnEnded;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>라운드 시작 시 딜러 필드를 초기화합니다.</summary>
        public void ResetField() => Field.Clear();

        // ─── 딜러 턴 ─────────────────────────────────────────────

        /// <summary>
        /// 딜러 턴을 시작합니다.
        /// GameManager가 StartCoroutine으로 실행합니다.
        /// ShouldHit 조건을 만족하는 동안 카드를 드로우합니다.
        /// </summary>
        public IEnumerator RunTurn(
            DeckRunner deckRunner,
            IDealerStrategy dealerStrategy,
            StageDataSO stage)
        {
            while (true)
            {
                long total = ExpressionEvaluator.Evaluate(
                    Field,
                    stage.useFlexibleAce,
                    stage.bustThreshold);

                if (!dealerStrategy.ShouldHit(total)) break;
                if (!deckRunner.TryDraw(out CardDataSO card)) break;

                // 딜러는 숫자 카드만 받음
                if (card.cardType != CardType.Number) continue;

                Field.Add(card);
                OnDealerFieldChanged?.Invoke();
                yield return new WaitForSeconds(0.8f);
            }

            OnDealerTurnEnded?.Invoke();
        }
    }
}