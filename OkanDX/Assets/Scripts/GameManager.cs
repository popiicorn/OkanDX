using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // どこからでも GameManager.Instance でアクセスできるようにする（シングルトン）
    public static GameManager Instance { get; private set; }

    [Header("進行データ")]
    // クリア済みの最大エピソード番号（例: 0=未クリア、1=Ep1クリア→Ep2まで遊べる）
    public int ClearedEpisodeIndex { get; private set; } = 0;

    // 現在プレイ中のエピソード番号
    public int CurrentEpisodeNumber { get; private set; } = 1;

    // ★リザルト画面に渡す現在のクリアデータ
    public EpisodeData CurrentEpisodeData { get; set; }

    // ★EpisodeTitleUI等から参照用（CurrentEpisodeDataのエイリアス）
    public EpisodeData CurrentEpisode => CurrentEpisodeData;

    // Easy Save用のキー名
    private const string SAVE_KEY_CLEARED_EP = "ClearedEpisodeIndex";

    private void Awake()
    {
        // GameManagerが重複しないように制御
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンが変わってもこのオブジェクトを消さない
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================================================
    //  シーン遷移の処理
    // ================================================

    /// <summary>
    /// タイトル画面へ移動
    /// </summary>
    public void GoToTitle()
    {
        SceneManager.LoadScene("Title");
    }

    /// <summary>
    /// エピソード選択画面へ移動
    /// </summary>
    public void GoToEpisodeSelect()
    {
        SceneManager.LoadScene("EpisodeSelect");
    }

    /// <summary>
    /// 指定したエピソードを EpisodeData を使用して開始
    /// </summary>
    public void StartEpisode(EpisodeData episodeData)
    {
        if (episodeData == null) return;

        CurrentEpisodeData = episodeData;
        CurrentEpisodeNumber = episodeData.episodeIndex;

        string sceneName = !string.IsNullOrEmpty(episodeData.nextSceneName)
            ? episodeData.nextSceneName
            : $"Episode_{episodeData.episodeIndex:D3}";

        Debug.Log($"シーン読み込み: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 指定したエピソードを開始（3桁フォーマット："Episode_001", "Episode_002" など）
    /// </summary>
    public void StartEpisode(int episodeNumber)
    {
        CurrentEpisodeNumber = episodeNumber;

        // :D3 で「数字を3桁でゼロ埋め（1 -> 001）」にする
        string sceneName = $"Episode_{episodeNumber:D3}";

        Debug.Log($"シーン読み込み: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 現在のエピソードをクリアした時に呼ぶ処理（自動セーブ＆リザルト用データ保存）
    /// </summary>
    public void CompleteCurrentEpisode(EpisodeData episodeData = null)
    {
        if (episodeData != null)
        {
            CurrentEpisodeData = episodeData;
            CurrentEpisodeNumber = episodeData.episodeIndex;
        }

        // 初めてクリアしたエピソードの場合のみ記録を更新
        if (CurrentEpisodeNumber > ClearedEpisodeIndex)
        {
            ClearedEpisodeIndex = CurrentEpisodeNumber;
            SaveProgress(); // ★自動セーブを実行
        }
    }

    // ================================================
    //  Easy Save (ES3) によるセーブ・ロード
    // ================================================

    private void SaveProgress()
    {
        ES3.Save(SAVE_KEY_CLEARED_EP, ClearedEpisodeIndex);
        Debug.Log($"[EasySave] 自動セーブ完了: クリア済みエピソード {ClearedEpisodeIndex}");
    }

    private void LoadProgress()
    {
        ClearedEpisodeIndex = ES3.Load(SAVE_KEY_CLEARED_EP, defaultValue: 0);
        Debug.Log($"[EasySave] ロード完了: クリア済みエピソード {ClearedEpisodeIndex}");
    }

    // ================================================
    //  デバッグ用機能
    // ================================================

    // Unityエディタのインスペクター上でコンポーネント名を右クリック ➔ "Reset Save Data" で実行可能
    [ContextMenu("Reset Save Data")]
    public void ResetSaveData()
    {
        ES3.DeleteKey(SAVE_KEY_CLEARED_EP);
        ClearedEpisodeIndex = 0;
        Debug.Log("[EasySave] セーブデータを初期化しました。");
    }
}