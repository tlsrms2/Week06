using System.Collections;
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
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("데이터")]
        [Tooltip("전체 스테이지 목록 SO")]
        [SerializeField] private StageRegistrySO _stageRegistry;

        [Header("UI")]
        [Tooltip("GameUIManager")]
        [SerializeField] private GameUIManager _gameUI;

        // ─── 하위 Manager 참조 ───────────────────────────────────
        private StageManager _stageManager;
        private BlackjackManager _blackjackManager;
        private PlayerHandManager _playerHandManager;
        private DealerManager _dealerManager;
        private VisionManager _visionManager;
        private EyeSpawnManager _eyeSpawnManager;
        private IDealerStrategy _dealerStrategy;
        private DeckRunner _deckRunner;

        // ─── 상태 ─────────────────────────────────────────────────
        private GameState _state;
        private int _hitCount = 0;
        private bool _isProcessingHit = false; // 중복 드로우 방지 플래그

        private readonly List<DeckSO.CardEntry> _pendingOperators = new();

        [Header("연출 지연 시간")]
        [SerializeField] private float _dealInterval = 0.5f;
        [SerializeField] private float _hitCooldown = 1f;
        [SerializeField] private float _bustDelay = 1.5f;
        [SerializeField] private float _standDelay = 1.0f;
        [SerializeField] private float _resultDelay = 1.5f;

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            _stageManager = Object.FindAnyObjectByType<StageManager>();
            if (_stageManager == null) _stageManager = gameObject.AddComponent<StageManager>();

            _blackjackManager = Object.FindAnyObjectByType<BlackjackManager>();
            if (_blackjackManager == null) _blackjackManager = gameObject.AddComponent<BlackjackManager>();

            _playerHandManager = Object.FindAnyObjectByType<PlayerHandManager>();
            if (_playerHandManager == null) _playerHandManager = gameObject.AddComponent<PlayerHandManager>();

            _dealerManager = Object.FindAnyObjectByType<DealerManager>();
            if (_dealerManager == null) _dealerManager = gameObject.AddComponent<DealerManager>();
            
            _visionManager = Object.FindAnyObjectByType<VisionManager>();
            if (_visionManager == null) _visionManager = gameObject.AddComponent<VisionManager>();

            _eyeSpawnManager = Object.FindAnyObjectByType<EyeSpawnManager>();

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
            _dealerManager.OnDealerOperatorPlaced += OnDealerOperatorPlaced;
        }

        private void Start() => TransitionTo(GameState.Title);

        private float _lastHitTime = -1f;

        private void OnDestroy()
        {
            if (_visionManager != null) _visionManager.OnVisionDepleted -= OnVisionDepleted;
            if (_playerHandManager != null)
            {
                _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
                _playerHandManager.OnHandChanged -= OnPlayerHandChanged;
                _playerHandManager.OnHandSelectionChanged -= OnHandSelectionChanged;
                _playerHandManager.OnSlotHighlightRequested -= OnSlotHighlightRequested;
            }
            if (_dealerManager != null)
            {
                _dealerManager.OnDealerCardAdded -= OnDealerCardAdded;
                _dealerManager.OnDealerTurnEnded -= OnDealerTurnEnded;
                _dealerManager.OnDealerCardRevealed -= OnDealerCardRevealed;
                _dealerManager.OnDealerOperatorPlaced -= OnDealerOperatorPlaced;
            }
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
                case GameState.DealerTurn: StartCoroutine(_dealerManager.RunTurn(_deckRunner, _dealerStrategy, _stageManager.CurrentStage)); break;
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
            var stage = _stageManager.CurrentStage;
            if (!_visionManager.CanBet(stage.visionBetMin))
            {
                TransitionTo(GameState.GameOver);
                return;
            }
            
            // 스테이지의 최대 베팅 제한과 현재 시야 중 작은 값을 한도로 설정
            int betMax = Mathf.Min(stage.visionBetMax, _visionManager.CurrentVision);
            UI_ShowBetting(stage.visionBetMin, betMax, OnConfirmBet);
        }

        private void OnEnterPlayerTurn()
        {
            if (_dealerStrategy is DealerAI ai)
                ai.SetBustThreshold(_stageManager.CurrentStage.bustValue);

            _gameUI?.DisableGameButtons();
            _gameUI?.ClearDealerValue();
            _gameUI?.ClearPlayerValue();
            _gameUI?.ClearAllFields();

            _lastHitTime = -1f;
            StartCoroutine(DealInitialAndEnable());

            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked += _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorDropped += OnOperatorDropped;
            }

            UI_ShowPlayerTurn();
        }

        private IEnumerator DealInitialAndEnable()
        {
            yield return StartCoroutine(DealInitialCardsRoutine());
            _gameUI?.EnableGameButtons();
        }

        private void OnEnterResult()
        {
            var stage = _stageManager.CurrentStage;
            long playerTotal = _blackjackManager.EvaluatePlayer(_playerHandManager.FieldData, stage);
            long dealerTotal = _blackjackManager.EvaluateDealer(_dealerManager.FieldData, stage);

            bool win = _blackjackManager.JudgeResult(playerTotal, dealerTotal, stage);

            if (win) _visionManager.WinBet();
            else _visionManager.LoseBet();

            if (win)
            {
                if (_eyeSpawnManager != null)
                {
                    _eyeSpawnManager.OnWin(() => OnStageWin());
                }
                else
                {
                    OnStageWin();
                }
            }
            else
            {
                if (_eyeSpawnManager != null)
                {
                    _eyeSpawnManager.OnLose(() => OnStageLose());
                }
                else
                {
                    OnStageLose();
                }
            }
        }

        private void OnEnterGameOver() => UI_ShowGameOver(OnRestartGame);
        private void OnEnterStageClear()
        {
            if (SceneLoadManager.Instance != null) SceneLoadManager.Instance.LoadEnding();
            else UI_ShowStageClear(OnRestartGame);
        }

        // ─── UI 호출 관련 ──────────────────────────────────────────

        private void UI_ShowTitle(System.Action onStart)
        {
            if (_gameUI != null) _gameUI.SetupTitle(onStart);
            else onStart?.Invoke();
        }

        private void UI_ShowBetting(int min, int max, System.Action<int> onConfirm)
        {
            if (_gameUI != null)
            {
                var stage = _stageManager.CurrentStage;
                // 배팅 패널이 뜨기 전에 현재 스테이지 번호와 목표 수치를 HUD에 먼저 반영
                _gameUI.SetStageInfo(stage.stageIndex, stage.bustValue);
                // 베팅 매니저에게 필요한 정보(시야, 스테이지 정보 등)를 모두 전달
                _gameUI.SetupBetting(min, max, _visionManager.CurrentVision, stage.stageIndex, stage.bustValue, onConfirm);
            }
            else onConfirm?.Invoke(min);
        }

        private void UI_ShowPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.SetStageInfo(_stageManager.CurrentStage.stageIndex, _stageManager.CurrentStage.bustValue);
                _gameUI.SetupGameButtons(OnHit, OnStand);
                _gameUI.EnableHandCardDrag();
            }
        }

        private void UI_ShowGameOver(System.Action onRestart) => _gameUI?.ShowGameOver(onRestart);
        private void UI_ShowStageClear(System.Action onRestart) => _gameUI?.ShowStageClear(onRestart);

        // ─── 플레이어 액션 ────────────────────────────────────────

        private void OnHit()
        {
            if (_playerHandManager.HasHandCard) return;
            if (_isProcessingHit || (Time.time < _lastHitTime + _hitCooldown)) return;
            
            _lastHitTime = Time.time;
            StartCoroutine(DrawAndProcess());
        }

        private IEnumerator DrawAndProcess(int retryCount = 0)
        {
            _isProcessingHit = true;
            _gameUI?.DisableGameButtons(); // 드로우 중 버튼 비활성화

            var stage = _stageManager.CurrentStage;
            if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
            {
                _isProcessingHit = false;
                OnStand();
                yield break;
            }

            if (retryCount > 10)
            {
                _isProcessingHit = false;
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                yield break;
            }

            if (!_deckRunner.TryDraw(out DeckSO.CardEntry entry))
            {
                _isProcessingHit = false;
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                yield break;
            }

            if (entry.data.cardType == CardType.Number)
            {
                if (stage.operatorOnlyHit)
                {
                    _deckRunner.ReturnCard(entry);
                    yield return StartCoroutine(DrawAndProcess(retryCount + 1));
                    yield break; // 내부에서 flag를 관리하므로 여기서 flag 리셋 불필요
                }
                _hitCount++;

                bool shouldDrawOp = stage.useOperatorCards && stage.operatorCardRatio > 0f && _playerHandManager.CanReceiveOperatorCard && Random.value < stage.operatorCardRatio;
                if (shouldDrawOp)
                {
                    var opCards = stage.deck.operatorCards;
                    if (opCards != null && opCards.Count > 0)
                    {
                        _deckRunner.ReturnCard(entry);
                        var opData = opCards[Random.Range(0, opCards.Count)];
                        var opEntry = new DeckSO.CardEntry { data = opData, suit = CardSuit.Spade };
                        _playerHandManager.AddOperatorToHand(opEntry);
                        _gameUI?.EnableHandCardDrag(); // UI 패널 대신 드래그 활성화
                        
                        _isProcessingHit = false;
                        // 조커가 손패에 들어왔으므로 버튼을 활성화하지 않고 종료
                        yield break;
                    }
                }

                yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(entry));
                _playerHandManager.AddNumberToField(entry);
                _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, stage);

                if (_blackjackManager.IsPlayerBust(_playerHandManager.FieldData, stage))
                {
                    _isProcessingHit = false;
                    ExitPlayerTurn();
                    StartCoroutine(DelayedTransition(_bustDelay, GameState.Result));
                    yield break;
                }
            }
            else
            {
                _hitCount++;
                _playerHandManager.AddOperatorToHand(entry);
            }

            if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
            {
                _gameUI?.DisableGameButtons();
            }
            else if (!_playerHandManager.HasHandCard)
            {
                _gameUI?.EnableGameButtons();
            }

            _isProcessingHit = false;
        }

        private IEnumerator ResolvePlayerAceChoiceIfNeeded(DeckSO.CardEntry entry, bool disableButtons = false)
        {
            var stage = _stageManager.CurrentStage;
            if (stage.stageIndex < 2 || entry == null || !entry.data.IsAce || entry.data.HasAceValueOverride) yield break;
            if (_gameUI == null) { entry.data.SetAceValue(11); yield break; }

            bool resolved = false;
            _gameUI.ShowAceChoice(() => { entry.data.SetAceValue(1); resolved = true; }, () => { entry.data.SetAceValue(11); resolved = true; });
            yield return new WaitUntil(() => resolved);
            _gameUI.SetupGameButtons(OnHit, OnStand);
            if (disableButtons || _playerHandManager.HasHandCard) _gameUI.DisableGameButtons();
        }

        private void ExitPlayerTurn()
        {
            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked -= _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorDropped -= OnOperatorDropped;
                _gameUI.DisableGameButtons();
                _gameUI.DisableHandCardDrag();
            }
            _playerHandManager.ClearSelection();
        }

        private void OnStand()
        {
            if (_playerHandManager.HasHandCard) return;
            if (_playerHandManager.Field.Count == 0) return;
            ExitPlayerTurn();
            StartCoroutine(DelayedTransition(_standDelay, GameState.DealerTurn));
        }

        private void OnOperatorDropped(int slotIndex)
        {
            if (_playerHandManager.TryPlaceOperator(slotIndex, out DeckSO.CardEntry placed))
            {
                var stage = _stageManager.CurrentStage;
                _gameUI?.PlaceOperatorOnSlot(slotIndex, placed.data.operatorType);
                _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, stage);

                if (_blackjackManager.IsPlayerBust(_playerHandManager.FieldData, stage))
                {
                    ExitPlayerTurn();
                    StartCoroutine(DelayedTransition(_bustDelay, GameState.Result));
                    return;
                }

                _gameUI?.EnableGameButtons();
            }
        }

        private IEnumerator DelayedTransition(float delay, GameState next)
        {
            yield return new WaitForSeconds(delay);
            TransitionTo(next);
        }

        // ─── 이벤트 핸들러 ───────────────────────────────────────

        private void OnPlayerFieldChanged()
        {
            if (_playerHandManager.Field.Count == 0) return;
            var last = _playerHandManager.Field[_playerHandManager.Field.Count - 1];
            if (last.data.cardType == CardType.Number)
                _gameUI?.AddPlayerFieldCard(last, _stageManager.CurrentStage.useOperatorCards);
            _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, _stageManager.CurrentStage);
        }

        private void OnPlayerHandChanged()
        {
            if (_playerHandManager.Hand.Count == 0) return;
            _gameUI?.AddHandCard(_playerHandManager.Hand[_playerHandManager.Hand.Count - 1]);
        }

        private void OnDealerCardAdded(DeckSO.CardEntry entry, bool isHidden)
        {
            _gameUI?.AddDealerFieldCard(entry, isHidden);
            if (!isHidden) _gameUI?.UpdateDealerValue(_dealerManager.FieldData, _stageManager.CurrentStage);
        }

        private void OnDealerOperatorPlaced(int index, DeckSO.CardEntry entry)
        {
            _gameUI?.InsertDealerFieldCard(index, entry);
            _gameUI?.UpdateDealerValue(_dealerManager.FieldData, _stageManager.CurrentStage);
        }

        private void OnDealerCardRevealed(System.Action onComplete)
        {
            _gameUI?.RevealDealerHiddenCard(() => {
                _gameUI?.UpdateDealerValue(_dealerManager.FieldData, _stageManager.CurrentStage);
                onComplete?.Invoke();
            });
        }

        private void OnDealerTurnEnded()
        {
            _gameUI?.UpdateDealerValue(_dealerManager.FieldData, _stageManager.CurrentStage);
            StartCoroutine(DelayedTransition(_resultDelay, GameState.Result));
        }

        private void OnVisionDepleted() => TransitionTo(GameState.GameOver);
        private void OnHandSelectionChanged(int idx) => _gameUI?.HighlightHandCard(idx);
        private void OnSlotHighlightRequested(bool h) => _gameUI?.HighlightAvailableSlots(h);

        // ─── 플로우 ──────────────────────────────────────────────

        private void OnStartGame() { _stageManager.ResetAndLoad(); LoadStage(); TransitionTo(GameState.Betting); }
        private void OnConfirmBet(int bet) { _visionManager.SetBet(bet); TransitionTo(GameState.PlayerTurn); }
        
        private void OnStageWin()
        {
            StartCoroutine(FinishStageRoutine(true));
        }

        private void OnStageLose()
        {
            StartCoroutine(FinishStageRoutine(false));
        }

        private IEnumerator FinishStageRoutine(bool win)
        {
            // 눈알 연출이 끝난 후, 카드들을 하나씩 원래 자리(덱)로 되돌리는 연출을 수행합니다.
            if (_gameUI != null)
            {
                yield return StartCoroutine(_gameUI.CollectCardsSequentially());
            }

            if (win)
            {
                if (_stageManager.IsLastStage) TransitionTo(GameState.StageClear);
                else { _stageManager.LoadNext(); LoadStage(); TransitionTo(GameState.Betting); }
            }
            else
            {
                if (_visionManager.IsBlind()) TransitionTo(GameState.GameOver);
                else { _stageManager.ReloadCurrent(); LoadStage(); TransitionTo(GameState.Betting); }
            }
        }

        private void OnRestartGame() => TransitionTo(GameState.Title);

        private void LoadStage()
        {
            _hitCount = 0;
            _playerHandManager.ResetAll();
            _dealerManager.ResetField();
            _deckRunner = new DeckRunner(_stageManager.CurrentStage.deck, _stageManager.CurrentStage);
            
            if (_eyeSpawnManager != null)
                _eyeSpawnManager.SpawnAndMoveToStage();
        }

        private IEnumerator DealInitialCardsRoutine()
        {
            // 1. 공통 인트로 (플레이어 2장, 딜러 2장)
            if (_deckRunner.TryDraw(out var p1)) { yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(p1, true)); _playerHandManager.AddNumberToField(p1); }
            yield return new WaitForSeconds(_dealInterval);
            if (_deckRunner.TryDraw(out var d1)) _dealerManager.AddOpenCard(d1);
            yield return new WaitForSeconds(_dealInterval);
            if (_deckRunner.TryDraw(out var p2)) { yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(p2, true)); _playerHandManager.AddNumberToField(p2); }
            yield return new WaitForSeconds(_dealInterval);
            if (_deckRunner.TryDraw(out var d2)) _dealerManager.SetHiddenCard(d2);

            // 2. operatorOnlyHit일 경우 남은 숫자 카드 모두 지급
            if (_stageManager.CurrentStage.operatorOnlyHit)
            {
                yield return new WaitForSeconds(_dealInterval);
                yield return StartCoroutine(DealAllNumberCardsRoutine());
            }
        }

        private IEnumerator DealAllNumberCardsRoutine()
        {
            while (_deckRunner.Remaining > 0)
            {
                if (!_deckRunner.TryDraw(out var entry)) break;
                if (entry.data.cardType == CardType.Number) { yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(entry, true)); _playerHandManager.AddNumberToField(entry); yield return new WaitForSeconds(_dealInterval); }
                else _pendingOperators.Add(entry);
            }
            foreach (var op in _pendingOperators) _deckRunner.ReturnCard(op);
            _pendingOperators.Clear();
        }

        public void SetDealerStrategy(IDealerStrategy s) { _dealerStrategy = s; _blackjackManager.SetDealerStrategy(s); }
    }
}
