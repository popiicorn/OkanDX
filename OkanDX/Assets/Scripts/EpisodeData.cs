using UnityEngine;

public enum EpisodeType
{
    NormalEscape, // 通常の脱出ゲーム
    MiniGame      // ミニゲーム
}

[CreateAssetMenu(fileName = "EpisodeData_001", menuName = "EscapeGame/EpisodeData")]
public class EpisodeData : ScriptableObject
{
    [Header("■ 進行順（何番目に遊ぶステージか）")]
    [Tooltip("ストーリー全体での解放・表示順番 (1, 2, 3, 4, 5, 6...)")]
    public int stageOrder = 1;

    [Header("■ 表示用の番号設定")]
    [Tooltip("通常エピソードの場合の表示番号（エピソード1, エピソード2...）")]
    public int episodeIndex = 1;

    [Tooltip("ミニゲームの場合の表示番号（ミニゲーム1, ミニゲーム2...）")]
    public int miniGameIndex = 1;

    [Header("■ 基本情報")]
    public string episodeTitle = "おかんの部屋";
    public EpisodeType episodeType = EpisodeType.NormalEscape;

    [Header("■ リザルト表示データ")]
    public Sprite clearSprite;
    [TextArea(3, 5)]
    public string clearText;
    public string nextButtonText = "次へ";

    [Header("■ 遷移設定")]
    public string nextSceneName; // 空欄の場合は自動補完

    // --- 表示用テキストプロパティ ---
    public string EpisodeNumberText
    {
        get
        {
            if (episodeType == EpisodeType.MiniGame)
            {
                return $"ミニゲーム{miniGameIndex}";
            }
            return $"エピソード{episodeIndex}";
        }
    }

    public string EpisodeTitleText => episodeTitle;
    public string FullTitleText => $"{EpisodeNumberText}  {EpisodeTitleText}";
}