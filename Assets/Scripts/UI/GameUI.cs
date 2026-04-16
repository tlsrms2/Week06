using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 블라인드잭 전체 UI를 프로그래밍 방식으로 생성하고 관리합니다.
/// Canvas, 패널, 버튼, 텍스트 등 모든 UI 요소를 런타임에 구축합니다.
/// 사용법: 빈 씬에서 GameManager 하나만 부착하면 자동 생성됩니다.
/// </summary>
public class GameUI : MonoBehaviour
{
    // ── 색상 팔레트 ──
    public static readonly Color COL_BG           = HexColor("#0D1B14");
    public static readonly Color COL_PANEL        = HexColor("#162B20");
    public static readonly Color COL_PANEL_LIGHT  = HexColor("#1E3A2B");
    public static readonly Color COL_CARD_NUM     = HexColor("#F5F0E1");
    public static readonly Color COL_CARD_OP      = HexColor("#D4A843");
    public static readonly Color COL_CARD_TEXT    = HexColor("#2D2D2D");
    public static readonly Color COL_SLOT_DEFAULT = HexColor("#2A4A3A");
    public static readonly Color COL_SLOT_TEXT    = HexColor("#5A8A6A");
    public static readonly Color COL_HIGHLIGHT    = HexColor("#4FC3F7");
    public static readonly Color COL_GOLD         = HexColor("#FFD700");
    public static readonly Color COL_GREEN        = HexColor("#4CAF50");
    public static readonly Color COL_RED          = HexColor("#F44336");
    public static readonly Color COL_BTN          = HexColor("#2E7D32");
    public static readonly Color COL_BTN_HOVER    = HexColor("#388E3C");
    public static readonly Color COL_TEXT         = HexColor("#E8E0D0");
    public static readonly Color COL_TEXT_DIM     = HexColor("#8A8A7A");
    public static readonly Color COL_VIGNETTE     = new Color(0, 0, 0, 0.85f);

    // ── UI 패널 참조 ──
    [HideInInspector] public GameObject titlePanel;
    [HideInInspector] public GameObject betPanel;
    [HideInInspector] public GameObject gamePanel;
    [HideInInspector] public GameObject resultPanel;
    [HideInInspector] public GameObject gameOverPanel;

    // ── Title Panel ──
    [HideInInspector] public Button startButton;

    // ── Bet Panel ──
    [HideInInspector] public Text betStageText;
    [HideInInspector] public Text betQuotaText;
    [HideInInspector] public Text betVisionText;
    [HideInInspector] public Text betAmountText;
    [HideInInspector] public Button betIncButton;
    [HideInInspector] public Button betDecButton;
    [HideInInspector] public Button betConfirmButton;

    // ── Game Panel ──
    [HideInInspector] public Text stageText;
    [HideInInspector] public Text quotaText;
    [HideInInspector] public Text visionValueText;
    [HideInInspector] public Image visionBarFill;
    [HideInInspector] public RectTransform fieldContainer;
    [HideInInspector] public Text currentValueText;
    [HideInInspector] public Text expressionText;
    [HideInInspector] public RectTransform handContainer;
    [HideInInspector] public Text handLabel;
    [HideInInspector] public RectTransform deckContainer;
    [HideInInspector] public Text deckLabel;
    [HideInInspector] public Button hitButton;
    [HideInInspector] public Button stayButton;

    // ── Result Panel ──
    [HideInInspector] public Text resultTitleText;
    [HideInInspector] public Text resultExprText;
    [HideInInspector] public Text resultCompareText;
    [HideInInspector] public Button resultButton;
    [HideInInspector] public Text resultButtonLabel;

    // ── Game Over Panel ──
    [HideInInspector] public Text gameOverTitleText;
    [HideInInspector] public Text gameOverDetailText;
    [HideInInspector] public Button restartButton;

    // ── Vignette Overlay ──
    [HideInInspector] public CanvasGroup vignetteGroup;
    [HideInInspector] public Image vignetteOverlay;

    // ── 동적 UI 요소 추적 ──
    private List<GameObject> fieldElements = new List<GameObject>();
    private List<Image> operatorSlotImages = new List<Image>();
    private List<Text> operatorSlotTexts = new List<Text>();
    private List<Button> operatorSlotButtons = new List<Button>();

    private List<GameObject> handElements = new List<GameObject>();
    private List<Image> handCardImages = new List<Image>();

    private List<GameObject> deckElements = new List<GameObject>();

    // ── 콜백 ──
    public event Action<int> OnOperatorSlotClicked;  // 필드 연산자 슬롯 클릭
    public event Action<int> OnHandCardClicked;      // 손패 카드 클릭

    // ═══════════════════════════════════════════════════════════
    //  UI 빌드
    // ═══════════════════════════════════════════════════════════

    /// <summary>전체 UI를 프로그래밍 방식으로 생성합니다.</summary>
    public void Build(Canvas canvas)
    {
        RectTransform root = canvas.GetComponent<RectTransform>();

        // 배경
        var bg = CreatePanel(root, "Background", COL_BG, Stretch());
        
        // ── 비네트 오버레이 (시야 효과) ──
        BuildVignette(root);

        // ── Title Panel ──
        BuildTitlePanel(root);

        // ── Bet Panel ──
        BuildBetPanel(root);

        // ── Game Panel ──
        BuildGamePanel(root);

        // ── Result Panel ──
        BuildResultPanel(root);

        // ── Game Over Panel ──
        BuildGameOverPanel(root);

        // 초기 상태: 모두 숨김
        HideAllPanels();
    }

    // ─────────────────────────────────────────────────────────
    //  Title Panel
    // ─────────────────────────────────────────────────────────
    private void BuildTitlePanel(RectTransform root)
    {
        titlePanel = CreatePanel(root, "TitlePanel", new Color(0, 0, 0, 0), Stretch()).gameObject;
        var rt = titlePanel.GetComponent<RectTransform>();

        // 타이틀 텍스트
        var titleText = CreateText(rt, "Title", "BLINDJACK", 80, COL_GOLD, TextAnchor.MiddleCenter);
        SetAnchors(titleText.rectTransform, 0.1f, 0.55f, 0.9f, 0.85f);
        titleText.fontStyle = FontStyle.Bold;

        // 서브타이틀
        var subText = CreateText(rt, "Subtitle", "— 당신의 시야를 걸고, 할당량을 맞춰라 —",
            22, COL_TEXT_DIM, TextAnchor.MiddleCenter);
        SetAnchors(subText.rectTransform, 0.1f, 0.45f, 0.9f, 0.55f);

        // 시작 버튼
        startButton = CreateButton(rt, "StartBtn", "게  임  시  작", 28, COL_BTN, COL_TEXT);
        SetAnchors(startButton.GetComponent<RectTransform>(), 0.3f, 0.2f, 0.7f, 0.35f);
    }

    // ─────────────────────────────────────────────────────────
    //  Bet Panel
    // ─────────────────────────────────────────────────────────
    private void BuildBetPanel(RectTransform root)
    {
        betPanel = CreatePanel(root, "BetPanel", new Color(0, 0, 0, 0.6f), Stretch()).gameObject;
        var rt = betPanel.GetComponent<RectTransform>();

        // 중앙 패널
        var center = CreatePanel(rt, "BetCenter", COL_PANEL, new Vector4(0.2f, 0.15f, 0.8f, 0.85f));
        var crt = center.GetComponent<RectTransform>();

        betStageText = CreateText(crt, "BetStage", "STAGE 1", 42, COL_GOLD, TextAnchor.MiddleCenter);
        SetAnchors(betStageText.rectTransform, 0.05f, 0.82f, 0.95f, 0.95f);
        betStageText.fontStyle = FontStyle.Bold;

        betQuotaText = CreateText(crt, "BetQuota", "할당량: 21", 28, COL_TEXT, TextAnchor.MiddleCenter);
        SetAnchors(betQuotaText.rectTransform, 0.05f, 0.7f, 0.95f, 0.82f);

        // 시야 잔량
        betVisionText = CreateText(crt, "BetVision", "보유 시야: 100", 22, COL_TEXT_DIM, TextAnchor.MiddleCenter);
        SetAnchors(betVisionText.rectTransform, 0.05f, 0.58f, 0.95f, 0.68f);

        // 배팅 조절
        var betLabel = CreateText(crt, "BetLabel", "배팅할 시야", 20, COL_TEXT_DIM, TextAnchor.MiddleCenter);
        SetAnchors(betLabel.rectTransform, 0.05f, 0.48f, 0.95f, 0.56f);

        betDecButton = CreateButton(crt, "BetDec", "−", 36, COL_BTN, COL_TEXT);
        SetAnchors(betDecButton.GetComponent<RectTransform>(), 0.15f, 0.32f, 0.3f, 0.48f);

        betAmountText = CreateText(crt, "BetAmount", "5", 48, COL_GOLD, TextAnchor.MiddleCenter);
        SetAnchors(betAmountText.rectTransform, 0.3f, 0.32f, 0.7f, 0.48f);
        betAmountText.fontStyle = FontStyle.Bold;

        betIncButton = CreateButton(crt, "BetInc", "+", 36, COL_BTN, COL_TEXT);
        SetAnchors(betIncButton.GetComponent<RectTransform>(), 0.7f, 0.32f, 0.85f, 0.48f);

        // 확인 버튼
        betConfirmButton = CreateButton(crt, "BetConfirm", "확 인", 28, COL_CARD_OP, COL_CARD_TEXT);
        SetAnchors(betConfirmButton.GetComponent<RectTransform>(), 0.25f, 0.1f, 0.75f, 0.25f);
    }

    // ─────────────────────────────────────────────────────────
    //  Game Panel (메인 플레이 화면)
    // ─────────────────────────────────────────────────────────
    private void BuildGamePanel(RectTransform root)
    {
        gamePanel = CreatePanel(root, "GamePanel", new Color(0, 0, 0, 0), Stretch()).gameObject;
        var rt = gamePanel.GetComponent<RectTransform>();

        // ── 상단 HUD ──
        var header = CreatePanel(rt, "Header", COL_PANEL, new Vector4(0f, 0.9f, 1f, 1f));
        var hrt = header.GetComponent<RectTransform>();

        stageText = CreateText(hrt, "StageText", "STAGE 1", 22, COL_GOLD, TextAnchor.MiddleLeft);
        SetAnchors(stageText.rectTransform, 0.02f, 0.1f, 0.2f, 0.9f);
        stageText.fontStyle = FontStyle.Bold;

        quotaText = CreateText(hrt, "QuotaText", "할당량: 21", 22, COL_TEXT, TextAnchor.MiddleCenter);
        SetAnchors(quotaText.rectTransform, 0.25f, 0.1f, 0.55f, 0.9f);

        // 시야 바
        var visionBg = CreatePanel(hrt, "VisionBg", HexColor("#1A1A1A"), new Vector4(0.6f, 0.25f, 0.88f, 0.75f));
        visionBarFill = CreatePanel(visionBg.GetComponent<RectTransform>(), "VisionFill",
            COL_GREEN, new Vector4(0f, 0f, 1f, 1f)).GetComponent<Image>();

        visionValueText = CreateText(hrt, "VisionVal", "100", 18, COL_TEXT, TextAnchor.MiddleRight);
        SetAnchors(visionValueText.rectTransform, 0.89f, 0.1f, 0.98f, 0.9f);

        // (할당량 큰 표시는 상단 HUD quotaText로 대체)

        // ── 수식 표시 영역 ──
        expressionText = CreateText(rt, "Expression", "", 24, COL_TEXT_DIM, TextAnchor.MiddleCenter);
        SetAnchors(expressionText.rectTransform, 0.05f, 0.75f, 0.95f, 0.83f);

        // ── 필드 영역 (카드 배치) ──
        var fieldBg = CreatePanel(rt, "FieldBg", COL_PANEL_LIGHT, new Vector4(0.03f, 0.52f, 0.97f, 0.74f));
        var fieldBgRt = fieldBg.GetComponent<RectTransform>();

        var fieldLabel = CreateText(fieldBgRt, "FieldLabel", "FIELD", 14, COL_TEXT_DIM, TextAnchor.UpperLeft);
        SetAnchors(fieldLabel.rectTransform, 0.02f, 0.85f, 0.2f, 1f);

        // 스크롤 가능한 필드 컨테이너
        var fieldScroll = new GameObject("FieldScroll", typeof(RectTransform));
        fieldScroll.transform.SetParent(fieldBgRt, false);
        SetAnchors(fieldScroll.GetComponent<RectTransform>(), 0.02f, 0.05f, 0.98f, 0.82f);

        fieldContainer = new GameObject("FieldContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        fieldContainer.SetParent(fieldScroll.transform, false);
        fieldContainer.anchorMin = new Vector2(0, 0);
        fieldContainer.anchorMax = new Vector2(1, 1);
        fieldContainer.offsetMin = Vector2.zero;
        fieldContainer.offsetMax = Vector2.zero;
        var fieldLayout = fieldContainer.GetComponent<HorizontalLayoutGroup>();
        fieldLayout.spacing = 6;
        fieldLayout.childAlignment = TextAnchor.MiddleCenter;
        fieldLayout.childForceExpandWidth = false;
        fieldLayout.childForceExpandHeight = false;
        fieldLayout.padding = new RectOffset(10, 10, 5, 5);

        // ── 현재 값 표시 ──
        currentValueText = CreateText(rt, "CurrentValue", "= 0", 36, COL_TEXT, TextAnchor.MiddleCenter);
        SetAnchors(currentValueText.rectTransform, 0.1f, 0.44f, 0.9f, 0.52f);
        currentValueText.fontStyle = FontStyle.Bold;

        // ── 손패(Hand) 영역 ──
        var handBg = CreatePanel(rt, "HandBg", COL_PANEL, new Vector4(0.03f, 0.3f, 0.97f, 0.43f));
        var handBgRt = handBg.GetComponent<RectTransform>();

        handLabel = CreateText(handBgRt, "HandLabel", "HAND — 연산 카드를 선택 후 필드 슬롯을 클릭", 13, COL_TEXT_DIM, TextAnchor.UpperLeft);
        SetAnchors(handLabel.rectTransform, 0.02f, 0.78f, 0.98f, 1f);

        handContainer = new GameObject("HandContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        handContainer.SetParent(handBgRt, false);
        SetAnchors(handContainer, 0.02f, 0.05f, 0.98f, 0.75f);
        var handLayout = handContainer.GetComponent<HorizontalLayoutGroup>();
        handLayout.spacing = 8;
        handLayout.childAlignment = TextAnchor.MiddleLeft;
        handLayout.childForceExpandWidth = false;
        handLayout.childForceExpandHeight = false;
        handLayout.padding = new RectOffset(10, 10, 5, 5);

        // ── 덱 영역 ──
        var deckBg = CreatePanel(rt, "DeckBg", COL_PANEL_LIGHT, new Vector4(0.03f, 0.13f, 0.97f, 0.29f));
        var deckBgRt = deckBg.GetComponent<RectTransform>();

        deckLabel = CreateText(deckBgRt, "DeckLabel", "DECK — 남은 카드", 13, COL_TEXT_DIM, TextAnchor.UpperLeft);
        SetAnchors(deckLabel.rectTransform, 0.02f, 0.78f, 0.6f, 1f);

        deckContainer = new GameObject("DeckContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        deckContainer.SetParent(deckBgRt, false);
        SetAnchors(deckContainer, 0.02f, 0.05f, 0.98f, 0.75f);
        var deckLayout = deckContainer.GetComponent<HorizontalLayoutGroup>();
        deckLayout.spacing = 4;
        deckLayout.childAlignment = TextAnchor.MiddleLeft;
        deckLayout.childForceExpandWidth = false;
        deckLayout.childForceExpandHeight = false;
        deckLayout.padding = new RectOffset(10, 10, 3, 3);

        // ── 액션 버튼 ──
        hitButton = CreateButton(rt, "HitBtn", "H I T", 26, COL_BTN, COL_TEXT);
        SetAnchors(hitButton.GetComponent<RectTransform>(), 0.08f, 0.02f, 0.48f, 0.11f);

        stayButton = CreateButton(rt, "StayBtn", "S T A Y", 26, HexColor("#B71C1C"), COL_TEXT);
        SetAnchors(stayButton.GetComponent<RectTransform>(), 0.52f, 0.02f, 0.92f, 0.11f);
    }

    // ─────────────────────────────────────────────────────────
    //  Result Panel
    // ─────────────────────────────────────────────────────────
    private void BuildResultPanel(RectTransform root)
    {
        resultPanel = CreatePanel(root, "ResultPanel", new Color(0, 0, 0, 0.7f), Stretch()).gameObject;
        var rt = resultPanel.GetComponent<RectTransform>();

        var center = CreatePanel(rt, "ResultCenter", COL_PANEL, new Vector4(0.15f, 0.2f, 0.85f, 0.8f));
        var crt = center.GetComponent<RectTransform>();

        resultTitleText = CreateText(crt, "ResultTitle", "SUCCESS!", 56, COL_GREEN, TextAnchor.MiddleCenter);
        SetAnchors(resultTitleText.rectTransform, 0.05f, 0.7f, 0.95f, 0.92f);
        resultTitleText.fontStyle = FontStyle.Bold;

        resultExprText = CreateText(crt, "ResultExpr", "7 + 3 + J = 21", 26, COL_TEXT, TextAnchor.MiddleCenter);
        SetAnchors(resultExprText.rectTransform, 0.05f, 0.52f, 0.95f, 0.68f);

        resultCompareText = CreateText(crt, "ResultCompare", "21 / 21", 32, COL_GOLD, TextAnchor.MiddleCenter);
        SetAnchors(resultCompareText.rectTransform, 0.05f, 0.36f, 0.95f, 0.52f);

        resultButton = CreateButton(crt, "ResultBtn", "다음 스테이지", 26, COL_BTN, COL_TEXT);
        SetAnchors(resultButton.GetComponent<RectTransform>(), 0.2f, 0.08f, 0.8f, 0.25f);
        resultButtonLabel = resultButton.GetComponentInChildren<Text>();
    }

    // ─────────────────────────────────────────────────────────
    //  Game Over Panel
    // ─────────────────────────────────────────────────────────
    private void BuildGameOverPanel(RectTransform root)
    {
        gameOverPanel = CreatePanel(root, "GameOverPanel", new Color(0, 0, 0, 0.85f), Stretch()).gameObject;
        var rt = gameOverPanel.GetComponent<RectTransform>();

        gameOverTitleText = CreateText(rt, "GOTitle", "GAME OVER", 64, COL_RED, TextAnchor.MiddleCenter);
        SetAnchors(gameOverTitleText.rectTransform, 0.1f, 0.55f, 0.9f, 0.8f);
        gameOverTitleText.fontStyle = FontStyle.Bold;

        gameOverDetailText = CreateText(rt, "GODetail", "Stage 1에서 시야를 모두 잃었습니다.", 24, COL_TEXT_DIM, TextAnchor.MiddleCenter);
        SetAnchors(gameOverDetailText.rectTransform, 0.1f, 0.4f, 0.9f, 0.55f);

        restartButton = CreateButton(rt, "RestartBtn", "처음부터 다시", 28, COL_BTN, COL_TEXT);
        SetAnchors(restartButton.GetComponent<RectTransform>(), 0.25f, 0.2f, 0.75f, 0.35f);
    }

    // ─────────────────────────────────────────────────────────
    //  Vignette (시야 효과)
    // ─────────────────────────────────────────────────────────
    private void BuildVignette(RectTransform root)
    {
        var vigGo = CreatePanel(root, "Vignette", Color.clear, Stretch());
        vignetteGroup = vigGo.gameObject.AddComponent<CanvasGroup>();
        vignetteGroup.alpha = 0;
        vignetteGroup.blocksRaycasts = false;
        vignetteGroup.interactable = false;

        // 상하좌우 그라데이션 바 (Vignette 효과)
        CreateVignetteEdge(vigGo.GetComponent<RectTransform>(), "Top",    new Vector4(0, 0.7f, 1, 1));
        CreateVignetteEdge(vigGo.GetComponent<RectTransform>(), "Bottom", new Vector4(0, 0, 1, 0.3f));
        CreateVignetteEdge(vigGo.GetComponent<RectTransform>(), "Left",   new Vector4(0, 0, 0.25f, 1));
        CreateVignetteEdge(vigGo.GetComponent<RectTransform>(), "Right",  new Vector4(0.75f, 0, 1, 1));

        // 중앙 오버레이 (극단적 시야 감소 시)
        var centerOverlay = CreatePanel(vigGo.GetComponent<RectTransform>(), "CenterOverlay",
            new Color(0, 0, 0, 0.5f), Stretch());
        vignetteOverlay = centerOverlay.GetComponent<Image>();
    }

    private void CreateVignetteEdge(RectTransform parent, string name, Vector4 anchors)
    {
        var edge = CreatePanel(parent, "Vig_" + name, COL_VIGNETTE, anchors);
    }

    // ═══════════════════════════════════════════════════════════
    //  패널 제어
    // ═══════════════════════════════════════════════════════════

    public void HideAllPanels()
    {
        titlePanel.SetActive(false);
        betPanel.SetActive(false);
        gamePanel.SetActive(false);
        resultPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void ShowTitle() { HideAllPanels(); titlePanel.SetActive(true); }
    public void ShowBet()   { HideAllPanels(); betPanel.SetActive(true); }
    public void ShowGame()  { HideAllPanels(); gamePanel.SetActive(true); }
    public void ShowResult(){ HideAllPanels(); resultPanel.SetActive(true); }
    public void ShowGameOver(){ HideAllPanels(); gameOverPanel.SetActive(true); }

    // ═══════════════════════════════════════════════════════════
    //  필드 (Field) 동적 UI
    // ═══════════════════════════════════════════════════════════

    /// <summary>필드를 초기화(모든 카드 제거)</summary>
    public void ClearField()
    {
        foreach (var go in fieldElements) if (go != null) Destroy(go);
        fieldElements.Clear();
        operatorSlotImages.Clear();
        operatorSlotTexts.Clear();
        operatorSlotButtons.Clear();
    }

    /// <summary>필드에 숫자 카드를 추가합니다. 두 번째 카드부터 앞에 연산자 슬롯도 생성.</summary>
    public void AddNumberToField(CardData card, int numberIndex)
    {
        // 두 번째 숫자부터 — 앞에 연산자 슬롯 생성
        if (numberIndex > 0)
        {
            var slotGo = CreateCardElement(fieldContainer, "OpSlot_" + (numberIndex - 1),
                "+", COL_SLOT_DEFAULT, COL_SLOT_TEXT, 45, 65, 28);
            fieldElements.Add(slotGo);

            var slotImg = slotGo.GetComponent<Image>();
            var slotTxt = slotGo.GetComponentInChildren<Text>();
            var slotBtn = slotGo.GetComponent<Button>();
            operatorSlotImages.Add(slotImg);
            operatorSlotTexts.Add(slotTxt);
            operatorSlotButtons.Add(slotBtn);

            int slotIndex = numberIndex - 1;
            slotBtn.onClick.AddListener(() => OnOperatorSlotClicked?.Invoke(slotIndex));
        }

        // 숫자 카드 생성
        var cardGo = CreateCardElement(fieldContainer, "NumCard_" + numberIndex,
            card.DisplayText, COL_CARD_NUM, COL_CARD_TEXT, 65, 90, 32);
        fieldElements.Add(cardGo);
    }

    /// <summary>필드 연산자 슬롯에 연산 카드를 배치합니다.</summary>
    public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
    {
        if (slotIndex < 0 || slotIndex >= operatorSlotTexts.Count) return;
        operatorSlotTexts[slotIndex].text = CardData.OperatorSymbol(op);
        operatorSlotImages[slotIndex].color = COL_CARD_OP;
        operatorSlotTexts[slotIndex].color = COL_CARD_TEXT;
    }

    /// <summary>배치 가능한(기본 +) 슬롯을 하이라이트합니다.</summary>
    public void HighlightAvailableSlots(List<OperatorType> currentOps)
    {
        for (int i = 0; i < operatorSlotImages.Count; i++)
        {
            if (i < currentOps.Count && currentOps[i] == OperatorType.None)
            {
                operatorSlotImages[i].color = COL_HIGHLIGHT;
                operatorSlotTexts[i].color = COL_CARD_TEXT;
            }
        }
    }

    /// <summary>모든 슬롯 하이라이트 해제</summary>
    public void ClearSlotHighlights(List<OperatorType> currentOps)
    {
        for (int i = 0; i < operatorSlotImages.Count; i++)
        {
            if (i < currentOps.Count && currentOps[i] == OperatorType.None)
            {
                operatorSlotImages[i].color = COL_SLOT_DEFAULT;
                operatorSlotTexts[i].color = COL_SLOT_TEXT;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  손패 (Hand) 동적 UI
    // ═══════════════════════════════════════════════════════════

    public void ClearHand()
    {
        foreach (var go in handElements) if (go != null) Destroy(go);
        handElements.Clear();
        handCardImages.Clear();
    }

    /// <summary>손패에 연산 카드 추가</summary>
    public void AddOperatorToHand(CardData card, int handIndex)
    {
        var cardGo = CreateCardElement(handContainer, "HandOp_" + handIndex,
            card.DisplayText, COL_CARD_OP, COL_CARD_TEXT, 55, 75, 30);
        handElements.Add(cardGo);

        var img = cardGo.GetComponent<Image>();
        handCardImages.Add(img);

        var btn = cardGo.GetComponent<Button>();
        int idx = handIndex;
        btn.onClick.AddListener(() => OnHandCardClicked?.Invoke(idx));
    }

    /// <summary>손패 카드 하이라이트</summary>
    public void HighlightHandCard(int index)
    {
        for (int i = 0; i < handCardImages.Count; i++)
        {
            if (handCardImages[i] != null)
            {
                handCardImages[i].color = (i == index) ? COL_HIGHLIGHT : COL_CARD_OP;
            }
        }
    }

    /// <summary>손패에서 카드 제거 (UI만)</summary>
    public void RemoveFromHand(int index)
    {
        if (index >= 0 && index < handElements.Count)
        {
            if (handElements[index] != null) Destroy(handElements[index]);
            handElements.RemoveAt(index);
            handCardImages.RemoveAt(index);
        }

        // 재인덱싱: 버튼 콜백 갱신
        RebuildHandCallbacks();
    }

    private void RebuildHandCallbacks()
    {
        for (int i = 0; i < handElements.Count; i++)
        {
            if (handElements[i] == null) continue;
            var btn = handElements[i].GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            int idx = i;
            btn.onClick.AddListener(() => OnHandCardClicked?.Invoke(idx));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  덱 (Deck) 동적 UI
    // ═══════════════════════════════════════════════════════════

    public void ClearDeck()
    {
        foreach (var go in deckElements) if (go != null) Destroy(go);
        deckElements.Clear();
    }

    /// <summary>덱의 남은 카드를 표시합니다 (고정 덱 → 모든 카드 공개)</summary>
    public void DisplayDeck(List<CardData> deck, int startIndex)
    {
        ClearDeck();
        for (int i = startIndex; i < deck.Count; i++)
        {
            var card = deck[i];
            Color bgCol = card.cardType == CardType.Number ? COL_CARD_NUM : COL_CARD_OP;
            Color txtCol = COL_CARD_TEXT;

            var cardGo = CreateCardElement(deckContainer, "DeckCard_" + i,
                card.DisplayText, bgCol, txtCol, 40, 56, 18);
            deckElements.Add(cardGo);

            // 다음 드로우 카드 강조
            if (i == startIndex)
            {
                var outline = cardGo.AddComponent<Outline>();
                outline.effectColor = COL_HIGHLIGHT;
                outline.effectDistance = new Vector2(2, 2);
            }

            // 덱 카드는 클릭 불가
            var btn = cardGo.GetComponent<Button>();
            if (btn != null) Destroy(btn);
        }

        deckLabel.text = $"DECK — 남은 카드 ({deck.Count - startIndex}장)";
    }

    // ═══════════════════════════════════════════════════════════
    //  시야(Vision) 효과 업데이트
    // ═══════════════════════════════════════════════════════════

    /// <summary>시야 비율에 따라 비네트 강도 업데이트</summary>
    public void UpdateVignetteEffect(float visionRatio)
    {
        if (visionRatio >= 0.8f)
        {
            vignetteGroup.alpha = 0f;
        }
        else if (visionRatio >= 0.5f)
        {
            vignetteGroup.alpha = Mathf.Lerp(0.3f, 0f, (visionRatio - 0.5f) / 0.3f);
            if (vignetteOverlay != null)
                vignetteOverlay.color = new Color(0, 0, 0, 0.05f);
        }
        else if (visionRatio >= 0.2f)
        {
            vignetteGroup.alpha = Mathf.Lerp(0.7f, 0.3f, (visionRatio - 0.2f) / 0.3f);
            if (vignetteOverlay != null)
                vignetteOverlay.color = new Color(0, 0, 0, 0.15f);
        }
        else
        {
            vignetteGroup.alpha = Mathf.Lerp(1f, 0.7f, visionRatio / 0.2f);
            if (vignetteOverlay != null)
                vignetteOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0.5f, 0.15f, visionRatio / 0.2f));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HUD 업데이트 유틸
    // ═══════════════════════════════════════════════════════════

    public void UpdateVisionBar(int current, int max)
    {
        float ratio = (float)current / max;
        visionBarFill.rectTransform.anchorMax = new Vector2(ratio, 1);
        visionValueText.text = current.ToString();

        // 색상 변화
        if (ratio > 0.5f)
            visionBarFill.color = Color.Lerp(HexColor("#FFC107"), COL_GREEN, (ratio - 0.5f) * 2f);
        else
            visionBarFill.color = Color.Lerp(COL_RED, HexColor("#FFC107"), ratio * 2f);
    }

    // ═══════════════════════════════════════════════════════════
    //  UI 생성 유틸리티
    // ═══════════════════════════════════════════════════════════

    /// <summary>카드 모양 UI 요소 생성 (배경 + 텍스트 + 버튼)</summary>
    private GameObject CreateCardElement(RectTransform parent, string name,
        string text, Color bgColor, Color textColor, float width, float height, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        var img = go.GetComponent<Image>();
        img.color = bgColor;

        // 텍스트
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = txtGo.GetComponent<Text>();
        txt.text = text;
        txt.font = GetDefaultFont();
        txt.fontSize = fontSize;
        txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        // 버튼 색상
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1),
            Mathf.Min(bgColor.g + 0.15f, 1),
            Mathf.Min(bgColor.b + 0.15f, 1), bgColor.a);
        colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, bgColor.a);
        btn.colors = colors;

        // LayoutElement 추가 (레이아웃 그룹 내에서 크기 유지)
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        le.minWidth = width;
        le.minHeight = height;

        return go;
    }

    private Image CreatePanel(RectTransform parent, string name, Color color, Vector4 anchors)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchors.x, anchors.y);
        rt.anchorMax = new Vector2(anchors.z, anchors.w);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = color;

        return img;
    }

    private Text CreateText(RectTransform parent, string name, string text,
        int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.font = GetDefaultFont();
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        return txt;
    }

    private Button CreateButton(RectTransform parent, string name, string text,
        int fontSize, Color bgColor, Color textColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = bgColor;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.1f, 1f),
            Mathf.Min(bgColor.g + 0.1f, 1f),
            Mathf.Min(bgColor.b + 0.1f, 1f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
        btn.colors = colors;

        // 버튼 텍스트
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(5, 2);
        txtRt.offsetMax = new Vector2(-5, -2);

        var txt = txtGo.GetComponent<Text>();
        txt.text = text;
        txt.font = GetDefaultFont();
        txt.fontSize = fontSize;
        txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;

        return btn;
    }

    // ── 앵커 유틸 ──

    private Vector4 Stretch() => new Vector4(0, 0, 1, 1);

    private void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── 폰트 유틸 ──

    /// <summary>
    /// Unity 버전 전반에서 사용 가능한 내장 폰트를 반환합니다.
    /// Unity 6: "Arial.ttf" | 구버전: "LegacyRuntime.ttf" 순으로 시도.
    /// </summary>
    private static Font GetDefaultFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null) return f;
        f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        // 마지막 수단: 씬에 있는 아무 폰트
        return Font.CreateDynamicFontFromOSFont("Arial", 14);
    }

    // ── 색상 유틸 ──

    public static Color HexColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            return color;
        return Color.white;
    }
}
