using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HTH
{
    /// <summary>
    /// 블라인드잭 전체 UI를 관리합니다.
    /// GameManager의 UI 프록시 메서드와 1:1 대응됩니다.
    /// UI 요소 생성(CreateCardElement 등)은 추후 직접 구현합니다.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("타이틀 패널")]
        [Tooltip("타이틀 화면 패널")]
        [SerializeField] private GameObject _titlePanel;
        [Header("TitlePanel 요소")]
        [Tooltip("타이틀 화면 패널")]
        [SerializeField] private Button _startButton;

        [Header("배팅 패널")]
        [Tooltip("배팅 화면 패널")]
        [SerializeField] private GameObject _bettingPanel;
        [Header("BetPanel 요소")]
        [Tooltip("스테이지 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI _betStageText;

        [Tooltip("할당량 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI _betQuotaText;

        [Tooltip("보유 시야 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI _betVisionText;

        [Tooltip("배팅량 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI _betAmountText;

        [Tooltip("배팅량 감소 버튼")]
        [SerializeField] private Button _betDecButton;

        [Tooltip("배팅량 증가 버튼")]
        [SerializeField] private Button _betIncButton;

        [Tooltip("배팅 확정 버튼")]
        [SerializeField] private Button _betConfirmButton;

        [Header("게임 패널")]
        [Tooltip("플레이어 턴 패널")]
        [SerializeField] private GameObject _playerTurnPanel;

        [Tooltip("딜러 턴 패널")]
        [SerializeField] private GameObject _dealerTurnPanel;

        [Header("게임 패널 요소")]
        [Tooltip("게임 패널 스테이지 텍스트")]
        [SerializeField] private TextMeshProUGUI _stageText;

        [Tooltip("할당량 텍스트")]
        [SerializeField] private TextMeshProUGUI _quotaText;

        [Tooltip("시야 바 Fill 이미지")]
        [SerializeField] private Image _visionBarFill;

        [Tooltip("시야 수치 텍스트")]
        [SerializeField] private TextMeshProUGUI _visionValueText;

        [Tooltip("딜러 카드 컨테이너")]
        [SerializeField] private RectTransform _dealerFieldContainer;

        [Tooltip("플레이어 카드 컨테이너")]
        [SerializeField] private RectTransform _fieldContainer;

        [Tooltip("현재 연산값 텍스트")]
        [SerializeField] private TextMeshProUGUI _currentValueText;

        [Tooltip("손패 컨테이너")]
        [SerializeField] private RectTransform _handContainer;

        [Tooltip("히트 버튼")]
        [SerializeField] private Button _hitButton;

        [Tooltip("스테이 버튼")]
        [SerializeField] private Button _stayButton;

        [Header("결과 화면 패널")]
        [Tooltip("결과 화면 패널")]
        [SerializeField] private GameObject _resultPanel;

        [Header("ResultPanel 요소")]
        [Tooltip("결과 타이틀 텍스트 (SUCCESS / BUST)")]
        [SerializeField] private TextMeshProUGUI _resultTitleText;

        [Tooltip("수식 문자열 텍스트")]
        [SerializeField] private TextMeshProUGUI _resultExprText;

        [Tooltip("수치 비교 텍스트 (21 ≤ 21)")]
        [SerializeField] private TextMeshProUGUI _resultCompareText;

        [Tooltip("다음/재도전 버튼")]
        [SerializeField] private Button _resultButton;

        [Tooltip("다음/재도전 버튼 텍스트")]
        [SerializeField] private TextMeshProUGUI _resultButtonLabel;

        [Header("게임 오버 패널")]
        [Tooltip("게임 오버/클리어 패널")]
        [SerializeField] private GameObject _gameOverPanel;

        [Header("GameOverPanel 요소")]
        [Tooltip("게임 오버 / 클리어 타이틀 텍스트")]
        [SerializeField] private TextMeshProUGUI _gameOverTitleText;

        [Tooltip("게임 오버 상세 설명 텍스트")]
        [SerializeField] private TextMeshProUGUI _gameOverDetailText;

        [Tooltip("재시작 버튼")]
        [SerializeField] private Button _restartButton;

        // ─── 내부 상태 ───────────────────────────────────────────
        /// <summary>현재 활성화된 패널. ShowOnly에서 관리합니다.</summary>
        private GameObject _activePanel;

        /// <summary>
        /// UI 내부에서 관리하는 배팅량.
        /// +/- 버튼이 이 값을 조작하고, 확정 버튼이 onConfirm에 전달합니다.
        /// </summary>
        private int _betAmount;

        // 플레이어 필드 동적 요소 — Hit 시마다 추가/제거
        private readonly List<GameObject> _fieldElements = new();
        private readonly List<Image> _operatorSlotImages = new();
        private readonly List<Text> _operatorSlotTexts = new();

        // 딜러 필드 동적 요소
        private readonly List<GameObject> _dealerFieldElements = new();

        // 손패 동적 요소
        private readonly List<GameObject> _handElements = new();
        private readonly List<Image> _handCardImages = new();

        // ─── 이벤트 ──────────────────────────────────────────────
        /// <summary>필드 연산자 슬롯 클릭 시 발행 (슬롯 인덱스)</summary>
        public event Action<int> OnOperatorSlotClicked;

        /// <summary>손패 카드 클릭 시 발행 (손패 인덱스)</summary>
        public event Action<int> OnHandCardClicked;

        // ═══════════════════════════════════════════════════════
        //  생명주기
        // ═══════════════════════════════════════════════════════

        private void OnEnable()
        {
            if (VisionManager.Instance != null)
                VisionManager.Instance.OnVisionChanged += HandleVisionChanged;
        }

        private void OnDisable()
        {
            if (VisionManager.Instance != null)
                VisionManager.Instance.OnVisionChanged -= HandleVisionChanged;
        }

        // ═══════════════════════════════════════════════════════
        //  패널 전환
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 지정한 패널만 활성화하고 나머지는 모두 비활성화합니다.
        /// null을 전달하면 모든 패널을 닫습니다.
        /// </summary>
        private void ShowOnly(GameObject target)
        {
            if (_activePanel != null) _activePanel.SetActive(false);
            _activePanel = target;
            if (_activePanel != null) _activePanel.SetActive(true);
        }

        /// <summary>
        /// 모든 패널을 비활성화합니다.
        /// Build() 완료 후 초기 상태로 사용합니다.
        /// </summary>
        public void HideAllPanels()
        {
            _titlePanel?.SetActive(false);
            _bettingPanel?.SetActive(false);
            _playerTurnPanel?.SetActive(false);
            _dealerTurnPanel?.SetActive(false);
            _resultPanel?.SetActive(false);
            _gameOverPanel?.SetActive(false);
            _activePanel = null;
        }

        // ═══════════════════════════════════════════════════════
        //  GameManager 호출 진입점
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 타이틀 화면을 표시합니다.
        /// 시작 버튼 클릭 시 onStart를 호출합니다.
        /// </summary>
        public void ShowTitle(Action onStart)
        {
            ShowOnly(_titlePanel);

            // StartButton 참조 필요 — Inspector에서 연결하거나 Find로 가져오기
            _startButton.onClick.RemoveAllListeners();
            _startButton.onClick.AddListener(() => onStart?.Invoke());

            Debug.Log("[GameUIManager] ShowTitle");
        }

        /// <summary>
        /// 시야 배팅 화면을 표시합니다.
        /// +/- 버튼이 _betAmount를 조작하고 확정 버튼이 onConfirm을 호출합니다.
        /// </summary>
        public void ShowBetting(int currentBet, Action<int> onConfirm)
        {
            ShowOnly(_bettingPanel);
            _betAmount = currentBet;

            // HUD 초기값 세팅
            _betAmountText.text = _betAmount.ToString();
            _betVisionText.text = $"보유 시야: {VisionManager.Instance?.CurrentVision}";

            // − 버튼
            _betDecButton.onClick.RemoveAllListeners();
            _betDecButton.onClick.AddListener(() =>
            {
                _betAmount = Mathf.Clamp(
                    _betAmount - 5,
                    1,
                    VisionManager.Instance?.CurrentVision ?? 1);
                _betAmountText.text = _betAmount.ToString();
            });

            // + 버튼
            _betIncButton.onClick.RemoveAllListeners();
            _betIncButton.onClick.AddListener(() =>
            {
                _betAmount = Mathf.Clamp(
                    _betAmount + 5,
                    1,
                    VisionManager.Instance?.CurrentVision ?? 1);
                _betAmountText.text = _betAmount.ToString();
            });

            // 확인 버튼
            _betConfirmButton.onClick.RemoveAllListeners();
            _betConfirmButton.onClick.AddListener(() => onConfirm?.Invoke(_betAmount));

            Debug.Log($"[GameUIManager] ShowBetting — bet:{currentBet}");
        }

        /// <summary>
        /// 플레이어 턴 화면을 표시합니다.
        /// Hit / Stand 버튼에 각 콜백을 연결합니다.
        /// </summary>
        public void ShowPlayerTurn(
            List<CardDataSO> field,
            List<CardDataSO> hand,
            Action onHit,
            Action onStand)
        {
            ShowOnly(_playerTurnPanel);
            RefreshPlayerArea(field, hand);

            _hitButton.onClick.RemoveAllListeners();
            _hitButton.onClick.AddListener(() => onHit?.Invoke());

            _stayButton.onClick.RemoveAllListeners();
            _stayButton.onClick.AddListener(() => onStand?.Invoke());

            Debug.Log("[GameUI] ShowPlayerTurn");
        }

        /// <summary>
        /// 딜러가 카드를 드로우하는 동안 대기 연출을 표시합니다.
        /// </summary>
        public void ShowDealerThinking()
        {
            ShowOnly(_dealerTurnPanel);

            // TODO: 딜러 대기 애니메이션 or 텍스트 표시
            Debug.Log("[GameUI] ShowDealerThinking");
        }

        /// <summary>
        /// 결과 화면을 표시합니다.
        /// description은 ExpressionEvaluator.ToExpressionString() 반환값입니다.
        /// 다음 버튼 클릭 시 onNext를 호출합니다.
        /// </summary>
        public void ShowResult(string description, bool win, Action onNext)
        {
            ShowOnly(_resultPanel);

            // 타이틀 텍스트
            _resultTitleText.text = win ? "S U C C E S S" : "B U S T !";
            _resultTitleText.color = win ? HexColor("#4CAF50") : HexColor("#F44336");

            // 수식 문자열 — ExpressionEvaluator.ToExpressionString() 반환값
            _resultExprText.text = description;

            // 수치 비교 텍스트 — GameManager에서 SetResultInfo()로 별도 전달
            // (description만으로는 finalValue / quota 분리가 어려움)

            // 버튼 텍스트 및 콜백
            _resultButtonLabel.text = win ? "다음 스테이지" : "재  도  전";
            _resultButton.onClick.RemoveAllListeners();
            _resultButton.onClick.AddListener(() => onNext?.Invoke());

            Debug.Log($"[GameUIManager] ShowResult — {description} win:{win}");
        }
        /// <summary>
        /// 결과 패널의 수치 비교 텍스트를 갱신합니다.
        /// GameManager의 OnEnterResult에서 ShowResult 호출 전에 실행합니다.
        /// </summary>
        public void SetResultInfo(long finalValue, long quota, bool win)
        {
            if (win)
                _resultCompareText.text = $"{finalValue:N0} ≤ {quota:N0}";
            else
                _resultCompareText.text = $"{finalValue:N0} > {quota:N0}";

            _resultCompareText.color = win ? HexColor("#FFD700") : HexColor("#F44336");
        }

        /// <summary>
        /// 게임 오버 화면을 표시합니다.
        /// 재시작 버튼 클릭 시 onRestart를 호출합니다.
        /// </summary>
        public void ShowGameOver(Action onRestart)
        {
            ShowOnly(_gameOverPanel);

            _gameOverTitleText.text = "GAME OVER";
            _gameOverTitleText.color = HexColor("#F44336");
            _gameOverDetailText.text =
                $"Stage {VisionManager.Instance?.CurrentVision}에서\n시야를 모두 잃었습니다.";

            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => onRestart?.Invoke());

            Debug.Log("[GameUIManager] ShowGameOver");
        }

        /// <summary>
        /// 전체 스테이지 클리어 화면을 표시합니다.
        /// 게임 오버 패널을 재활용합니다.
        /// </summary>
        public void ShowStageClear(Action onRestart)
        {
            ShowOnly(_gameOverPanel);

            _gameOverTitleText.text = "C L E A R !";
            _gameOverTitleText.color = HexColor("#FFD700");
            _gameOverDetailText.text =
                $"모든 스테이지를 클리어했습니다!\n남은 시야: {VisionManager.Instance?.CurrentVision}";

            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => onRestart?.Invoke());

            Debug.Log("[GameUIManager] ShowStageClear");
        }

        /// <summary>
        /// 배팅 패널의 스테이지 정보를 갱신합니다.
        /// GameManager가 OnEnterBetting에서 호출합니다.
        /// </summary>
        public void SetBetPanelInfo(int stageIndex, long quota)
        {
            _betStageText.text = $"STAGE {stageIndex}";
            _betQuotaText.text = $"할당량: {quota:N0}";
        }

        // ═══════════════════════════════════════════════════════
        //  갱신
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// 플레이어 필드와 손패 UI를 갱신합니다.
        /// Hit 시마다 GameManager가 호출합니다.
        /// </summary>
        public void RefreshPlayerArea(List<CardDataSO> field, List<CardDataSO> hand)
        {
            ClearField();
            for (int i = 0; i < field.Count; i++)
                AddCardToField(field[i], i);

            ClearHand();
            for (int i = 0; i < hand.Count; i++)
                AddOperatorToHand(hand[i], i);

            // 현재 연산값 갱신
            UpdateCurrentValue(field);

            Debug.Log($"[GameUI] RefreshPlayerArea — field:{field.Count} hand:{hand.Count}");
        }
        /// <summary>
        /// 현재 필드 연산값을 계산해 텍스트를 갱신합니다.
        /// 할당량 초과 시 빨간색, 일치 시 금색, 정상 시 흰색으로 표시합니다.
        /// </summary>
        public void UpdateCurrentValue(List<CardDataSO> field, long quota = 0)
        {
            if (field.Count == 0)
            {
                _currentValueText.text = "= 0";
                _currentValueText.color = HexColor("#E8E0D0");
                return;
            }

            long value = ExpressionEvaluator.Evaluate(field);
            _currentValueText.text = $"= {value:N0}";

            if (quota > 0 && value > quota)
                _currentValueText.color = HexColor("#F44336"); // 초과 — 빨강
            else if (quota > 0 && value == quota)
                _currentValueText.color = HexColor("#FFD700"); // 정확히 일치 — 금색
            else
                _currentValueText.color = HexColor("#E8E0D0"); // 정상 — 흰색
        }

        /// <summary>
        /// 딜러 필드 UI를 갱신합니다.
        /// 딜러 턴에서 카드가 드로우될 때마다 GameManager가 호출합니다.
        /// </summary>
        public void RefreshDealerArea(List<CardDataSO> field)
        {
            ClearDealerField();
            for (int i = 0; i < field.Count; i++)
                AddCardToDealerField(field[i], i);

            Debug.Log($"[GameUI] RefreshDealerArea — field:{field.Count}");
        }

        // ═══════════════════════════════════════════════════════
        //  플레이어 필드 동적 UI
        // ═══════════════════════════════════════════════════════

        /// <summary>플레이어 필드의 모든 카드 UI를 제거합니다.</summary>
        private void ClearField()
        {
            foreach (var go in _fieldElements) if (go != null) Destroy(go);
            _fieldElements.Clear();
            _operatorSlotImages.Clear();
            _operatorSlotTexts.Clear();
        }

        /// <summary>
        /// 플레이어 필드에 카드 UI를 추가합니다.
        /// 두 번째 카드부터 앞에 연산자 슬롯을 자동 생성합니다.
        /// </summary>
        private void AddCardToField(CardDataSO card, int index)
        {
            if (index > 0)
            {
                // ── 연산자 슬롯 생성 ──
                int slotIndex = index - 1;
                var slotGo = CreateClickableCardObject(
                    _fieldContainer,
                    "+",
                    HexColor("#2A4A3A"),
                    HexColor("#5A8A6A"),
                    width: 45, height: 65, fontSize: 28,
                    onClick: () => OnOperatorSlotClicked?.Invoke(slotIndex));

                _fieldElements.Add(slotGo);
                _operatorSlotImages.Add(slotGo.GetComponent<Image>());
                _operatorSlotTexts.Add(slotGo.GetComponentInChildren<Text>());
            }

            // ── 숫자 카드 생성 ──
            string label = string.IsNullOrEmpty(card.displayLabel)
                ? card.numberValue.ToString()
                : card.displayLabel;
            Color bgCol = card.cardType == CardType.Number
                ? HexColor("#F5F0E1")
                : HexColor("#D4A843");

            var cardGo = CreateCardObject(
                _fieldContainer,
                label,
                bgCol,
                HexColor("#2D2D2D"),
                width: 65, height: 90, fontSize: 32);

            _fieldElements.Add(cardGo);
        }

        /// <summary>연산자 슬롯에 연산자를 배치하고 색상을 변경합니다.</summary>
        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
        {
            if (slotIndex < 0 || slotIndex >= _operatorSlotTexts.Count) return;

            // TODO: _operatorSlotTexts[slotIndex].text  = op의 기호 문자열;
            // TODO: _operatorSlotImages[slotIndex].color = COL_CARD_OP;
            Debug.Log($"[GameUI] PlaceOperatorOnSlot — slot:{slotIndex} op:{op}");
        }

        // ═══════════════════════════════════════════════════════
        //  딜러 필드 동적 UI
        // ═══════════════════════════════════════════════════════

        /// <summary>딜러 필드의 모든 카드 UI를 제거합니다.</summary>
        private void ClearDealerField()
        {
            foreach (var go in _dealerFieldElements) if (go != null) Destroy(go);
            _dealerFieldElements.Clear();
        }

        /// <summary>딜러 필드에 카드 UI를 추가합니다.</summary>
        private void AddCardToDealerField(CardDataSO card, int index)
        {
            string label = string.IsNullOrEmpty(card.displayLabel)
         ? card.numberValue.ToString()
         : card.displayLabel;
            Color bgCol = card.cardType == CardType.Number
                ? HexColor("#F5F0E1")
                : HexColor("#D4A843");

            var cardGo = CreateCardObject(
                _dealerFieldContainer,
                label,
                bgCol,
                HexColor("#2D2D2D"),
                width: 65, height: 90, fontSize: 32);

            _dealerFieldElements.Add(cardGo);
        }

        // ═══════════════════════════════════════════════════════
        //  손패 동적 UI
        // ═══════════════════════════════════════════════════════

        /// <summary>손패의 모든 카드 UI를 제거합니다.</summary>
        private void ClearHand()
        {
            foreach (var go in _handElements) if (go != null) Destroy(go);
            _handElements.Clear();
            _handCardImages.Clear();
        }

        /// <summary>
        /// 손패에 연산자 카드 UI를 추가합니다.
        /// 카드 클릭 시 OnHandCardClicked를 발행합니다.
        /// </summary>
        private void AddOperatorToHand(CardDataSO card, int index)
        {
            string label = string.IsNullOrEmpty(card.displayLabel)
         ? "?"
         : card.displayLabel;

            int capturedIndex = index;
            var cardGo = CreateClickableCardObject(
                _handContainer,
                label,
                HexColor("#D4A843"),
                HexColor("#2D2D2D"),
                width: 55, height: 75, fontSize: 30,
                onClick: () => OnHandCardClicked?.Invoke(capturedIndex));

            _handElements.Add(cardGo);
            _handCardImages.Add(cardGo.GetComponent<Image>());
        }

        /// <summary>
        /// 손패에서 카드 UI를 제거하고 인덱스를 재정렬합니다.
        /// 연산자 슬롯 배치 후 GameManager가 호출합니다.
        /// </summary>
        public void RemoveFromHand(int index)
        {
            if (index < 0 || index >= _handElements.Count) return;

            if (_handElements[index] != null) Destroy(_handElements[index]);
            _handElements.RemoveAt(index);
            _handCardImages.RemoveAt(index);

            // 버튼 콜백 인덱스 재정렬
            RebuildHandCallbacks();
        }

        /// <summary>손패 카드 버튼 콜백을 현재 인덱스 기준으로 재등록합니다.</summary>
        private void RebuildHandCallbacks()
        {
            for (int i = 0; i < _handElements.Count; i++)
            {
                if (_handElements[i] == null) continue;
                var btn = _handElements[i].GetComponent<Button>();
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                int idx = i;
                btn.onClick.AddListener(() => OnHandCardClicked?.Invoke(idx));
            }
        }

        /// <summary>지정한 손패 카드를 하이라이트합니다. -1 전달 시 전체 해제.</summary>
        public void HighlightHandCard(int index)
        {
            for (int i = 0; i < _handCardImages.Count; i++)
            {
                if (_handCardImages[i] == null) continue;
                // TODO: _handCardImages[i].color = (i == index) ? COL_HIGHLIGHT : COL_CARD_OP;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  Vision 이벤트 핸들러
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// VisionManager.OnVisionChanged 구독 핸들러.
        /// 시야 바 갱신과 비네트 효과를 처리합니다.
        /// </summary>
        private void HandleVisionChanged(int current, int max)
        {
            // 시야 바 비율 갱신
            float ratio = (float)current / max;
            _visionBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            _visionValueText.text = current.ToString();

            // 색상 변화 (초록 → 노랑 → 빨강)
            if (ratio > 0.5f)
                _visionBarFill.color = Color.Lerp(
                    HexColor("#FFC107"), HexColor("#4CAF50"), (ratio - 0.5f) * 2f);
            else
                _visionBarFill.color = Color.Lerp(
                    HexColor("#F44336"), HexColor("#FFC107"), ratio * 2f);

            Debug.Log($"[GameUIManager] Vision — {current}/{max}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        /// <summary>
        /// 카드 UI GameObject를 생성합니다.
        /// 숫자/연산자 카드 모두 이 메서드로 생성합니다.
        /// </summary>
        /// <param name="parent">부모 RectTransform (FieldContainer / HandContainer)</param>
        /// <param name="label">카드에 표시할 문자열</param>
        /// <param name="bgColor">카드 배경색</param>
        /// <param name="textColor">카드 텍스트색</param>
        /// <param name="width">카드 너비</param>
        /// <param name="height">카드 높이</param>
        /// <param name="fontSize">폰트 크기</param>
        /// <returns>생성된 카드 GameObject</returns>
        private GameObject CreateCardObject(
            RectTransform parent,
            string label,
            Color bgColor,
            Color textColor,
            float width,
            float height,
            int fontSize)
        {
            // ── 카드 루트 ──
            var go = new GameObject("Card_" + label, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.color = bgColor;

            // ── 텍스트 ──
            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);

            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.GetComponent<Text>();
            txt.text = label;
            txt.font = GetDefaultFont();
            txt.fontSize = fontSize;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // ── LayoutElement (HorizontalLayoutGroup 내 크기 고정) ──
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;

            return go;
        }

        /// <summary>
        /// 버튼이 달린 카드 UI GameObject를 생성합니다.
        /// 손패 카드, 연산자 슬롯처럼 클릭 가능한 카드에 사용합니다.
        /// </summary>
        private GameObject CreateClickableCardObject(
            RectTransform parent,
            string label,
            Color bgColor,
            Color textColor,
            float width,
            float height,
            int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = CreateCardObject(parent, label, bgColor, textColor, width, height, fontSize);

            // Button 컴포넌트 추가
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(
                Mathf.Min(bgColor.r + 0.15f, 1f),
                Mathf.Min(bgColor.g + 0.15f, 1f),
                Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
            colors.pressedColor = new Color(
                bgColor.r * 0.75f,
                bgColor.g * 0.75f,
                bgColor.b * 0.75f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            return go;
        }

        /// <summary>
        /// Unity 버전 전반에서 사용 가능한 내장 폰트를 반환합니다.
        /// </summary>
        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) return f;
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
            return Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
    }
}