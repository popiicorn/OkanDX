using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomNavigationManager : MonoBehaviour
{
    [Header("画面（View）リスト（左から順にセット）")]
    [SerializeField] private List<GameObject> roomViews;

    [Header("移動ボタン")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    [Header("特殊画面（天井・床・拡大画面など）")]
    [SerializeField] private GameObject topView;    // 上を押した時の画面
    [SerializeField] private GameObject bottomView; // 下を押した時の画面

    [Header("設定")]
    [Tooltip("端まで行ったらループするか（一番右からさらに右で一番左へ戻るか）")]
    [SerializeField] private bool isLooping = false;

    private int currentIndex = 0; // 現在の横画面インデックス
    private bool isInSubView = false; // 上下や拡大画面に入っているかフラグ

    private void Start()
    {
        // ボタンイベントの登録
        if (leftButton != null) leftButton.onClick.AddListener(OnLeftButtonClicked);
        if (rightButton != null) rightButton.onClick.AddListener(OnRightButtonClicked);
        if (upButton != null) upButton.onClick.AddListener(OnUpButtonClicked);
        if (downButton != null) downButton.onClick.AddListener(OnDownButtonClicked);

        UpdateView();
    }

    /// <summary>
    /// 左ボタン押下
    /// </summary>
    public void OnLeftButtonClicked()
    {
        if (isInSubView) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = isLooping ? roomViews.Count - 1 : 0;
        }
        UpdateView();
    }

    /// <summary>
    /// 右ボタン押下
    /// </summary>
    public void OnRightButtonClicked()
    {
        if (isInSubView) return;

        currentIndex++;
        if (currentIndex >= roomViews.Count)
        {
            currentIndex = isLooping ? 0 : roomViews.Count - 1;
        }
        UpdateView();
    }

    /// <summary>
    /// 上ボタン押下（天井や拡大）
    /// </summary>
    public void OnUpButtonClicked()
    {
        if (topView == null || isInSubView) return;

        isInSubView = true;
        HideAllViews();
        topView.SetActive(true);
        UpdateButtonStates();
    }

    /// <summary>
    /// 下ボタン押下（元に戻る・床）
    /// </summary>
    public void OnDownButtonClicked()
    {
        if (isInSubView)
        {
            // 上下画面から元のメイン画面へ戻る
            isInSubView = false;
            UpdateView();
        }
        else if (bottomView != null)
        {
            // 床画面へ移動
            isInSubView = true;
            HideAllViews();
            bottomView.SetActive(true);
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 画面の表示切り替え
    /// </summary>
    private void UpdateView()
    {
        HideAllViews();

        if (roomViews != null && roomViews.Count > 0)
        {
            roomViews[currentIndex].SetActive(true);
        }

        UpdateButtonStates();
    }

    /// <summary>
    /// 全画面を非表示
    /// </summary>
    private void HideAllViews()
    {
        foreach (var view in roomViews)
        {
            if (view != null) view.SetActive(false);
        }
        if (topView != null) topView.SetActive(false);
        if (bottomView != null) bottomView.SetActive(false);
    }

    /// <summary>
    /// 状況に応じてボタンの表示/非表示を切り替え
    /// </summary>
    private void UpdateButtonStates()
    {
        if (isInSubView)
        {
            // 特殊画面（天井・床）にいる時は「下ボタン（戻る）」だけ表示
            if (leftButton != null) leftButton.gameObject.SetActive(false);
            if (rightButton != null) rightButton.gameObject.SetActive(false);
            if (upButton != null) upButton.gameObject.SetActive(false);
            if (downButton != null) downButton.gameObject.SetActive(true);
        }
        else
        {
            // 通常の横移動画面
            bool isFirst = currentIndex == 0;
            bool isLast = currentIndex == roomViews.Count - 1;

            if (leftButton != null) leftButton.gameObject.SetActive(isLooping || !isFirst);
            if (rightButton != null) rightButton.gameObject.SetActive(isLooping || !isLast);
            if (upButton != null) upButton.gameObject.SetActive(topView != null);
            if (downButton != null) downButton.gameObject.SetActive(bottomView != null);
        }
    }
}