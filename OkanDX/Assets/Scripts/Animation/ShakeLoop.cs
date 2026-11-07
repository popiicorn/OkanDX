using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI オブジェクトを表示中ずっと、元の位置を中心に小刻みにブルブル震わせる。
/// ClickableObject のクリック演出（transform.DOKill）で止まらないよう、Transform を対象にしない Tween で動かす。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ShakeLoop : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 震えの設定")]
    [Tooltip("震える幅（ピクセル）")]
    [SerializeField] private Vector2 strength = new Vector2(3f, 3f);

    [Tooltip("1 秒あたりに震える回数")]
    [SerializeField] private int vibrato = 30;

    [Tooltip("震える方向のばらつき（0〜180）")]
    [Range(0f, 180f)]
    [SerializeField] private float randomness = 90f;

    // 1 ループの長さ（秒）
    private const float LOOP_DURATION = 1f;

    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Vector3 shakeOffset;
    private Tween shakeTween;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        basePosition = rectTransform.anchoredPosition;
        shakeOffset = Vector3.zero;

        shakeTween = DOTween.Shake(
                () => shakeOffset,
                offset =>
                {
                    shakeOffset = offset;
                    rectTransform.anchoredPosition = basePosition + (Vector2)offset;
                },
                LOOP_DURATION,
                new Vector3(strength.x, strength.y, 0f),
                vibrato,
                randomness,
                false)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (shakeTween != null) shakeTween.Kill();

        // 元の位置に戻す
        rectTransform.anchoredPosition = basePosition;
    }
}
