using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    [CreateAssetMenu(fileName = "Deck", menuName = "Blindjack/Deck")]
    public class DeckSO : ScriptableObject
    {
        public enum DeckType { Standard52, Custom }

        [Header("덱 설정")]
        public DeckType deckType = DeckType.Standard52;

        [Header("Standard52 — 무늬 SO 4개")]
        [Tooltip("스페이드 SO")]
        public SuitDataSO spade;
        [Tooltip("클로버 SO")]
        public SuitDataSO club;
        [Tooltip("하트 SO")]
        public SuitDataSO heart;
        [Tooltip("다이아몬드 SO")]
        public SuitDataSO diamond;

        [Header("연산자 카드 목록")]
        public List<CardDataSO> operatorCards = new();

        /// <summary>
        /// 전체 카드 목록을 CardEntry 리스트로 반환합니다.
        /// Standard52 : 4무늬 × 13장 = 52장
        /// </summary>
        public List<CardEntry> GetAllCards()
        {
            var result = new List<CardEntry>();

            if (deckType == DeckType.Standard52)
            {
                AddSuit(result, spade, CardSuit.Spade);
                AddSuit(result, club, CardSuit.Club);
                AddSuit(result, heart, CardSuit.Heart);
                AddSuit(result, diamond, CardSuit.Diamond);
            }

            return result;
        }

        private void AddSuit(List<CardEntry> result, SuitDataSO suitData, CardSuit suit)
        {
            if (suitData == null) return;

            // 1~13 카드 생성
            for (int i = 1; i <= 13; i++)
            {
                // 숫자에 맞는 게임 데이터 SO가 없으므로
                // 런타임에 CardDataSO를 생성
                var data = ScriptableObject.CreateInstance<CardDataSO>();
                data.cardType = CardType.Number;
                data.numberValue = i;
                if (i == 1)
                    data.ClearAceValueOverride();
                else
                {
                    data.displayLabel = i switch
                    {
                        11 => "J",
                        12 => "Q",
                        13 => "K",
                        _ => i.ToString()
                    };
                }

                result.Add(new CardEntry
                {
                    data = data,
                    suit = suit,
                    suitData = suitData
                });
            }
        }

        [System.Serializable]
        public class CardEntry
        {
            public CardDataSO data;
            public CardSuit suit;
            public SuitDataSO suitData; // ← 비주얼 참조
        }
    }
}
