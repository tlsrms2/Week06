using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 카드 덱을 관리합니다.
    /// 표준 52장 덱을 생성하거나 커스텀 덱을 Inspector에서 구성할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Deck", menuName = "Blindjack/Deck")]
    public class DeckSO : ScriptableObject
    {
        public enum DeckType
        {
            /// <summary>표준 블랙잭 52장 덱 (자동 생성)</summary>
            Standard52,
            /// <summary>Inspector에서 직접 구성한 커스텀 덱</summary>
            Custom
        }

        [Header("덱 타입")]
        [Tooltip("Standard52: 표준 52장 자동 생성 / Custom: 직접 구성")]
        public DeckType deckType = DeckType.Standard52;

        [Header("커스텀 덱 (DeckType = Custom일 때만 사용)")]
        [Tooltip("Custom 타입일 때 사용할 카드 목록")]
        public List<CardDataSO> customCards = new();

        /// <summary>
        /// 덱 타입에 따라 카드 Queue를 생성합니다.
        /// Standard52는 52장을 자동 생성하고,
        /// Custom은 Inspector 목록을 그대로 사용합니다.
        /// </summary>
        public List<CardDataSO> GetCards()
        {
            return deckType == DeckType.Standard52
                ? GenerateStandard52()
                : new List<CardDataSO>(customCards);
        }

        /// <summary>
        /// 표준 블랙잭 52장 덱을 생성합니다.
        /// 숫자 카드만 포함합니다 (연산자 카드는 StageDataSO에서 주입).
        /// A=1, 2~10, J=11, Q=12, K=13 × 4무늬
        /// </summary>
        private List<CardDataSO> GenerateStandard52()
        {
            var cards = new List<CardDataSO>();

            for (int suit = 0; suit < 4; suit++)
            {
                for (int value = 1; value <= 13; value++)
                {
                    // 런타임 전용 CardDataSO 인스턴스 생성
                    var card = CreateInstance<CardDataSO>();
                    card.cardType = CardType.Number;
                    card.numberValue = value;
                    card.displayLabel = value switch
                    {
                        1 => "A",
                        11 => "J",
                        12 => "Q",
                        13 => "K",
                        _ => value.ToString()
                    };
                    cards.Add(card);
                }
            }
            return cards;
        }
    }
}