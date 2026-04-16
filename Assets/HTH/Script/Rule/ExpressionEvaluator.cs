using System.Collections.Generic;
using System.Text;

namespace HTH
{
    /// <summary>
    /// 좌→우 순차 연산 평가기.
    /// 수학적 우선순위(곱셈/나눗셈 선행)를 의도적으로 무시합니다.
    /// 예: [10][×][5][+][3] = ((10 × 5) + 3) = 53
    /// </summary>
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// CardDataSO 리스트를 좌→우로 순차 연산합니다.
        /// 내부에서 숫자/연산자를 분리해 처리하며, 연산자 없이
        /// 숫자만 나열되면 모두 덧셈(+)으로 처리합니다.
        /// </summary>
        /// <param name="expression">필드에 배치된 CardDataSO 순서 리스트</param>
        /// <returns>좌→우 순차 연산 결과값</returns>
        public static long Evaluate(List<CardDataSO> expression)
        {
            if (expression == null || expression.Count == 0) return 0;

            SplitExpression(expression, out List<int> numbers, out List<OperatorType> operators);
            return EvaluateSplit(numbers, operators);
        }

        /// <summary>
        /// 현재 필드 상태를 사람이 읽을 수 있는 수식 문자열로 반환합니다.
        /// 예: "10 × 5 + 3"
        /// displayLabel이 있으면 사용하고, 없으면 numberValue를 사용합니다.
        /// </summary>
        /// <param name="expression">필드에 배치된 CardDataSO 순서 리스트</param>
        /// <returns>수식 문자열 (빈 필드면 "—" 반환)</returns>
        public static string ToExpressionString(List<CardDataSO> expression)
        {
            if (expression == null || expression.Count == 0) return "—";

            var sb = new StringBuilder();
            bool needsOperator = false; // 직전 토큰이 숫자였는지 추적

            foreach (var card in expression)
            {
                if (card.cardType == CardType.Number)
                {
                    // 두 번째 숫자부터 앞에 연산자 기호 삽입
                    // (연산자 카드가 없으면 기본 + 로 표시)
                    if (needsOperator)
                    {
                        sb.Append(" + ");
                    }
                    sb.Append(string.IsNullOrEmpty(card.displayLabel)
                        ? card.numberValue.ToString()
                        : card.displayLabel);
                    needsOperator = true;
                }
                else
                {
                    // 연산자 카드는 이전 + 를 덮어쓰는 방식으로 삽입
                    // pending 연산자를 수식 문자열에 반영
                    if (needsOperator)
                    {
                        sb.Append(" ");
                        sb.Append(card.displayLabel);
                        sb.Append(" ");
                        needsOperator = false; // 연산자 카드가 처리됨
                    }
                }
            }

            return sb.ToString();
        }

        // ─── private ─────────────────────────────────────────────

        /// <summary>
        /// CardDataSO 리스트를 숫자 목록과 연산자 목록으로 분리합니다.
        /// Evaluate와 ToExpressionString이 공통으로 사용합니다.
        /// </summary>
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
                    if (numbers.Count > 1)
                        operators.Add(pendingOp);
                    pendingOp = OperatorType.None;
                }
                else
                {
                    pendingOp = card.operatorType;
                }
            }
        }

        /// <summary>
        /// 분리된 숫자/연산자 목록을 좌→우로 계산합니다.
        /// operators[i]는 numbers[i]와 numbers[i+1] 사이의 연산자입니다.
        /// </summary>
        private static long EvaluateSplit(List<int> numbers, List<OperatorType> operators)
        {
            if (numbers.Count == 0) return 0;

            long result = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                OperatorType op = (i - 1 < operators.Count)
                    ? operators[i - 1]
                    : OperatorType.None;

                result = ApplyOperator(result, op, numbers[i]);
            }

            return result;
        }

        /// <summary>
        /// left op right 를 계산합니다.
        /// 0 나누기 방어: right가 0이면 0을 반환합니다.
        /// </summary>
        private static long ApplyOperator(long left, OperatorType op, long right)
        {
            return op switch
            {
                OperatorType.Subtract => left - right,
                OperatorType.Multiply => left * right,
                OperatorType.Divide => right != 0 ? left / right : 0,
                _ => left + right   // None = 덧셈
            };
        }
    }
}