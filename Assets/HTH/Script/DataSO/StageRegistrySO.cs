using System.Collections.Generic;
using UnityEngine;

namespace HTH
{
    /// <summary>
    /// 모든 스테이지의 단일 진입점.
    /// 스테이지 추가 시 이 SO의 리스트에 드래그만 하면 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageRegistry", menuName = "Blindjack/Stage Registry")]
    public class StageRegistrySO : ScriptableObject
    {
        [Tooltip("스테이지 순서대로 등록")]
        public List<StageDataSO> stages = new();

        /// <summary>
        /// 인덱스로 스테이지를 가져옵니다.
        /// 범위 초과 시 마지막 스테이지를 반환합니다.
        /// </summary>
        public StageDataSO GetStage(int index)
        {
            if (stages == null || stages.Count == 0)
            {
                Debug.LogError("[StageRegistry] 등록된 스테이지가 없습니다.");
                return null;
            }
            return stages[Mathf.Clamp(index, 0, stages.Count - 1)];
        }

        public int StageCount => stages?.Count ?? 0;
    }
}