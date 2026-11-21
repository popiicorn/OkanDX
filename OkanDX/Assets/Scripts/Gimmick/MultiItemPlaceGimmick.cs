using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 登録した複数のアイテムを、それぞれ選んだ状態でクリックして「乗せる」ギミック。
/// 乗せるたびに対応するオブジェクトを表示し、全部そろったら指定したオブジェクト（メーターの針など）を 1 周回して、
/// 回り終わったらオブジェクトの表示・非表示を切り替える。乗せる順番は自由。
/// ClickableObject と同じオブジェクトに付ける。登録したアイテムを選んでいないときは ClickableObject の通常の処理（メッセージなど）になる。
/// </summary>
[RequireComponent(typeof(ClickableObject))]
public class MultiItemPlaceGimmick : MonoBehaviour, IClickInterceptor
{
    [System.Serializable]
    public class PlaceEntry
    {
        [Tooltip("乗せるアイテムの ID")]
        public string itemID;

        [Tooltip("乗せたときに表示するオブジェクト")]
        public GameObject[] showObjects;

        [HideInInspector] public bool isPlaced;
    }

    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 乗せるアイテム")]
    [SerializeField] private List<PlaceEntry> entries = new List<PlaceEntry>();

    [Tooltip("乗せたときにアイテムを持ち物から消すか")]
    [SerializeField] private bool consumeItem = true;

    [Header("■ 全部そろったら回すもの")]
    [Tooltip("1 周回すオブジェクト（回転の中心は Pivot）。空なら回さずにすぐ切り替える")]
    [SerializeField] private RectTransform rotateTarget;

    [Tooltip("1 周にかける時間（秒）")]
    [SerializeField] private float rotateDuration = 2f;

    [Tooltip("ON: 時計回り / OFF: 反時計回り")]
    [SerializeField] private bool clockwise = true;

    [Tooltip("回り方")]
    [SerializeField] private Ease rotateEase = Ease.InOutSine;

    [Tooltip("回っている間だけ表示して、画面のクリックを止める板（画面全体を覆う透明な Image。最初は非表示にしておく）")]
    [SerializeField] private GameObject inputBlocker;

    [Header("■ 回り終わったら")]
    [Tooltip("非表示にするオブジェクト")]
    [SerializeField] private GameObject[] hideOnComplete;

    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showOnComplete;

    [Tooltip("全部そろった後にクリックしたときのメッセージ（空ならメッセージなしで押した演出だけ）")]
    [TextArea(2, 5)]
    [SerializeField] private string completedMessage = "";

    private bool isCompleted = false;

    // ================================================
    // クリック処理（ClickableObject から呼ばれる）
    // ================================================
    /// <summary>
    /// 登録したアイテムを選んでいれば乗せる
    /// </summary>
    public bool TryInterceptClick()
    {
        // 全部そろった後は、決めたメッセージだけを出す（空なら押した演出だけ）
        if (isCompleted)
        {
            if (!string.IsNullOrEmpty(completedMessage) && MessageUI.Instance != null)
            {
                MessageUI.Instance.ShowMessage(completedMessage);
            }
            return true;
        }

        if (InventoryManager.Instance == null) return false;

        Item selected = InventoryManager.Instance.SelectedItem;
        if (selected == null || string.IsNullOrEmpty(selected.id)) return false;

        foreach (var entry in entries)
        {
            if (entry.isPlaced || entry.itemID != selected.id) continue;

            entry.isPlaced = true;
            if (consumeItem) InventoryManager.Instance.RemoveSelectedItem();
            SetActiveAll(entry.showObjects, true);

            if (AreAllPlaced()) Complete();
            return true;
        }

        return false;
    }

    // ----
    // 全部乗せたか
    // ----
    private bool AreAllPlaced()
    {
        foreach (var entry in entries)
        {
            if (!entry.isPlaced) return false;
        }
        return entries.Count > 0;
    }

    // ================================================
    // 全部そろったとき
    // ================================================
    private void Complete()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (rotateTarget == null)
        {
            Finish();
            return;
        }

        // 回っている間は画面のクリックを止める
        if (inputBlocker != null) inputBlocker.SetActive(true);

        // 今の角度から 1 周回す
        float startAngle = rotateTarget.localEulerAngles.z;
        float endAngle = startAngle + (clockwise ? -360f : 360f);
        rotateTarget.DOKill();
        rotateTarget.DOLocalRotate(new Vector3(0f, 0f, endAngle), rotateDuration, RotateMode.FastBeyond360)
            .SetEase(rotateEase)
            .SetLink(rotateTarget.gameObject)
            .OnComplete(Finish);
    }

    // ----
    // 回り終わったら表示を切り替える
    // ----
    private void Finish()
    {
        if (inputBlocker != null) inputBlocker.SetActive(false);

        SetActiveAll(hideOnComplete, false);
        SetActiveAll(showOnComplete, true);
    }

    private static void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj != null) obj.SetActive(active);
        }
    }
}
