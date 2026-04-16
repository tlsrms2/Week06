using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    public enum GameState { Title, Betting, PlayerTurn, DealerTurn, Result, GameOver, StageClear }

    /// <summary>
    /// 블랙잭 게임 흐름(상태 머신)을 총괄합니다.
    /// 시야 배팅/승패 처리는 VisionManager에 위임합니다.
    /// UI 호출은 GameUI 프록시를 통해 null 안전하게 처리합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("데이터")]
        [Tooltip("StageRegistry SO를 여기에 드래그")]
        [SerializeField] private StageRegistrySO _stageRegistry;

        [Header("참조")]
        [Tooltip("GameUI 컴포넌트 — 미연결 시 로그만 출력하는 더미로 동작")]
        [SerializeField] private GameUIManager _gameUI;

        // ─── 런타임 전용 ──────────────────────────────────────────
        private IDealerStrategy _dealerStrategy;
        private VisionManager _visionManager;

        private GameState _state;
        private StageDataSO _currentStage;
        private DeckRunner _playerDeckRunner;
        private DeckRunner _dealerDeckRunner;
        private List<CardDataSO> _playerField = new();
        private List<CardDataSO> _playerHand = new();
        private List<CardDataSO> _dealerField = new();
        private int _stageIndex = 0;

        // ─── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            _dealerStrategy = new DealerAI();

            _visionManager = gameObject.AddComponent<VisionManager>();
            _visionManager.OnVisionDepleted += OnVisionDepleted;
        }

        private void Start() => TransitionTo(GameState.Title);

        // ─── UI 프록시 ────────────────────────────────────────────
        // GameUI 미구현/미연결 상태에서 null 호출을 막는 래퍼.
        // GameUI 완성 후 프록시를 제거하고 _gameUI 직접 호출로 교체.

        private void UI_ShowTitle(System.Action onStart)
        {
            if (_gameUI != null) { _gameUI.ShowTitle(onStart); return; }
            Debug.Log("[GM] ShowTitle — 자동 진행");
            onStart?.Invoke();
        }

        private void UI_ShowBetting(int currentBet, System.Action<int> onConfirm)
        {
            if (_gameUI != null) { _gameUI.ShowBetting(currentBet, onConfirm); return; }
            Debug.Log($"[GM] ShowBetting — bet:{currentBet} 자동 확정");
            onConfirm?.Invoke(currentBet);
        }

        private void UI_ShowPlayerTurn(
            List<CardDataSO> field, List<CardDataSO> hand,
            System.Action onHit, System.Action onStand)
        {
            if (_gameUI != null) { _gameUI.ShowPlayerTurn(field, hand, onHit, onStand); return; }
            Debug.Log("[GM] ShowPlayerTurn — 자동 Stand");
            onStand?.Invoke();
        }

        private void UI_ShowDealerThinking()
        {
            if (_gameUI != null) { _gameUI.ShowDealerThinking(); return; }
            Debug.Log("[GM] ShowDealerThinking");
        }

        private void UI_RefreshPlayerArea(List<CardDataSO> field, List<CardDataSO> hand)
        {
            if (_gameUI != null)
            {
                _gameUI.RefreshPlayerArea(field, hand);
                _gameUI.UpdateCurrentValue(field, _currentStage.quota);
                return;
            }
            Debug.Log($"[GM] RefreshPlayerArea — field:{field.Count} hand:{hand.Count}");
        }

        private void UI_RefreshDealerArea(List<CardDataSO> field)
        {
            if (_gameUI != null) { _gameUI.RefreshDealerArea(field); return; }
            Debug.Log($"[GM] RefreshDealerArea — field:{field.Count}");
        }

        private void UI_ShowResult(string desc, bool win, System.Action onNext)
        {
            if (_gameUI != null) { _gameUI.ShowResult(desc, win, onNext); return; }
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
        /// <summary>지정한 상태로 전환하고 진입 로직을 실행합니다.</summary>
        public void TransitionTo(GameState next)
        {
            _state = next;
            switch (next)
            {
                case GameState.Title: OnEnterTitle(); break;
                case GameState.Betting: OnEnterBetting(); break;
                case GameState.PlayerTurn: OnEnterPlayerTurn(); break;
                case GameState.DealerTurn: StartCoroutine(RunDealerTurn()); break;
                case GameState.Result: OnEnterResult(); break;
                case GameState.GameOver: OnEnterGameOver(); break;
                case GameState.StageClear: OnEnterStageClear(); break;
            }
        }

        // ─── 상태 진입 ────────────────────────────────────────────
        /// <summary>타이틀 화면을 표시하고 스테이지/시야를 초기화합니다.</summary>
        private void OnEnterTitle()
        {
            _stageIndex = 0;
            _visionManager.ResetVision();
            UI_ShowTitle(OnStartGame);
        }

        /// <summary>
        /// 배팅 화면을 표시합니다.
        /// 최소 배팅량을 충족하지 못하면 즉시 게임 오버로 전환합니다.
        /// </summary>
        private void OnEnterBetting()
        {
            _currentStage = _stageRegistry.GetStage(_stageIndex);
            if (_currentStage == null) return;

            // 최소 배팅량 미달 → 게임 오버
            // StageDataSO에 minimumBet 필드가 없으면 visionBetMin 사용
            int minBet = Mathf.RoundToInt(_currentStage.visionBetMin);
            if (!_visionManager.CanBet(minBet))
            {
                TransitionTo(GameState.GameOver);
                return;
            }

            // 스테이지 정보를 UI에 반영
            if (_gameUI != null)
            {
                _gameUI.SetBetPanelInfo(
                    _currentStage.stageIndex,
                    _currentStage.quota);
            }

            UI_ShowBetting(minBet, OnConfirmBet);
        }

        /// <summary>플레이어 턴 UI를 표시하고 Hit / Stand 콜백을 등록합니다.</summary>
        private void OnEnterPlayerTurn()
            => UI_ShowPlayerTurn(_playerField, _playerHand, OnHit, OnStand);

        /// <summary>게임 오버 화면을 표시합니다.</summary>
        private void OnEnterGameOver()
            => UI_ShowGameOver(OnRestartGame);

        /// <summary>
        /// 전체 스테이지 클리어 화면을 표시합니다.
        /// 모든 스테이지를 통과했을 때 진입합니다.
        /// </summary>
        private void OnEnterStageClear()
            => UI_ShowStageClear(OnRestartGame);

        /// <summary>
        /// 플레이어와 딜러의 최종 연산값을 비교해 승패를 판정합니다.
        /// 승패 결과를 VisionManager에 전달하고 스테이지를 진행합니다.
        /// </summary>
        private void OnEnterResult()
        {
            long playerTotal = ExpressionEvaluator.Evaluate(_playerField);
            long dealerTotal = ExpressionEvaluator.Evaluate(_dealerField);
            long quota = _currentStage.quota;

            bool playerBust = playerTotal > quota;
            bool dealerBust = dealerTotal > _dealerStrategy.BustThreshold;
            bool win = !playerBust && (dealerBust || playerTotal >= dealerTotal);

            // 수식 문자열 생성
            string exprStr = ExpressionEvaluator.ToExpressionString(_playerField);

            // VisionManager에 승패 전달
            if (win) _visionManager.WinBet();
            else _visionManager.LoseBet();

            // 수치 비교 텍스트 먼저 갱신
            _gameUI?.SetResultInfo(playerTotal, quota, win);

            // 결과 설명 문자열 생성
            string desc = _dealerStrategy.GetResultDescription(dealerTotal, playerTotal, quota);

            // 스테이지 진행
            System.Action onNext = win ? OnStageWin : (System.Action)OnStageLose;

            Debug.Log($"[GM] Result — player:{FormatNumber(playerTotal)} " +
                      $"dealer:{FormatNumber(dealerTotal)} quota:{FormatNumber(quota)} win:{win}");

            UI_ShowResult($"{exprStr} = {FormatNumber(playerTotal)}", win, onNext);
        }

        // ─── 딜러 턴 ─────────────────────────────────────────────
        /// <summary>
        /// 딜러가 ShouldHit 조건을 만족하는 동안 카드를 드로우합니다.
        /// 덱이 소진되거나 조건 미충족 시 Result 상태로 전환합니다.
        /// </summary>
        private IEnumerator RunDealerTurn()
        {
            UI_ShowDealerThinking();

            while (true)
            {
                long total = ExpressionEvaluator.Evaluate(_dealerField);

                if (!_dealerStrategy.ShouldHit(total)) break;
                if (!_dealerDeckRunner.TryDraw(out CardDataSO card)) break;

                _dealerField.Add(card);
                UI_RefreshDealerArea(_dealerField);
                yield return new WaitForSeconds(0.8f);
            }

            TransitionTo(GameState.Result);
        }

        // ─── 플레이어 액션 ────────────────────────────────────────
        /// <summary>
        /// 덱에서 카드 한 장을 드로우합니다.
        /// 숫자 카드는 필드에, 연산자 카드는 손패에 추가됩니다.
        /// 덱이 비었으면 자동으로 딜러 턴으로 전환합니다.
        /// </summary>
        private void OnHit()
        {
            if (!_playerDeckRunner.TryDraw(out CardDataSO card))
            {
                Debug.Log("[GM] 덱 소진 — 딜러 턴 전환");
                TransitionTo(GameState.DealerTurn);
                return;
            }

            if (card.cardType == CardType.Number) _playerField.Add(card);
            else _playerHand.Add(card);

            UI_RefreshPlayerArea(_playerField, _playerHand);
        }

        /// <summary>플레이어가 Stand를 선택해 딜러 턴으로 전환합니다.</summary>
        private void OnStand() => TransitionTo(GameState.DealerTurn);

        // ─── 플로우 ──────────────────────────────────────────────
        /// <summary>게임 시작 시 첫 스테이지로 진입합니다.</summary>
        private void OnStartGame()
        {
            LoadStage();
            TransitionTo(GameState.Betting);
        }

        /// <summary>배팅을 확정하고 플레이어 턴으로 진입합니다.</summary>
        private void OnConfirmBet(int betAmount)
        {
            _visionManager.SetBet(betAmount);
            LoadStage();
            TransitionTo(GameState.PlayerTurn);
        }

        /// <summary>
        /// 스테이지 승리 처리.
        /// 마지막 스테이지면 StageClear, 아니면 다음 스테이지로 진행합니다.
        /// </summary>
        private void OnStageWin()
        {
            _stageIndex++;
            if (_stageIndex >= _stageRegistry.StageCount)
                TransitionTo(GameState.StageClear);
            else
                TransitionTo(GameState.Betting);
        }

        /// <summary>
        /// 스테이지 패배 처리.
        /// 시야가 남아있으면 같은 스테이지 재도전, 소진되면 게임 오버입니다.
        /// </summary>
        private void OnStageLose()
        {
            if (_visionManager.IsBlind())
                TransitionTo(GameState.GameOver);
            else
                TransitionTo(GameState.Betting);
        }

        /// <summary>게임 오버/클리어 후 타이틀로 돌아갑니다.</summary>
        private void OnRestartGame() => TransitionTo(GameState.Title);

        /// <summary>
        /// 현재 스테이지 SO를 기반으로 런타임 덱을 초기화합니다.
        /// SO 에셋 자체는 변경하지 않으며, 매 라운드 새 Queue를 생성합니다.
        /// </summary>
        private void LoadStage()
        {
            _currentStage = _stageRegistry.GetStage(_stageIndex);
            if (_currentStage == null)
            {
                Debug.LogError("[GM] LoadStage — StageDataSO를 불러오지 못했습니다.");
                return;
            }

            _playerField.Clear();
            _playerHand.Clear();
            _dealerField.Clear();

            _playerDeckRunner = new DeckRunner(_currentStage.playerDeck);
            _dealerDeckRunner = new DeckRunner(_currentStage.dealerDeck);

            Debug.Log($"[GM] Stage {_currentStage.stageIndex} 로드 — quota:{FormatNumber(_currentStage.quota)}");
        }

        /// <summary>시야가 0이 되면 게임 오버로 전환합니다.</summary>
        private void OnVisionDepleted() => TransitionTo(GameState.GameOver);

        // ─── 유틸 ─────────────────────────────────────────────────
        /// <summary>큰 숫자를 쉼표 포맷으로 변환합니다. 예: 1037836800 → "1,037,836,800"</summary>
        private string FormatNumber(long number) => number.ToString("N0");

        // ─── 공개 API ─────────────────────────────────────────────
        /// <summary>딜러 전략을 런타임에 교체합니다. (DealerAI → BlindJackDealerAI 등)</summary>
        public void SetDealerStrategy(IDealerStrategy strategy)
            => _dealerStrategy = strategy;
    }
}