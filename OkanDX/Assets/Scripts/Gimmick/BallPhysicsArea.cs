using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボールが落ちて跳ね、転がって止まる範囲を管理するギミック。
/// 床やキャラクターの形を折れ線（surfaces）で指定し、左右の壁・ボール同士の衝突も計算する。
/// 座標はボールの親（View）の中の座標で、画面の解像度が変わっても動きは変わらない。
/// 線に followTarget を指定すると、そのオブジェクトの移動・回転・拡大に合わせて線も動く。
/// 折れ線は Scene ビューで緑色の線として確認できる。
/// </summary>
public class BallPhysicsArea : MonoBehaviour
{
    [System.Serializable]
    public class Surface
    {
        [Tooltip("分かりやすい名前（動作には影響しない）")]
        public string name;

        [Tooltip("線の頂点（左から順に並べる）。followTarget があればその中心からの座標、なければ View の中の座標")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("指定すると、このオブジェクトの移動・回転・拡大に合わせて線も一緒に動く（空なら動かない）")]
        public RectTransform followTarget;

        // 計算用：View の中での現在の頂点と、その速さ
        [System.NonSerialized] public Vector2[] currentPoints = new Vector2[0];
        [System.NonSerialized] public Vector2[] pointVelocities = new Vector2[0];
        [System.NonSerialized] public bool hasPreviousPoints;

        // 計算用：followTarget に付いているシーソー（なければ null）
        [System.NonSerialized] public SeesawTilt seesaw;
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

    [Tooltip("線からこの距離まで浮いていても、シーソー（SeesawTilt）に重さがかかっているとみなす")]
    [SerializeField] private float seesawContactMargin = 12f;

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
    /// 線の現在位置とシーソーを読み直す（線の速さは 0 から数え直す）
    /// </summary>
    public void Initialize()
    {
        foreach (var surface in surfaces)
        {
            surface.hasPreviousPoints = false;
            surface.seesaw = surface.followTarget != null ? surface.followTarget.GetComponent<SeesawTilt>() : null;
        }
        UpdateSurfacePoints(0f);
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
        UpdateSurfacePoints(deltaTime);

        accumulatedTime += Mathf.Min(deltaTime, MAX_FRAME_TIME);

        while (accumulatedTime >= FIXED_STEP)
        {
            accumulatedTime -= FIXED_STEP;
            Step(FIXED_STEP);
        }

        foreach (var ball in balls)
        {
            if (IsActive(ball)) ball.ApplyToTransform();
        }
    }

    // ----
    // 追いかけるオブジェクトに合わせて線の頂点を置き直し、頂点ごとの速さを求める
    // ----
    private void UpdateSurfacePoints(float deltaTime)
    {
        foreach (var surface in surfaces)
        {
            int count = surface.points.Count;
            if (surface.currentPoints.Length != count)
            {
                surface.currentPoints = new Vector2[count];
                surface.pointVelocities = new Vector2[count];
                surface.hasPreviousPoints = false;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 point = ToAreaSpace(surface, surface.points[i]);
                surface.pointVelocities[i] = surface.hasPreviousPoints && deltaTime > 0f
                    ? (point - surface.currentPoints[i]) / deltaTime
                    : Vector2.zero;
                surface.currentPoints[i] = point;
            }
            surface.hasPreviousPoints = true;
        }
    }

    // ----
    // 線の頂点を View の中の座標に変換する
    // ----
    private Vector2 ToAreaSpace(Surface surface, Vector2 point)
    {
        if (surface.followTarget == null) return point;

        Transform space = transform.parent != null ? transform.parent : transform;
        return space.InverseTransformPoint(surface.followTarget.TransformPoint(point));
    }

    // ----
    // 計算の対象か（非表示のボールや、BallPhysicsBody を無効にしたボールは除く）
    // ----
    private static bool IsActive(BallPhysicsBody ball)
    {
        return ball != null && ball.isActiveAndEnabled;
    }

    // ----
    // 線が有効か（followTarget のオブジェクトが非表示なら、その線は無いものとして扱う）
    // ----
    private static bool IsSurfaceActive(Surface surface)
    {
        return surface.followTarget == null || surface.followTarget.gameObject.activeInHierarchy;
    }

    // ----
    // 1 ステップ分の計算
    // ----
    private void Step(float deltaTime)
    {
        foreach (var ball in balls)
        {
            if (!IsActive(ball)) continue;

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
                if (IsActive(balls[i]) && IsActive(balls[j])) ResolveBalls(balls[i], balls[j]);
            }
        }

        foreach (var surface in surfaces)
        {
            if (surface.seesaw != null && IsSurfaceActive(surface)) surface.seesaw.Step(deltaTime);
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
        bool hasContact = false;
        Vector2 contactNormal = Vector2.up;
        Vector2 contactVelocity = Vector2.zero;

        foreach (var surface in surfaces)
        {
            if (!IsSurfaceActive(surface)) continue;
            for (int i = 0; i + 1 < surface.currentPoints.Length; i++)
            {
                if (ResolveSegment(ball, surface, i, out Vector2 normal, out Vector2 surfaceVelocity))
                {
                    hasContact = true;
                    contactNormal = normal;
                    contactVelocity = surfaceVelocity;
                }
            }

            if (surface.seesaw != null) AddSeesawLoad(surface, ball);
        }

        if (hasContact) ApplyRolling(ball, contactNormal, contactVelocity, deltaTime);
    }

    // ----
    // シーソーに、乗っているボールの重さを伝える（触れている点と回転の中心の横の距離で傾きが決まる）
    // 揺れで小さく弾んでいる間も重さがかかるよう、少し浮いていても乗っているとみなす
    // ----
    private void AddSeesawLoad(Surface surface, BallPhysicsBody ball)
    {
        float distance = FindNearestPoint(surface, ball.Position, out Vector2 nearestPoint);
        if (distance >= ball.Radius + seesawContactMargin) return;

        Transform space = transform.parent != null ? transform.parent : transform;
        surface.seesaw.AddLoad(nearestPoint.x - surface.seesaw.GetPivotPosition(space).x, ball.Weight);
    }

    // ----
    // 線の上で、指定位置に一番近い点と、その距離を求める
    // ----
    private static float FindNearestPoint(Surface surface, Vector2 position, out Vector2 nearestPoint)
    {
        float nearestDistance = float.MaxValue;
        nearestPoint = position;

        for (int i = 0; i + 1 < surface.currentPoints.Length; i++)
        {
            Vector2 start = surface.currentPoints[i];
            Vector2 end = surface.currentPoints[i + 1];
            Vector2 point = Vector2.Lerp(start, end, ClosestRatioOnSegment(position, start, end));
            float distance = (position - point).magnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = point;
            }
        }
        return nearestDistance;
    }

    // ----
    // 線分 1 本（index 番目の頂点から次の頂点まで）との衝突：めり込みを戻して跳ね返す
    // ----
    private bool ResolveSegment(BallPhysicsBody ball, Surface surface, int index, out Vector2 normal, out Vector2 surfaceVelocity)
    {
        Vector2 start = surface.currentPoints[index];
        Vector2 end = surface.currentPoints[index + 1];
        float ratio = ClosestRatioOnSegment(ball.Position, start, end);
        Vector2 closest = Vector2.Lerp(start, end, ratio);
        Vector2 offset = ball.Position - closest;
        float distance = offset.magnitude;

        // 当たった場所の線の速さ（回転していると場所ごとに違う）
        surfaceVelocity = Vector2.Lerp(surface.pointVelocities[index], surface.pointVelocities[index + 1], ratio);
        normal = distance > MIN_DISTANCE ? offset / distance : Vector2.up;
        if (distance >= ball.Radius) return false;

        ball.Position = closest + normal * ball.Radius;

        // 線が動いているときは、線から見た速さで押し返す。
        // 跳ねるかどうかはボール自身が落ちてきた勢いで決める（線の揺れだけでは弾み続けない）
        float normalSpeed = Vector2.Dot(ball.Velocity - surfaceVelocity, normal);
        if (normalSpeed < 0f)
        {
            float fallingSpeed = -Vector2.Dot(ball.Velocity, normal);
            float bounce = fallingSpeed >= minBounceSpeed ? ball.Bounciness : 0f;
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
    // 線分上で、指定位置に一番近い点の位置（0: 始点 〜 1: 終点）を求める
    // ----
    private static float ClosestRatioOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0f) return 0f;

        return Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
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
    // 乗っているボールの数
    // ================================================
    /// <summary>
    /// target を followTarget にした線の上に乗っているボールの数を返す。
    /// 線に直接触れているボールと、その上に積み重なっているボールを数える。
    /// ドラッグ中のボールと、ほかの線（床など）に触れているボールは数えない。
    /// </summary>
    /// <param name="target">乗せる先のオブジェクト（線の followTarget）</param>
    /// <param name="contactMargin">この距離まで浮いていても触れているとみなす</param>
    public int CountBallsOn(RectTransform target, float contactMargin)
    {
        int count = balls.Count;
        var candidates = new List<BallPhysicsBody>(count);
        var queue = new Queue<BallPhysicsBody>();
        var counted = new HashSet<BallPhysicsBody>();

        // 対象になるボールを集め、target の線に直接触れているものを起点にする
        foreach (var ball in balls)
        {
            if (!IsActive(ball) || ball.IsDragging) continue;

            bool touchesTarget = false;
            bool touchesOther = false;
            foreach (var surface in surfaces)
            {
                if (!IsSurfaceActive(surface)) continue;
                if (FindNearestPoint(surface, ball.Position, out _) > ball.Radius + contactMargin) continue;

                if (target != null && surface.followTarget == target) touchesTarget = true;
                else touchesOther = true;
            }

            if (touchesOther) continue;

            candidates.Add(ball);
            if (touchesTarget && counted.Add(ball)) queue.Enqueue(ball);
        }

        // 乗っているボールに触れているボールも、乗っているとみなす（積み重ね）
        while (queue.Count > 0)
        {
            BallPhysicsBody lower = queue.Dequeue();
            foreach (var upper in candidates)
            {
                if (counted.Contains(upper)) continue;

                float limit = lower.Radius + upper.Radius + contactMargin;
                if ((upper.Position - lower.Position).sqrMagnitude > limit * limit) continue;

                counted.Add(upper);
                queue.Enqueue(upper);
            }
        }

        return counted.Count;
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
                    space.TransformPoint(ToAreaSpace(surface, surface.points[i])),
                    space.TransformPoint(ToAreaSpace(surface, surface.points[i + 1])));
            }
        }
    }
}
