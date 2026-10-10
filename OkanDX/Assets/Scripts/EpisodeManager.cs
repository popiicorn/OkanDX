using UnityEngine;



public class EpisodeManager : MonoBehaviour

{

    [Header("■ このエピソード用ヒントデータ")]

    [SerializeField] private HintData episodeHintData;



    private void Start()

    {

        // ★テスト用に 1,000 円追加する

        HesokuriManager.Instance.AddHesokuri(1000);



        // ★シーン開始時に HintModalUI へこのエピソードのヒントデータを渡す

        if (HintModalUI.Instance != null && episodeHintData != null)

        {

            HintModalUI.Instance.SetHintData(episodeHintData);

        }

    }



    /// <summary>
    /// 脱出成功（ゴールボタン押下）時
    /// </summary>
    public void OnClearEpisode()
    {
        if (GameManager.Instance != null)
        {
            // クリアフラグを立てて EpisodeTitlePanel の誤表示を防ぐ
            GameManager.Instance.IsClearing = true;

            // クリア記録をセーブ
            GameManager.Instance.CompleteCurrentEpisode();

            // ★ ここでリザルトモーダル（UI）を開く、またはリザルトシーンへ遷移
            // 例: ResultModalUI.Instance.Show();
        }
    }


    /// <summary>

    /// アプリを諦めてエピソード選択に戻るボタン用

    /// </summary>

    public void OnClickGiveUp()

    {

        if (GameManager.Instance != null)

        {

            GameManager.Instance.GoToEpisodeSelect();

        }

    }

}

