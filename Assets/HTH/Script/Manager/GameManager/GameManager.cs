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
    /// 게임 상태 머신과 UI 프록시를 담당합니다.
    /// 게임 로직은 하위 Manager에 위임하고
    /// 상태 전환과 UI 호출만 처리합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("데이터")]
        [SerializeField] private StageRegistrySO _stageRegistry;

        [Header("UI")]
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

        // ─── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            // 같은 GameObject에 컴포넌트 추가
            _stageManager = gameObject.AddComponent<StageManager>();
            _blackjackManager = gameObject.AddComponent<BlackjackManager>();
            _playerHandManager = gameObject.AddComponent<PlayerHandManager>();
            _dealerManager = gameObject.AddComponent<DealerManager>();
            _visionManager = gameObject.AddComponent<VisionManager>();

            _stageManager.Initialize(_stageRegistry);

            _dealerStrategy = new DealerAI();
            _blackjackManager.Initialize(_dealerStrategy);

            // 이벤트 구독
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
            _visionManager.OnVisionDepleted -= OnVisionDepleted;
            _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
            _playerHandManager.OnHandSelectionChanged -= OnHandSelectionChanged;
            _playerHandManager.OnSlotHighlightRequested -= OnSlotHighlightRequested;
            _dealerManager.OnDealerFieldChanged -= OnDealerFieldChanged;
            _dealerManager.OnDealerTurnEnded -= OnDealerTurnEnded;
        }

        // ─── UI 프록시 ────────────────────────────────────────────

        private void UI_ShowTitle(System.Action onStart)
        {
            if (_gameUI != null) { _gameUI.ShowTitle(onStart); return; }
            Debug.Log("[GM] ShowTitle — 자동 진행");
            onStart?.Invoke();
        }

        private void UI_ShowBetting(int currentBet, System.Action<int> onConfirm)
        {
            if (_gameUI != null)
            {
                _gameUI.SetBetPanelInfo(
                    _stageManager.CurrentStage.stageIndex,
                    _stageManager.CurrentStage.quota);
                _gameUI.ShowBetting(currentBet, onConfirm);
                return;
            }
            Debug.Log($"[GM] ShowBetting — 자동 확정:{currentBet}");
            onConfirm?.Invoke(currentBet);
        }

        private void UI_ShowPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.SetGamePanelInfo(
                    _stageManager.CurrentStage.stageIndex,
                    _stageManager.CurrentStage.quota);
                _gameUI.ShowPlayerTurn(
                    _playerHandManager.Field,
                    _playerHandManager.Hand,
                    OnHit, OnStand);
                return;
            }
            Debug.Log("[GM] ShowPlayerTurn — 자동 Stand");
            OnStand();
        }

        private void UI_ShowDealerThinking()
        {
            if (_gameUI != null) { _gameUI.ShowDealerThinking(); return; }
            Debug.Log("[GM] ShowDealerThinking");
        }

        private void UI_RefreshPlayerArea()
        {
            var stage = _stageManager.CurrentStage;
            if (_gameUI != null)
            {
                _gameUI.RefreshPlayerArea(
                    _playerHandManager.Field,
                    _playerHandManager.Hand);
                _gameUI.UpdateCurrentValue(
                    _playerHandManager.Field,
                    stage.quota,
                    stage.useFlexibleAce,
                    stage.bustThreshold);
                return;
            }
            long val = _blackjackManager.EvaluatePlayer(
                _playerHandManager.Field, stage);
            Debug.Log($"[GM] RefreshPlayer — value:{val:N0}");
        }

        private void UI_RefreshDealerArea()
        {
            if (_gameUI != null)
            {
                _gameUI.RefreshDealerArea(_dealerManager.Field);
                return;
            }
            long val = _blackjackManager.EvaluateDealer(
                _dealerManager.Field, _stageManager.CurrentStage);
            Debug.Log($"[GM] RefreshDealer — value:{val:N0}");
        }

        private void UI_ShowResult(string desc, bool win, System.Action onNext)
        {
            if (_gameUI != null)
            {
                long playerTotal = _blackjackManager.EvaluatePlayer(
                    _playerHandManager.Field, _stageManager.CurrentStage);
                _gameUI.SetResultInfo(
                    playerTotal, _stageManager.CurrentStage.quota, win);
                _gameUI.ShowResult(desc, win, onNext);
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

        /// <summary>지정 상태로 전환하고 진입 로직을 실행합니다.</summary>
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
                ai.SetBustThreshold(_stageManager.CurrentStage.bustThreshold);

            //패널 진입 시 딜러/플레이어 UI 초기화
            _gameUI?.RefreshDealerArea(_dealerManager.Field);   // 빈 리스트 전달 → UI 클리어
            _gameUI?.RefreshPlayerArea(
                _playerHandManager.Field,
                _playerHandManager.Hand);

            //연산값 텍스트 초기화
            _gameUI?.UpdateCurrentValue(
                _playerHandManager.Field,   // 빈 리스트
                _stageManager.CurrentStage.quota,
                _stageManager.CurrentStage.useFlexibleAce,
                _stageManager.CurrentStage.bustThreshold);

            // GameUIManager 이벤트 구독
            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked += _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorSlotClicked += OnOperatorSlotSelected;
            }

            UI_ShowPlayerTurn();
        }

        private void ExitPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked -= _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorSlotClicked -= OnOperatorSlotSelected;
            }
            _playerHandManager.ClearSelection();
        }

        private void OnEnterResult()
        {
            var stage = _stageManager.CurrentStage;
            long playerTotal = _blackjackManager.EvaluatePlayer(
                _playerHandManager.Field, stage);
            long dealerTotal = _blackjackManager.EvaluateDealer(
                _dealerManager.Field, stage);

            bool win = _blackjackManager.JudgeResult(playerTotal, dealerTotal, stage);
            string desc = _blackjackManager.BuildResultDescription(
                playerTotal, dealerTotal, stage, win);
            string exprStr = ExpressionEvaluator.ToExpressionString(
                _playerHandManager.Field);

            if (win) _visionManager.WinBet();
            else _visionManager.LoseBet();

            Debug.Log($"[GM] Result — player:{playerTotal:N0} " +
                      $"dealer:{dealerTotal:N0} win:{win}");

            System.Action onNext = win
                ? (System.Action)OnStageWin
                : OnStageLose;

            UI_ShowResult($"{exprStr} = {playerTotal:N0}", win, onNext);
        }

        private void OnEnterGameOver() => UI_ShowGameOver(OnRestartGame);
        private void OnEnterStageClear() => UI_ShowStageClear(OnRestartGame);

        // ─── 플레이어 액션 ────────────────────────────────────────

        private void OnHit()
        {
            if (!_deckRunner.TryDraw(out CardDataSO card))
            {
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                return;
            }

            if (card.cardType == CardType.Number)
            {
                _playerHandManager.AddNumberToField(card);

                if (_blackjackManager.IsPlayerBust(
                    _playerHandManager.Field, _stageManager.CurrentStage))
                {
                    UI_RefreshPlayerArea();
                    ExitPlayerTurn();
                    TransitionTo(GameState.DealerTurn);
                    return;
                }
            }
            else
            {
                if (_stageManager.CurrentStage.useOperatorCards)
                    _playerHandManager.AddOperatorToHand(card);
                else
                {
                    // Stage 1 — 연산자 카드 스킵, 재드로우
                    OnHit();
                    return;
                }
            }
        }

        private void OnStand()
        {
            if (_playerHandManager.Field.Count == 0) return;
            ExitPlayerTurn();
            TransitionTo(GameState.DealerTurn);
        }

        private void OnOperatorSlotSelected(int slotIndex)
        {
            if (_playerHandManager.TryPlaceOperator(slotIndex, out CardDataSO placed))
                _gameUI?.PlaceOperatorOnSlot(slotIndex, placed.operatorType);
        }

        // ─── 이벤트 핸들러 ───────────────────────────────────────

        private void OnPlayerFieldChanged() => UI_RefreshPlayerArea();
        private void OnDealerFieldChanged() => UI_RefreshDealerArea();
        private void OnDealerTurnEnded() => TransitionTo(GameState.Result);
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
            _playerHandManager.ResetAll();
            _dealerManager.ResetField();
            _deckRunner = new DeckRunner(
                _stageManager.CurrentStage.deck,
                _stageManager.CurrentStage);

            Debug.Log($"[GM] LoadStage — " +
              $"Stage {_stageManager.CurrentStage.stageIndex} " +
              $"딜러필드:{_dealerManager.Field.Count}");  // ← 0이어야 함
        }

        // ─── 공개 API ─────────────────────────────────────────────

        /// <summary>딜러 전략을 런타임에 교체합니다.</summary>
        public void SetDealerStrategy(IDealerStrategy strategy)
        {
            _dealerStrategy = strategy;
            _blackjackManager.SetDealerStrategy(strategy);
        }
    }
}