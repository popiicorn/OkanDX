using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI オブジェクトを表示中ずっと、振り子のように左右に揺らす。
/// 回転の中心は RectTransform の Pivot（吊るしている点に Pivot を合わせて使う）。
/// ClickableObject のクリック演出（transform.DOKill）で止まらないよう、Transform を対象にしない Tween で動かす。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SwingLoop : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 揺れの設定")]
    [Tooltip("左右に揺れる角度（最初の角度からの差）")]
    [SerializeField] private float swingAngle = 5f;

    [Tooltip("片側から反対側まで揺れるのにかかる時間（秒）")]
    [SerializeField] private float halfPeriod = 1.2f;

    [Tooltip("揺れの動き方")]
    [SerializeField] private Ease ease = Ease.InOutSine;

    private RectTransform rectTransform;
    private float baseAngle;
    private float currentSwing;
    private Tween swingTween;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseAngle = rectTransform.localEulerAngles.z;
    }

    private void OnEnable()
    {
        currentSwing = -swingAngle;

        swingTween = DOTween.To(
                () => currentSwing,
                value =>
                {
                    currentSwing = value;
                    rectTransform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + value);
                },
                swingAngle,
                halfPeriod)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

        // 真ん中（まっすぐ吊るされた状態）から揺れ始める
        swingTween.Goto(halfPeriod * 0.5f, true);
    }

    private void OnDisable()
    {
        if (swingTween != null) swingTween.Kill();

        // 元の角度に戻す
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, baseAngle);
    }
}
