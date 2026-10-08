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
        // ★ "Episode_" または "MiniGame_" から始まるシーン、あるいは EpisodeData が存在するときにタイトルUIを自動生成
        if (scene.name.StartsWith("Episode_") || scene.name.StartsWith("MiniGame_") || CurrentEpisodeData != null)
        {
            // タイトル画面や選択画面などの基本画面でなければ表示
            if (scene.name != "Title" && scene.name != "EpisodeSelect")
            {
                SpawnEpisodeTitleUI();
            }
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

        string sceneName = "";

        // ★ nextSceneName が指定されていればそれを優先、無ければ型や番号から自動生成
        if (!string.IsNullOrEmpty(episodeData.nextSceneName))
        {
            sceneName = episodeData.nextSceneName;
        }
        else if (episodeData.episodeType == EpisodeType.MiniGame)
        {
            // ミニゲーム用シーン名（例: "MiniGame_005" など）
            sceneName = $"MiniGame_{episodeData.episodeIndex:D3}";
        }
        else
        {
            // 通常エピソード（例: "Episode_001" など）
            sceneName = $"Episode_{episodeData.episodeIndex:D3}";
        }

        Debug.Log($"シーン読み込み: {sceneName} (種類: {episodeData.episodeType})");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 指定したエピソードを開始（エピソード番号指定）
    /// </summary>
    public void StartEpisode(int episodeNumber)
    {
        CurrentEpisodeNumber = episodeNumber;

        // Resources/EpisodeData/ フォルダからデータを検索
        EpisodeData loadedData = GetEpisodeData(episodeNumber);

        if (loadedData != null)
        {
            // ★ EpisodeData 経由でスタート（これで nextSceneName や MiniGame 判定が正しく反映される！）
            StartEpisode(loadedData);
        }
        else
        {
            Debug.LogError($"[GameManager] エピソード番号 {episodeNumber} (または MiniGameData) の EpisodeData が見つかりませんでした。Resources/EpisodeData/ フォルダ内を確認してください。");
        }
    }

    /// <summary>
    /// 指定したエピソード番号の EpisodeData を Resources から自動検索して取得する
    /// </summary>
    public EpisodeData GetEpisodeData(int episodeNumber)
    {
        // ① まず "EpisodeData_005" などの名前を探す
        string epPath = $"EpisodeData/EpisodeData_{episodeNumber:D3}";
        EpisodeData data = Resources.Load<EpisodeData>(epPath);

        if (data != null) return data;

        // ② 見つからなければ "MiniGameData_005" や "MiniGameData_001" などの名前を探す
        string miniPath = $"EpisodeData/MiniGameData_{episodeNumber:D3}";
        data = Resources.Load<EpisodeData>(miniPath);

        if (data != null) return data;

        // ③ それでも見つからない場合、Resources/EpisodeData/ 内の全アセットから episodeIndex が一致するものを探す
        EpisodeData[] allData = Resources.LoadAll<EpisodeData>("EpisodeData");
        foreach (var d in allData)
        {
            if (d != null && d.episodeIndex == episodeNumber)
            {
                return d;
            }
        }

        return null;
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