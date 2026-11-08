using UnityEngine;
using DG.Tweening;

/// <summary>
/// クリックすると、指定したオブジェクトを非表示にして、別のオブジェクトを表示する。
/// 閉じたロッカーと開いたロッカーのように、2 つのオブジェクトにお互いを指定すれば何回でも開け閉めできる。
/// ClickableObject と同じオブジェクトに付ける（ClickableObject の通常の処理（メッセージ表示など）は行われず、押した演出だけ出る）。
/// </summary>
[RequireComponent(typeof(ClickableObject))]
public class ClickToggleObjects : MonoBehaviour, IClickInterceptor
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ クリックしたときの切り替え")]
    [Tooltip("非表示にするオブジェクト（空なら自分自身）")]
    [SerializeField] private GameObject[] hideObjects;

    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showObjects;

    [Header("■ 表示したときの演出")]
    [Tooltip("ON にすると、表示したオブジェクトがポヨンと動く")]
    [SerializeField] private bool playPopOnShow = true;

    [Tooltip("ポヨンの最初の大きさ")]
    [SerializeField] private float popStartScale = 0.9f;

    [Tooltip("ポヨンにかける時間（秒）")]
    [SerializeField] private float popDuration = 0.2f;

    // ================================================
    // クリック処理（ClickableObject から呼ばれる）
    // ================================================
    /// <summary>
    /// 表示を切り替える
    /// </summary>
    public bool TryInterceptClick()
    {
        if (showObjects != null)
        {
            foreach (var obj in showObjects)
            {
                if (obj == null) continue;

                obj.SetActive(true);
                if (playPopOnShow) PlayPop(obj.transform);
            }
        }

        if (hideObjects == null || hideObjects.Length == 0)
        {
            gameObject.SetActive(false);
        }
        else
        {
            foreach (var obj in hideObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        return true;
    }

    // ----
    // 少し小さい状態から、ポヨンと元の大きさに戻す
    // ----
    private void PlayPop(Transform target)
    {
        target.DOKill();
        target.localScale = Vector3.one * popStartScale;
        target.DOScale(Vector3.one, popDuration)
            .SetEase(Ease.OutBack)
            .SetLink(target.gameObject);
    }
}
