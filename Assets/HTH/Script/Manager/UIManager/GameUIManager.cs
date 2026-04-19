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

        // ─── 이벤트 허브 ──────────────────────────────────────────────
        /// <summary>손패 카드 클릭 시 발행</summary>
        public event Action<int> OnHandCardClicked;

        /// <summary>조커 드래그 드롭 시 슬롯 인덱스 발행</summary>
        public event Action<int> OnOperatorDropped;

        // ─── 생명주기 ──────────────────────────────────────────────────

        private void Awake()
        {
            _fieldManager.Initialize(_cardCreateManager);

            _fieldManager.OnHandCardClicked +=
                idx => OnHandCardClicked?.Invoke(idx);
            _fieldManager.OnOperatorDropped +=
                idx => OnOperatorDropped?.Invoke(idx);
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

        /// <summary>플레이어 값 텍스트를 초기화합니다.</summary>
        public void ClearPlayerValue()
            => _hudManager.ClearPlayerValue();

        // ─── 배팅 ────────────────────────────────────────────────

        /// <summary>
        /// 배팅 패널을 초기화하고 엽니다.
        /// Confirm 시 FieldManager 활성화 후 onConfirm 호출합니다.
        /// </summary>
        public void SetupBetting(int betMin, int betMax, int currentVision, int stageIndex, long bustValue, Action<int> onConfirm)
        {
            // 필드 초기화 및 숨김
            _fieldManager.ClearAll();
            _fieldManager.Hide();

            // HUD 초기화
            _hudManager.ClearDealerValue();

            _visionBettingManager.Setup(betMin, betMax, currentVision, stageIndex, bustValue, betAmount =>
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

        /// <summary>게임 오버 패널을 표시합니다.</summary>
        public void ShowGameOver(Action onRestart)
            => _gamePanelManager.ShowGameOver(onRestart);

        /// <summary>스테이지 클리어 패널을 표시합니다.</summary>
        public void ShowStageClear(Action onRestart)
            => _gamePanelManager.ShowStageClear(onRestart);

        // ─── 필드 — Add 방식 ─────────────────────────────────────

        /// <summary>플레이어 필드에 카드 1장을 추가합니다.</summary>
        public void AddPlayerFieldCard(DeckSO.CardEntry entry, bool createSlot = false)
            => _fieldManager.AddPlayerFieldCard(entry, createSlot);

        /// <summary>딜러 필드에 카드 1장을 추가합니다.</summary>
        public void AddDealerFieldCard(DeckSO.CardEntry entry, bool isHidden = false)
            => _fieldManager.AddDealerFieldCard(entry, isHidden);

        /// <summary>딜러 필드의 지정된 위치에 카드를 삽입합니다.</summary>
        public void InsertDealerFieldCard(int index, DeckSO.CardEntry entry)
            => _fieldManager.InsertDealerFieldCard(index, entry);

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

        /// <summary>손패의 조커 카드에 드래그 이벤트를 활성화합니다.</summary>
        public void EnableHandCardDrag()
            => _fieldManager.EnableHandCardDrag();

        /// <summary>손패의 조커 카드 드래그 이벤트를 비활성화합니다.</summary>
        public void DisableHandCardDrag()
            => _fieldManager.DisableHandCardDrag();

        /// <summary>손패 카드 선택 상태를 갱신합니다.</summary>
        public void HighlightHandCard(int index)
            => _fieldManager.HighlightHandCard(index);

        /// <summary>딜러 비공개 카드를 앞면으로 뒤집습니다.</summary>
        public void RevealDealerHiddenCard(System.Action onComplete = null)
            => _fieldManager.RevealDealerHiddenCard(onComplete);

        /// <summary>
        /// 연산자 카드 배치 UI를 표시합니다.
        /// </summary>
        public void ShowOperatorChoice()
            => _gamePanelManager.ShowOperatorChoice();

        public void ShowAceChoice(Action onSelectOne, Action onSelectEleven)
            => _gamePanelManager.ShowAceChoice(onSelectOne, onSelectEleven);

        public void HideOperatorChoice()
            => _gamePanelManager.HideOperatorChoice();

        public void RemoveHandCard()
            => _fieldManager.RemoveHandCard();
    }
}
