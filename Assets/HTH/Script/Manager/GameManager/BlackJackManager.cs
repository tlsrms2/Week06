using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 블랙잭 룰 담당 Manager.
    ///
    /// 1단계 : 기본 블랙잭 룰
    ///   - bustValue 초과 시 버스트
    ///   - bustValue에 더 근접한 쪽 승리
    ///
    /// 2단계 : 사칙연산 룰
    ///   - bustValue가 스테이지마다 변경됨
    ///   - 연산자 카드(+,-,×,÷)를 숫자 카드 사이에 배치
    ///   - 좌→우 순차 연산으로 합산
    ///   - 딜러도 연산자를 활용해 bustValue에 근접
    ///   - 승패 판정 방식은 1단계와 동일
    ///     (bustValue에 더 근접한 쪽 승리)
    /// </summary>
    public class BlackjackManager : MonoBehaviour
    {
        // ─── 의존성 ───────────────────────────────────────────────
        private IDealerStrategy _dealerStrategy;

        // ─── 초기화 ───────────────────────────────────────────────

        /// <summary>딜러 전략을 주입합니다.</summary>
        public void Initialize(IDealerStrategy dealerStrategy)
        {
            _dealerStrategy = dealerStrategy;
        }

        // ─── 연산 ─────────────────────────────────────────────────

        /// <summary>
        /// 플레이어 필드를 연산합니다.
        /// 연산자 카드가 포함되어 있으면 좌→우 순차 연산합니다.
        /// useFlexibleAce = true면 Ace를 1 또는 11로 자동 선택합니다.
        /// </summary>
        public long EvaluatePlayer(List<CardDataSO> field, StageDataSO stage)
            => ExpressionEvaluator.Evaluate(field, stage.useFlexibleAce, 
                stage.bustValue, stage.allowNegative);

        /// <summary>딜러 필드를 연산합니다.</summary>
        public long EvaluateDealer(List<CardDataSO> field, StageDataSO stage)
            => ExpressionEvaluator.Evaluate(field, stage.useFlexibleAce,
                stage.bustValue, stage.allowNegative);

        // ─── 버스트 감지 ──────────────────────────────────────────

        /// <summary>
        /// HIT 중 플레이어 버스트 여부를 확인합니다.
        ///
        /// 기본 블랙잭 / 사칙연산 공통
        /// → 합산이 bustValue를 초과하면 버스트
        ///
        /// 단, 연산자 배치 전에는 버스트 감지를 하지 않습니다.
        /// (숫자만 있는 상태에서는 연산자로 값을 낮출 수 있으므로)
        /// </summary>
        public bool IsPlayerBust(List<CardDataSO> field, StageDataSO stage)
        {
            // 연산자 스테이지 — 연산자 미배치 시 스킵
            if (stage.useOperatorCards && !HasOperatorPlaced(field))
                return false;

            long total = EvaluatePlayer(field, stage);

            // 연산자 스테이지이고 손패에 연산자가 있으면 스킵
            // → GameManager에서 hand 정보를 넘겨줘야 하므로
            //    여기서는 필드에 연산자가 없을 때만 체크
            if (stage.useOperatorCards && !HasOperatorPlaced(field))
                return false;

            return total > stage.bustValue;
        }
        /// <summary>
        /// Stay 후 최종 버스트 여부를 확인합니다.
        /// </summary>
        public bool IsFinalBust(List<CardDataSO> field, StageDataSO stage)
        {
            long total = EvaluatePlayer(field, stage);

            return total > stage.bustValue;
        }

        /// <summary>
        /// Field에 연산자 카드가 배치되어 있는지 확인합니다.
        /// </summary>
        private bool HasOperatorPlaced(List<CardDataSO> field)
        {
            foreach (var card in field)
                if (card.cardType == CardType.Operator) return true;
            return false;
        }

        // ─── 승패 판정 ────────────────────────────────────────────

        /// <summary>
        /// 승패를 판정합니다.
        ///
        /// 기본 블랙잭 / 사칙연산 공통 룰
        /// ├── 플레이어 bustValue 초과 → 패배 (버스트)
        /// ├── 딜러 bustValue 초과     → 승리 (딜러 버스트)
        /// └── bustValue에 더 근접한 쪽 승리
        ///     동점 → 무승부 (패배 처리)
        ///
        /// bustValue가 21이면 기본 블랙잭과 동일하게 동작합니다.
        /// bustValue가 다른 값이면 그 값에 근접한 쪽이 승리합니다.
        /// 
        /// normalJudge = true  : bustValue 이하 최대값이 유리
        /// normalJudge = false : bustValue 이상 최소값이 유리
        /// </summary>
        public bool JudgeResult(long playerTotal, long dealerTotal, StageDataSO stage)
        {
            if (playerTotal > stage.bustValue) return false;
            if (dealerTotal > stage.bustValue) return true;

            long playerDiff = stage.bustValue - playerTotal;
            long dealerDiff = stage.bustValue - dealerTotal;

            if (playerDiff < dealerDiff) return true;
            if (playerDiff > dealerDiff) return false;
            return false;
        }

        /// <summary>결과 설명 문자열을 생성합니다.</summary>
        public string BuildResultDescription(long playerTotal, long dealerTotal, StageDataSO stage, bool win)
        {
            if (playerTotal > stage.bustValue)
                return $"버스트! {playerTotal:N0} > {stage.bustValue:N0}";
            if (dealerTotal > stage.bustValue)
                return $"딜러 버스트! 딜러: {dealerTotal:N0}";

            long playerDiff = stage.bustValue - playerTotal;
            long dealerDiff = stage.bustValue - dealerTotal;

            if (playerDiff == dealerDiff)
                return $"무승부. 둘 다 {stage.bustValue:N0}까지 {playerDiff:N0} 차이";

            return win
                ? $"승리! {playerTotal:N0} (차이:{playerDiff:N0}) " +
                  $"vs 딜러 {dealerTotal:N0} (차이:{dealerDiff:N0})"
                : $"패배. {playerTotal:N0} (차이:{playerDiff:N0}) " +
                  $"vs 딜러 {dealerTotal:N0} (차이:{dealerDiff:N0})";
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>딜러 전략을 런타임에 교체합니다.</summary>
        public void SetDealerStrategy(IDealerStrategy strategy)
            => _dealerStrategy = strategy;
    }
}
