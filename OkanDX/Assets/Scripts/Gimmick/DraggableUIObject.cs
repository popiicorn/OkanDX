using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// マウスのドラッグで自由に移動できる UI オブジェクト。離した場所にそのまま留まる。
/// 親の RectTransform の範囲からははみ出さない。
/// 同じオブジェクトの ClickableObject は、ドラッグせずにクリックしたときだけ反応する。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableUIObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>ドラッグを始めたときに呼ばれる</summary>
    public event System.Action DragStarted;

    /// <summary>ドラッグを終えたときに呼ばれる</summary>
    public event System.Action DragEnded;

    /// <summary>ドラッグ中か</summary>
    public bool IsDragging { get; private set; }

    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 移動できる範囲")]
    [Tooltip("親の下端から、この分だけ上までしか動かせない（アイテム欄の裏に隠れないようにする）")]
    [SerializeField] private float bottomMargin = 0f;

    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Vector2 grabOffset;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;
    }

    // ================================================
    // ドラッグ処理
    // ================================================
    /// <summary>
    /// ドラッグ開始時：掴んだ位置を覚えて、離したときのクリック判定を止める
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // ドラッグの後に ClickableObject のメッセージが出ないようにする
        eventData.eligibleForClick = false;

        if (TryGetLocalPoint(eventData, out Vector2 localPoint))
        {
            grabOffset = (Vector2)rectTransform.localPosition - localPoint;
        }

        IsDragging = true;
        DragStarted?.Invoke();
    }

    /// <summary>
    /// ドラッグ中：マウスに合わせて移動する
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!TryGetLocalPoint(eventData, out Vector2 localPoint)) return;

        Vector2 position = ClampToParent(localPoint + grabOffset);
        rectTransform.localPosition = new Vector3(position.x, position.y, rectTransform.localPosition.z);
    }

    /// <summary>
    /// ドラッグ終了時
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        DragEnded?.Invoke();
    }

    // ----
    // マウス位置を親の座標に変換する
    // ----
    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (parentRect == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out localPoint);
    }

    // ----
    // 親の範囲からはみ出さない位置に収める
    // ----
    private Vector2 ClampToParent(Vector2 position)
    {
        Rect parent = parentRect.rect;
        Rect self = rectTransform.rect;
        Vector2 pivot = rectTransform.pivot;

        float minX = parent.xMin + self.width * pivot.x;
        float maxX = parent.xMax - self.width * (1f - pivot.x);
        float minY = parent.yMin + bottomMargin + self.height * pivot.y;
        float maxY = parent.yMax - self.height * (1f - pivot.y);

        return new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
    }
}
