using System.Collections.Generic;
using System.Text;

namespace HTH
{
    /// <summary>
    /// 좌→우 순차 연산 평가기.
    /// 수학적 우선순위를 무시하고 배치 순서대로만 계산합니다.
    /// Ace(1)는 useFlexibleAce = true일 때 1 또는 11로 자동 선택됩니다.
    /// 예: [10][×][5][+][3] = 53
    /// </summary>
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// CardDataSO 리스트를 좌→우로 순차 연산합니다.
        /// useFlexibleAce = true면 Ace를 버스트 없이 최대값이 되도록 자동 선택합니다.
        /// </summary>
        public static long Evaluate(
            List<CardDataSO> expression,
            bool useFlexibleAce = false,
            long bustThreshold = 21)
        {
            if (expression == null || expression.Count == 0) return 0;

            SplitExpression(expression, out List<int> numbers, out List<OperatorType> operators);

            return useFlexibleAce
                ? EvaluateWithFlexibleAce(numbers, operators, bustThreshold)
                : EvaluateSplit(numbers, operators);
        }

        /// <summary>
        /// 현재 필드를 수식 문자열로 반환합니다.
        /// 예: "A + 7 + 3"
        /// </summary>
        public static string ToExpressionString(List<CardDataSO> expression)
        {
            if (expression == null || expression.Count == 0) return "—";

            var sb = new StringBuilder();
            bool needsOperator = false;

            foreach (var card in expression)
            {
                if (card.cardType == CardType.Number)
                {
                    if (needsOperator) sb.Append(" + ");
                    sb.Append(string.IsNullOrEmpty(card.displayLabel)
                        ? card.numberValue.ToString()
                        : card.displayLabel);
                    needsOperator = true;
                }
                else
                {
                    if (needsOperator)
                    {
                        sb.Append(" ");
                        sb.Append(card.displayLabel);
                        sb.Append(" ");
                        needsOperator = false;
                    }
                }
            }
            return sb.ToString();
        }

        // ─── private ─────────────────────────────────────────────

        /// <summary>
        /// Ace를 유연하게 처리하는 연산.
        /// 모든 Ace를 1로 먼저 계산한 뒤,
        /// Ace를 11로 바꿔도 bustThreshold를 초과하지 않으면 11로 적용합니다.
        /// 여러 Ace가 있을 경우 각각 독립적으로 판단합니다.
        /// </summary>
        private static long EvaluateWithFlexibleAce(
            List<int> numbers,
            List<OperatorType> operators,
            long bustThreshold)
        {
            // 1단계: 모든 Ace를 1로 계산
            long baseResult = EvaluateSplit(numbers, operators);

            // 2단계: Ace를 11로 바꿀 수 있는지 확인
            // Ace(1)를 11로 바꾸면 +10 효과
            // 단, 덧셈 슬롯에 있는 Ace만 유연 적용 가능
            // (곱셈/나눗셈 슬롯의 Ace는 의미가 달라 고정값 사용)
            for (int i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] != 1) continue; // Ace가 아니면 스킵

                // i번째 숫자의 연산자 확인
                // i == 0 이면 첫 번째 숫자 (기본 덧셈)
                OperatorType op = i == 0
                    ? OperatorType.None
                    : (i - 1 < operators.Count ? operators[i - 1] : OperatorType.None);

                // 덧셈(None) 슬롯의 Ace만 유연 처리
                if (op != OperatorType.None) continue;

                // 11로 바꿨을 때 버스트하지 않으면 적용
                if (baseResult + 10 <= bustThreshold)
                    baseResult += 10;
            }

            return baseResult;
        }

        /// <summary>분리된 숫자/연산자를 좌→우로 계산합니다.</summary>
        private static long EvaluateSplit(
            List<int> numbers,
            List<OperatorType> operators)
        {
            if (numbers.Count == 0) return 0;

            long result = numbers[0];
            for (int i = 1; i < numbers.Count; i++)
            {
                OperatorType op = i - 1 < operators.Count
                    ? operators[i - 1]
                    : OperatorType.None;
                result = ApplyOperator(result, op, numbers[i]);
            }
            return result;
        }

        /// <summary>CardDataSO 리스트를 숫자/연산자 리스트로 분리합니다.</summary>
        private static void SplitExpression(
            List<CardDataSO> expression,
            out List<int> numbers,
            out List<OperatorType> operators)
        {
            numbers = new List<int>();
            operators = new List<OperatorType>();

            var pendingOp = OperatorType.None;

            foreach (var card in expression)
            {
                if (card.cardType == CardType.Number)
                {
                    numbers.Add(card.numberValue);
                    if (numbers.Count > 1) operators.Add(pendingOp);
                    pendingOp = OperatorType.None;
                }
                else
                {
                    pendingOp = card.operatorType;
                }
            }
        }

        /// <summary>left op right를 계산합니다. 0 나누기 방어 포함.</summary>
        private static long ApplyOperator(long left, OperatorType op, long right)
        {
            return op switch
            {
                OperatorType.Subtract => left - right,
                OperatorType.Multiply => left * right,
                OperatorType.Divide => right != 0 ? left / right : 0,
                _ => left + right
            };
        }
    }
}