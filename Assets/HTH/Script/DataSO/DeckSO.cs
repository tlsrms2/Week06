using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    // 스테이지 하나에 쓰이는 카드 묶음.
    // Inspector에서 CardDataSO 에셋을 순서대로 드래그해 구성.
    [CreateAssetMenu(fileName = "Deck", menuName = "Blindjack/Deck")]
    public class DeckSO : ScriptableObject
    {
        [Tooltip("배치 순서 그대로 사용됨 (섞지 않음)")]
        public List<CardDataSO> cards = new();

        // 런타임에서 Queue로 변환하는 유틸
        public Queue<CardDataSO> ToQueue() => new(cards);
    }
}