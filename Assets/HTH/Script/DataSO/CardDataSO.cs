using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 카드의 게임 데이터만 정의합니다.
    /// 비주얼 데이터는 SuitDataSO가 담당합니다.
    ///
    /// 숫자 카드 : numberValue + suit → SuitDataSO에서 프리팹/UV 조회
    /// 연산자 카드 : operatorType + JokerPrefab + uvOffset
    /// </summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "Blindjack/Card Data")]
    public class CardDataSO : ScriptableObject
    {
        [Header("게임 데이터")]
        public CardType cardType;
        public int numberValue;
        public OperatorType operatorType;
        public string displayLabel;
        [SerializeField] private int _aceValueOverride;

        [Header("연산자 카드 비주얼 (Operator 전용)")]
        [Tooltip("연산자 카드 프리팹 (Joker 프리팹)")]
        public GameObject jokerPrefab;

        [Tooltip("연산자 카드 UV Offset")]
        public Vector2 operatorUvOffset;

        [Tooltip("연산자 카드 공유 Material")]
        public Material operatorMaterial;

        [Tooltip("연산자 카드 UV Scale")]
        public Vector2 operatorUvScale = new Vector2(1f, 1f);

        // ─── 유틸 프로퍼티 ───────────────────────────────────────

        public bool IsAce => cardType == CardType.Number && numberValue == 1;
        public bool HasAceValueOverride => IsAce && (_aceValueOverride == 1 || _aceValueOverride == 11);
        public bool IsFlexibleAceCandidate => IsAce && !HasAceValueOverride;

        public int BlackjackValue
            => HasAceValueOverride
                ? _aceValueOverride
                : numberValue > 10 ? 10 : numberValue;

        public void SetAceValue(int value)
        {
            if (!IsAce) return;

            _aceValueOverride = value == 11 ? 11 : 1;
            displayLabel = _aceValueOverride == 11 ? "A(11)" : "A(1)";
        }

        public void ClearAceValueOverride()
        {
            if (!IsAce) return;

            _aceValueOverride = 0;
            displayLabel = "A";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cardType == CardType.Number
             && string.IsNullOrEmpty(displayLabel))
            {
                displayLabel = numberValue switch
                {
                    1 => "A",
                    11 => "J",
                    12 => "Q",
                    13 => "K",
                    _ => numberValue.ToString()
                };
            }
            else if (cardType == CardType.Operator
                  && string.IsNullOrEmpty(displayLabel))
            {
                displayLabel = operatorType switch
                {
                    OperatorType.Subtract => "−",
                    OperatorType.Multiply => "×",
                    OperatorType.Divide => "÷",
                    _ => "?"
                };
            }
        }
#endif
    }
}
