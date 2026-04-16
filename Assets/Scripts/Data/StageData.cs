using System.Collections.Generic;

/// <summary>
/// 스테이지 정보. 각 스테이지의 할당량과 고정 덱(카드 목록 + 드로우 순서)을 정의합니다.
/// 기획안: 운(RNG)이 배제된, 고정된 패로 구성된 퍼즐.
/// </summary>
[System.Serializable]
public class StageInfo
{
    public int stageNumber;
    public long quota;           // 목표 수치 (할당량)
    public int minimumBet;       // 최소 배팅량
    public List<CardData> deck;  // 고정 덱 (드로우 순서)

    public StageInfo(int stage, long quota, int minBet, List<CardData> deck)
    {
        this.stageNumber = stage;
        this.quota = quota;
        this.minimumBet = minBet;
        this.deck = deck;
    }
}

/// <summary>
/// 전체 스테이지 데이터 관리. 모든 스테이지의 고정 덱과 할당량을 정의합니다.
/// A(1)~K(13) 숫자 카드와 −, ×, ÷ 연산 카드로 구성.
/// </summary>
public static class StageDatabase
{
    /// <summary>전체 스테이지 목록 반환</summary>
    public static List<StageInfo> GetAllStages()
    {
        var stages = new List<StageInfo>();

        // ─── Stage 1: Quota 21 ───
        // 덧셈 기초. 7 + 3 + J(11) = 21. 5, K(13) 은 함정.
        stages.Add(new StageInfo(1, 21, 5, new List<CardData>
        {
            CardData.Number(CardFace.Seven),
            CardData.Number(CardFace.Three),
            CardData.Number(CardFace.Jack),    // 11
            CardData.Number(CardFace.Five),
            CardData.Number(CardFace.King),    // 13 (함정)
        }));

        // ─── Stage 2: Quota 42 ───
        // 곱셈 도입. 6 × 7 = 42.
        stages.Add(new StageInfo(2, 42, 5, new List<CardData>
        {
            CardData.Number(CardFace.Six),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Seven),
            CardData.Number(CardFace.Nine),     // 함정
            CardData.Number(CardFace.Queen),    // 12 (함정)
            CardData.Number(CardFace.Three),
        }));

        // ─── Stage 3: Quota 100 ───
        // Q(12) × 8 + 4 = 100.
        stages.Add(new StageInfo(3, 100, 8, new List<CardData>
        {
            CardData.Number(CardFace.Queen),   // 12
            CardData.Number(CardFace.Eight),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Four),
            CardData.Number(CardFace.Jack),    // 11 (함정)
            CardData.Number(CardFace.Seven),
        }));

        // ─── Stage 4: Quota 520 ───
        // K(13) × 8 × 5 = 520. 두 번째 곱셈 활용.
        stages.Add(new StageInfo(4, 520, 10, new List<CardData>
        {
            CardData.Number(CardFace.King),    // 13
            CardData.Number(CardFace.Eight),
            CardData.Operator(OperatorType.Multiply),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Five),
            CardData.Number(CardFace.Seven),   // 함정
            CardData.Number(CardFace.Four),
        }));

        // ─── Stage 5: Quota 5148 ───
        // K(13) × Q(12) × J(11) × 3 = 5148. 체인 곱셈 + Stay 타이밍.
        stages.Add(new StageInfo(5, 5148, 12, new List<CardData>
        {
            CardData.Number(CardFace.King),    // 13
            CardData.Number(CardFace.Queen),   // 12
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Jack),    // 11
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Three),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Five),    // 함정
            CardData.Operator(OperatorType.Subtract),
            CardData.Number(CardFace.Two),
        }));

        // ─── Stage 6: Quota 25740 ───
        // K(13) × Q(12) × J(11) × 3 × 5 = 25740.
        stages.Add(new StageInfo(6, 25740, 15, new List<CardData>
        {
            CardData.Number(CardFace.King),
            CardData.Number(CardFace.Queen),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Jack),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Three),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Five),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Nine),    // 함정
            CardData.Number(CardFace.Seven),
            CardData.Operator(OperatorType.Subtract),
        }));

        // ─── Stage 7: Quota 360360 ───
        // K(13) × Q(12) × J(11) × 10 × 9 − 8 × 7 = ...
        // 13×12×11×10 = 17160, ×(9−8) 활용? 좌→우이므로:
        // 13 × 12 × 11 × 10 × 3 − 9 + 9  → 복잡. 간단히:
        // K(13) × Q(12) × 10 × 3 × 7 + J(11) × 9 = ?
        // 실제: (((13*12)*10)*3)*7 = 32760... 
        // 좀 더 현실적으로: 13×12×11×10×3 = 51480. Quota: 51480
        stages.Add(new StageInfo(7, 51480, 18, new List<CardData>
        {
            CardData.Number(CardFace.King),
            CardData.Number(CardFace.Queen),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Jack),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Ten),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Three),
            CardData.Operator(OperatorType.Multiply),
            CardData.Operator(OperatorType.Subtract),
            CardData.Number(CardFace.Eight),
            CardData.Number(CardFace.Nine),
            CardData.Number(CardFace.Four),
        }));

        // ─── Stage 8: Quota 360360 ───
        // K(13) × Q(12) × J(11) × 10 × 9 × Ace(1) ÷ 4 + ... 
        // 13×12=156, ×11=1716, ×10=17160, ×9=154440, ×3=463320...
        // 간단히: 13×12×11×10×(9−6) = 17160×3 = 51480... 
        // 더 높은 숫자: 13×12×11×10×9 = 154440, ÷ 3 = 51480
        // 나눗셈 도입: 154440 → Quota: 154440
        stages.Add(new StageInfo(8, 154440, 20, new List<CardData>
        {
            CardData.Number(CardFace.King),
            CardData.Number(CardFace.Queen),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Jack),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Ten),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Nine),
            CardData.Operator(OperatorType.Multiply),
            CardData.Operator(OperatorType.Divide),
            CardData.Number(CardFace.Ace),
            CardData.Number(CardFace.Five),
            CardData.Number(CardFace.Two),
            CardData.Number(CardFace.Six),
        }));

        // ─── Stage 9: Quota 1235520 ───
        // 13×12×11×10×9×8 = 1235520
        stages.Add(new StageInfo(9, 1235520, 22, new List<CardData>
        {
            CardData.Number(CardFace.King),
            CardData.Number(CardFace.Queen),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Jack),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Ten),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Nine),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Eight),
            CardData.Operator(OperatorType.Multiply),
            CardData.Operator(OperatorType.Subtract),
            CardData.Operator(OperatorType.Divide),
            CardData.Number(CardFace.Seven),
            CardData.Number(CardFace.Four),
            CardData.Number(CardFace.Three),
        }));

        // ─── Stage 10: Quota 1037836800 ───
        // 13×12×11×10×9×8×7×6 = 1037836800 (≈ 10억)
        stages.Add(new StageInfo(10, 1037836800, 25, new List<CardData>
        {
            CardData.Number(CardFace.King),
            CardData.Number(CardFace.Queen),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Jack),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Ten),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Nine),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Eight),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Seven),
            CardData.Operator(OperatorType.Multiply),
            CardData.Number(CardFace.Six),
            CardData.Operator(OperatorType.Multiply),
            CardData.Operator(OperatorType.Subtract),
            CardData.Operator(OperatorType.Divide),
            CardData.Number(CardFace.Five),
            CardData.Number(CardFace.Four),
            CardData.Number(CardFace.Ace),
        }));

        return stages;
    }
}
