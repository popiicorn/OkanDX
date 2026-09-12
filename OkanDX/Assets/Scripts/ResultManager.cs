using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Image clearImage;
    [SerializeField] private TMP_Text clearTextMessage;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText; // ★追加: 次へボタン内のテキストUI

    private void Start()
    {
        // クリアしたエピソードのデータを表示
        EpisodeData currentData = GameManager.Instance != null ? GameManager.Instance.CurrentEpisodeData : null;

        if (currentData != null)
        {
            if (clearImage != null && currentData.clearSprite != null)
            {
                clearImage.sprite = currentData.clearSprite;
            }

            if (clearTextMessage != null)
            {
                clearTextMessage.text = currentData.clearText;
            }

            // ★追加: ボタンのテキストを変更（未設定なら"次へ"）
            if (nextButtonText != null)
            {
                nextButtonText.text = !string.IsNullOrEmpty(currentData.nextButtonText)
                    ? currentData.nextButtonText
                    : "次へ";
            }
        }
    }

    /// <summary>
    /// 「次へ」ボタンを押した時の処理
    /// </summary>
    public void OnClickNextEpisode()
    {
        if (GameManager.Instance == null) return;

        int nextEpisodeNumber = GameManager.Instance.CurrentEpisodeNumber + 1;
        GameManager.Instance.StartEpisode(nextEpisodeNumber);
    }

    /// <summary>
    /// 「エピソード選択へ戻る」ボタン用
    /// </summary>
    public void OnClickGoToSelect()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToEpisodeSelect();
        }
    }
}