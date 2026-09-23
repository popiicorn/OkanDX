using UnityEngine;
using DG.Tweening;

public class RotatePendulum : MonoBehaviour
{
    [Header("■ 回転角度設定")]
    [SerializeField] private float minAngle = -10f;  // 開始角度
    [SerializeField] private float maxAngle = 10f;   // 終了角度

    [Header("■ 時間設定（秒）")]
    [SerializeField] private float moveDuration = 1.0f; // 回転にかかる時間
    [SerializeField] private float interval = 1.0f;     // 一呼吸（待機時間）

    private Sequence rotateSequence;

    private void Start()
    {
        StartPendulumAnimation();
    }

    private void StartPendulumAnimation()
    {
        // 初期角度を -10 に設定
        transform.localRotation = Quaternion.Euler(0, 0, minAngle);

        // シーケンスの作成
        rotateSequence = DOTween.Sequence();

        rotateSequence
            // 1. -10度から 10度へ回転
            .Append(transform.DOLocalRotate(new Vector3(0, 0, maxAngle), moveDuration).SetEase(Ease.InOutSine))
            // 2. 一呼吸おく（待機）
            .AppendInterval(interval)
            // 3. 10度から -10度へ回転
            .Append(transform.DOLocalRotate(new Vector3(0, 0, minAngle), moveDuration).SetEase(Ease.InOutSine))
            // 4. 一呼吸おく（待機）
            .AppendInterval(interval)
            // 無限ループ設定
            .SetLoops(-1);
    }

    private void OnDestroy()
    {
        // オブジェクト破棄時にTweenを安全に停止
        rotateSequence?.Kill();
    }
}