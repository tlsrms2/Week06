using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 패널 전환과 이벤트 허브 역할만 담당합니다.
    /// 모든 패널 로직은 각 패널 Manager에 위임합니다.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        // ─── Inspector — 패널 루트 ───────────────────────────────
        [Header("패널 루트")]
        [SerializeField] private GameObject _titlePanel;
        [SerializeField] private GameObject _bettingPanel;
        [SerializeField] private GameObject _playerTurnPanel;
        [SerializeField] private GameObject _dealerTurnPanel;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private GameObject _gameOverPanel;

        // ─── 패널 Manager ────────────────────────────────────────
        [Header("패널 Manager")]
        [SerializeField] private TitlePanelManager _titlePanelManager;
        [SerializeField] private BettingPanelManager _bettingPanelManager;
        [SerializeField] private GamePanelManager _gamePanelManager;
        [SerializeField] private ResultPanelManager _resultPanelManager;
        [SerializeField] private GameOverPanelManager _gameOverPanelManager;

        // ─── 하위 UI Manager ─────────────────────────────────────
        [Header("UI Manager")]
        [SerializeField] private HUDManager _hudManager;
        [SerializeField] private FieldUIManager _fieldUIManager;
        [SerializeField] private HandUIManager _handUIManager;
        [SerializeField] private CardCreateManager _cardCreateManager;

        // ─── 내부 상태 ───────────────────────────────────────────
        private GameObject _activePanel;

        // ─── 이벤트 허브 ─────────────────────────────────────────
        /// <summary>연산자 슬롯 클릭 시 발행 — FieldUIManager에서 중계</summary>
        public event Action<int> OnOperatorSlotClicked;

        /// <summary>손패 카드 클릭 시 발행 — HandUIManager에서 중계</summary>
        public event Action<int> OnHandCardClicked;

        // ─── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            _fieldUIManager.Initialize(_cardCreateManager);
            _handUIManager.Initialize(_cardCreateManager);
            _hudManager.Initialize(_fieldUIManager, _handUIManager);

            _fieldUIManager.OnOperatorSlotClicked +=
                idx => OnOperatorSlotClicked?.Invoke(idx);
            _handUIManager.OnHandCardClicked +=
                idx => OnHandCardClicked?.Invoke(idx);
        }

        // ─── 패널 전환 ───────────────────────────────────────────

        /// <summary>지정 패널만 활성화하고 나머지를 비활성화합니다.</summary>
        private void ShowOnly(GameObject target)
        {
            if (_activePanel != null) _activePanel.SetActive(false);
            _activePanel = target;
            if (_activePanel != null) _activePanel.SetActive(true);
        }

        /// <summary>모든 패널을 비활성화합니다.</summary>
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

        // ─── GameManager 호출 진입점 ─────────────────────────────

        /// <summary>타이틀 화면을 표시합니다.</summary>
        public void ShowTitle(Action onStart)
        {
            ShowOnly(_titlePanel);
            _titlePanelManager.Setup(onStart);
        }

        /// <summary>배팅 화면을 표시합니다.</summary>
        public void ShowBetting(int currentBet, Action<int> onConfirm)
        {
            ShowOnly(_bettingPanel);
            _bettingPanelManager.Setup(currentBet, onConfirm);
        }

        /// <summary>플레이어 턴 화면을 표시합니다.</summary>
        public void ShowPlayerTurn(
            List<CardDataSO> field,
            List<CardDataSO> hand,
            Action onHit,
            Action onStand)
        {
            ShowOnly(_playerTurnPanel);
            RefreshPlayerArea(field, hand);
            _gamePanelManager.Setup(onHit, onStand);
        }

        /// <summary>딜러 대기 화면을 표시합니다.</summary>
        public void ShowDealerThinking() => ShowOnly(_dealerTurnPanel);

        /// <summary>결과 화면을 표시합니다.</summary>
        public void ShowResult(string description, bool win, Action onNext)
        {
            ShowOnly(_resultPanel);
            _resultPanelManager.Setup(description, win, onNext);
        }

        /// <summary>게임 오버 화면을 표시합니다.</summary>
        public void ShowGameOver(Action onRestart)
        {
            ShowOnly(_gameOverPanel);
            _gameOverPanelManager.SetupGameOver(onRestart);
        }

        /// <summary>스테이지 클리어 화면을 표시합니다.</summary>
        public void ShowStageClear(Action onRestart)
        {
            ShowOnly(_gameOverPanel);
            _gameOverPanelManager.SetupStageClear(onRestart);
        }

        // ─── HUDManager 위임 ──────────────────────────────────────

        public void SetBetPanelInfo(int stageIndex, long quota)
            => _hudManager.SetBetPanelInfo(stageIndex, quota);

        public void SetGamePanelInfo(int stageIndex, long quota)
            => _hudManager.SetGamePanelInfo(stageIndex, quota);

        public void UpdateCurrentValue(
            List<CardDataSO> field,
            long quota = 0,
            bool flexibleAce = false,
            long bustThreshold = 21)
            => _hudManager.UpdateCurrentValue(field, quota, flexibleAce, bustThreshold);

        public void SetResultInfo(long finalValue, long quota, bool win)
            => _resultPanelManager.SetCompareInfo(finalValue, quota, win);

        // ─── FieldUIManager / HandUIManager 위임 ─────────────────

        public void RefreshPlayerArea(List<CardDataSO> field, List<CardDataSO> hand)
        {
            _fieldUIManager.RefreshField(field);
            _handUIManager.RefreshHand(hand);
        }

        public void RefreshDealerArea(List<CardDataSO> field)
            => _fieldUIManager.RefreshDealerField(field);

        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
            => _fieldUIManager.PlaceOperatorOnSlot(slotIndex, op);

        public void HighlightAvailableSlots(bool highlight)
            => _fieldUIManager.HighlightAvailableSlots(highlight);

        public void HighlightHandCard(int index)
            => _handUIManager.HighlightHandCard(index);

        public void RemoveFromHand(int index)
            => _handUIManager.RemoveFromHand(index);
    }
}