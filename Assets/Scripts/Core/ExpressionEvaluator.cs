using System.Collections.Generic;

/// <summary>
/// 좌→우 순차 연산 평가기.
/// 기획안: "수학적 우선순위(곱셈/나눗셈 우선)를 무시하고, 철저히 배치된 순서(좌→우)대로만 계산."
/// 예: [10] [×] [5] [+] [3] = (10 × 5) + 3 = 53
/// </summary>
public static class ExpressionEvaluator
{
    /// <summary>
    /// 필드의 숫자 목록과 연산자 목록을 좌→우로 계산합니다.
    /// operators[i]는 numbers[i]와 numbers[i+1] 사이의 연산자입니다.
    /// OperatorType.None은 기본 덧셈(+)으로 처리됩니다.
    /// </summary>
    /// <param name="numbers">필드에 배치된 숫자 카드 값 목록</param>
    /// <param name="operators">숫자 사이의 연산자 목록 (길이 = numbers.Count - 1)</param>
    /// <returns>최종 연산 결과</returns>
    public static long Evaluate(List<int> numbers, List<OperatorType> operators)
    {
        if (numbers == null || numbers.Count == 0)
            return 0;

        long result = numbers[0];

        for (int i = 1; i < numbers.Count; i++)
        {
            OperatorType op = (i - 1 < operators.Count) ? operators[i - 1] : OperatorType.None;

            switch (op)
            {
                case OperatorType.Subtract:
                    result -= numbers[i];
                    break;
                case OperatorType.Multiply:
                    result *= numbers[i];
                    break;
                case OperatorType.Divide:
                    if (numbers[i] != 0)
                        result /= numbers[i];
                    break;
                default: // None = 기본 덧셈
                    result += numbers[i];
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// 현재 필드 상태를 사람이 읽을 수 있는 수식 문자열로 변환합니다.
    /// 예: "10 × 5 + 3"
    /// </summary>
    public static string ToExpressionString(List<int> numbers, List<OperatorType> operators,
        List<string> displayTexts = null)
    {
        if (numbers == null || numbers.Count == 0)
            return "—";

        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < numbers.Count; i++)
        {
            if (i > 0)
            {
                OperatorType op = (i - 1 < operators.Count) ? operators[i - 1] : OperatorType.None;
                sb.Append(" ");
                sb.Append(CardData.OperatorSymbol(op));
                sb.Append(" ");
            }

            // 카드 표시 텍스트 사용 (있으면), 없으면 숫자값
            if (displayTexts != null && i < displayTexts.Count)
                sb.Append(displayTexts[i]);
            else
                sb.Append(numbers[i]);
        }

        return sb.ToString();
    }
}
