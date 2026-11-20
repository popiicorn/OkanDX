using UnityEngine;
using DG.Tweening;

/// <summary>
/// このオブジェクトが表示されてから指定した秒数たったら、指定したオブジェクトを非表示にして、別のオブジェクトを表示する。
/// 例: 殴られた絵を 1 秒見せてから、怒った絵に切り替える。
/// </summary>
public class TimedObjectSwap : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 切り替えるまでの時間")]
    [Tooltip("表示されてから切り替えるまでの秒数")]
    [SerializeField] private float delay = 1f;

    [Header("■ 切り替え")]
    [Tooltip("非表示にするオブジェクト（空なら自分自身）")]
    [SerializeField] private GameObject[] hideObjects;

    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showObjects;

    private Tween delayTween;

    // ================================================
    // 表示・非表示
    // ================================================
    private void OnEnable()
    {
        if (delayTween != null) delayTween.Kill();
        delayTween = DOVirtual.DelayedCall(delay, Swap).SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (delayTween != null) delayTween.Kill();
        delayTween = null;
    }

    // ----
    // 表示を切り替える
    // ----
    private void Swap()
    {
        delayTween = null;

        if (showObjects != null)
        {
            foreach (var obj in showObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (hideObjects == null || hideObjects.Length == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        foreach (var obj in hideObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
