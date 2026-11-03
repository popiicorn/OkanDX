using UnityEngine;
using DG.Tweening;

/// <summary>
/// SwitchAfterDelay を呼ぶと、指定秒数後にオブジェクトの表示／非表示を切り替える。
/// 例: アイテムを拾ってから 1 秒後に、開いたロッカーを閉じた状態に戻す。
/// ActivateEventTrigger 等の UnityEvent から呼び出して使う。
/// このオブジェクト自身が非表示になっても待ち時間は止まらない（DOTween で待つため）。
/// </summary>
public class DelayedObjectSwitcher : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 待ち時間")]
    [Tooltip("SwitchAfterDelay が呼ばれてから切り替えるまでの秒数")]
    [SerializeField] private float delay = 1f;

    [Header("■ 切り替えるオブジェクト")]
    [Tooltip("非表示にするオブジェクト")]
    [SerializeField] private GameObject[] hideObjects;
    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showObjects;

    [Header("■ 呼び出し回数")]
    [Tooltip("ON: 最初の 1 回だけ切り替える / OFF: 呼ばれるたびに切り替える")]
    [SerializeField] private bool switchOnce = true;

    private bool hasSwitched = false;
    private Tween delayTween;

    private void OnDestroy()
    {
        if (delayTween != null) delayTween.Kill();
    }

    /// <summary>
    /// delay 秒待ってから、表示／非表示を切り替える
    /// </summary>
    public void SwitchAfterDelay()
    {
        if (switchOnce && hasSwitched) return;
        hasSwitched = true;

        if (delayTween != null) delayTween.Kill();
        delayTween = DOVirtual.DelayedCall(delay, SwitchNow).SetLink(gameObject);
    }

    /// <summary>
    /// 待たずにすぐ表示／非表示を切り替える
    /// </summary>
    public void SwitchNow()
    {
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
