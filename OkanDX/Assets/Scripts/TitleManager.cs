using UnityEngine;

public class TitleManager : MonoBehaviour
{
    /// <summary>
    /// スタートボタンが押された時に実行するメソッド
    /// </summary>
    public void OnClickStart()
    {
        // GameManagerの指示を出してエピソード選択画面へ移動する
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToEpisodeSelect();
        }
        else
        {
            Debug.LogError("GameManagerが見つかりません！Bootシーンから起動しているか確認してください。");
        }
    }
}