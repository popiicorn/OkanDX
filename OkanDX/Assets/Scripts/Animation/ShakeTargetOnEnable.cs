using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// このオブジェクトが表示された瞬間に、指定した RectTransform（部屋の View など）をガガッと揺らす。
/// 指定したアイテムを選んだ状態でこのオブジェクトをクリックしたときも揺らせる（ClickableObject の処理はそのまま行われる）。
/// 揺れは位置への「上乗せ」で動かすので、揺れている途中で View の切り替え（スライド）が起きても位置がずれない。
/// </summary>
public class ShakeTargetOnEnable : MonoBehaviour, IPointerClickHandler
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 揺らす対象")]
    [Tooltip("揺らす RectTransform（例: View_2）。UI を揺らしたくないときは部屋の View を指定する")]
    [SerializeField] private RectTransform target;

    [Header("■ 揺れの設定")]
    [Tooltip("揺れる時間（秒）")]
    [SerializeField] private float duration = 0.5f;

    [Tooltip("揺れる幅（ピクセル）")]
    [SerializeField] private Vector2 strength = new Vector2(30f, 30f);

    [Tooltip("揺れの細かさ（大きいほど激しく小刻みに揺れる）")]
    [SerializeField] private int vibrato = 40;

    [Tooltip("揺れる方向のばらつき（0〜180）")]
    [Range(0f, 180f)]
    [SerializeField] private float randomness = 90f;

    [Tooltip("揺れ始めるまでの待ち時間（秒）")]
    [SerializeField] private float delay = 0f;

    [Header("■ 表示されたときに揺らす")]
    [Tooltip("ON にすると、このオブジェクトが表示された瞬間に揺らす")]
    [SerializeField] private bool shakeOnEnable = true;

    [Header("■ クリックでも揺らす")]
    [Tooltip("このアイテムを選んだ状態でクリックしたときにも揺らす（空ならクリックでは揺らさない）")]
    [SerializeField] private string shakeOnClickItemID = "";

    private Tween shakeTween;
    private Vector3 currentOffset;

    // ================================================
    // 表示・非表示
    // ================================================
    private void OnEnable()
    {
        if (shakeOnEnable) PlayShake();
    }

    private void OnDisable()
    {
        StopShake();
    }

    // ================================================
    // クリック
    // ================================================
    /// <summary>
    /// 指定したアイテムを選んでいれば揺らす
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(shakeOnClickItemID) || InventoryManager.Instance == null) return;

        Item selected = InventoryManager.Instance.SelectedItem;
        if (selected != null && selected.id == shakeOnClickItemID) PlayShake();
    }

    /// <summary>
    /// 対象を揺らす
    /// </summary>
    public void PlayShake()
    {
        if (target == null) return;

        StopShake();

        shakeTween = DOTween.Shake(
                () => currentOffset,
                offset =>
                {
                    // 前回の揺れを取り除いてから、今回の揺れを上乗せする
                    target.anchoredPosition += (Vector2)(offset - currentOffset);
                    currentOffset = offset;
                },
                duration,
                new Vector3(strength.x, strength.y, 0f),
                vibrato,
                randomness,
                true)
            .SetDelay(delay)
            .SetLink(gameObject)
            .OnComplete(RemoveOffset);
    }

    // ----
    // 揺れを止めて、上乗せした分を元に戻す
    // ----
    private void StopShake()
    {
        if (shakeTween != null) shakeTween.Kill();
        shakeTween = null;
        RemoveOffset();
    }

    private void RemoveOffset()
    {
        if (target != null) target.anchoredPosition -= (Vector2)currentOffset;
        currentOffset = Vector3.zero;
    }
}
