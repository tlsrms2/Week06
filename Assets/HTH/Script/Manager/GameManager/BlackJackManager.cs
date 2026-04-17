using System.Collections.Generic;
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
            => ExpressionEvaluator.Evaluate(
                field, stage.useFlexibleAce, stage.bustThreshold);

        /// <summary>딜러 필드를 연산합니다.</summary>
        public long EvaluateDealer(List<CardDataSO> field, StageDataSO stage)
            => ExpressionEvaluator.Evaluate(
                field, stage.useFlexibleAce, stage.bustThreshold);

        // ─── 버스트 감지 ──────────────────────────────────────────

        /// <summary>플레이어가 버스트 상태인지 확인합니다.</summary>
        public bool IsPlayerBust(List<CardDataSO> field, StageDataSO stage)
            => EvaluatePlayer(field, stage) > stage.bustThreshold;

        // ─── 승패 판정 ────────────────────────────────────────────

        /// <summary>
        /// 스테이지 룰에 따라 승패를 판정합니다.
        /// Stage 1 : 표준 블랙잭 (21 기준, 플레이어 vs 딜러)
        /// Stage 2+ : 할당량 기준 (quota 초과 여부)
        /// </summary>
        public bool JudgeResult(
            long playerTotal,
            long dealerTotal,
            StageDataSO stage)
        {
            bool playerBust = playerTotal > stage.quota;
            bool dealerBust = dealerTotal > _dealerStrategy.BustThreshold;
            if (playerBust) return false;
            if (dealerBust) return true;
            return playerTotal >= dealerTotal;
        }

        /// <summary>결과 설명 문자열을 생성합니다.</summary>
        public string BuildResultDescription(
            long playerTotal,
            long dealerTotal,
            StageDataSO stage,
            bool win)
        {
            if (stage.stageIndex == 1)
            {
                if (playerTotal > 21)
                    return $"버스트! {playerTotal:N0} > 21";
                if (dealerTotal > 21)
                    return $"딜러 버스트! 딜러: {dealerTotal:N0}";
                return win
                    ? $"승리! {playerTotal:N0} ≥ {dealerTotal:N0}"
                    : $"패배. {playerTotal:N0} < {dealerTotal:N0}";
            }
            else
            {
                if (playerTotal > stage.quota)
                    return $"버스트! {playerTotal:N0} > {stage.quota:N0}";
                return win
                    ? $"성공! {playerTotal:N0} ≤ {stage.quota:N0}"
                    : $"실패. {playerTotal:N0} > {stage.quota:N0}";
            }
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>딜러 전략을 런타임에 교체합니다.</summary>
        public void SetDealerStrategy(IDealerStrategy strategy)
            => _dealerStrategy = strategy;
    }
}