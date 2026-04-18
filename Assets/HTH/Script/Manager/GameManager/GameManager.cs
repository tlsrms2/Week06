using System.Collections.Generic;
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
        private StageManager _stageManager;
        private BlackjackManager _blackjackManager;
        private PlayerHandManager _playerHandManager;
        private DealerManager _dealerManager;
        private VisionManager _visionManager;
        private IDealerStrategy _dealerStrategy;
        private DeckRunner _deckRunner;

        // ─── 상태 ─────────────────────────────────────────────────
        private GameState _state;
        private int _hitCount = 0;

        // operatorOnlyHit 스테이지 — 연산자 카드 임시 보관용
        private readonly List<DeckSO.CardEntry> _pendingOperators = new();

        // ─── 연출 지연 시간 ───────────────────────────────────────
        [Header("연출 지연 시간")]
        [SerializeField] private float _bustDelay = 1.5f;
        [SerializeField] private float _standDelay = 1.0f;
        [SerializeField] private float _resultDelay = 1.5f;

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            _stageManager = gameObject.AddComponent<StageManager>();
            _blackjackManager = gameObject.AddComponent<BlackjackManager>();
            _playerHandManager = gameObject.AddComponent<PlayerHandManager>();
            _dealerManager = gameObject.AddComponent<DealerManager>();
            _visionManager = gameObject.AddComponent<VisionManager>();

            _stageManager.Initialize(_stageRegistry);

            _dealerStrategy = new DealerAI();
            _blackjackManager.Initialize(_dealerStrategy);

            _visionManager.OnVisionDepleted += OnVisionDepleted;
            _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;
            _playerHandManager.OnHandChanged += OnPlayerHandChanged;
            _playerHandManager.OnHandSelectionChanged += OnHandSelectionChanged;
            _playerHandManager.OnSlotHighlightRequested += OnSlotHighlightRequested;
            _dealerManager.OnDealerCardAdded += OnDealerCardAdded;
            _dealerManager.OnDealerTurnEnded += OnDealerTurnEnded;
            _dealerManager.OnDealerCardRevealed += OnDealerCardRevealed;
        }

        private void Start() => TransitionTo(GameState.Title);

        private void OnDestroy()
        {
            _visionManager.OnVisionDepleted -= OnVisionDepleted;
            _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
            _playerHandManager.OnHandChanged -= OnPlayerHandChanged;
            _playerHandManager.OnHandSelectionChanged -= OnHandSelectionChanged;
            _playerHandManager.OnSlotHighlightRequested -= OnSlotHighlightRequested;
            _dealerManager.OnDealerCardAdded -= OnDealerCardAdded;
            _dealerManager.OnDealerTurnEnded -= OnDealerTurnEnded;
            _dealerManager.OnDealerCardRevealed -= OnDealerCardRevealed;
        }

        // ─── UI 프록시 ────────────────────────────────────────────

        private void UI_ShowTitle(System.Action onStart)
        {
            Debug.Log("[GM] ShowTitle — 자동 진행");
            onStart?.Invoke();
        }

        private void UI_ShowBetting(int currentBet, System.Action<int> onConfirm)
        {
            if (_gameUI != null)
            {
                _gameUI.SetupBetting(
                    _stageManager.CurrentStage.visionBetMin,
                    _visionManager.CurrentVision,
                    onConfirm);
                return;
            }
            Debug.Log($"[GM] ShowBetting — 자동 확정:{currentBet}");
            onConfirm?.Invoke(currentBet);
        }

        private void UI_ShowPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.SetStageInfo(
                    _stageManager.CurrentStage.stageIndex,
                    _stageManager.CurrentStage.bustValue);
                _gameUI.SetupGameButtons(OnHit, OnStand);
                return;
            }
            Debug.Log("[GM] ShowPlayerTurn — 자동 Stand");
            OnStand();
        }

        private void UI_ShowResult(string desc, bool win, System.Action onNext)
        {
            if (_gameUI != null)
            {
                long playerTotal = _blackjackManager.EvaluatePlayer(
                    _playerHandManager.FieldData, _stageManager.CurrentStage);
                long dealerTotal = _blackjackManager.EvaluateDealer(
                    _dealerManager.FieldData, _stageManager.CurrentStage);

                _gameUI.ShowResult(
                    desc, win,
                    playerTotal, dealerTotal,
                    _stageManager.CurrentStage.bustValue,
                    onNext);
                return;
            }
            Debug.Log($"[GM] ShowResult — {desc} win:{win}");
            onNext?.Invoke();
        }

        private void UI_ShowGameOver(System.Action onRestart)
        {
            if (_gameUI != null) { _gameUI.ShowGameOver(onRestart); return; }
            Debug.Log("[GM] ShowGameOver");
        }

        private void UI_ShowStageClear(System.Action onRestart)
        {
            if (_gameUI != null) { _gameUI.ShowStageClear(onRestart); return; }
            Debug.Log("[GM] ShowStageClear");
        }

        // ─── 상태 전환 ────────────────────────────────────────────

        public void TransitionTo(GameState next)
        {
            _state = next;
            switch (next)
            {
                case GameState.Title: OnEnterTitle(); break;
                case GameState.Betting: OnEnterBetting(); break;
                case GameState.PlayerTurn: OnEnterPlayerTurn(); break;
                case GameState.DealerTurn:
                    StartCoroutine(_dealerManager.RunTurn(
                        _deckRunner, _dealerStrategy,
                        _stageManager.CurrentStage)); break;
                case GameState.Result: OnEnterResult(); break;
                case GameState.GameOver: OnEnterGameOver(); break;
                case GameState.StageClear: OnEnterStageClear(); break;
            }
        }

        // ─── 상태 진입 ────────────────────────────────────────────

        private void OnEnterTitle()
        {
            _stageManager.ResetAndLoad();
            _visionManager.ResetVision();
            UI_ShowTitle(OnStartGame);
        }

        private void OnEnterBetting()
        {
            if (!_visionManager.CanBet(_stageManager.CurrentStage.visionBetMin))
            {
                TransitionTo(GameState.GameOver);
                return;
            }
            UI_ShowBetting(_stageManager.CurrentStage.visionBetMin, OnConfirmBet);
        }

        private void OnEnterPlayerTurn()
        {
            if (_dealerStrategy is DealerAI ai)
                ai.SetBustThreshold(_stageManager.CurrentStage.bustValue);

            _gameUI?.EnableGameButtons();
            _gameUI?.ClearDealerValue();

            // ← 변경 — Refresh 대신 ClearAllFields 후 초기 딜링
            // 초기 딜링에서 AddPlayerFieldCard / AddDealerFieldCard가 호출됨
            _gameUI?.ClearAllFields();

            DealInitialCards();

            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked += _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorSlotClicked += OnOperatorSlotSelected;
            }

            UI_ShowPlayerTurn();
        }

        // ─── 초기 딜링 ────────────────────────────────────────────

        private void DealInitialCards()
        {
            if (_stageManager.CurrentStage.operatorOnlyHit)
            {
                DealAllNumberCards();
                return;
            }

            if (_deckRunner.TryDraw(out DeckSO.CardEntry p1))
                _playerHandManager.AddNumberToField(p1);

            if (_deckRunner.TryDraw(out DeckSO.CardEntry d1))
                _dealerManager.AddOpenCard(d1);

            if (_deckRunner.TryDraw(out DeckSO.CardEntry p2))
                _playerHandManager.AddNumberToField(p2);

            if (_deckRunner.TryDraw(out DeckSO.CardEntry d2))
                _dealerManager.SetHiddenCard(d2);
        }

        private void DealAllNumberCards()
        {
            while (_deckRunner.Remaining > 0)
            {
                if (!_deckRunner.TryDraw(out DeckSO.CardEntry entry)) break;

                if (entry.data.cardType == CardType.Number)
                    _playerHandManager.AddNumberToField(entry);
                else
                    _pendingOperators.Add(entry);
            }

            foreach (var op in _pendingOperators)
                _deckRunner.ReturnCard(op);
            _pendingOperators.Clear();
        }

        private DeckSO.CardEntry DrawNumberCard(int maxRetry = 20)
        {
            for (int i = 0; i < maxRetry; i++)
            {
                if (!_deckRunner.TryDraw(out DeckSO.CardEntry entry))
                {
                    Debug.LogWarning("[GM] DrawNumberCard — 덱 소진");
                    return null;
                }
                if (entry.data.cardType == CardType.Number)
                    return entry;
            }
            return null;
        }

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

        private void OnEnterResult()
        {
            var stage = _stageManager.CurrentStage;
            long playerTotal = _blackjackManager.EvaluatePlayer(
                _playerHandManager.FieldData, stage);
            long dealerTotal = _blackjackManager.EvaluateDealer(
                _dealerManager.FieldData, stage);

            bool win = _blackjackManager.JudgeResult(playerTotal, dealerTotal, stage);
            string desc = _blackjackManager.BuildResultDescription(
                playerTotal, dealerTotal, stage, win);
            string exprStr = ExpressionEvaluator.ToExpressionString(
                _playerHandManager.FieldData);

            if (win) _visionManager.WinBet();
            else _visionManager.LoseBet();

            Debug.Log($"[GM] Result — player:{playerTotal:N0} dealer:{dealerTotal:N0} win:{win}");

            System.Action onNext = win
                ? (System.Action)OnStageWin
                : OnStageLose;

            UI_ShowResult($"{exprStr} = {playerTotal:N0}", win, onNext);
        }

        private void OnEnterGameOver() => UI_ShowGameOver(OnRestartGame);
        private void OnEnterStageClear() => UI_ShowStageClear(OnRestartGame);

        // ─── 플레이어 액션 ────────────────────────────────────────

        private void OnHit() => DrawAndProcess();

        private void DrawAndProcess(int retryCount = 0)
        {
            var stage = _stageManager.CurrentStage;

            if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
            {
                Debug.Log("[GM] 최대 Hit 횟수 도달 — Stand 전환");
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

            if (!_deckRunner.TryDraw(out DeckSO.CardEntry entry))
            {
                Debug.Log("[GM] 덱 소진 — 딜러 턴 전환");
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                return;
            }

            if (entry.data.cardType == CardType.Number)
            {
                if (stage.operatorOnlyHit)
                {
                    _deckRunner.ReturnCard(entry);
                    DrawAndProcess(retryCount + 1);
                    return;
                }

                _hitCount++;

                // OnFieldChanged 이벤트 일시 차단
                _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
                _playerHandManager.AddNumberToField(entry);

                _gameUI?.AddPlayerFieldCard(entry);

                bool bust = _blackjackManager.IsPlayerBust(_playerHandManager.FieldData, stage);

                Debug.Log($"[GM] Hit — " +
                          $"total:{_blackjackManager.EvaluatePlayer(_playerHandManager.FieldData, stage)} " +
                          $"bust:{bust}");

                if (bust)
                {
                    // 버스트 — HUD 값 갱신 후 Result 전환
                    _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, stage);
                    _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;
                    ExitPlayerTurn();
                    StartCoroutine(DelayedTransition(_bustDelay, GameState.Result));
                    return;
                }

                _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;
                _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, stage);

                if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
                    _gameUI?.DisableGameButtons();
            }
            else
            {
                if (stage.useOperatorCards || stage.operatorOnlyHit)
                {
                    _hitCount++;
                    _playerHandManager.AddOperatorToHand(entry);

                    if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
                        _gameUI?.DisableGameButtons();
                }
                else
                {
                    DrawAndProcess(retryCount + 1);
                }
            }
        }

        private void OnStand()
        {
            if (_playerHandManager.Field.Count == 0) return;

            if (_blackjackManager.IsFinalBust(
                _playerHandManager.FieldData, _stageManager.CurrentStage))
            {
                _gameUI?.UpdatePlayerValue(
                    _playerHandManager.FieldData, _stageManager.CurrentStage);
                ExitPlayerTurn();
                StartCoroutine(DelayedTransition(_bustDelay, GameState.Result));
                return;
            }

            ExitPlayerTurn();
            StartCoroutine(DelayedTransition(_standDelay, GameState.DealerTurn));
        }

        private void OnOperatorSlotSelected(int slotIndex)
        {
            if (_playerHandManager.TryPlaceOperator(slotIndex, out DeckSO.CardEntry placed))
                _gameUI?.PlaceOperatorOnSlot(slotIndex, placed.data.operatorType);
        }

        private System.Collections.IEnumerator DelayedTransition(
            float delay, GameState next)
        {
            yield return new WaitForSeconds(delay);
            TransitionTo(next);
        }

        // ─── 이벤트 핸들러 ───────────────────────────────────────

        /// <summary>
        /// 플레이어 필드에 새 숫자 카드가 추가됐을 때 호출됩니다.
        /// 마지막으로 추가된 카드 1장만 UI에 추가합니다.
        /// </summary>
        private void OnPlayerFieldChanged()
        {
            if (_playerHandManager.Field.Count == 0) return;

            // 마지막으로 추가된 카드만 UI에 추가
            var last = _playerHandManager.Field[_playerHandManager.Field.Count - 1];
            _gameUI?.AddPlayerFieldCard(last);
            _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, _stageManager.CurrentStage);
        }

        /// <summary>
        /// 플레이어 손패에 새 연산자 카드가 추가됐을 때 호출됩니다.
        /// 마지막으로 추가된 카드 1장만 UI에 추가합니다.
        /// </summary>
        private void OnPlayerHandChanged()
        {
            if (_playerHandManager.Hand.Count == 0) return;

            var last = _playerHandManager.Hand[_playerHandManager.Hand.Count - 1];
            _gameUI?.AddHandCard(last);
        }

        /// <summary>
        /// 딜러 필드에 새 카드가 추가됐을 때 호출됩니다.
        /// 마지막으로 추가된 카드 1장만 UI에 추가합니다.
        /// </summary>
        private void OnDealerCardAdded(DeckSO.CardEntry entry, bool isHidden)
        {
            _gameUI?.AddDealerFieldCard(entry, isHidden);
        }

        private void OnDealerCardRevealed()
        {
            _gameUI?.RevealDealerHiddenCard(); // ← 기존 카드 뒤집기
        }

        private void OnDealerTurnEnded()
        {
            _gameUI?.UpdateDealerValue(
                _dealerManager.FieldData,
                _stageManager.CurrentStage);
            StartCoroutine(DelayedTransition(_resultDelay, GameState.Result));
        }

        private void OnVisionDepleted() => TransitionTo(GameState.GameOver);

        private void OnHandSelectionChanged(int index)
            => _gameUI?.HighlightHandCard(index);

        private void OnSlotHighlightRequested(bool highlight)
            => _gameUI?.HighlightAvailableSlots(highlight);

        // ─── 플로우 ──────────────────────────────────────────────

        private void OnStartGame()
        {
            _stageManager.ResetAndLoad();
            LoadStage();
            TransitionTo(GameState.Betting);
        }

        private void OnConfirmBet(int betAmount)
        {
            _visionManager.SetBet(betAmount);
            TransitionTo(GameState.PlayerTurn);
        }

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

        private void OnRestartGame()
        {
            _stageManager.ResetAndLoad();
            TransitionTo(GameState.Title);
        }

        private void LoadStage()
        {
            _hitCount = 0;
            _playerHandManager.ResetAll();
            _dealerManager.ResetField();
            _deckRunner = new DeckRunner(
                _stageManager.CurrentStage.deck,
                _stageManager.CurrentStage);

            Debug.Log($"[GM] LoadStage — Stage {_stageManager.CurrentStage.stageIndex}");
        }

        // ─── 공개 API ─────────────────────────────────────────────

        public void SetDealerStrategy(IDealerStrategy strategy)
        {
            _dealerStrategy = strategy;
            _blackjackManager.SetDealerStrategy(strategy);
        }
    }
}