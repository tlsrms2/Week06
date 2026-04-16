using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 블라인드잭 메인 게임 컨트롤러 (싱글톤).
/// 게임 상태 머신, 덱 관리, 필드 로직, UI 연동을 총괄합니다.
/// 
/// 사용법:
/// 1. 씬에 빈 GameObject 생성
/// 2. 이 스크립트를 부착
/// 3. Play → 모든 UI가 자동 생성됩니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── 게임 상태 ──
    public enum GameState { Title, Betting, Playing, Result, GameOver, StageClear }
    public GameState CurrentState { get; private set; }

    // ── 스테이지 데이터 ──
    private List<StageInfo> allStages;
    private int currentStageIndex = 0;
    private StageInfo CurrentStage => allStages[currentStageIndex];

    // ── 덱 상태 ──
    private int deckDrawIndex;           // 다음 드로우할 카드 인덱스

    // ── 필드 상태 ──
    private List<int> fieldNumbers;      // 필드에 놓인 숫자값
    private List<string> fieldDisplays;  // 필드에 놓인 카드 표시 텍스트 (A, J 등)
    private List<OperatorType> fieldOperators; // 숫자 사이 연산자 (None=기본 +)

    // ── 손패 상태 ──
    private List<CardData> handOperators;    // 손에 든 연산 카드
    private int selectedHandIndex = -1;      // 선택된 손패 인덱스 (-1 = 미선택)

    // ── 배팅 ──
    private int betAmount;

    // ── 매니저/UI 참조 ──
    private VisionManager visionManager;
    private GameUI gameUI;
    private VisionEffect visionEffect;
    private Canvas mainCanvas;

    // ═══════════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // 스테이지 데이터 로드
        allStages = StageDatabase.GetAllStages();

        // VisionManager 부착
        visionManager = gameObject.AddComponent<VisionManager>();

        // VisionEffect 부착
        visionEffect = gameObject.AddComponent<VisionEffect>();

        // EventSystem 확인/생성
        EnsureEventSystem();

        // Canvas 생성
        mainCanvas = CreateCanvas();

        // GameUI 생성 & 빌드
        gameUI = mainCanvas.gameObject.AddComponent<GameUI>();
        gameUI.Build(mainCanvas);

        // 이벤트 바인딩
        BindUIEvents();

        // 시야 이벤트 구독
        visionManager.OnVisionChanged += OnVisionChanged;

        // 타이틀 화면으로 시작
        EnterState(GameState.Title);
    }

    void OnDestroy()
    {
        if (visionManager != null)
            visionManager.OnVisionChanged -= OnVisionChanged;
    }

    // ═══════════════════════════════════════════════════════════
    //  Canvas & EventSystem 생성
    // ═══════════════════════════════════════════════════════════

    private Canvas CreateCanvas()
    {
        var canvasGo = new GameObject("BlindJackCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private void EnsureEventSystem()
    {
        // Unity 6 API: FindFirstObjectByType (FindObjectOfType는 obsolete)
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        // Unity 6 기본 Input System
        esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ═══════════════════════════════════════════════════════════
    //  UI 이벤트 바인딩
    // ═══════════════════════════════════════════════════════════

    private void BindUIEvents()
    {
        // Title
        gameUI.startButton.onClick.AddListener(OnStartClicked);

        // Bet
        gameUI.betIncButton.onClick.AddListener(() => AdjustBet(5));
        gameUI.betDecButton.onClick.AddListener(() => AdjustBet(-5));
        gameUI.betConfirmButton.onClick.AddListener(OnBetConfirmed);

        // Game
        gameUI.hitButton.onClick.AddListener(OnHitClicked);
        gameUI.stayButton.onClick.AddListener(OnStayClicked);

        // Result
        gameUI.resultButton.onClick.AddListener(OnResultContinue);

        // Game Over
        gameUI.restartButton.onClick.AddListener(OnRestartClicked);

        // Field & Hand 동적 이벤트
        gameUI.OnOperatorSlotClicked += OnFieldSlotClicked;
        gameUI.OnHandCardClicked += OnHandCardClicked;
    }

    // ═══════════════════════════════════════════════════════════
    //  상태 머신
    // ═══════════════════════════════════════════════════════════

    private void EnterState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Title:
                EnterTitle();
                break;
            case GameState.Betting:
                EnterBetting();
                break;
            case GameState.Playing:
                EnterPlaying();
                break;
            case GameState.Result:
                EnterResult();
                break;
            case GameState.GameOver:
                EnterGameOver();
                break;
            case GameState.StageClear:
                EnterStageClear();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Title
    // ─────────────────────────────────────────────────────────
    private void EnterTitle()
    {
        gameUI.ShowTitle();
        currentStageIndex = 0;
        visionManager.ResetVision();
    }

    private void OnStartClicked()
    {
        EnterState(GameState.Betting);
    }

    // ─────────────────────────────────────────────────────────
    //  Betting
    // ─────────────────────────────────────────────────────────
    private void EnterBetting()
    {
        gameUI.ShowBet();

        betAmount = CurrentStage.minimumBet;

        // UI 업데이트
        gameUI.betStageText.text = $"STAGE {CurrentStage.stageNumber}";
        gameUI.betQuotaText.text = $"할당량: {FormatNumber(CurrentStage.quota)}";
        gameUI.betVisionText.text = $"보유 시야: {visionManager.CurrentVision}";
        gameUI.betAmountText.text = betAmount.ToString();

        // 시야 부족 체크
        if (!visionManager.CanBet(CurrentStage.minimumBet))
        {
            EnterState(GameState.GameOver);
        }
    }

    private void AdjustBet(int delta)
    {
        betAmount = Mathf.Clamp(betAmount + delta, CurrentStage.minimumBet, visionManager.CurrentVision);
        gameUI.betAmountText.text = betAmount.ToString();
    }

    private void OnBetConfirmed()
    {
        visionManager.SetBet(betAmount);
        EnterState(GameState.Playing);
    }

    // ─────────────────────────────────────────────────────────
    //  Playing
    // ─────────────────────────────────────────────────────────
    private void EnterPlaying()
    {
        gameUI.ShowGame();

        // 필드 초기화
        fieldNumbers = new List<int>();
        fieldDisplays = new List<string>();
        fieldOperators = new List<OperatorType>();
        handOperators = new List<CardData>();
        selectedHandIndex = -1;
        deckDrawIndex = 0;

        // UI 초기화
        gameUI.ClearField();
        gameUI.ClearHand();

        // HUD 업데이트
        gameUI.stageText.text = $"STAGE {CurrentStage.stageNumber}";
        gameUI.quotaText.text = $"할당량: {FormatNumber(CurrentStage.quota)}";
        UpdateVisionHUD();

        // 덱 표시
        gameUI.DisplayDeck(CurrentStage.deck, deckDrawIndex);

        // 현재 값 초기화
        UpdateExpressionDisplay();

        // 버튼 상태
        UpdatePlayButtons();
    }

    private void OnHitClicked()
    {
        if (CurrentState != GameState.Playing) return;
        if (deckDrawIndex >= CurrentStage.deck.Count) return;

        // 카드 드로우
        CardData drawnCard = CurrentStage.deck[deckDrawIndex];
        deckDrawIndex++;

        if (drawnCard.cardType == CardType.Number)
        {
            // 숫자 카드 → 필드에 자동 배치 (오른쪽 끝)
            fieldNumbers.Add(drawnCard.NumberValue);
            fieldDisplays.Add(drawnCard.DisplayText);

            // 두 번째 숫자부터 기본 연산자(+) 추가
            if (fieldNumbers.Count > 1)
                fieldOperators.Add(OperatorType.None); // None = 기본 덧셈

            gameUI.AddNumberToField(drawnCard, fieldNumbers.Count - 1);
        }
        else
        {
            // 연산 카드 → 손패에 추가
            handOperators.Add(drawnCard);
            gameUI.AddOperatorToHand(drawnCard, handOperators.Count - 1);
        }

        // 디스플레이 갱신
        UpdateExpressionDisplay();
        gameUI.DisplayDeck(CurrentStage.deck, deckDrawIndex);
        UpdatePlayButtons();

        // 선택된 손패가 있으면 슬롯 하이라이트
        if (selectedHandIndex >= 0)
            gameUI.HighlightAvailableSlots(fieldOperators);
    }

    private void OnStayClicked()
    {
        if (CurrentState != GameState.Playing) return;
        if (fieldNumbers.Count == 0) return; // 최소 1장은 필요

        EnterState(GameState.Result);
    }

    private void OnHandCardClicked(int handIndex)
    {
        if (CurrentState != GameState.Playing) return;
        if (handIndex < 0 || handIndex >= handOperators.Count) return;

        // 이미 선택된 카드를 다시 클릭 → 선택 해제
        if (selectedHandIndex == handIndex)
        {
            selectedHandIndex = -1;
            gameUI.HighlightHandCard(-1);
            gameUI.ClearSlotHighlights(fieldOperators);
            return;
        }

        // 새 카드 선택
        selectedHandIndex = handIndex;
        gameUI.HighlightHandCard(handIndex);
        gameUI.HighlightAvailableSlots(fieldOperators);
    }

    private void OnFieldSlotClicked(int slotIndex)
    {
        if (CurrentState != GameState.Playing) return;
        if (selectedHandIndex < 0) return;
        if (slotIndex < 0 || slotIndex >= fieldOperators.Count) return;
        if (fieldOperators[slotIndex] != OperatorType.None) return; // 이미 배치됨

        // 연산 카드 배치
        CardData opCard = handOperators[selectedHandIndex];
        fieldOperators[slotIndex] = opCard.operatorType;

        // UI 업데이트
        gameUI.PlaceOperatorOnSlot(slotIndex, opCard.operatorType);

        // 손패에서 제거
        handOperators.RemoveAt(selectedHandIndex);
        gameUI.RemoveFromHand(selectedHandIndex);

        // 선택 해제
        selectedHandIndex = -1;
        gameUI.ClearSlotHighlights(fieldOperators);

        // 수식 재계산
        UpdateExpressionDisplay();
    }

    private void UpdateExpressionDisplay()
    {
        long value = ExpressionEvaluator.Evaluate(fieldNumbers, fieldOperators);
        string expr = ExpressionEvaluator.ToExpressionString(fieldNumbers, fieldOperators, fieldDisplays);

        gameUI.currentValueText.text = fieldNumbers.Count > 0 ? $"= {FormatNumber(value)}" : "= 0";
        gameUI.expressionText.text = expr;

        // 할당량 초과 시 빨간색 경고
        if (fieldNumbers.Count > 0 && value > CurrentStage.quota)
        {
            gameUI.currentValueText.color = GameUI.COL_RED;
        }
        else if (fieldNumbers.Count > 0 && value == CurrentStage.quota)
        {
            gameUI.currentValueText.color = GameUI.COL_GREEN;
        }
        else
        {
            gameUI.currentValueText.color = GameUI.COL_TEXT;
        }
    }

    private void UpdatePlayButtons()
    {
        // Hit 버튼: 덱에 카드가 남아있을 때만 가능
        gameUI.hitButton.interactable = deckDrawIndex < CurrentStage.deck.Count;

        // Stay 버튼: 필드에 숫자가 1장 이상일 때 가능
        gameUI.stayButton.interactable = fieldNumbers.Count > 0;
    }

    // ─────────────────────────────────────────────────────────
    //  Result
    // ─────────────────────────────────────────────────────────
    private void EnterResult()
    {
        long finalValue = ExpressionEvaluator.Evaluate(fieldNumbers, fieldOperators);
        string exprStr = ExpressionEvaluator.ToExpressionString(fieldNumbers, fieldOperators, fieldDisplays);
        bool isBust = finalValue > CurrentStage.quota;
        bool isPerfect = finalValue == CurrentStage.quota;

        gameUI.ShowResult();

        if (isBust)
        {
            // 실패: 할당량 초과 (Bust)
            gameUI.resultTitleText.text = "B U S T !";
            gameUI.resultTitleText.color = GameUI.COL_RED;
            gameUI.resultCompareText.text = $"{FormatNumber(finalValue)} > {FormatNumber(CurrentStage.quota)}";
            gameUI.resultCompareText.color = GameUI.COL_RED;

            // 시야 상실
            visionManager.LoseBet();

            if (visionManager.IsBlind())
            {
                gameUI.resultButtonLabel.text = "게임 오버";
            }
            else
            {
                gameUI.resultButtonLabel.text = "재도전";
            }
        }
        else if (isPerfect)
        {
            // 완벽 성공
            gameUI.resultTitleText.text = "P E R F E C T !";
            gameUI.resultTitleText.color = GameUI.COL_GOLD;
            gameUI.resultCompareText.text = $"{FormatNumber(finalValue)} = {FormatNumber(CurrentStage.quota)}";
            gameUI.resultCompareText.color = GameUI.COL_GOLD;

            visionManager.WinBet();
            gameUI.resultButtonLabel.text = "다음 스테이지";
        }
        else
        {
            // 성공: 할당량 이하
            gameUI.resultTitleText.text = "S U C C E S S";
            gameUI.resultTitleText.color = GameUI.COL_GREEN;
            gameUI.resultCompareText.text = $"{FormatNumber(finalValue)} ≤ {FormatNumber(CurrentStage.quota)}";
            gameUI.resultCompareText.color = GameUI.COL_GREEN;

            visionManager.WinBet();
            gameUI.resultButtonLabel.text = "다음 스테이지";
        }

        gameUI.resultExprText.text = $"{exprStr} = {FormatNumber(finalValue)}";

        // 시야 HUD 갱신
        UpdateVisionHUD();
    }

    private void OnResultContinue()
    {
        long finalValue = ExpressionEvaluator.Evaluate(fieldNumbers, fieldOperators);
        bool isBust = finalValue > CurrentStage.quota;

        if (isBust)
        {
            if (visionManager.IsBlind())
            {
                EnterState(GameState.GameOver);
            }
            else
            {
                // 같은 스테이지 재도전
                EnterState(GameState.Betting);
            }
        }
        else
        {
            // 다음 스테이지
            currentStageIndex++;
            if (currentStageIndex >= allStages.Count)
            {
                EnterState(GameState.StageClear);
            }
            else
            {
                EnterState(GameState.Betting);
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Game Over
    // ─────────────────────────────────────────────────────────
    private void EnterGameOver()
    {
        gameUI.ShowGameOver();
        gameUI.gameOverDetailText.text = $"Stage {CurrentStage.stageNumber}에서 시야를 모두 잃었습니다.";
    }

    private void OnRestartClicked()
    {
        EnterState(GameState.Title);
    }

    // ─────────────────────────────────────────────────────────
    //  Stage Clear (전체 클리어)
    // ─────────────────────────────────────────────────────────
    private void EnterStageClear()
    {
        gameUI.ShowGameOver(); // 게임 오버 패널 재활용

        gameUI.gameOverTitleText.text = "C L E A R !";
        gameUI.gameOverTitleText.color = GameUI.COL_GOLD;
        gameUI.gameOverDetailText.text = $"모든 스테이지를 클리어했습니다!\n남은 시야: {visionManager.CurrentVision}";
        gameUI.gameOverDetailText.color = GameUI.COL_TEXT;

        var restartLabel = gameUI.restartButton.GetComponentInChildren<Text>();
        if (restartLabel != null) restartLabel.text = "처음부터 다시";
    }

    // ═══════════════════════════════════════════════════════════
    //  시야 (Vision) 이벤트
    // ═══════════════════════════════════════════════════════════

    private void OnVisionChanged(int current, int max)
    {
        UpdateVisionHUD();

        // 비네트 효과 업데이트
        float ratio = (float)current / max;
        gameUI.UpdateVignetteEffect(ratio);
    }

    private void UpdateVisionHUD()
    {
        if (gameUI.visionBarFill != null)
            gameUI.UpdateVisionBar(visionManager.CurrentVision, visionManager.MaxVision);
    }

    // ═══════════════════════════════════════════════════════════
    //  유틸리티
    // ═══════════════════════════════════════════════════════════

    /// <summary>큰 숫자를 쉼표 포맷으로 표시</summary>
    private string FormatNumber(long number)
    {
        return number.ToString("N0");
    }
}
