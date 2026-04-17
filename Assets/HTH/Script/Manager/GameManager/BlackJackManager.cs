using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 블랙잭 룰 적용, 승패 판정, 버스트 감지를 담당합니다.
    /// 스테이지별로 표준 블랙잭(Stage 1)과
    /// 할당량 기반 룰(Stage 2+)을 분기합니다.
    /// </summary>
    public class BlackjackManager : MonoBehaviour
    {
        // ─── 의존성 ───────────────────────────────────────────────
        private IDealerStrategy _dealerStrategy;

        // ─── 초기화 ───────────────────────────────────────────────
        /// <summary>딜러 전략을 주입합니다. GameManager가 Awake에서 호출합니다.</summary>
        public void Initialize(IDealerStrategy dealerStrategy)
        {
            _dealerStrategy = dealerStrategy;
        }

        // ─── 연산 ─────────────────────────────────────────────────

        /// <summary>
        /// 스테이지 설정으로 플레이어 필드를 연산합니다.
        /// FlexibleAce / bustThreshold를 StageDataSO에서 읽습니다.
        /// </summary>
        public long EvaluatePlayer(List<CardDataSO> field, StageDataSO stage)
            => ExpressionEvaluator.Evaluate(field, stage.useFlexibleAce, stage.bustValue);

        /// <summary>딜러 필드를 연산합니다.</summary>
        public long EvaluateDealer(List<CardDataSO> field, StageDataSO stage)
            => ExpressionEvaluator.Evaluate(field, stage.useFlexibleAce, stage.bustValue);

        // ─── 버스트 감지 ──────────────────────────────────────────

        /// <summary>
        /// HIT 중 플레이어 버스트 여부를 확인합니다.
        ///
        /// currentValueSet = true  : 높아야 하는 스테이지
        ///   → HIT 중 버스트 없음 (return false)
        ///   → Stay 시 IsFinalBust로 판정
        ///
        /// currentValueSet = false : 낮아야 하는 스테이지 (블랙잭 기본 룰)
        ///   → bustValue 초과 시 버스트
        /// </summary>
        public bool IsPlayerBust(List<CardDataSO> field, StageDataSO stage)
        {
            long total = EvaluatePlayer(field, stage);

            if (stage.currentValueSet)
                return false;           // 높아야 하는 스테이지 — HIT 중 버스트 없음
            else
                return total > stage.bustValue; // 낮아야 하는 스테이지 — 초과면 버스트
        }

        /// <summary>
        /// Stay 후 최종 버스트 여부를 확인합니다.
        /// currentValueSet = true  : bustValue 미만이면 실패 (목표값에 못 미침)
        /// currentValueSet = false : bustValue 초과면 실패 (목표값을 넘김)
        /// IsPlayerBust와 달리 Stay 확정 시점에만 호출합니다.
        /// </summary>
        public bool IsFinalBust(List<CardDataSO> field, StageDataSO stage)
        {
            long total = EvaluatePlayer(field, stage);

            return stage.currentValueSet
                ? total < stage.bustValue   // 높아야 함 → 미만이면 실패
                : total > stage.bustValue;  // 낮아야 함 → 초과면 실패
        }

        // ─── 승패 판정 ────────────────────────────────────────────

        /// <summary>
        /// 스테이지 룰에 따라 승패를 판정합니다.
        ///
        /// Stage 1 (표준 블랙잭)
        ///   - 21 초과 → 플레이어 버스트 패배
        ///   - 딜러 21 초과 → 딜러 버스트 승리
        ///   - 플레이어 >= 딜러 → 승리
        ///
        /// Stage 2+ currentValueSet = true (높아야 하는 스테이지)
        ///   - bustValue 미만 → 패배
        ///   - 딜러 bustValue 미만 → 딜러 실패 → 승리
        ///   - 플레이어 >= 딜러 → 승리
        ///
        /// Stage 2+ currentValueSet = false (낮아야 하는 스테이지)
        ///   - bustValue 초과 → 패배
        ///   - 딜러 bustValue 초과 → 딜러 버스트 → 승리
        ///   - 플레이어 <= 딜러 → 승리 (낮을수록 유리)
        /// </summary>
        public bool JudgeResult(long playerTotal, long dealerTotal, StageDataSO stage)
        {
            if (stage.currentValueSet)
            {
                // 높아야 하는 스테이지
                bool playerFail = playerTotal < stage.bustValue;
                bool dealerFail = dealerTotal < stage.bustValue;
                if (playerFail) return false;
                if (dealerFail) return true;
                return playerTotal >= dealerTotal; // 높을수록 유리
            }
            else
            {
                // 낮아야 하는 스테이지
                bool playerBust = playerTotal > stage.bustValue;
                bool dealerBust = dealerTotal > stage.bustValue;
                if (playerBust) return false;
                if (dealerBust) return true;
                return playerTotal <= dealerTotal; // 낮을수록 유리
            }
        }
        /// <summary>결과 설명 문자열을 생성합니다.</summary>
        public string BuildResultDescription(long playerTotal, long dealerTotal, StageDataSO stage, bool win)
        {

            if (stage.currentValueSet)
            {
                // 높아야 하는 스테이지
                if (playerTotal < stage.bustValue)
                    return $"미달! {playerTotal:N0} < {stage.bustValue:N0}";
                if (dealerTotal < stage.bustValue)
                    return $"딜러 미달! — 플레이어 승리";
                return win
                    ? $"승리! {playerTotal:N0} ≥ {dealerTotal:N0}"
                    : $"패배. {playerTotal:N0} < {dealerTotal:N0}";
            }
            else
            {
                // 낮아야 하는 스테이지
                if (playerTotal > stage.bustValue)
                    return $"버스트! {playerTotal:N0} > {stage.bustValue:N0}";
                if (dealerTotal > stage.bustValue)
                    return $"딜러 버스트! — 플레이어 승리";
                return win
                    ? $"성공! {playerTotal:N0} ≤ {dealerTotal:N0}"
                    : $"실패. {playerTotal:N0} > {dealerTotal:N0}";
            }
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>딜러 전략을 런타임에 교체합니다.</summary>
        public void SetDealerStrategy(IDealerStrategy strategy)
            => _dealerStrategy = strategy;
    }
}