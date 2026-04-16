using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    // DeckSO(에셋) → 런타임 Queue 변환 담당.
    // SO는 불변이므로 매 라운드 새 Queue를 생성해 사용.
    public class DeckRunner
    {
        private Queue<CardDataSO> _queue;

        public int Remaining => _queue.Count;
        public bool IsEmpty => _queue.Count == 0;

        public DeckRunner(DeckSO deck)
        {
            if (deck == null)
            {
                Debug.LogError("[DeckRunner] DeckSO가 null입니다.");
                _queue = new Queue<CardDataSO>();
                return;
            }
            _queue = deck.ToQueue();
        }

        public bool TryDraw(out CardDataSO card)
        {
            if (_queue.Count == 0) { card = null; return false; }
            card = _queue.Dequeue();
            return true;
        }
    }
}