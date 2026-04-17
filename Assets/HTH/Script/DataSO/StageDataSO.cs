using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 스테이지 하나의 모든 규칙을 정의합니다.
    /// 스테이지마다 블랙잭 기본 룰에 어떤 요소가 추가되는지
    /// 플래그로 제어합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "Blindjack/Stage Data")]
    public class StageDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("표시용 스테이지 번호 (1-based)")]
        public int stageIndex;

        [Tooltip("이 스테이지의 목표 할당량")]
        public long bustValue;

        [Header("덱 설정")]
        [Tooltip("사용할 덱 SO — Standard52 또는 Custom")]
        public DeckSO deck;

        [Header("스테이지 규칙 플래그")]
        [Tooltip("true = 연산자 카드가 덱에 포함됨 (Stage 2+)")]
        public bool useOperatorCards = false;

        [Tooltip("연산자 카드 비율 (덱 전체 대비, 0.0~1.0) useOperatorCards = true일 때 사용")]
        [Range(0f, 0.3f)]
        public float operatorCardRatio = 0.1f;

        [Tooltip("true = 기하급수적 할당량 적용 (Stage 3+)")]
        public bool useExponentialQuota = false;

        [Header("시야 배팅 범위")]
        [Tooltip("이 스테이지 최소 배팅량")]
        public int visionBetMin = 5;

        [Tooltip("이 스테이지 최대 배팅량")]
        public int visionBetMax = 50;

        [Header("버스트 기준 값")]
        [Tooltip("true: 이 값보다 높아야한다, false: 이 값보다 낮아야 한다")]
        public bool currentValueSet = false;

        [Tooltip("true = Ace를 1 또는 11 중 유리한 값으로 자동 계산 (Stage 1 블랙잭 룰)")]
        public bool useFlexibleAce = true;
    }
}