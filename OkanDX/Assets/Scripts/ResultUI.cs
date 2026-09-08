using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultUI : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private Image clearImage;         // クリア画像
    [SerializeField] private TMP_Text clearMessageText; // クリアテキスト
    [SerializeField] private Button nextEpisodeButton; // 次のエピソードへボタン

    private EpisodeData episodeData;

    private void Start()
    {
        // GameManager から現在のクリアデータを取得
        if (GameManager.Instance != null && GameManager.Instance.CurrentEpisodeData != null)
        {
            episodeData = GameManager.Instance.CurrentEpisodeData;
            SetupResult(episodeData);
        }

        // ボタンイベントの登録
        if (nextEpisodeButton != null)
        {
            nextEpisodeButton.onClick.AddListener(OnNextEpisodeButtonClicked);
        }
    }

    /// <summary>
    /// リザルト表示のセットアップ
    /// </summary>
    public void SetupResult(EpisodeData data)
    {
        if (clearImage != null) clearImage.sprite = data.clearSprite;
        if (clearMessageText != null) clearMessageText.text = data.clearText;
    }

    /// <summary>
    /// 「次のエピソードへ」ボタンをクリックした時
    /// </summary>
    private void OnNextEpisodeButtonClicked()
    {
        if (episodeData != null && !string.IsNullOrEmpty(episodeData.nextSceneName))
        {
            // 直接次のエピソード画面（Scene）へジャンプ！
            SceneManager.LoadScene(episodeData.nextSceneName);
        }
        else
        {
            // 次のシーンが設定されていない場合はエピソード選択画面などへバックアップ遷移
            Debug.LogWarning("次のシーン名が設定されていません。エピソード選択へ戻ります。");
            SceneManager.LoadScene("EpisodeSelect");
        }
    }
}