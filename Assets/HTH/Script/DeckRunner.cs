using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// DeckSO와 StageDataSO를 받아 런타임 덱을 구성합니다.
    /// 덱에는 숫자 카드만 포함됩니다.
    /// 연산자 카드는 GameManager에서 확률 판정으로 별도 지급합니다.
    /// </summary>
    public class DeckRunner
    {
        private Queue<DeckSO.CardEntry> _queue = new();

        public int Remaining => _queue.Count;
        public bool IsEmpty => _queue.Count == 0;

        /// <summary>
        /// DeckSO와 StageDataSO를 받아 런타임 덱을 구성합니다.
        /// useOperatorCards = true면 operatorCards 목록에서 연산자를 혼합합니다.
        /// </summary>
        public DeckRunner(DeckSO deck, StageDataSO stage, bool shuffle = true)
        {
            if (deck == null)
            {
                Debug.LogError("[DeckRunner] DeckSO가 null입니다.");
                return;
            }

            // DeckSO에서 전체 카드 목록 가져오기 (CardEntry 리스트)
            var cards = new List<DeckSO.CardEntry>(deck.GetAllCards());
            // 셔플
            if (shuffle) Shuffle(cards);

            _queue = new Queue<DeckSO.CardEntry>(cards);

            Debug.Log($"[DeckRunner] 덱 구성 완료 — {_queue.Count}장 " +
                      $"(연산자 포함: {stage.useOperatorCards})");
        }

        // ─── 드로우 ───────────────────────────────────────────────

        /// <summary>
        /// 카드를 한 장 드로우합니다.
        /// data와 suit를 함께 반환합니다.
        /// </summary>
        public bool TryDraw(out DeckSO.CardEntry entry)
        {
            if (_queue.Count == 0)
            {
                entry = null;
                return false;
            }
            entry = _queue.Dequeue();
            return true;
        }

        /// <summary>
        /// 카드를 덱 앞쪽에 반환합니다.
        /// operatorOnlyHit 스테이지에서 연산자 카드를 재삽입할 때 사용합니다.
        /// </summary>
        public void ReturnCard(DeckSO.CardEntry entry)
        {
            var temp = new Queue<DeckSO.CardEntry>();
            temp.Enqueue(entry);
            while (_queue.Count > 0)
                temp.Enqueue(_queue.Dequeue());
            _queue = temp;
        }

        // ─── 내부 ─────────────────────────────────────────────────

        /// <summary>Fisher-Yates 셔플</summary>
        private void Shuffle(List<DeckSO.CardEntry> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}