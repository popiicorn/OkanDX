using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // どこからでも GameManager.Instance でアクセスできるようにする（シングルトン）
    public static GameManager Instance { get; private set; }

    [Header("進行データ")]
    // クリア済みの最大ステージ進行順（例: 0=未クリア、1=ステージ1クリア）
    public int ClearedEpisodeIndex { get; private set; } = 0;

    // 現在プレイ中のエピソード番号
    public int CurrentEpisodeNumber { get; private set; } = 1;

    // ★リザルト画面に渡す現在のクリアデータ
    public EpisodeData CurrentEpisodeData { get; set; }

    // ★EpisodeTitleUI等から参照用（CurrentEpisodeDataのエイリアス）
    public EpisodeData CurrentEpisode => CurrentEpisodeData;

    // ★クリア処理中/リザルト表示中かどうかのフラグ（タイトルUIの割り込み防止用）
    public bool IsClearing { get; set; } = false;

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
    //  自動UI生成イベントの登録
    // ================================================

    private void OnEnable()
    {
        // シーン読み込み完了イベントを登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // イベント解除（メモリリーク防止）
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// シーン読み込み完了時に自動実行される処理
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ★ 新しいシーンがロードされたので、まず IsClearing を解除する！
        bool wasClearing = IsClearing;
        IsClearing = false;

        // タイトル画面やエピソード選択画面、リザルト画面ではタイトルUIを出さない
        if (scene.name == "Title" || scene.name == "EpisodeSelect" || scene.name.Contains("Result"))
        {
            return;
        }

        // エピソードまたはミニゲームのプレイシーン開始時のみタイトルUIを自動生成
        if (scene.name.StartsWith("Episode_") || scene.name.StartsWith("MiniGame_"))
        {
            SpawnEpisodeTitleUI();
        }
    }

    /// <summary>
    /// Resources/UI/EpisodeTitlePanel を読み込んで現在のCanvas内に生成する
    /// </summary>
    private void SpawnEpisodeTitleUI()
    {
        // シーン内の Canvas を検索
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogWarning("[GameManager] シーン内に Canvas が見つかりませんでした。");
            return;
        }

        // Resources/UI/EpisodeTitlePanel をロード
        GameObject prefab = Resources.Load<GameObject>("UI/EpisodeTitlePanel");
        if (prefab != null)
        {
            Instantiate(prefab, mainCanvas.transform);
        }
        else
        {
            Debug.LogWarning("[GameManager] Resources/UI/EpisodeTitlePanel が見つかりませんでした。フォルダ配置を確認してください。");
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
        IsClearing = false;
        SceneManager.LoadScene("Title");
    }

    /// <summary>
    /// エピソード選択画面へ移動
    /// </summary>
    public void GoToEpisodeSelect()
    {
        IsClearing = false;
        SceneManager.LoadScene("EpisodeSelect");
    }

    /// <summary>
    /// 指定したエピソードを EpisodeData を使用して開始
    /// </summary>
    public void StartEpisode(EpisodeData episodeData)
    {
        if (episodeData == null) return;

        IsClearing = false; // 新しいエピソードを開始するのでクリアフラグを解除

        CurrentEpisodeData = episodeData;
        CurrentEpisodeNumber = episodeData.episodeIndex;

        string sceneName = "";

        if (!string.IsNullOrEmpty(episodeData.nextSceneName))
        {
            sceneName = episodeData.nextSceneName;
        }
        else if (episodeData.episodeType == EpisodeType.MiniGame)
        {
            sceneName = $"MiniGame_{episodeData.episodeIndex:D3}";
        }
        else
        {
            sceneName = $"Episode_{episodeData.episodeIndex:D3}";
        }

        Debug.Log($"シーン読み込み: {sceneName} (種類: {episodeData.episodeType})");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 指定した番号のエピソードを開始（stageOrder ➔ episodeIndex の順で検索）
    /// </summary>
    public void StartEpisode(int episodeNumber)
    {
        CurrentEpisodeNumber = episodeNumber;

        EpisodeData loadedData = GetEpisodeData(episodeNumber);

        if (loadedData != null)
        {
            StartEpisode(loadedData);
        }
        else
        {
            Debug.LogError($"[GameManager] 番号 {episodeNumber} の EpisodeData が見つかりませんでした。Resources/EpisodeData/ フォルダ内を確認してください。");
        }
    }

    /// <summary>
    /// 指定した進行順 (stageOrder) またはエピソード番号から EpisodeData を自動検索して取得する
    /// </summary>
    public EpisodeData GetEpisodeData(int number)
    {
        // 全アセットを取得
        EpisodeData[] allData = Resources.LoadAll<EpisodeData>("EpisodeData");

        // ① まず stageOrder（進行順）が一致するものを探す
        foreach (var d in allData)
        {
            if (d != null && d.stageOrder == number)
            {
                return d;
            }
        }

        // ② 見つからなければ従来の episodeIndex（エピソード番号）で探す（互換性確保）
        foreach (var d in allData)
        {
            if (d != null && d.episodeIndex == number)
            {
                return d;
            }
        }

        // ③ それでも見つからなければファイル名で直接ロードしてみる
        string epPath = $"EpisodeData/EpisodeData_{number:D3}";
        EpisodeData fileData = Resources.Load<EpisodeData>(epPath);
        if (fileData != null) return fileData;

        string miniPath = $"EpisodeData/MiniGameData_{number:D3}";
        fileData = Resources.Load<EpisodeData>(miniPath);
        if (fileData != null) return fileData;

        return null;
    }

    /// <summary>
    /// 現在のエピソードをクリアした時に呼ぶ処理（自動セーブ＆リザルト用データ保存）
    /// </summary>
    public void CompleteCurrentEpisode(EpisodeData episodeData = null)
    {
        IsClearing = true; // クリア処理が始まったのでフラグを立てる（UI割り込み防止）

        if (episodeData != null)
        {
            CurrentEpisodeData = episodeData;
            CurrentEpisodeNumber = episodeData.episodeIndex;
        }

        // 進行順（stageOrder）を判定用基準にする（無ければ episodeIndex）
        int currentProgressIndex = (CurrentEpisodeData != null && CurrentEpisodeData.stageOrder > 0)
            ? CurrentEpisodeData.stageOrder
            : CurrentEpisodeNumber;

        // 初めてクリアしたステージの場合のみ記録を更新
        if (currentProgressIndex > ClearedEpisodeIndex)
        {
            ClearedEpisodeIndex = currentProgressIndex;
            SaveProgress(); // 自動セーブを実行
        }
    }

    // ================================================
    //  Easy Save (ES3) によるセーブ・ロード
    // ================================================

    private void SaveProgress()
    {
        ES3.Save(SAVE_KEY_CLEARED_EP, ClearedEpisodeIndex);
        Debug.Log($"[EasySave] 自動セーブ完了: クリア済みステージ進行順 {ClearedEpisodeIndex}");
    }

    private void LoadProgress()
    {
        ClearedEpisodeIndex = ES3.Load(SAVE_KEY_CLEARED_EP, defaultValue: 0);
        Debug.Log($"[EasySave] ロード完了: クリア済みステージ進行順 {ClearedEpisodeIndex}");
    }

    // ================================================
    //  デバッグ用機能
    // ================================================

    [ContextMenu("Reset Save Data")]
    public void ResetSaveData()
    {
        ES3.DeleteKey(SAVE_KEY_CLEARED_EP);
        ClearedEpisodeIndex = 0;
        Debug.Log("[EasySave] セーブデータを初期化しました。");
    }
}