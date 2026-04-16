using UnityEngine;

namespace HTH
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Blindjack/Stage Data")]
    public class StageDataSO : ScriptableObject
    {
        [Header("스테이지 기본 정보")]
        [Tooltip("표시용 스테이지 번호 (1-based)")]
        public int stageIndex;

        [Tooltip("기하급수적으로 증가하는 목표값")]
        public long quota;

        [Header("덱")]
        [Tooltip("플레이어에게 드로우되는 카드 묶음")]
        public DeckSO playerDeck;

        [Tooltip("딜러가 사용하는 카드 묶음")]
        public DeckSO dealerDeck;

        [Header("시야 배팅 범위 (추후 활성화)")]
        [Tooltip("해당 스테이지에서 배팅 가능한 시야 최솟값")]
        public float visionBetMin = 10f;

        [Tooltip("해당 스테이지에서 배팅 가능한 시야 최댓값")]
        public float visionBetMax = 50f;
    }
}