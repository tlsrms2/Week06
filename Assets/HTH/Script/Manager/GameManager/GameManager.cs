using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HTH
{
    public enum GameState
    {
        Title,
        Betting,
        PlayerTurn,
        DealerTurn,
        Result,
        GameOver,
        StageClear
    }

    /// <summary>
    /// 블라인드잭 게임의 상태 머신 허브입니다.
    /// 직접 게임 로직을 처리하지 않고 하위 Manager에 위임하며
    /// 상태 전환과 UI 프록시 호출만 담당합니다.
    ///
    /// 상태 흐름:
    /// Title → Betting → PlayerTurn → DealerTurn → Result
    ///                                                ├─ 승리 → 다음 스테이지 Betting
    ///                                                ├─ 패배 → 재도전 Betting
    ///                                                └─ 시야 소진 → GameOver
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("데이터")]
        [Tooltip("전체 스테이지 목록 SO — 스테이지 추가 시 여기에만 드래그")]
        [SerializeField] private StageRegistrySO _stageRegistry;

        [Header("UI")]
        [Tooltip("GameUIManager — 미연결 시 Console 로그로 자동 진행")]
        [SerializeField] private GameUIManager _gameUI;

        // ─── 하위 Manager 참조 ───────────────────────────────────
        // Awake에서 같은 GameObject에 AddComponent로 자동 생성됩니다.
        private StageManager _stageManager;
        private BlackjackManager _blackjackManager;
        private PlayerHandManager _playerHandManager;
        private DealerManager _dealerManager;
        private VisionManager _visionManager;
        private IDealerStrategy _dealerStrategy;
        private DeckRunner _deckRunner;

        // ─── 상태 ─────────────────────────────────────────────────
        private GameState _state;
        // hit 카운트
        private int _hitCount = 0;

        // 임시 보관용 리스트
        private readonly List<CardDataSO> _pendingOperators = new();

        // ─── 연출 지연 시간 ───────────────────────────────────────
        [Header("연출 지연 시간")]
        [Tooltip("버스트 후 결과 화면 전환까지 대기 시간 (초)")]
        [SerializeField] private float _bustDelay = 1.5f;

        [Tooltip("Stay 후 딜러 턴 전환까지 대기 시간 (초)")]
        [SerializeField] private float _standDelay = 1.0f;

        [Tooltip("딜러 턴 종료 후 결과 화면까지 대기 시간 (초)")]
        [SerializeField] private float _resultDelay = 1.5f;

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            // 하위 Manager를 같은 GameObject에 자동 생성합니다.
            // Inspector 연결 없이 코드로만 의존성을 구성합니다.
            _stageManager = gameObject.AddComponent<StageManager>();
            _blackjackManager = gameObject.AddComponent<BlackjackManager>();
            _playerHandManager = gameObject.AddComponent<PlayerHandManager>();
            _dealerManager = gameObject.AddComponent<DealerManager>();
            _visionManager = gameObject.AddComponent<VisionManager>();

            // StageRegistrySO를 StageManager에 주입합니다.
            _stageManager.Initialize(_stageRegistry);

            // 딜러 전략 초기화 — 교체 시 SetDealerStrategy() 사용
            _dealerStrategy = new DealerAI();
            _blackjackManager.Initialize(_dealerStrategy);

            // 하위 Manager 이벤트 구독
            // Manager들이 상태 변화를 이벤트로 알리면 GameManager가 처리합니다.
            _visionManager.OnVisionDepleted += OnVisionDepleted;
            _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;
            _playerHandManager.OnHandSelectionChanged += OnHandSelectionChanged;
            _playerHandManager.OnSlotHighlightRequested += OnSlotHighlightRequested;
            _dealerManager.OnDealerFieldChanged += OnDealerFieldChanged;
            _dealerManager.OnDealerTurnEnded += OnDealerTurnEnded;

        }

        private void Start() => TransitionTo(GameState.Title);

        private void OnDestroy()
        {
            // 씬 전환 또는 오브젝트 파괴 시 이벤트 구독을 해제합니다.
            // 해제하지 않으면 null 참조 에러가 발생할 수 있습니다.
            _visionManager.OnVisionDepleted -= OnVisionDepleted;
            _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
            _playerHandManager.OnHandSelectionChanged -= OnHandSelectionChanged;
            _playerHandManager.OnSlotHighlightRequested -= OnSlotHighlightRequested;
            _dealerManager.OnDealerFieldChanged -= OnDealerFieldChanged;
            _dealerManager.OnDealerTurnEnded -= OnDealerTurnEnded;

        }

        // ─── UI 프록시 ────────────────────────────────────────────
        // GameUIManager가 미연결 상태일 때 null 호출을 방지하는 래퍼입니다.
        // GameUIManager가 null이면 Debug.Log로 상태를 출력하고 자동 진행합니다.

        /// <summary>타이틀 화면을 표시합니다. GameUIManager 미연결 시 자동 진행합니다.</summary>
        private void UI_ShowTitle(System.Action onStart)
        {
            if (_gameUI != null) { _gameUI.ShowTitle(onStart); return; }
            Debug.Log("[GM] ShowTitle — 자동 진행");
            onStart?.Invoke();
        }

        /// <summary>배팅 화면을 표시합니다. 현재 스테이지 정보를 함께 전달합니다.</summary>
        private void UI_ShowBetting(int currentBet, System.Action<int> onConfirm)
        {
            if (_gameUI != null)
            {
                _gameUI.SetBetPanelInfo(_stageManager.CurrentStage.stageIndex, _stageManager.CurrentStage.bustValue);
                _gameUI.ShowBetting(currentBet, onConfirm);
                return;
            }
            Debug.Log($"[GM] ShowBetting — 자동 확정:{currentBet}");
            onConfirm?.Invoke(currentBet);
        }

        /// <summary>플레이어 턴 화면을 표시합니다. Hit / Stay 콜백을 등록합니다.</summary>
        private void UI_ShowPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.SetGamePanelInfo(_stageManager.CurrentStage.stageIndex, _stageManager.CurrentStage.bustValue);
                _gameUI.ShowPlayerTurn(_playerHandManager.Field, _playerHandManager.Hand, OnHit, OnStand);
                return;
            }
            Debug.Log("[GM] ShowPlayerTurn — 자동 Stand");
            OnStand();
        }

        /// <summary>딜러 대기 화면을 표시합니다.</summary>
        private void UI_ShowDealerThinking()
        {
            if (_gameUI != null) { _gameUI.ShowDealerThinking(); return; }
            Debug.Log("[GM] ShowDealerThinking");
        }

        /// <summary>
        /// 플레이어 필드와 손패 UI를 갱신합니다.
        /// 현재 연산값과 버스트 여부에 따른 색상도 함께 갱신합니다.
        /// </summary>
        private void UI_RefreshPlayerArea()
        {
            var stage = _stageManager.CurrentStage;
            if (_gameUI != null)
            {
                _gameUI.RefreshPlayerArea(_playerHandManager.Field, _playerHandManager.Hand);
                _gameUI.UpdateCurrentValue(_playerHandManager.Field, stage.bustValue, stage.useFlexibleAce,
                    stage.bustValue);
                return;
            }
            long val = _blackjackManager.EvaluatePlayer(_playerHandManager.Field, stage);
            Debug.Log($"[GM] RefreshPlayer — value:{val:N0}");
        }

        /// <summary>
        /// 딜러 필드 UI를 갱신합니다.
        /// 비공개 카드 보유 여부를 함께 전달해 뒷면 슬롯을 표시합니다.
        /// </summary>
        private void UI_RefreshDealerArea()
        {
            if (_gameUI != null)
            {
                _gameUI.RefreshDealerArea(
                    _dealerManager.Field,
                    _dealerManager.HasHiddenCard);
                return;
            }
            long val = _blackjackManager.EvaluateDealer(
                _dealerManager.Field, _stageManager.CurrentStage);
            Debug.Log($"[GM] RefreshDealer — value:{val:N0}");
        }

        /// <summary>
        /// 결과 화면을 표시합니다.
        /// 플레이어 최종 연산값과 할당량 비교 정보를 함께 전달합니다.
        /// </summary>
        private void UI_ShowResult(string desc, bool win, System.Action onNext)
        {
            if (_gameUI != null)
            {
                long playerTotal = _blackjackManager.EvaluatePlayer(
                    _playerHandManager.Field, _stageManager.CurrentStage);
                _gameUI.SetResultInfo(
                    playerTotal, _stageManager.CurrentStage.bustValue, win);
                _gameUI.ShowResult(desc, win, onNext);
                return;
            }
            Debug.Log($"[GM] ShowResult — {desc} win:{win}");
            onNext?.Invoke();
        }

        /// <summary>게임 오버 화면을 표시합니다.</summary>
        private void UI_ShowGameOver(System.Action onRestart)
        {
            if (_gameUI != null) { _gameUI.ShowGameOver(onRestart); return; }
            Debug.Log("[GM] ShowGameOver");
        }

        /// <summary>스테이지 전체 클리어 화면을 표시합니다.</summary>
        private void UI_ShowStageClear(System.Action onRestart)
        {
            if (_gameUI != null) { _gameUI.ShowStageClear(onRestart); return; }
            Debug.Log("[GM] ShowStageClear");
        }

        // ─── 상태 전환 ────────────────────────────────────────────

        /// <summary>
        /// 지정 상태로 전환하고 진입 로직을 실행합니다.
        /// DealerTurn은 코루틴으로 실행되며 완료 시 OnDealerTurnEnded 이벤트로 알립니다.
        /// </summary>
        public void TransitionTo(GameState next)
        {
            _state = next;
            switch (next)
            {
                case GameState.Title: OnEnterTitle(); break;
                case GameState.Betting: OnEnterBetting(); break;
                case GameState.PlayerTurn: OnEnterPlayerTurn(); break;
                case GameState.DealerTurn: StartCoroutine(_dealerManager.RunTurn(_deckRunner, 
                    _dealerStrategy, _stageManager.CurrentStage)); break;
                case GameState.Result: OnEnterResult(); break;
                case GameState.GameOver: OnEnterGameOver(); break;
                case GameState.StageClear: OnEnterStageClear(); break;
            }
        }

        // ─── 상태 진입 ────────────────────────────────────────────

        /// <summary>
        /// 타이틀 상태 진입.
        /// 스테이지와 시야를 초기화하고 타이틀 화면을 표시합니다.
        /// </summary>
        private void OnEnterTitle()
        {
            _stageManager.ResetAndLoad();
            _visionManager.ResetVision();
            UI_ShowTitle(OnStartGame);
        }

        /// <summary>
        /// 배팅 상태 진입.
        /// 최소 배팅량 미달 시 즉시 게임 오버로 전환합니다.
        /// </summary>
        private void OnEnterBetting()
        {
            if (!_visionManager.CanBet(_stageManager.CurrentStage.visionBetMin))
            {
                TransitionTo(GameState.GameOver);
                return;
            }
            UI_ShowBetting(_stageManager.CurrentStage.visionBetMin, OnConfirmBet);
        }

        /// <summary>
        /// 플레이어 턴 상태 진입.
        /// UI를 초기화한 뒤 표준 블랙잭 초기 딜링을 수행하고
        /// GameUIManager의 인터랙션 이벤트를 구독합니다.
        /// </summary>
        private void OnEnterPlayerTurn()
        {
            if (_dealerStrategy is DealerAI ai)
                ai.SetBustThreshold(_stageManager.CurrentStage.bustValue);

            // 재도전/다음 스테이지 진입 시 버튼 재활성화
            _gameUI.EnableGameButtons();

            // 패널 진입 시 딜러/플레이어 UI 초기화
            _gameUI?.RefreshDealerArea(_dealerManager.Field);
            _gameUI?.RefreshPlayerArea(
                _playerHandManager.Field,
                _playerHandManager.Hand);

            // 연산값 텍스트 초기화 (= 0 표시)
            _gameUI?.UpdateCurrentValue(
                _playerHandManager.Field,
                _stageManager.CurrentStage.bustValue,
                _stageManager.CurrentStage.useFlexibleAce,
                _stageManager.CurrentStage.bustValue);

            // 초기 딜링 — 플레이어2장 / 딜러1장 공개 + 1장 비공개
            DealInitialCards();

            // 손패 클릭 / 슬롯 클릭 이벤트 구독
            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked += _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorSlotClicked += OnOperatorSlotSelected;
            }

            UI_ShowPlayerTurn();
        }

        // ─── 초기 딜링 ────────────────────────────────────────────

        /// <summary>
        /// 표준 블랙잭 초기 딜링을 수행합니다.
        /// 플레이어 1장 → 딜러 1장(공개) → 플레이어 1장 → 딜러 1장(비공개) 순서입니다.
        /// 딜러 비공개 카드는 플레이어 Stand 후 RevealHiddenCard()로 공개됩니다.
        /// </summary>
        private void DealInitialCards()
        {
            if (_stageManager.CurrentStage.operatorOnlyHit)
            {
                DealAllNumberCards();
                return;
            }

            // 1. 플레이어 첫 번째 카드
            CardDataSO p1 = DrawNumberCard();
            if (p1 != null) _playerHandManager.AddNumberToField(p1);

            // 2. 딜러 공개 카드
            CardDataSO d1 = DrawNumberCard();
            if (d1 != null) _dealerManager.AddOpenCard(d1);

            // 3. 플레이어 두 번째 카드
            CardDataSO p2 = DrawNumberCard();
            if (p2 != null) _playerHandManager.AddNumberToField(p2);

            // 4. 딜러 비공개 카드 — UI에 뒷면으로 표시됨
            CardDataSO d2 = DrawNumberCard();
            if (d2 != null) _dealerManager.SetHiddenCard(d2);
        }
        /// <summary>
        /// operatorOnlyHit 스테이지 전용 초기 딜링.
        /// 덱의 숫자 카드를 전부 플레이어 필드에 지급하고
        /// 연산자 카드는 덱에 남겨둡니다.
        /// </summary>
        private void DealAllNumberCards()
        {
            // 덱에서 숫자 카드만 전부 꺼내 플레이어 필드에 지급
            while (_deckRunner.Remaining > 0)
            {
                if (!_deckRunner.TryDraw(out CardDataSO card)) break;

                if (card.cardType == CardType.Number)
                    _playerHandManager.AddNumberToField(card);
                else
                {
                    // 연산자 카드는 다시 덱에 넣어야 하는데
                    // Queue는 순서가 있으므로 별도 보관
                    _pendingOperators.Add(card);
                }
            }

            // 연산자 카드를 덱에 재삽입
            foreach (var op in _pendingOperators)
                _deckRunner.ReturnCard(op);
            _pendingOperators.Clear();

            Debug.Log($"[GM] DealAllNumberCards — " +
                      $"플레이어필드:{_playerHandManager.Field.Count} " +
                      $"연산자덱:{_deckRunner.Remaining}");
        }

        /// <summary>
        /// 숫자 카드가 나올 때까지 덱에서 드로우합니다.
        /// 연산자 카드는 건너뛰고 재드로우합니다.
        /// 덱이 소진되거나 최대 재시도 횟수 초과 시 null을 반환합니다.
        /// </summary>
        private CardDataSO DrawNumberCard(int maxRetry = 20)
        {
            for (int i = 0; i < maxRetry; i++)
            {
                if (!_deckRunner.TryDraw(out CardDataSO card))
                {
                    Debug.LogWarning("[GM] DrawNumberCard — 덱 소진");
                    return null;
                }

                if (card.cardType == CardType.Number)
                    return card;

                Debug.Log("[GM] DrawNumberCard — 연산자 카드 스킵");
            }

            Debug.LogWarning("[GM] DrawNumberCard — 최대 재시도 초과");
            return null;
        }

        /// <summary>
        /// 플레이어 턴 종료 시 이벤트 구독을 해제합니다.
        /// DealerTurn / Result 전환 전 반드시 호출해야 합니다.
        /// </summary>
        private void ExitPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked -= _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorSlotClicked -= OnOperatorSlotSelected;

                _gameUI.DisableGameButtons();
            }
            _playerHandManager.ClearSelection();
        }

        /// <summary>
        /// 결과 판정 상태 진입.
        /// BlackjackManager에 승패 판정을 위임하고
        /// VisionManager에 승패 결과를 전달합니다.
        /// </summary>
        private void OnEnterResult()
        {
            var stage = _stageManager.CurrentStage;
            long playerTotal = _blackjackManager.EvaluatePlayer(_playerHandManager.Field, stage);
            long dealerTotal = _blackjackManager.EvaluateDealer(_dealerManager.Field, stage);

            bool win = _blackjackManager.JudgeResult(playerTotal, dealerTotal, stage);
            string desc = _blackjackManager.BuildResultDescription(playerTotal, dealerTotal, stage, win);
            string exprStr = ExpressionEvaluator.ToExpressionString(_playerHandManager.Field);

            // 승패를 VisionManager에 위임 — 시야 차감/유지 처리
            if (win) _visionManager.WinBet();
            else _visionManager.LoseBet();

            Debug.Log($"[GM] Result — player:{playerTotal:N0} dealer:{dealerTotal:N0} win:{win}");

            System.Action onNext = win ? (System.Action)OnStageWin : OnStageLose;

            UI_ShowResult($"{exprStr} = {playerTotal:N0}", win, onNext);
        }

        /// <summary>게임 오버 상태 진입.</summary>
        private void OnEnterGameOver() => UI_ShowGameOver(OnRestartGame);

        /// <summary>전체 스테이지 클리어 상태 진입.</summary>
        private void OnEnterStageClear() => UI_ShowStageClear(OnRestartGame);

        // ─── 플레이어 액션 ────────────────────────────────────────

        /// <summary>
        /// Hit 버튼 입력 처리.
        /// DrawAndProcess()로 위임합니다.
        /// </summary>
        private void OnHit() => DrawAndProcess();

        /// <summary>
        /// 덱에서 카드를 드로우하고 숫자/연산자 여부에 따라 처리합니다.
        /// 숫자 카드 → 플레이어 필드에 추가, 버스트 감지 후 딜러 턴 전환.
        /// 연산자 카드 → Stage 2+는 손패에 추가, Stage 1은 재드로우.
        /// 무한 루프 방지를 위해 재드로우는 최대 10회로 제한합니다.
        /// </summary>
        private void DrawAndProcess(int retryCount = 0)
        {
            var stage = _stageManager.CurrentStage;

            if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
            {
                Debug.Log($"[GM] 최대 Hit 횟수 초과 ({stage.maxHitCount}회) — Stand 전환");
                OnStand();
                return;
            }

            if (retryCount > 10)
            {
                Debug.LogWarning("[GM] DrawAndProcess — 재드로우 한도 초과");
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                return;
            }

            if (!_deckRunner.TryDraw(out CardDataSO card))
            {
                Debug.Log("[GM] 덱 소진 — 딜러 턴 전환");
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                return;
            }

            if (card.cardType == CardType.Number)
            {
                // operatorOnlyHit 스테이지에서는 숫자 카드가 나오면 안 됨
                // 나왔다면 반환 후 재드로우
                if (stage.operatorOnlyHit)
                {
                    _deckRunner.ReturnCard(card);
                    Debug.Log("[GM] operatorOnlyHit — 숫자 카드 반환 후 재드로우");
                    DrawAndProcess(retryCount + 1);
                    return;
                }

                _hitCount++;
                // OnFieldChanged 이벤트 일시 차단
                _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
                _playerHandManager.AddNumberToField(card);

                bool bust = _blackjackManager.IsPlayerBust(_playerHandManager.Field, _stageManager.CurrentStage);

                Debug.Log($"[GM] Hit — total:{_blackjackManager.EvaluatePlayer(_playerHandManager.Field, _stageManager.CurrentStage)} " +
                          $"bustValue:{_stageManager.CurrentStage.bustValue} " +
                          $"bust:{bust}");

                if (bust)
                {
                    // UI 직접 갱신 후 종료
                    UI_RefreshPlayerArea();

                    // 이벤트 재구독 후 종료
                    _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;

                    Debug.Log("[GM] 플레이어 버스트 — Result 전환");
                    ExitPlayerTurn();
                    StartCoroutine(DelayedTransition(_bustDelay, GameState.Result));
                    return;
                }

                // 버스트 아닐 때 이벤트 재구독 후 UI 갱신
                _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;
                UI_RefreshPlayerArea();

                if(stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
                {
                    Debug.Log($"[GM] Hit 한도 도달 - Hit 버튼 비활성화");
                    _gameUI?.DisableGameButtons();
                }
            }
            else
            {
                if (stage.useOperatorCards || stage.operatorOnlyHit)
                {
                    _hitCount++;
                    _playerHandManager.AddOperatorToHand(card);

                    // 연산자 한도 체크
                    if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
                    {
                        Debug.Log("[GM] 연산자 Hit 한도 도달 — 버튼 비활성화");
                        _gameUI?.DisableGameButtons();
                    }
                }
                else
                {
                    Debug.Log("[GM] Stage1 연산자 카드 스킵 — 재드로우");
                    DrawAndProcess(retryCount + 1);
                }
            }

        }

        /// <summary>
        /// Stay 버튼 입력 처리.
        /// 필드가 비어있으면 Stand 불가. 딜러 턴으로 전환합니다.
        /// </summary>
        private void OnStand()
        {
            if (_playerHandManager.Field.Count == 0) return;

            // IsFinalBust 제거 — Stay 후 무조건 딜러 턴으로
            ExitPlayerTurn();
            StartCoroutine(DelayedTransition(_standDelay, GameState.DealerTurn));
        }

        /// <summary>
        /// 연산자 슬롯 클릭 처리.
        /// PlayerHandManager에 배치를 위임하고 성공 시 UI 슬롯을 갱신합니다.
        /// </summary>
        private void OnOperatorSlotSelected(int slotIndex)
        {
            if (_playerHandManager.TryPlaceOperator(slotIndex, out CardDataSO placed))
                _gameUI?.PlaceOperatorOnSlot(slotIndex, placed.operatorType);
        }

        /// <summary>
        /// 지정 시간 후 상태를 전환합니다.
        /// 버스트 / Stay / 결과 연출 지연에 사용합니다.
        /// </summary>
        private System.Collections.IEnumerator DelayedTransition(
            float delay, GameState next)
        {
            yield return new WaitForSeconds(delay);
            TransitionTo(next);
        }

        // ─── 이벤트 핸들러 ───────────────────────────────────────

        /// <summary>PlayerHandManager.OnFieldChanged 핸들러 — 플레이어 UI 갱신</summary>
        private void OnPlayerFieldChanged() => UI_RefreshPlayerArea();

        /// <summary>DealerManager.OnDealerFieldChanged 핸들러 — 딜러 UI 갱신</summary>
        private void OnDealerFieldChanged() => UI_RefreshDealerArea();

        /// <summary>DealerManager.OnDealerTurnEnded 핸들러 — Result 상태로 전환</summary>
        private void OnDealerTurnEnded() => StartCoroutine(DelayedTransition(_resultDelay, GameState.Result));

        /// <summary>VisionManager.OnVisionDepleted 핸들러 — 시야 소진 시 GameOver 전환</summary>
        private void OnVisionDepleted() => TransitionTo(GameState.GameOver);

        /// <summary>PlayerHandManager.OnHandSelectionChanged 핸들러 — 손패 하이라이트 갱신</summary>
        private void OnHandSelectionChanged(int index)
            => _gameUI?.HighlightHandCard(index);

        /// <summary>PlayerHandManager.OnSlotHighlightRequested 핸들러 — 슬롯 하이라이트 갱신</summary>
        private void OnSlotHighlightRequested(bool highlight)
            => _gameUI?.HighlightAvailableSlots(highlight);

        // ─── 플로우 ──────────────────────────────────────────────

        /// <summary>
        /// 타이틀에서 게임 시작.
        /// 스테이지를 초기화하고 첫 번째 Betting으로 진입합니다.
        /// </summary>
        private void OnStartGame()
        {
            _stageManager.ResetAndLoad();
            LoadStage();
            TransitionTo(GameState.Betting);
        }

        /// <summary>
        /// 배팅 확정.
        /// VisionManager에 배팅량을 설정하고 PlayerTurn으로 진입합니다.
        /// </summary>
        private void OnConfirmBet(int betAmount)
        {
            _visionManager.SetBet(betAmount);
            TransitionTo(GameState.PlayerTurn);
        }

        /// <summary>
        /// 스테이지 승리 처리.
        /// 마지막 스테이지면 StageClear, 아니면 다음 스테이지 Betting으로 진입합니다.
        /// </summary>
        private void OnStageWin()
        {
            if (_stageManager.IsLastStage)
            {
                TransitionTo(GameState.StageClear);
                return;
            }
            _stageManager.LoadNext();
            LoadStage();
            TransitionTo(GameState.Betting);
        }

        /// <summary>
        /// 스테이지 패배 처리.
        /// 시야가 남아있으면 같은 스테이지를 재도전합니다.
        /// 시야가 소진되면 GameOver로 전환합니다.
        /// </summary>
        private void OnStageLose()
        {
            if (_visionManager.IsBlind())
            {
                TransitionTo(GameState.GameOver);
                return;
            }
            _stageManager.ReloadCurrent();
            LoadStage();
            TransitionTo(GameState.Betting);
        }

        /// <summary>
        /// 게임 재시작.
        /// 스테이지를 초기화하고 타이틀로 돌아갑니다.
        /// </summary>
        private void OnRestartGame()
        {
            _stageManager.ResetAndLoad();
            TransitionTo(GameState.Title);
        }

        /// <summary>
        /// 현재 스테이지를 로드합니다.
        /// 플레이어/딜러 필드를 초기화하고 새 DeckRunner를 생성합니다.
        /// SO 에셋은 변경하지 않으며 매 라운드 새 Queue를 생성합니다.
        /// </summary>
        private void LoadStage()
        {
            _hitCount = 0;

            _playerHandManager.ResetAll();
            _dealerManager.ResetField();
            _deckRunner = new DeckRunner(
                _stageManager.CurrentStage.deck,
                _stageManager.CurrentStage);

            Debug.Log($"[GM] LoadStage — " +
                      $"Stage {_stageManager.CurrentStage.stageIndex} " +
                      $"딜러필드:{_dealerManager.Field.Count}"); // 0이어야 함
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>
        /// 딜러 전략을 런타임에 교체합니다.
        /// DealerAI → BlindJackDealerAI 등으로 교체 시 사용합니다.
        /// </summary>
        public void SetDealerStrategy(IDealerStrategy strategy)
        {
            _dealerStrategy = strategy;
            _blackjackManager.SetDealerStrategy(strategy);
        }
    }
}