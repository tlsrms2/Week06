using UnityEngine;

namespace HTH
{
    public enum CardType { Number, Operator }
    public enum OperatorType { None, Subtract, Multiply, Divide }

    [CreateAssetMenu(fileName = "CardData", menuName = "Blindjack/Card Data")]
    public class CardDataSO : ScriptableObject
    {
        [Header("카드 정보")]
        [Tooltip("숫자 카드 / 연산 카드")]
        public CardType cardType;

        [Tooltip("숫자 카드일 때의 값 (1=A, 11=J, 12=Q, 13=K)")]
        public int numberValue;

        [Tooltip("연산 카드일 때의 연산자 종류")]
        public OperatorType operatorType;

        // 표시 문자열 — Inspector에서 직접 입력 (A, J, Q, K, ×, ÷ 등)
        [Tooltip("카드에 표시될 문자열")]
        public string displayLabel;

#if UNITY_EDITOR
        // Inspector에서 값 변경 시 displayLabel 자동 제안 (편의 기능)
        private void OnValidate()
        {
            if (cardType == CardType.Number && string.IsNullOrEmpty(displayLabel))
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
            else if (cardType == CardType.Operator && string.IsNullOrEmpty(displayLabel))
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