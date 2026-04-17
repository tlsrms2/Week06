using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// DeckSO를 런타임 Queue로 변환하고 드로우를 담당합니다.
    /// SO 에셋 자체는 불변이며, 매 라운드 새 인스턴스를 생성합니다.
    /// </summary>
    public class DeckRunner
    {
        private Queue<CardDataSO> _queue;

        public int Remaining => _queue.Count;
        public bool IsEmpty => _queue.Count == 0;

        /// <summary>
        /// DeckSO와 StageDataSO를 받아 런타임 덱을 구성합니다.
        /// useOperatorCards가 true면 연산자 카드를 덱에 혼합합니다.
        /// </summary>
        public DeckRunner(DeckSO deck, StageDataSO stage, bool shuffle = true)
        {
            if (deck == null)
            {
                Debug.LogError("[DeckRunner] DeckSO가 null입니다.");
                _queue = new Queue<CardDataSO>();
                return;
            }

            var cards = deck.GetCards();

            // 연산자 카드 주입 (Stage 2+)
            if (stage.useOperatorCards)
                InjectOperatorCards(cards, stage.operatorCardRatio);

            // 셔플
            if (shuffle) Shuffle(cards);

            _queue = new Queue<CardDataSO>(cards);
            Debug.Log($"[DeckRunner] 덱 구성 완료 — {_queue.Count}장 " +
                      $"(연산자 포함: {stage.useOperatorCards})");
        }

        /// <summary>카드를 한 장 드로우합니다.</summary>
        public bool TryDraw(out CardDataSO card)
        {
            if (_queue.Count == 0) { card = null; return false; }
            card = _queue.Dequeue();
            return true;
        }

        /// <summary>
        /// 덱에 연산자 카드를 비율에 따라 주입합니다.
        /// 기존 카드 수 × ratio 만큼의 연산자 카드를 추가합니다.
        /// </summary>
        private void InjectOperatorCards(List<CardDataSO> cards, float ratio)
        {
            int count = Mathf.RoundToInt(cards.Count * ratio);
            var opTypes = new[]
            {
                OperatorType.Subtract,
                OperatorType.Multiply,
                OperatorType.Divide
            };

            for (int i = 0; i < count; i++)
            {
                var op = CreateInstance<CardDataSO>();
                op.cardType = CardType.Operator;
                op.operatorType = opTypes[i % opTypes.Length];
                op.displayLabel = op.operatorType switch
                {
                    OperatorType.Subtract => "−",
                    OperatorType.Multiply => "×",
                    OperatorType.Divide => "÷",
                    _ => "?"
                };
                cards.Add(op);
            }
        }

        /// <summary>
        /// 카드를 덱 앞쪽에 반환합니다.
        /// operatorOnlyHit 스테이지에서 연산자 카드를 재삽입할 때 사용합니다.
        /// </summary>
        public void ReturnCard(CardDataSO card)
        {
            // Queue는 앞쪽 삽입이 불가하므로 새 Queue로 재구성
            var temp = new Queue<CardDataSO>();
            temp.Enqueue(card);
            while (_queue.Count > 0)
                temp.Enqueue(_queue.Dequeue());
            _queue = temp;
        }

        /// <summary>Fisher-Yates 셔플</summary>
        private void Shuffle(List<CardDataSO> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        // ScriptableObject 런타임 인스턴스 생성 유틸
        private static T CreateInstance<T>() where T : ScriptableObject
            => ScriptableObject.CreateInstance<T>();
    }
}