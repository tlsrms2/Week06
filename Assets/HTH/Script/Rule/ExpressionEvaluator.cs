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
                : EvaluateInfix(numbers, operators);

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
        /// 비덧셈 연산자를 오른쪽부터 묶어서 처리한 뒤
        /// 나머지를 덧셈으로 합산합니다.
        ///
        /// 처리 순서
        /// 1. 오른쪽부터 순회하며 비덧셈 연산자 연속 구간을 찾음
        /// 2. 연속 구간 내에서 오른쪽부터 묶어서 계산
        /// 3. 단일 값으로 축약 후 덧셈으로 합산
        /// </summary>
        private static long EvaluateInfix(List<long> numbers, List<OperatorType> operators)
        {
            var nums = new List<long>(numbers);
            var ops = new List<OperatorType>(operators);

            // 오른쪽부터 순회하며 비덧셈 연산자 처리
            int i = ops.Count - 1;
            while (i >= 0)
            {
                if (IsNonAdditive(ops[i]))
                {
                    // 연속된 비덧셈 구간의 시작을 찾음
                    int start = i;
                    while (start > 0 && IsNonAdditive(ops[start - 1]))
                        start--;

                    // 오른쪽부터 묶어서 계산
                    // 예: A − B ÷ C → A − (B ÷ C)
                    //     start=0, i=1 이면
                    //     먼저 B ÷ C 계산 후 A − 결과
                    int j = i;
                    while (j >= start)
                    {
                        long left = nums[j];
                        long right = nums[j + 1];
                        long val = ApplyOperator(left, ops[j], right);

                        UnityEngine.Debug.Log(
                            $"[Evaluator] {left} {ops[j]} {right} = {val}");

                        nums.RemoveAt(j + 1);
                        nums[j] = val;
                        ops.RemoveAt(j);
                        j--;
                    }

                    i = start - 1;
                }
                else
                {
                    i--;
                }
            }

            // 남은 숫자 덧셈 합산
            long result = 0;
            foreach (var n in nums) result += n;

            UnityEngine.Debug.Log(
                $"[Evaluator] 최종합산 [{string.Join("+", nums)}] = {result}");

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
