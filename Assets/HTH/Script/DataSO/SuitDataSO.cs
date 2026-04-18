using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 무늬 하나(Spade/Club/Heart/Diamond)의 전체 카드 데이터를 정의합니다.
    /// 1~13번 카드 정보를 배열로 관리합니다.
    ///
    /// SO 에셋 구성
    /// ├── Spade.asset   — 스페이드 1~13
    /// ├── Club.asset    — 클로버 1~13
    /// ├── Heart.asset   — 하트 1~13
    /// └── Diamond.asset — 다이아몬드 1~13
    /// </summary>
    [CreateAssetMenu(fileName = "SuitData", menuName = "Blindjack/Suit Data")]
    public class SuitDataSO : ScriptableObject
    {
        [Header("무늬 정보")]
        [Tooltip("이 SO의 무늬 종류")]
        public SuitDataSO suit;

        [Tooltip("공유 Material (CardsAndTables.mat)")]
        public Material sharedMaterial;

        [Tooltip("UV Scale — 카드 한 장의 UV 크기 비율")]
        public Vector2 uvScale = new Vector2(1f, 1f);

        [Header("카드 데이터 (1~13)")]
        [Tooltip("index 0 = A(1), index 1 = 2 ... index 12 = K(13)")]
        public CardInfo[] cards = new CardInfo[13];

        // ─── 유틸 메서드 ─────────────────────────────────────────

        /// <summary>
        /// numberValue(1~13)에 해당하는 CardInfo를 반환합니다.
        /// </summary>
        public CardInfo GetCard(int numberValue)
        {
            int index = Mathf.Clamp(numberValue - 1, 0, 12);
            return cards[index];
        }

        /// <summary>
        /// numberValue에 해당하는 프리팹을 반환합니다.
        /// </summary>
        public GameObject GetPrefab(int numberValue)
            => GetCard(numberValue).prefab;

        /// <summary>
        /// numberValue에 해당하는 UV Offset을 반환합니다.
        /// </summary>
        public Vector2 GetOffset(int numberValue)
            => GetCard(numberValue).uvOffset;
    }

    /// <summary>
    /// 카드 한 장의 비주얼 정보입니다.
    /// SuitDataSO 배열의 원소로 사용됩니다.
    /// </summary>
    [System.Serializable]
    public class CardInfo
    {
        [Tooltip("카드 3D 프리팹")]
        public GameObject prefab;

        [Tooltip("아틀라스 UV Offset")]
        public Vector2 uvOffset;
    }
}