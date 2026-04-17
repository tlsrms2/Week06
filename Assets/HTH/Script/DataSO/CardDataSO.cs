using UnityEngine;

namespace HTH
{
    public enum CardType { Number, Operator }
    public enum OperatorType { None, Subtract, Multiply, Divide }

    [CreateAssetMenu(fileName = "CardData", menuName = "Blindjack/Card Data")]
    public class CardDataSO : ScriptableObject
    {
        [Header("카드 기본 정보")]
        [Tooltip("숫자 카드 / 연산자 카드")]
        public CardType cardType;

        [Tooltip("숫자 카드일 때의 값 (1=A, 11=J, 12=Q, 13=K)")]
        public int numberValue;

        [Tooltip("연산자 카드일 때의 연산자 종류")]
        public OperatorType operatorType;

        [Tooltip("카드에 표시될 문자열 (A, J, Q, K, ×, ÷ 등)")]
        public string displayLabel;

        [Header("3D 카드 비주얼")]
        [Tooltip("카드 앞면 텍스처 — 없으면 displayLabel 텍스트로 대체")]
        public Texture2D frontTexture;

        [Tooltip("카드 뒷면 텍스처 — 없으면 기본 뒷면 색상으로 대체")]
        public Texture2D backTexture;

        [Tooltip("카드 가운데 레이어 프리팹 — 앞면과 뒷면 사이에 배치되는 얇은 프리미티브")]
        public GameObject middlePrimitivePrefab;

        // ─── 유틸 프로퍼티 ───────────────────────────────────────
        /// <summary>앞면 텍스처가 설정되어 있는지 확인합니다.</summary>
        public bool HasFrontTexture => frontTexture != null;

        /// <summary>뒷면 텍스처가 설정되어 있는지 확인합니다.</summary>
        public bool HasBackTexture => backTexture != null;

        /// <summary>가운데 프리미티브 프리팹이 설정되어 있는지 확인합니다.</summary>
        public bool HasMiddlePrimitive => middlePrimitivePrefab != null;

#if UNITY_EDITOR
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