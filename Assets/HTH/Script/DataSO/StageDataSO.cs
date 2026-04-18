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

        [Header("드로우 제한")]
        [Tooltip("플레이어 최대 Hit 횟수 — 0이면 덱 소진까지 무제한")]
        public int maxHitCount = 0;

        [Header("덱 설정")]
        [Tooltip("사용할 덱 SO — Standard52 또는 Custom")]
        public DeckSO deck;
        [Tooltip("연산자 카드 비율 (덱 전체 대비, 0.0~1.0) useOperatorCards = true일 때 사용")]
        [Range(0f, 1f)]
        public float operatorCardRatio = 0.1f;

        [Header("드로우 설정")]
        [Tooltip("true = Hit 시 연산자 카드만 드로우 (숫자는 초기 딜링으로 모두 지급)")]
        public bool operatorOnlyHit = false;

        [Header("스테이지 규칙 플래그")]
        [Tooltip("true = Ace를 1 또는 11 중 유리한 값으로 자동 계산 (Stage 1 블랙잭 룰)")]
        public bool useFlexibleAce = true;

        [Tooltip("true = 연산자 카드가 덱에 포함됨 (Stage 2+)")]
        public bool useOperatorCards = false;

        [Header("판별식 설정")]

        [Tooltip("true = 기본 판별식 (bustValue 초과 시 버스트)\n" +
         "false = 리버스 판별식 (bustValue 미만 시 버스트)")]
        public bool normalJudge = true;

        [Tooltip("true = 음수 허용 false = 0 미만이면 0으로 고정")]
        public bool allowNegative = false;

        [Header("시야 배팅 범위")]
        [Tooltip("이 스테이지 최소 배팅량")]
        public int visionBetMin = 5;

        [Tooltip("이 스테이지 최대 배팅량")]
        public int visionBetMax = 50;
    }
}