using UnityEngine;

public class ToiletPaperItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private RectTransform rectTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 物理的な力を加えて上空へ放り投げる
    /// </summary>
    public void LaunchPhysics(Vector2 force, float torque)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(force, ForceMode2D.Impulse);
            rb.AddTorque(torque, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        // ⑦ 画面下（Y座標 -800以下）に落ちたペーパーは処理負荷軽減のため自動消去
        if (rectTransform != null && rectTransform.anchoredPosition.y < -800f)
        {
            Destroy(gameObject);
        }
    }
}