using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// UI 중추 Manager.
    /// FieldManager / HUDManager / VisionBettingManager /
    /// CardCreateManager / GamePanelManager를 통합 제어합니다.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("하위 Manager")]
        [SerializeField] private FieldManager _fieldManager;
        [SerializeField] private HUDManager _hudManager;
        [SerializeField] private VisionBettingManager _visionBettingManager;
        [SerializeField] private CardCreateManager _cardCreateManager;
        [SerializeField] private GamePanelManager _gamePanelManager;

        // ─── 이벤트 허브 ─────────────────────────────────────────
        /// <summary>연산자 슬롯 클릭 시 발행</summary>
        public event Action<int> OnOperatorSlotClicked;

        /// <summary>손패 카드 클릭 시 발행</summary>
        public event Action<int> OnHandCardClicked;

        // ─── 생명주기 ─────────────────────────────────────────────

        private void Awake()
        {
            _fieldManager.Initialize(_cardCreateManager);

            _fieldManager.OnOperatorSlotClicked +=
                idx => OnOperatorSlotClicked?.Invoke(idx);
            _fieldManager.OnHandCardClicked +=
                idx => OnHandCardClicked?.Invoke(idx);
        }

        // ─── HUD ─────────────────────────────────────────────────

        /// <summary>스테이지 정보를 갱신합니다.</summary>
        public void SetStageInfo(int stageIndex, long bustValue)
            => _hudManager.SetStageInfo(stageIndex, bustValue);

        /// <summary>플레이어 현재 값을 갱신합니다.</summary>
        public void UpdatePlayerValue(List<CardDataSO> field, StageDataSO stage)
            => _hudManager.UpdatePlayerValue(field, stage);

        /// <summary>딜러 Stay 후 값을 갱신합니다.</summary>
        public void UpdateDealerValue(List<CardDataSO> field, StageDataSO stage)
            => _hudManager.UpdateDealerValue(field, stage);

        /// <summary>딜러 값 텍스트를 초기화합니다.</summary>
        public void ClearDealerValue()
            => _hudManager.ClearDealerValue();

        // ─── 배팅 ────────────────────────────────────────────────

        /// <summary>
        /// 배팅 패널을 초기화하고 엽니다.
        /// Confirm 시 FieldManager 활성화 후 onConfirm 호출합니다.
        /// </summary>
        public void SetupBetting(int betMin, int betMax, Action<int> onConfirm)
        {
            // 필드 초기화 및 숨김
            _fieldManager.ClearAll();
            _fieldManager.Hide();

            // HUD 초기화
            _hudManager.ClearDealerValue();

            _visionBettingManager.Setup(betMin, betMax, betAmount =>
            {
                _fieldManager.Show();
                onConfirm?.Invoke(betAmount);
            });

            // 배팅 패널 강제 오픈
            _visionBettingManager.OpenPanel();
        }

        // ─── 게임 버튼 ────────────────────────────────────────────

        /// <summary>Hit / Stay 버튼 콜백을 등록합니다.</summary>
        public void SetupGameButtons(Action onHit, Action onStand)
            => _gamePanelManager.Setup(onHit, onStand);

        /// <summary>Hit / Stay 버튼을 활성화합니다.</summary>
        public void EnableGameButtons()
            => _gamePanelManager.EnableButtons();

        /// <summary>Hit / Stay 버튼을 비활성화합니다.</summary>
        public void DisableGameButtons()
            => _gamePanelManager.DisableButtons();

        // ─── Result / GameOver ────────────────────────────────────

        /// <summary>Result 패널을 표시합니다.</summary>
        public void ShowResult(
            string description,
            bool win,
            long playerTotal,
            long dealerTotal,
            long bustValue,
            Action onNext)
        {
            _gamePanelManager.ShowResult(description, win, onNext);
            _gamePanelManager.SetResultCompare(
                playerTotal, dealerTotal, bustValue, win);
        }

        /// <summary>게임 오버 패널을 표시합니다.</summary>
        public void ShowGameOver(Action onRestart)
            => _gamePanelManager.ShowGameOver(onRestart);

        /// <summary>스테이지 클리어 패널을 표시합니다.</summary>
        public void ShowStageClear(Action onRestart)
            => _gamePanelManager.ShowStageClear(onRestart);

        // ─── 필드 — Add 방식 ─────────────────────────────────────

        /// <summary>플레이어 필드에 카드 1장을 추가합니다.</summary>
        public void AddPlayerFieldCard(DeckSO.CardEntry entry)
            => _fieldManager.AddPlayerFieldCard(entry);

        /// <summary>딜러 필드에 카드 1장을 추가합니다.</summary>
        public void AddDealerFieldCard(DeckSO.CardEntry entry, bool isHidden = false)
            => _fieldManager.AddDealerFieldCard(entry, isHidden);

        /// <summary>손패에 카드 1장을 추가합니다.</summary>
        public void AddHandCard(DeckSO.CardEntry entry)
            => _fieldManager.AddHandCard(entry);

        /// <summary>전체 필드를 초기화합니다. 라운드 시작 시 호출합니다.</summary>
        public void ClearAllFields()
            => _fieldManager.ClearAll();

        // ─── 슬롯 / 하이라이트 ───────────────────────────────────

        /// <summary>연산자 슬롯에 연산자를 배치합니다.</summary>
        public void PlaceOperatorOnSlot(int slotIndex, OperatorType op)
            => _fieldManager.PlaceOperatorOnSlot(slotIndex, op);

        /// <summary>슬롯 하이라이트를 갱신합니다.</summary>
        public void HighlightAvailableSlots(bool highlight)
            => _fieldManager.HighlightAvailableSlots(highlight);

        /// <summary>손패 카드 선택 상태를 갱신합니다.</summary>
        public void HighlightHandCard(int index)
            => _fieldManager.HighlightHandCard(index);

        /// <summary>딜러 비공개 카드를 앞면으로 뒤집습니다.</summary>
        public void RevealDealerHiddenCard()
            => _fieldManager.RevealDealerHiddenCard();
    }
}