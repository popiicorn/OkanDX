using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// UIボタンを押した時に「ポヨン」と拡縮させる汎用アニメーションスクリプト
/// </summary>
public class UIButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("アニメーション設定")]
    [Tooltip("押した時に縮むスケール倍率 (デフォルト: 0.85)")]
    [SerializeField] private float pressScale = 0.85f;

    [Tooltip("縮むまでの時間（秒）")]
    [SerializeField] private float pressDuration = 0.08f;

    [Tooltip("元に戻るまでの時間（秒）")]
    [SerializeField] private float releaseDuration = 0.15f;

    private Vector3 defaultScale;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void OnEnable()
    {
        // オブジェクトが有効化したタイミングでスケールを初期化
        transform.DOKill();
        transform.localScale = defaultScale;
    }

    // ボタンを押した瞬間（指・マウスが触れた時）
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(defaultScale * pressScale, pressDuration).SetEase(Ease.OutQuad);
    }

    // ボタンを離した瞬間
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(defaultScale, releaseDuration).SetEase(Ease.OutBack);
    }
}