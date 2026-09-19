using UnityEngine;

public class HesokuriManager : MonoBehaviour
{
    private static HesokuriManager instance;

    /// <summary>
    /// シーン上に存在しない場合でも自動でオブジェクトを生成してインスタンスを返す Singleton
    /// </summary>
    public static HesokuriManager Instance
    {
        get
        {
            if (instance == null)
            {
                // シーン内を検索
                instance = FindFirstObjectByType<HesokuriManager>();

                // それでも見つからない場合は自動生成
                if (instance == null)
                {
                    GameObject obj = new GameObject("HesokuriManager");
                    instance = obj.AddComponent<HesokuriManager>();
                }
            }
            return instance;
        }
    }

    private const string SAVE_KEY_HESOKURI = "PlayerHesokuriAmount";

    /// <summary>現在の所持へそくり数</summary>
    public int CurrentHesokuri { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // シーン切替でも破棄しない
            LoadHesokuri();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// へそくりを増やす（拾った時・ミニゲーム勝利時）
    /// </summary>
    public void AddHesokuri(int amount)
    {
        CurrentHesokuri += amount;
        SaveHesokuri();
    }

    /// <summary>
    /// へそくりを消費する（消費できたら true）
    /// </summary>
    public bool ConsumeHesokuri(int amount)
    {
        if (CurrentHesokuri >= amount)
        {
            CurrentHesokuri -= amount;
            SaveHesokuri();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 特定のヒントが解放済みか判定
    /// </summary>
    public bool IsHintUnlocked(string hintKey)
    {
        return PlayerPrefs.GetInt("Unlocked_" + hintKey, 0) == 1;
    }

    /// <summary>
    /// ヒントを解放済みとして保存
    /// </summary>
    public void UnlockHint(string hintKey)
    {
        PlayerPrefs.SetInt("Unlocked_" + hintKey, 1);
        PlayerPrefs.Save();
    }

    private void SaveHesokuri()
    {
        PlayerPrefs.SetInt(SAVE_KEY_HESOKURI, CurrentHesokuri);
        PlayerPrefs.Save();
    }

    private void LoadHesokuri()
    {
        // 初期値（デフォルト: 0円）
        CurrentHesokuri = PlayerPrefs.GetInt(SAVE_KEY_HESOKURI, 0);
    }
}