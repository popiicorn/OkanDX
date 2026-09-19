using UnityEngine;

[System.Serializable]
public class HintItem
{
    [Tooltip("ヒントの一意な識別ID (例: Ep1_Hint1)")]
    public string hintKey = "Ep1_Hint1";

    [Tooltip("ボタンに表示するタイトル (例: ヒント 1)")]
    public string buttonTitle = "ヒント 1";

    [Tooltip("閲覧に必要なへそくり数")]
    public int cost = 1;

    [TextArea(3, 6)]
    [Tooltip("表示されるヒント本文")]
    public string hintText = "ここにヒントの内容が入ります。";
}

// ★ この記述があることで、右クリックメニューからアセットを作成できるようになります
[CreateAssetMenu(fileName = "HintData_Ep1", menuName = "Hint Data")]
public class HintData : ScriptableObject
{
    [Header("■ 最大3つのヒント設定")]
    public HintItem[] hints = new HintItem[3];
}