/// <summary>
/// 블라인드잭 카드 데이터 정의.
/// 숫자 카드(A~K)와 연산 카드(−, ×, ÷)의 타입, 값, 표시 문자열을 관리합니다.
/// </summary>

/// <summary>카드 종류: 숫자 또는 연산자</summary>
public enum CardType { Number, Operator }

/// <summary>
/// 연산자 종류.
/// None = 기본 덧셈(+), 연산 카드가 배치되지 않은 슬롯의 기본값.
/// </summary>
public enum OperatorType { None, Subtract, Multiply, Divide }

/// <summary>
/// 카드 숫자 면 (A~K). int 값은 해당 카드의 수치.
/// A=1, 2~10=액면가, J=11, Q=12, K=13
/// </summary>
public enum CardFace
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13
}

/// <summary>
/// 카드 한 장의 데이터. 숫자 카드이면 face 값, 연산 카드이면 operatorType 값을 사용합니다.
/// </summary>
[System.Serializable]
public class CardData
{
    public CardType cardType;
    public CardFace face;              // Number 카드 전용
    public OperatorType operatorType;  // Operator 카드 전용

    /// <summary>숫자 카드의 정수값 (A=1 ~ K=13)</summary>
    public int NumberValue => (int)face;

    /// <summary>UI에 표시할 문자열</summary>
    public string DisplayText
    {
        get
        {
            if (cardType == CardType.Number)
            {
                return face switch
                {
                    CardFace.Ace => "A",
                    CardFace.Jack => "J",
                    CardFace.Queen => "Q",
                    CardFace.King => "K",
                    _ => ((int)face).ToString()
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorType.Subtract => "−",
                    OperatorType.Multiply => "×",
                    OperatorType.Divide => "÷",
                    _ => "+"
                };
            }
        }
    }

    /// <summary>연산자의 UI 표시 문자열 (OperatorType → 기호)</summary>
    public static string OperatorSymbol(OperatorType op)
    {
        return op switch
        {
            OperatorType.Subtract => "−",
            OperatorType.Multiply => "×",
            OperatorType.Divide => "÷",
            _ => "+"
        };
    }

    // ── 팩토리 메서드 ──

    public static CardData Number(CardFace face)
    {
        return new CardData { cardType = CardType.Number, face = face };
    }

    public static CardData Operator(OperatorType op)
    {
        return new CardData { cardType = CardType.Operator, operatorType = op };
    }
}
