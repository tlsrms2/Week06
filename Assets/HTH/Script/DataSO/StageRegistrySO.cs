using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    // 모든 스테이지의 단일 진입점.
    // GameManager는 이 SO 하나만 참조하면 됨.
    // 스테이지 추가 시 Inspector에서 리스트에 드래그만 하면 끝.
    [CreateAssetMenu(fileName = "StageRegistry", menuName = "Blindjack/Stage Registry")]
    public class StageRegistrySO : ScriptableObject
    {
        [Tooltip("스테이지 순서대로 등록. 추가 시 여기에만 드래그.")]
        public List<StageDataSO> stages = new();

        public StageDataSO GetStage(int index)
        {
            if (stages == null || stages.Count == 0)
            {
                Debug.LogError("[StageRegistry] 등록된 스테이지가 없습니다.");
                return null;
            }
            // 인덱스 초과 시 마지막 스테이지 반복
            int clamped = Mathf.Clamp(index, 0, stages.Count - 1);
            return stages[clamped];
        }

        public int StageCount => stages?.Count ?? 0;
    }
}