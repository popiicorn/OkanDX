using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 指定したアイテムを選んだ状態でクリックすると、アイテムを消費して、
/// 対応するボール（BallPhysicsBody）を指定した範囲のランダムな位置に出して落とす。
/// ClickableObject と同じオブジェクトに付ける。対応するアイテムを選んでいないときは ClickableObject の通常の処理になる。
/// 出てくる前のボールはドラッグできず、出てきた後はアイテムとしてのクリック処理を止めてドラッグできるようにする。
/// </summary>
public class ItemDropSpawner : MonoBehaviour, IClickInterceptor
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [System.Serializable]
    public class DropEntry
    {
        [Tooltip("使うアイテムのID")]
        public string itemID;

        [Tooltip("出てくるボール")]
        public BallPhysicsBody ball;

        [HideInInspector] public bool hasDropped;
    }

    [Header("■ アイテムと出てくるボール")]
    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();

    [Header("■ 出てくる位置（ボールの親の中の座標）")]
    [Tooltip("出てくる範囲の左端の X")]
    [SerializeField] private float spawnMinX = -345f;

    [Tooltip("出てくる範囲の右端の X")]
    [SerializeField] private float spawnMaxX = 336f;

    [Tooltip("出てくる高さの Y")]
    [SerializeField] private float spawnY = 377f;

    // ================================================
    // 初期化
    // ================================================
    private void Start()
    {
        // 出てくる前（床に置いてある間）は物理で動かさず、ドラッグもさせない（置いた場所にそのまま表示する）
        foreach (var entry in entries)
        {
            if (entry.ball == null) continue;

            // すでに表示されている（出てきた後の）ボールはそのまま動かす
            //（同じボールを登録した別の ItemDropSpawner が後から表示されたときに止めないため）
            if (entry.ball.gameObject.activeInHierarchy) continue;

            entry.ball.enabled = false;

            var draggable = entry.ball.GetComponent<DraggableUIObject>();
            if (draggable != null) draggable.enabled = false;
        }
    }

    // ================================================
    // クリック処理（ClickableObject から呼ばれる）
    // ================================================
    /// <summary>
    /// 対応するアイテムを選んでいれば、アイテムを消費してボールを出す
    /// </summary>
    public bool TryInterceptClick()
    {
        if (InventoryManager.Instance == null) return false;

        Item selected = InventoryManager.Instance.SelectedItem;
        if (selected == null || string.IsNullOrEmpty(selected.id)) return false;

        foreach (var entry in entries)
        {
            if (entry.hasDropped || entry.ball == null || entry.itemID != selected.id) continue;

            entry.hasDropped = true;
            InventoryManager.Instance.RemoveSelectedItem();
            Drop(entry.ball);
            return true;
        }

        return false;
    }

    // ----
    // ボールを表示して、ランダムな位置から落とす
    // ----
    private void Drop(BallPhysicsBody ball)
    {
        GameObject ballObject = ball.gameObject;

        // アイテム取得の演出で縮んだ大きさを戻す
        ballObject.transform.DOKill();
        ballObject.transform.localScale = Vector3.one;
        ballObject.SetActive(true);

        // もうアイテムとしては反応させず、ドラッグできるようにする
        var clickable = ballObject.GetComponent<ClickableObject>();
        if (clickable != null) clickable.enabled = false;

        var draggable = ballObject.GetComponent<DraggableUIObject>();
        if (draggable != null) draggable.enabled = true;

        ball.enabled = true;

        float x = Random.Range(Mathf.Min(spawnMinX, spawnMaxX), Mathf.Max(spawnMinX, spawnMaxX));
        ball.Teleport(new Vector2(x, spawnY));
    }

    // ================================================
    // エディタ表示
    // ================================================
    private void OnDrawGizmosSelected()
    {
        // 出てくる範囲を黄色の線で表示する（ボールの親の座標で描く）
        foreach (var entry in entries)
        {
            if (entry.ball == null || entry.ball.transform.parent == null) continue;

            Transform space = entry.ball.transform.parent;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(space.TransformPoint(new Vector3(spawnMinX, spawnY, 0f)),
                            space.TransformPoint(new Vector3(spawnMaxX, spawnY, 0f)));
            break;
        }
    }
}
