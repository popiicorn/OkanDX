using UnityEngine;

/// <summary>
/// このオブジェクトが初めて表示されたときだけ、指定したオブジェクトを非表示にして、別のオブジェクトを表示する。
/// 2 回目以降に表示されたときは何もしない。
/// 例: トレーナーが初めて殴られたときだけ、キャラを吹っ飛んだ絵に切り替える。
/// </summary>
public class FirstEnableSwap : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 初めて表示されたときの切り替え")]
    [Tooltip("非表示にするオブジェクト")]
    [SerializeField] private GameObject[] hideObjects;

    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showObjects;

    private bool hasSwapped = false;

    // ================================================
    // 表示
    // ================================================
    private void OnEnable()
    {
        if (hasSwapped) return;
        hasSwapped = true;

        if (hideObjects != null)
        {
            foreach (var obj in hideObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (showObjects != null)
        {
            foreach (var obj in showObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
    }
}
