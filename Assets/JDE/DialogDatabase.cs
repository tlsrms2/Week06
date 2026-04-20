using UnityEngine;

/// <summary>
/// 딜러 대사 시퀀스를 인덱스로 저장하는 ScriptableObject.
/// Assets 우클릭 > Create > Dialog > DialogDatabase 로 생성.
/// </summary>
[CreateAssetMenu(fileName = "DialogDatabase", menuName = "Dialog/DialogDatabase")]
public class DialogDatabase : ScriptableObject
{
    public DialogSequence[] sequences;

    public bool TryGetSequence(int index, out DialogSequence result)
    {
        if (sequences == null || index < 0 || index >= sequences.Length)
        {
            Debug.LogWarning($"[DialogDatabase] 잘못된 인덱스: {index}");
            result = null;
            return false;
        }
        result = sequences[index];
        return true;
    }
}

[System.Serializable]
public class DialogSequence
{
    [Tooltip("에디터 식별용 이름 (예: Stage1_RuleExplain)")]
    public string label;

    [TextArea(2, 4)]
    public string[] lines; // 딜러가 순서대로 출력할 대사 목록
}