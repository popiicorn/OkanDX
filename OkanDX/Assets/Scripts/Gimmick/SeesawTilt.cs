using UnityEngine;

/// <summary>
/// 上に乗ったボールの重さで、シーソーのように傾くオブジェクト。
/// BallPhysicsArea の線の followTarget に指定したオブジェクトに付けると、
/// 乗っているボールの重さと位置から傾く角度を決め、ばねのように揺れながらその角度に近づく。
/// 回転の中心は RectTransform の Pivot。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SeesawTilt : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 傾きやすさ")]
    [Tooltip("重さ 1 のボールが、回転の中心から 100 離れた場所に乗ったときに傾く角度")]
    [SerializeField] private float degreesPerWeight = 4f;

    [Tooltip("傾く角度の上限（最初の角度からの差）")]
    [SerializeField] private float maxAngle = 20f;

    [Header("■ 傾く動き")]
    [Tooltip("目標の角度へ戻ろうとする強さ（大きいほど素早く傾く）")]
    [SerializeField] private float stiffness = 40f;

    [Tooltip("揺れの収まりやすさ（大きいほど揺り戻しが少ない）")]
    [SerializeField] private float damping = 8f;

    // 「100 離れた場所」の 100
    private const float REFERENCE_DISTANCE = 100f;

    private RectTransform rectTransform;
    private float baseAngle;
    private float angle;
    private float angularVelocity;
    private float loadMoment;

    /// <summary>最初の角度からの、今の傾き</summary>
    public float TiltAngle => angle;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 今の角度を、ボールが乗っていないときの角度として記録する
    /// </summary>
    public void Initialize()
    {
        rectTransform = GetComponent<RectTransform>();
        baseAngle = rectTransform.localEulerAngles.z;
        angle = 0f;
        angularVelocity = 0f;
        loadMoment = 0f;
    }

    // ================================================
    // 傾きの計算（BallPhysicsArea から呼ばれる）
    // ================================================
    /// <summary>
    /// ボールが乗っている重さを加える
    /// </summary>
    /// <param name="horizontalDistance">回転の中心からの横の距離（右がプラス）</param>
    /// <param name="weight">ボールの重さ</param>
    public void AddLoad(float horizontalDistance, float weight)
    {
        loadMoment += horizontalDistance * weight;
    }

    /// <summary>
    /// 加えられた重さから傾きを 1 ステップ進める
    /// </summary>
    public void Step(float deltaTime)
    {
        // 右に重さがかかると時計回り（マイナスの角度）に傾く
        float targetAngle = Mathf.Clamp(-loadMoment / REFERENCE_DISTANCE * degreesPerWeight, -maxAngle, maxAngle);
        loadMoment = 0f;

        float acceleration = (targetAngle - angle) * stiffness - angularVelocity * damping;
        angularVelocity += acceleration * deltaTime;
        angle = Mathf.Clamp(angle + angularVelocity * deltaTime, -maxAngle, maxAngle);

        rectTransform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + angle);
    }

    /// <summary>
    /// 回転の中心の位置（space の中の座標）
    /// </summary>
    public Vector2 GetPivotPosition(Transform space)
    {
        return space.InverseTransformPoint(rectTransform.position);
    }
}
