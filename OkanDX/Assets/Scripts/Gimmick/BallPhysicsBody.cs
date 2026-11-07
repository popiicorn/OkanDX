using UnityEngine;

/// <summary>
/// BallPhysicsArea の中で、重力で落ちて跳ね、転がるボール。
/// DraggableUIObject でドラッグ中はマウスに従い、離すとそのときの勢いで動き出す。
/// 座標はすべて親（View）の中の座標で、計算は BallPhysicsArea が行う。
/// </summary>
[RequireComponent(typeof(DraggableUIObject))]
public class BallPhysicsBody : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ ボールの形")]
    [Tooltip("当たり判定の半径。0 なら画像の幅と高さの平均から自動で決める")]
    [SerializeField] private float radius = 0f;

    [Header("■ ボールの性質")]
    [Tooltip("跳ねやすさ（0: 跳ねない / 1: 同じ高さまで跳ね返る）")]
    [Range(0f, 1f)]
    [SerializeField] private float bounciness = 0.55f;

    [Tooltip("シーソー（SeesawTilt）に乗ったときにかかる重さ")]
    [SerializeField] private float weight = 1f;

    [Tooltip("離したときに引き継ぐ勢いの上限（1 秒あたりのピクセル）")]
    [SerializeField] private float maxReleaseSpeed = 1200f;

    // 変化がこの値より小さいときは Transform を書き換えない（止まっている間の UI 再描画を防ぐ）
    private const float APPLY_EPSILON = 0.01f;

    // ドラッグ中の勢いを求めるときの平滑化の強さ
    private const float DRAG_VELOCITY_SMOOTHING = 20f;

    private RectTransform rectTransform;
    private DraggableUIObject draggable;
    private float angle;

    /// <summary>親の中での位置</summary>
    public Vector2 Position { get; set; }

    /// <summary>速度（1 秒あたりのピクセル）</summary>
    public Vector2 Velocity { get; set; }

    /// <summary>回転の速さ（1 秒あたりの角度）</summary>
    public float Spin { get; set; }

    /// <summary>当たり判定の半径</summary>
    public float Radius { get; private set; }

    /// <summary>跳ねやすさ</summary>
    public float Bounciness => bounciness;

    /// <summary>ボール同士がぶつかるときの重さ（大きいボールほど重い）</summary>
    public float Mass => Radius * Radius;

    /// <summary>シーソーにかかる重さ</summary>
    public float Weight => weight;

    /// <summary>ドラッグ中か</summary>
    public bool IsDragging => draggable != null && draggable.IsDragging;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 参照と、現在の位置・半径を読み込む
    /// </summary>
    public void Initialize()
    {
        rectTransform = GetComponent<RectTransform>();
        draggable = GetComponent<DraggableUIObject>();

        Rect rect = rectTransform.rect;
        Radius = radius > 0f ? radius : (rect.width + rect.height) * 0.25f;
        Position = rectTransform.localPosition;
        angle = rectTransform.localEulerAngles.z;
    }

    private void OnEnable()
    {
        if (draggable == null) return;

        draggable.DragStarted += OnDragStarted;
        draggable.DragEnded += OnDragEnded;
    }

    private void OnDisable()
    {
        if (draggable == null) return;

        draggable.DragStarted -= OnDragStarted;
        draggable.DragEnded -= OnDragEnded;
    }

    // ================================================
    // ドラッグ
    // ================================================
    // ----
    // ドラッグ開始：物理の動きを止める
    // ----
    private void OnDragStarted()
    {
        Velocity = Vector2.zero;
        Spin = 0f;
    }

    // ----
    // ドラッグ終了：勢いが強すぎないようにして動き出す
    // ----
    private void OnDragEnded()
    {
        Position = rectTransform.localPosition;
        Velocity = Vector2.ClampMagnitude(Velocity, maxReleaseSpeed);
    }

    /// <summary>
    /// ドラッグ中の位置を読み取り、勢いを記録する
    /// </summary>
    public void TrackDrag(float deltaTime)
    {
        Vector2 current = rectTransform.localPosition;
        Vector2 dragVelocity = (current - Position) / deltaTime;

        Velocity = Vector2.Lerp(Velocity, dragVelocity, Mathf.Clamp01(DRAG_VELOCITY_SMOOTHING * deltaTime));
        Position = current;
    }

    /// <summary>
    /// 指定した位置に移動させ、止まった状態にする（そこから落ち始める）
    /// </summary>
    public void Teleport(Vector2 position)
    {
        if (rectTransform == null) Initialize();

        Position = position;
        Velocity = Vector2.zero;
        Spin = 0f;
        rectTransform.localPosition = new Vector3(position.x, position.y, rectTransform.localPosition.z);
    }

    // ================================================
    // 表示への反映
    // ================================================
    /// <summary>
    /// 回転を進める
    /// </summary>
    public void AdvanceRotation(float deltaTime)
    {
        angle = Mathf.Repeat(angle + Spin * deltaTime, 360f);
    }

    /// <summary>
    /// 計算した位置と回転を画面に反映する
    /// </summary>
    public void ApplyToTransform()
    {
        Vector3 current = rectTransform.localPosition;
        if (!IsDragging && ((Vector2)current - Position).sqrMagnitude > APPLY_EPSILON * APPLY_EPSILON)
        {
            rectTransform.localPosition = new Vector3(Position.x, Position.y, current.z);
        }

        if (Mathf.Abs(Mathf.DeltaAngle(rectTransform.localEulerAngles.z, angle)) > APPLY_EPSILON)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
