using System.Collections.Generic;

namespace HTH
{
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// 카드 리스트를 연산합니다.
        ///
        /// 규칙
        /// 1. 연산자는 바로 앞 숫자와 뒤 숫자 사이에만 적용
        /// 2. 연산자가 없는 숫자 사이는 기본 덧셈
        /// 3. 비덧셈 연산자가 연속될 경우 오른쪽부터 묶음 (선입 조건)
        ///
        /// 예: 2 + A + J × 3 + 9
        ///   = 2 + 1 + (10 × 3) + 9 = 42
        ///
        /// 예: 2 + A − J ÷ 3 + 9
        ///   = 2 + (1 − (10 ÷ 3)) + 9
        ///   = 2 + (1 − 3) + 9
        ///   = 2 + (-2) + 9 = 9
        /// </summary>
        public static long Evaluate(List<CardDataSO> expression, bool useFlexibleAce = false, long bustThreshold = 21, bool allowNegative = false)
        {
            if (expression == null || expression.Count == 0) return 0;

            SplitExpression(expression, out List<long> numbers, out List<OperatorType> operators, out List<bool> flexibleAces);

            long result = useFlexibleAce
                ? EvaluateWithFlexibleAce(numbers, operators, flexibleAces, bustThreshold)
                : EvaluateInfix(numbers, operators, bustThreshold);

            // 음수 제한
            if (!allowNegative && result < 0)
                result = 0;

            return result;
        }

        /// <summary>수식 문자열을 반환합니다.</summary>
        public static string ToExpressionString(List<CardDataSO> expression)
        {
            if (expression == null || expression.Count == 0) return "—";

            var sb = new System.Text.StringBuilder();
            foreach (var card in expression)
            {
                if (card.cardType == CardType.Number)
                    sb.Append(string.IsNullOrEmpty(card.displayLabel) ? card.BlackjackValue.ToString()
                        : card.displayLabel);
                else
                    sb.Append($" {card.displayLabel} ");
            }
            return sb.ToString().Trim();
        }

        // ─── private ─────────────────────────────────────────────

        private static void SplitExpression(
            List<CardDataSO> expression,
            out List<long> numbers,
            out List<OperatorType> operators,
            out List<bool> flexibleAces)
        {
            numbers = new List<long>();
            operators = new List<OperatorType>();
            flexibleAces = new List<bool>();

            OperatorType pendingOp = OperatorType.None;

            foreach (var card in expression)
            {
                if (card.cardType == CardType.Number)
                {
                    if (numbers.Count > 0)
                        operators.Add(pendingOp);

                    numbers.Add(card.BlackjackValue);
                    flexibleAces.Add(card.IsFlexibleAceCandidate);
                    pendingOp = OperatorType.None;
                }
                else
                {
                    pendingOp = card.operatorType;
                }
            }
        }

        /// <summary>
        /// 표준 사칙연산 우선순위와 좌에서 우 결합 법칙을 따릅니다.
        /// 1. 곱셈(×)과 나눗셈(÷)을 왼쪽부터 먼저 계산합니다.
        /// 2. 덧셈(빈칸 포함)과 뺄셈(−)을 왼쪽부터 차례대로 계산합니다.
        /// </summary>
        private static long EvaluateInfix(List<long> numbers, List<OperatorType> operators, long bustThreshold = long.MaxValue)
        {
            var nums = new List<long>(numbers);
            var ops = new List<OperatorType>(operators);

            // 1단계: 곱셈, 나눗셈 우선 처리 (왼쪽부터)
            int i = 0;
            while (i < ops.Count)
            {
                if (ops[i] == OperatorType.Multiply || ops[i] == OperatorType.Divide)
                {
                    long left = nums[i];
                    long right = nums[i + 1];
                    long val = ApplyOperator(left, ops[i], right);

                    UnityEngine.Debug.Log($"[Evaluator] {left} {ops[i]} {right} = {val}");

                    nums.RemoveAt(i + 1);
                    nums[i] = val;
                    ops.RemoveAt(i);

                    // 곱/나 결과가 이미 bust이면 즉시 반환 (음수 예외: Divide 결과는 작아질 수 있음)
                    if (val > bustThreshold)
                    {
                        UnityEngine.Debug.Log($"[Evaluator] 곱셈/나눗셈 중간값 {val} > bust({bustThreshold}) → 즉시 버스트 반환");
                        return val;
                    }
                }
                else
                {
                    i++;
                }
            }

            // 2단계: 덧셈(None), 뺄셈 처리 (왼쪽부터)
            i = 0;
            while (i < ops.Count)
            {
                long left = nums[i];
                long right = nums[i + 1];
                long val = ApplyOperator(left, ops[i], right);

                UnityEngine.Debug.Log($"[Evaluator] {left} {ops[i]} {right} = {val}");

                nums.RemoveAt(i + 1);
                nums[i] = val;
                ops.RemoveAt(i);
            }

            long result = nums.Count > 0 ? nums[0] : 0;
            UnityEngine.Debug.Log($"[Evaluator] 최종합산 = {result}");

            return result;
        }

        /// <summary>FlexibleAce 적용 버전.</summary>
        private static long EvaluateWithFlexibleAce(
            List<long> numbers,
            List<OperatorType> operators,
            List<bool> flexibleAces,
            long bustThreshold)
        {
            long baseResult = EvaluateInfix(numbers, operators);

            for (int i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] != 1) continue;
                if (i >= flexibleAces.Count || !flexibleAces[i]) continue;

                OperatorType op = i == 0
                    ? OperatorType.None
                    : (i - 1 < operators.Count
                        ? operators[i - 1]
                        : OperatorType.None);

                if (op != OperatorType.None) continue;

                if (baseResult + 10 <= bustThreshold)
                    baseResult += 10;
            }

            return baseResult;
        }

        /// <summary>비덧셈 연산자 여부를 확인합니다.</summary>
        private static bool IsNonAdditive(OperatorType op)
            => op == OperatorType.Subtract
            || op == OperatorType.Multiply
            || op == OperatorType.Divide;

        /// <summary>left op right를 계산합니다.</summary>
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
