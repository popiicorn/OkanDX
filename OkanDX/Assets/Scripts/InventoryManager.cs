using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("スロット設定")]
    [SerializeField] private List<ItemSlot> itemSlots; // 7個のスロットを登録
    private const int MAX_SLOTS = 7;

    [Header("ツールチップ (マウスオーバー説明欄)")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipNameText;
    [SerializeField] private TMP_Text tooltipDescText;

    // インスペクターから表示位置のズレ（高さ等）を自由に変更できます
    [Tooltip("アイテムスロットからの表示位置のズレ（X, Y）")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 100f);

    [Header("ツールチップ アニメーション設定")]
    [Tooltip("ポヨン演出の開始スケール（例: 0.5）")]
    [SerializeField] private float baseStartScale = 0.5f;

    [Tooltip("表示完了時の目標スケール（通常: 1.0）")]
    [SerializeField] private float targetScale = 1.0f;

    [Tooltip("表示にかかる時間（秒）")]
    [SerializeField] private float showDuration = 0.15f;

    [Tooltip("非表示にかかる時間（秒）")]
    [SerializeField] private float hideDuration = 0.1f;

    private int selectedIndex = -1; // -1 は未選択

    public Item SelectedItem => (selectedIndex >= 0 && selectedIndex < itemSlots.Count) ? itemSlots[selectedIndex].CurrentItem : null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 全スロットの初期化
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i] != null)
            {
                itemSlots[i].SetupSlot(i, this);
            }
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.transform.localScale = Vector3.zero;
            tooltipPanel.SetActive(false);
        }
    }

    /// <summary>
    /// アイテムを入手して左詰めで空きスロットへ格納する
    /// </summary>
    public bool AddItem(Item item)
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i].CurrentItem == null)
            {
                itemSlots[i].SetItem(item);
                Debug.Log($"アイテム [{item.itemName}] をゲット！");
                return true;
            }
        }

        Debug.LogWarning("アイテムボックスが満タンです！");
        return false;
    }

    /// <summary>
    /// 現在選択中のアイテムを消費/削除する（使用時など）
    /// </summary>
    public void RemoveSelectedItem()
    {
        if (selectedIndex == -1) return;

        itemSlots[selectedIndex].ClearSlot();
        DeselectAll();
        ReorganizeSlots(); // 左詰めに整理
    }

    /// <summary>
    /// スロットをクリックしたときの選択切り替え処理
    /// </summary>
    public void OnSelectSlot(int index)
    {
        // 同じアイテムをもう一度押したら選択解除
        if (selectedIndex == index)
        {
            DeselectAll();
            return;
        }

        DeselectAll();
        selectedIndex = index;
        itemSlots[selectedIndex].SetSelectState(true);
    }

    /// <summary>
    /// 全スロットの選択枠を消す
    /// </summary>
    public void DeselectAll()
    {
        selectedIndex = -1;
        for (int i = 0; i < itemSlots.Count; i++)
        {
            itemSlots[i].SetSelectState(false);
        }
    }

    /// <summary>
    /// 左詰めに整理する
    /// </summary>
    private void ReorganizeSlots()
    {
        List<Item> currentItems = new List<Item>();
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i].CurrentItem != null)
            {
                currentItems.Add(itemSlots[i].CurrentItem);
            }
            itemSlots[i].ClearSlot();
        }

        for (int i = 0; i < currentItems.Count; i++)
        {
            itemSlots[i].SetItem(currentItems[i]);
        }
    }

    // --- ツールチップ表示制御 ---
    public void ShowItemTooltip(Item item, Vector3 slotWorldPos)
    {
        if (tooltipPanel == null) return;

        if (tooltipNameText != null) tooltipNameText.text = item.itemName;
        if (tooltipDescText != null) tooltipDescText.text = item.description;

        // 設定したオフセット分ズラした位置に移動
        tooltipPanel.transform.position = slotWorldPos + (Vector3)tooltipOffset;

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.DOKill();

        // ★ 一度 0.5 の大きさからスタートさせて、そこから 1.0 へポヨンと拡大
        tooltipPanel.transform.localScale = Vector3.one * baseStartScale;

        tooltipPanel.transform.DOScale(Vector3.one * targetScale, showDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void HideItemTooltip()
    {
        if (tooltipPanel == null) return;

        tooltipPanel.transform.DOKill();

        // ★ 1.0 から 0.5 へシュッと縮み、終わったら一瞬で 0 に落として非表示にする
        tooltipPanel.transform.DOScale(Vector3.one * baseStartScale, hideDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                tooltipPanel.transform.localScale = Vector3.zero; // 完全に消す
                tooltipPanel.SetActive(false);
            });
    }
}