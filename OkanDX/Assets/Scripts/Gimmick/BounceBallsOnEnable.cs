using UnityEngine;

/// <summary>
/// このオブジェクトが表示されるたびに、BallPhysicsArea のボールを全部ポンッと跳ね上げる。
/// 例: トレーナーが殴られる（ダメージの絵が出る）たびに、部屋のボールが衝撃で跳ねる。
/// </summary>
public class BounceBallsOnEnable : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 跳ね上げるボール")]
    [Tooltip("ボールを計算している BallPhysicsArea")]
    [SerializeField] private BallPhysicsArea area;

    [Header("■ 跳ねる強さ")]
    [Tooltip("跳ね上がる高さの最小（ピクセル）")]
    [SerializeField] private float minHeight = 150f;

    [Tooltip("跳ね上がる高さの最大（ピクセル）")]
    [SerializeField] private float maxHeight = 200f;

    [Tooltip("左右に散らばる速さの最大（1 秒あたりのピクセル。0 なら真上に跳ねる）")]
    [SerializeField] private float maxSideSpeed = 250f;

    // ================================================
    // 表示
    // ================================================
    private void OnEnable()
    {
        if (area == null) return;

        // 高さ h まで跳ねる速さ = √(2 × 重力 × h)
        float minUp = Mathf.Sqrt(2f * area.Gravity * Mathf.Max(0f, minHeight));
        float maxUp = Mathf.Sqrt(2f * area.Gravity * Mathf.Max(0f, maxHeight));
        area.BounceAll(minUp, maxUp, maxSideSpeed);
    }
}
