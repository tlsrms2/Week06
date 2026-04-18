namespace HTH
{
    /// <summary>
    /// 카드 관련 열거형 정의.
    /// CardDataSO / SuitDataSO / DeckSO 등 카드 관련 모든 클래스에서 참조합니다.
    /// </summary>

    /// <summary>카드 종류 — 숫자 카드 / 연산자 카드</summary>
    public enum CardType { Number, Operator }

    /// <summary>연산자 종류</summary>
    public enum OperatorType { None, Subtract, Multiply, Divide }

    /// <summary>카드 무늬</summary>
    public enum CardSuit { Spade, Club, Heart, Diamond }
}