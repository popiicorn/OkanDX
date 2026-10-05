using UnityEngine;

public class ToiletPaperItem : MonoBehaviour
{
    [Header("■ ペーパー個別の設定")]
    [SerializeField] private int paperValue = 100; // このペーパー1個あたりのへそくり額（金なら500円など）

    private Rigidbody2D rb;
    private RectTransform rectTransform;

    public int PaperValue => paperValue; // 外部から金額を取得するプロパティ

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
        // 画面下に落ちたペーパーは消去
        if (rectTransform != null && rectTransform.anchoredPosition.y < -800f)
        {
            Destroy(gameObject);
        }
    }
}