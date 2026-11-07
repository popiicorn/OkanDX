using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボールが落ちて跳ね、転がって止まる範囲を管理するギミック。
/// 床やキャラクターの形を折れ線（surfaces）で指定し、左右の壁・ボール同士の衝突も計算する。
/// 座標はボールの親（View）の中の座標で、画面の解像度が変わっても動きは変わらない。
/// 折れ線は Scene ビューで緑色の線として確認できる。
/// </summary>
public class BallPhysicsArea : MonoBehaviour
{
    [System.Serializable]
    public class Surface
    {
        [Tooltip("分かりやすい名前（動作には影響しない）")]
        public string name;

        [Tooltip("線の頂点（左から順に並べる）")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("指定すると、このオブジェクトが動いた分だけ線も一緒に動く（空なら動かない）")]
        public RectTransform followTarget;

        [System.NonSerialized] public Vector2 baseTargetPosition;
        [System.NonSerialized] public Vector2 offset;
        [System.NonSerialized] public Vector2 velocity;
    }

    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 動かすボール")]
    [SerializeField] private List<BallPhysicsBody> balls = new List<BallPhysicsBody>();

    [Header("■ ボールが当たる線")]
    [SerializeField] private List<Surface> surfaces = new List<Surface>();

    [Header("■ 見えない壁")]
    [SerializeField] private float leftWallX = -960f;
    [SerializeField] private float rightWallX = 960f;
    [SerializeField] private float ceilingY = 540f;

    [Header("■ 動きの調整")]
    [Tooltip("重力の強さ（大きいほど速く落ちる）")]
    [SerializeField] private float gravity = 2400f;

    [Tooltip("この速さより弱くぶつかったときは跳ねない")]
    [SerializeField] private float minBounceSpeed = 90f;

    [Tooltip("転がっている間に勢いが減る割合（大きいほど早く止まる）")]
    [SerializeField] private float rollingDamping = 0.8f;

    [Tooltip("転がりを止める力（大きいほど、ゆるい坂でも止まる）")]
    [SerializeField] private float rollingResistance = 120f;

    [Tooltip("ボール同士の跳ね返りやすさ")]
    [Range(0f, 1f)]
    [SerializeField] private float ballBounciness = 0.6f;

    [Tooltip("動く線の上でボールが揺さぶられる強さ（大きいほど、線の動きにボールがついていく）")]
    [SerializeField] private float surfaceGrip = 30f;

    // 1 回の計算で進める時間（小さいほど、すり抜けにくい）
    private const float FIXED_STEP = 1f / 240f;

    // 処理落ち時に一度に計算する時間の上限
    private const float MAX_FRAME_TIME = 0.05f;

    // この距離より近いときは向きを計算できないので、真上へ押し出す
    private const float MIN_DISTANCE = 0.0001f;

    private float accumulatedTime = 0f;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 線が追いかけるオブジェクトの基準位置を記録する
    /// </summary>
    public void Initialize()
    {
        foreach (var surface in surfaces)
        {
            surface.offset = Vector2.zero;
            surface.velocity = Vector2.zero;
            if (surface.followTarget != null) surface.baseTargetPosition = surface.followTarget.anchoredPosition;
        }
    }

    // ================================================
    // 更新
    // ================================================
    private void Update()
    {
        Simulate(Time.deltaTime);
    }

    /// <summary>
    /// 指定した時間ぶん、ボールの動きを進める
    /// </summary>
    public void Simulate(float deltaTime)
    {
        UpdateSurfaceOffsets(deltaTime);

        accumulatedTime += Mathf.Min(deltaTime, MAX_FRAME_TIME);

        while (accumulatedTime >= FIXED_STEP)
        {
            accumulatedTime -= FIXED_STEP;
            Step(FIXED_STEP);
        }

        foreach (var ball in balls)
        {
            if (ball != null) ball.ApplyToTransform();
        }
    }

    // ----
    // 追いかけるオブジェクトの動きに合わせて線をずらし、線の速さを求める
    // ----
    private void UpdateSurfaceOffsets(float deltaTime)
    {
        foreach (var surface in surfaces)
        {
            if (surface.followTarget == null) continue;

            Vector2 offset = surface.followTarget.anchoredPosition - surface.baseTargetPosition;
            surface.velocity = deltaTime > 0f ? (offset - surface.offset) / deltaTime : Vector2.zero;
            surface.offset = offset;
        }
    }

    // ----
    // 1 ステップ分の計算
    // ----
    private void Step(float deltaTime)
    {
        foreach (var ball in balls)
        {
            if (ball == null) continue;

            if (ball.IsDragging)
            {
                ball.TrackDrag(deltaTime);
                continue;
            }

            ball.Velocity += Vector2.down * (gravity * deltaTime);
            ball.Position += ball.Velocity * deltaTime;
            ball.AdvanceRotation(deltaTime);

            ResolveSurfaces(ball, deltaTime);
            ResolveWalls(ball);
        }

        for (int i = 0; i < balls.Count; i++)
        {
            for (int j = i + 1; j < balls.Count; j++)
            {
                if (balls[i] != null && balls[j] != null) ResolveBalls(balls[i], balls[j]);
            }
        }
    }

    // ================================================
    // 線との衝突
    // ================================================
    // ----
    // すべての線との衝突を処理し、線に触れていれば転がす
    // ----
    private void ResolveSurfaces(BallPhysicsBody ball, float deltaTime)
    {
        Surface contactSurface = null;
        Vector2 contactNormal = Vector2.up;

        foreach (var surface in surfaces)
        {
            for (int i = 0; i + 1 < surface.points.Count; i++)
            {
                Vector2 start = surface.points[i] + surface.offset;
                Vector2 end = surface.points[i + 1] + surface.offset;

                if (ResolveSegment(ball, start, end, surface.velocity, out Vector2 normal))
                {
                    contactSurface = surface;
                    contactNormal = normal;
                }
            }
        }

        if (contactSurface != null) ApplyRolling(ball, contactNormal, contactSurface.velocity, deltaTime);
    }

    // ----
    // 線分 1 本との衝突：めり込みを戻して跳ね返す
    // ----
    private bool ResolveSegment(BallPhysicsBody ball, Vector2 start, Vector2 end, Vector2 surfaceVelocity, out Vector2 normal)
    {
        Vector2 closest = ClosestPointOnSegment(ball.Position, start, end);
        Vector2 offset = ball.Position - closest;
        float distance = offset.magnitude;

        normal = distance > MIN_DISTANCE ? offset / distance : Vector2.up;
        if (distance >= ball.Radius) return false;

        ball.Position = closest + normal * ball.Radius;

        // 線が動いているときは、線から見た速さで跳ね返す（線に押されたボールが弾む）
        float normalSpeed = Vector2.Dot(ball.Velocity - surfaceVelocity, normal);
        if (normalSpeed < 0f)
        {
            float bounce = -normalSpeed >= minBounceSpeed ? ball.Bounciness : 0f;
            ball.Velocity -= normal * ((1f + bounce) * normalSpeed);
        }
        return true;
    }

    // ----
    // 線に触れている間の転がり：線の動きに揺さぶられ、勢いを減らし、進む方向に絵を回す
    // ----
    private void ApplyRolling(BallPhysicsBody ball, Vector2 normal, Vector2 surfaceVelocity, float deltaTime)
    {
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        float tangentSpeed = Vector2.Dot(ball.Velocity, tangent);
        float surfaceSpeed = Vector2.Dot(surfaceVelocity, tangent);
        float gripRatio = 1f - Mathf.Exp(-surfaceGrip * deltaTime);
        float shakenSpeed = Mathf.Lerp(tangentSpeed, surfaceSpeed, gripRatio);
        float dampedSpeed = shakenSpeed * Mathf.Exp(-rollingDamping * deltaTime);
        dampedSpeed = Mathf.MoveTowards(dampedSpeed, 0f, rollingResistance * deltaTime);

        ball.Velocity += tangent * (dampedSpeed - tangentSpeed);
        ball.Spin = -dampedSpeed / ball.Radius * Mathf.Rad2Deg;
    }

    // ----
    // 線分上で、指定位置に一番近い点を求める
    // ----
    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0f) return start;

        float ratio = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return start + segment * ratio;
    }

    // ================================================
    // 壁との衝突
    // ================================================
    // ----
    // 左右の壁と天井で跳ね返す
    // ----
    private void ResolveWalls(BallPhysicsBody ball)
    {
        Vector2 position = ball.Position;
        Vector2 velocity = ball.Velocity;

        if (position.x - ball.Radius < leftWallX)
        {
            position.x = leftWallX + ball.Radius;
            if (velocity.x < 0f) velocity.x *= -ball.Bounciness;
        }
        else if (position.x + ball.Radius > rightWallX)
        {
            position.x = rightWallX - ball.Radius;
            if (velocity.x > 0f) velocity.x *= -ball.Bounciness;
        }

        if (position.y + ball.Radius > ceilingY)
        {
            position.y = ceilingY - ball.Radius;
            if (velocity.y > 0f) velocity.y *= -ball.Bounciness;
        }

        ball.Position = position;
        ball.Velocity = velocity;
    }

    // ================================================
    // ボール同士の衝突
    // ================================================
    // ----
    // 2 つのボールを弾き合わせる（ドラッグ中のボールは動かない壁として扱う）
    // ----
    private void ResolveBalls(BallPhysicsBody first, BallPhysicsBody second)
    {
        Vector2 offset = first.Position - second.Position;
        float distance = offset.magnitude;
        float minDistance = first.Radius + second.Radius;
        if (distance >= minDistance) return;

        Vector2 normal = distance > MIN_DISTANCE ? offset / distance : Vector2.up;
        float firstInverseMass = first.IsDragging ? 0f : 1f / first.Mass;
        float secondInverseMass = second.IsDragging ? 0f : 1f / second.Mass;
        float totalInverseMass = firstInverseMass + secondInverseMass;
        if (totalInverseMass <= 0f) return;

        // めり込みを重さの比率で戻す
        Vector2 correction = normal * ((minDistance - distance) / totalInverseMass);
        first.Position += correction * firstInverseMass;
        second.Position -= correction * secondInverseMass;

        // 近づいているときだけ弾き合う
        float approachSpeed = Vector2.Dot(first.Velocity - second.Velocity, normal);
        if (approachSpeed >= 0f) return;

        float bounce = -approachSpeed >= minBounceSpeed ? ballBounciness : 0f;
        float impulse = -(1f + bounce) * approachSpeed / totalInverseMass;
        first.Velocity += normal * (impulse * firstInverseMass);
        second.Velocity -= normal * (impulse * secondInverseMass);
    }

    // ================================================
    // エディタ表示
    // ================================================
    private void OnDrawGizmos()
    {
        Transform space = transform.parent != null ? transform.parent : transform;

        Gizmos.color = Color.green;
        foreach (var surface in surfaces)
        {
            for (int i = 0; i + 1 < surface.points.Count; i++)
            {
                Gizmos.DrawLine(
                    space.TransformPoint(surface.points[i] + surface.offset),
                    space.TransformPoint(surface.points[i + 1] + surface.offset));
            }
        }
    }
}
