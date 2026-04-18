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
        [Header("초기 딜링 지연 시간")]
        [Tooltip("초기 딜링 카드 사이 대기 시간 (초)")]
        [SerializeField] private float _dealInterval = 0.5f;
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
                case GameState.DealerTurn: StartCoroutine(_dealerManager.RunTurn(
                        _deckRunner, _dealerStrategy, _stageManager.CurrentStage)); break;
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

            _gameUI?.DisableGameButtons();
            _gameUI?.ClearDealerValue();

            // Refresh 대신 ClearAllFields 후 초기 딜링
            // 초기 딜링에서 AddPlayerFieldCard / AddDealerFieldCard가 호출됨
            _gameUI?.ClearAllFields();

            StartCoroutine(DealInitialAndEnable());

            if (_gameUI != null)
            {
                _gameUI.OnHandCardClicked += _playerHandManager.SelectHandCard;
                _gameUI.OnOperatorSlotClicked += OnOperatorSlotSelected;
            }

            UI_ShowPlayerTurn();
        }
        private IEnumerator DealInitialAndEnable()
        {
            yield return StartCoroutine(DealInitialCardsRoutine());
            _gameUI?.EnableGameButtons();
        }

        // ─── 초기 딜링 ────────────────────────────────────────────
        /// <summary>
        /// 초기 딜링을 순차적으로 수행합니다.
        /// 플레이어 1장 → 딜러 1장(공개) → 플레이어 1장 → 딜러 1장(비공개)
        /// 각 카드 사이에 _dealInterval만큼 대기합니다.
        /// </summary>
        private IEnumerator DealInitialCardsRoutine()
        {
            if(_stageManager.CurrentStage.operatorOnlyHit)
    {
                yield return StartCoroutine(DealAllNumberCardsRoutine());
                yield break;
            }

            // 1. 플레이어 첫 번째 카드
            if (_deckRunner.TryDraw(out DeckSO.CardEntry p1))
            {
                yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(p1, disableButtonsAfterChoice: true));
                _playerHandManager.AddNumberToField(p1);
            }

            yield return new WaitForSeconds(_dealInterval);

            // 2. 딜러 공개 카드
            if (_deckRunner.TryDraw(out DeckSO.CardEntry d1))
                _dealerManager.AddOpenCard(d1);

            yield return new WaitForSeconds(_dealInterval);

            // 3. 플레이어 두 번째 카드
            if (_deckRunner.TryDraw(out DeckSO.CardEntry p2))
            {
                yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(p2, disableButtonsAfterChoice: true));
                _playerHandManager.AddNumberToField(p2);
            }

            yield return new WaitForSeconds(_dealInterval);

            // 4. 딜러 비공개 카드
            if (_deckRunner.TryDraw(out DeckSO.CardEntry d2))
                _dealerManager.SetHiddenCard(d2);
        }

        /// <summary>
        /// operatorOnlyHit 스테이지 전용 초기 딜링.
        /// 숫자 카드를 하나씩 순차적으로 지급합니다.
        /// </summary>
        private IEnumerator DealAllNumberCardsRoutine()
        {
            while (_deckRunner.Remaining > 0)
            {
                if (!_deckRunner.TryDraw(out DeckSO.CardEntry entry)) break;

                if (entry.data.cardType == CardType.Number)
                {
                    yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(entry, disableButtonsAfterChoice: true));
                    _playerHandManager.AddNumberToField(entry);
                    yield return new WaitForSeconds(_dealInterval);
                }
                else
                {
                    _pendingOperators.Add(entry);
                }
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

        private void OnHit() => StartCoroutine(DrawAndProcess());

        private IEnumerator DrawAndProcess(int retryCount = 0)
        {
            var stage = _stageManager.CurrentStage;

            if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
            {
                Debug.Log("[GM] 최대 Hit 횟수 도달 — Stand 전환");
                OnStand();
                yield break;
            }

            if (retryCount > 10)
            {
                Debug.LogWarning("[GM] DrawAndProcess — 재드로우 한도 초과");
                ExitPlayerTurn();
                TransitionTo(GameState.DealerTurn);
                yield break;
            }

            if (!_deckRunner.TryDraw(out DeckSO.CardEntry entry))
            {
                Debug.Log("[GM] 덱 소진 — 딜러 턴 전환");
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
                    yield break;
                }

                _hitCount++;

                bool shouldDrawOperator =
                    stage.useOperatorCards &&
                    stage.operatorCardRatio > 0f &&
                    _playerHandManager.CanReceiveOperatorCard &&
                    Random.value < stage.operatorCardRatio;

                // 한 번의 Hit에서는 숫자/연산자 중 한 장만 지급합니다.
                if (shouldDrawOperator)
                {
                    var opCards = stage.deck.operatorCards;
                    if (opCards != null && opCards.Count > 0)
                    {
                        _deckRunner.ReturnCard(entry);

                        var opData = opCards[Random.Range(0, opCards.Count)];
                        var opEntry = new DeckSO.CardEntry
                        {
                            data = opData,
                            suit = CardSuit.Spade,
                            suitData = null
                        };

                        Debug.Log($"[GM] 연산자 카드 추가 — {opData.displayLabel} " +
                                  $"(확률:{stage.operatorCardRatio:P0})");

                        _gameUI?.DisableGameButtons();
                        _playerHandManager.AddOperatorToHand(opEntry);

                        _gameUI?.ShowOperatorChoice();

                        if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
                            _gameUI?.DisableGameButtons();

                        yield break;
                    }
                }

                yield return StartCoroutine(ResolvePlayerAceChoiceIfNeeded(entry));

                _playerHandManager.OnFieldChanged -= OnPlayerFieldChanged;
                _playerHandManager.AddNumberToField(entry);

                // 연산자 카드 없을 때 — 기존 흐름 계속
                _gameUI?.AddPlayerFieldCard(entry, createSlot: stage.useOperatorCards);

                bool bust = _blackjackManager.IsPlayerBust(_playerHandManager.FieldData, stage);

                if (bust)
                {
                    _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, stage);
                    _playerHandManager.OnFieldChanged += OnPlayerFieldChanged;
                    ExitPlayerTurn();
                    StartCoroutine(DelayedTransition(_bustDelay, GameState.Result));
                    yield break;
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
                    if (!_playerHandManager.CanReceiveOperatorCard)
                    {
                        _deckRunner.ReturnCard(entry);
                        yield return StartCoroutine(DrawAndProcess(retryCount + 1));
                        yield break;
                    }

                    _hitCount++;
                    _playerHandManager.AddOperatorToHand(entry);

                    if (stage.maxHitCount > 0 && _hitCount >= stage.maxHitCount)
                        _gameUI?.DisableGameButtons();
                }
                else
                {
                    yield return StartCoroutine(DrawAndProcess(retryCount + 1));
                }
            }
        }

        private IEnumerator ResolvePlayerAceChoiceIfNeeded(
            DeckSO.CardEntry entry,
            bool disableButtonsAfterChoice = false)
        {
            var stage = _stageManager.CurrentStage;
            if (!NeedsPlayerAceChoice(stage, entry))
                yield break;

            if (_gameUI == null)
            {
                entry.data.SetAceValue(11);
                yield break;
            }

            bool resolved = false;

            _gameUI.ShowAceChoice(
                onSelectOne: () =>
                {
                    entry.data.SetAceValue(1);
                    resolved = true;
                },
                onSelectEleven: () =>
                {
                    entry.data.SetAceValue(11);
                    resolved = true;
                });

            yield return new WaitUntil(() => resolved);

            _gameUI.SetupGameButtons(OnHit, OnStand);
            if (disableButtonsAfterChoice)
                _gameUI.DisableGameButtons();
        }

        private bool NeedsPlayerAceChoice(StageDataSO stage, DeckSO.CardEntry entry)
            => stage != null
            && stage.stageIndex >= 2
            && entry != null
            && entry.data != null
            && entry.data.IsAce
            && !entry.data.HasAceValueOverride;

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
            {
                _gameUI?.PlaceOperatorOnSlot(slotIndex, placed.data.operatorType);
                //_gameUI?.RemoveHandCard();
                _gameUI?.HideOperatorChoice();
                _gameUI?.EnableGameButtons();

                // ← 연산자 배치 후 값 갱신
                _gameUI?.UpdatePlayerValue(_playerHandManager.FieldData, _stageManager.CurrentStage);
            }
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

            var last = _playerHandManager.Field[_playerHandManager.Field.Count - 1];

            // ← 연산자 카드면 AddPlayerFieldCard 스킵
            if (last.data.cardType == CardType.Operator)
            {
                _gameUI?.UpdatePlayerValue(
                    _playerHandManager.FieldData,
                    _stageManager.CurrentStage);
                return;
            }

            _gameUI?.AddPlayerFieldCard(last,
                createSlot: _stageManager.CurrentStage.useOperatorCards);
            _gameUI?.UpdatePlayerValue(
                _playerHandManager.FieldData,
                _stageManager.CurrentStage);
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

        private void OnDealerCardRevealed(System.Action onComplete)
        {
            _gameUI?.RevealDealerHiddenCard(onComplete); // ← 기존 카드 뒤집기
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
