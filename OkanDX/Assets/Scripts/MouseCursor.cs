using UnityEngine;

public class MouseCursor : MonoBehaviour
{
    public static MouseCursor Instance { get; private set; }

    [Header("カーソル先端の調整 (ピクセル)")]
    [Tooltip("画像の中心からクリック先端までのズレ。矢印の先端が左上なら X:-16, Y:16 のように調整")]
    [SerializeField] private Vector2 hotSpotOffset = Vector2.zero;

    [Header("設定")]
    [SerializeField] private bool hideHardwareCursor = true;

    private RectTransform rectTransform;

    private void Awake()
    {
        // 重複生成を防ぐシングルトン処理
        if (Instance == null)
        {
            Instance = this;
            // 親オブジェクト（Canvasごと）シーン移動で消えないようにする
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(transform.root.gameObject);
            return;
        }

        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (hideHardwareCursor)
        {
            Cursor.visible = false; // PC標準のカーソルを消す
        }
    }

    private void Update()
    {
        // マウスの画面位置にUIカーソルを追従させる
        Vector2 mousePos = Input.mousePosition;
        rectTransform.position = mousePos + hotSpotOffset;
    }

    private void OnDisable()
    {
        // オブジェクトが非アクティブ・削除されたら標準カーソルを復元
        Cursor.visible = true;
    }
}