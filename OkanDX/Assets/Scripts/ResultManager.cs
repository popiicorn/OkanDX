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

        // 進行順（stageOrder）の次へ進む（stageOrder が無いときはエピソード番号の次）
        EpisodeData current = GameManager.Instance.CurrentEpisodeData;
        int currentNumber = (current != null && current.stageOrder > 0)
            ? current.stageOrder
            : GameManager.Instance.CurrentEpisodeNumber;

        GameManager.Instance.StartEpisode(currentNumber + 1);
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