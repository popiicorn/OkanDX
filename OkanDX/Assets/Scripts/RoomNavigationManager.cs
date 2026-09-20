using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RoomNavigationManager : MonoBehaviour
{
    [Header("画面（View）リスト（左から順にセット）")]
    [SerializeField] private List<RectTransform> roomViews;

    [Header("初期画面設定")]
    [Tooltip("ゲーム開始時に表示するViewのインデックス（0 = View_0, 1 = View_1...）")]
    [SerializeField] private int initialIndex = 0; // ★ 開始Viewの設定項目を追加

    [Header("移動ボタン")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    [Header("特殊画面（天井・床・拡大画面など）")]
    [SerializeField] private RectTransform topView;    // 上を押した時の画面
    [SerializeField] private RectTransform bottomView; // 下を押した時の画面

    [Header("設定")]
    [Tooltip("端まで行ったらループするか（一番右からさらに右で一番左へ戻るか）")]
    [SerializeField] private bool isLooping = false;

    [Header("スライドアニメーション設定")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [SerializeField] private float screenWidth = 1920f;
    [SerializeField] private float screenHeight = 1080f;

    private int currentIndex = 0; // 現在の横画面インデックス
    private bool isInSubView = false; // 上下や拡大画面に入っているかフラグ
    private bool isAnimating = false; // アニメーション中フラグ

    private enum SlideDirection { Right, Left, Up, Down }

    private void Start()
    {
        currentIndex = initialIndex; // ★ 指定した初期Viewの番号を適用
        InitializeViews();
        SetupButtonListeners();
        UpdateButtonStates();
    }

    private void InitializeViews()
    {
        if (roomViews == null || roomViews.Count == 0) return;

        // 設定値が範囲外の場合は安全な範囲に収める
        currentIndex = Mathf.Clamp(currentIndex, 0, roomViews.Count - 1);

        for (int i = 0; i < roomViews.Count; i++)
        {
            if (roomViews[i] == null || roomViews[i].Equals(null)) continue;

            if (i == currentIndex)
            {
                roomViews[i].gameObject.SetActive(true);
                roomViews[i].anchoredPosition = Vector2.zero;
            }
            else
            {
                roomViews[i].gameObject.SetActive(false);
                roomViews[i].anchoredPosition = new Vector2(screenWidth, 0);
            }
        }

        if (topView != null && !topView.Equals(null))
        {
            topView.gameObject.SetActive(false);
            topView.anchoredPosition = new Vector2(0, screenHeight);
        }

        if (bottomView != null && !bottomView.Equals(null))
        {
            bottomView.gameObject.SetActive(false);
            bottomView.anchoredPosition = new Vector2(0, -screenHeight);
        }
    }

    private void SetupButtonListeners()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveAllListeners();
            leftButton.onClick.AddListener(() => OnClickAnimated(leftButton.transform, OnLeftButtonClicked));
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveAllListeners();
            rightButton.onClick.AddListener(() => OnClickAnimated(rightButton.transform, OnRightButtonClicked));
        }

        if (upButton != null)
        {
            upButton.onClick.RemoveAllListeners();
            upButton.onClick.AddListener(() => OnClickAnimated(upButton.transform, OnUpButtonClicked));
        }

        if (downButton != null)
        {
            downButton.onClick.RemoveAllListeners();
            downButton.onClick.AddListener(() => OnClickAnimated(downButton.transform, OnDownButtonClicked));
        }
    }

    private void OnClickAnimated(Transform btnTransform, System.Action action)
    {
        if (isAnimating) return;

        btnTransform.DOKill();
        btnTransform.localScale = Vector3.one;

        btnTransform.DOScale(0.95f, 0.08f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                btnTransform.DOScale(1f, 0.15f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnComplete(() => action?.Invoke());
            });
    }

    /// <summary>
    /// ★移動開始時に MessageUI が開いていれば閉じる
    /// </summary>
    private void CloseMessageUI()
    {
        if (MessageUI.Instance != null)
        {
            MessageUI.Instance.HideMessage();
        }
    }

    // ================================================
    //  移動処理
    // ================================================

    public void OnLeftButtonClicked()
    {
        if (isInSubView || isAnimating || roomViews == null || roomViews.Count <= 1) return;

        int nextIndex = currentIndex - 1;
        if (nextIndex < 0)
        {
            if (isLooping) nextIndex = roomViews.Count - 1;
            else return;
        }

        CloseMessageUI(); // ★メッセージ吹き出しをシュッと閉じる

        RectTransform fromView = roomViews[currentIndex];
        currentIndex = nextIndex;
        RectTransform toView = roomViews[currentIndex];

        UpdateButtonStates();
        SlideView(fromView, toView, SlideDirection.Left);
    }

    public void OnRightButtonClicked()
    {
        if (isInSubView || isAnimating || roomViews == null || roomViews.Count <= 1) return;

        int nextIndex = currentIndex + 1;
        if (nextIndex >= roomViews.Count)
        {
            if (isLooping) nextIndex = 0;
            else return;
        }

        CloseMessageUI(); // ★メッセージ吹き出しをシュッと閉じる

        RectTransform fromView = roomViews[currentIndex];
        currentIndex = nextIndex;
        RectTransform toView = roomViews[currentIndex];

        UpdateButtonStates();
        SlideView(fromView, toView, SlideDirection.Right);
    }

    public void OnUpButtonClicked()
    {
        if (topView == null || isInSubView || isAnimating) return;

        CloseMessageUI(); // ★メッセージ吹き出しをシュッと閉じる

        RectTransform fromView = roomViews[currentIndex];
        isInSubView = true;

        UpdateButtonStates();
        SlideView(fromView, topView, SlideDirection.Up);
    }

    public void OnDownButtonClicked()
    {
        if (isAnimating) return;

        CloseMessageUI(); // ★メッセージ吹き出しをシュッと閉じる

        if (isInSubView)
        {
            RectTransform currentSubView = (topView != null && topView.gameObject.activeSelf) ? topView : bottomView;
            if (currentSubView == null) return;

            SlideDirection dir = (currentSubView == topView) ? SlideDirection.Down : SlideDirection.Up;
            isInSubView = false;

            UpdateButtonStates();
            SlideView(currentSubView, roomViews[currentIndex], dir);
        }
        else if (bottomView != null)
        {
            RectTransform fromView = roomViews[currentIndex];
            isInSubView = true;

            UpdateButtonStates();
            SlideView(fromView, bottomView, SlideDirection.Down);
        }
    }

    // ================================================
    //  スライドアニメーション本体
    // ================================================

    private void SlideView(RectTransform fromView, RectTransform toView, SlideDirection direction)
    {
        if (fromView == null || toView == null) return;

        isAnimating = true;

        Vector2 startOffset = Vector2.zero;
        Vector2 endOffset = Vector2.zero;

        switch (direction)
        {
            case SlideDirection.Right:
                startOffset = new Vector2(screenWidth, 0);
                endOffset = new Vector2(-screenWidth, 0);
                break;
            case SlideDirection.Left:
                startOffset = new Vector2(-screenWidth, 0);
                endOffset = new Vector2(screenWidth, 0);
                break;
            case SlideDirection.Up:
                startOffset = new Vector2(0, -screenHeight);
                endOffset = new Vector2(0, screenHeight);
                break;
            case SlideDirection.Down:
                startOffset = new Vector2(0, screenHeight);
                endOffset = new Vector2(0, -screenHeight);
                break;
        }

        toView.anchoredPosition = startOffset;
        toView.gameObject.SetActive(true);

        fromView.DOAnchorPos(endOffset, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true);

        toView.DOAnchorPos(Vector2.zero, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                fromView.gameObject.SetActive(false);
                isAnimating = false;
            });
    }

    private void UpdateButtonStates()
    {
        if (isInSubView)
        {
            if (leftButton != null) leftButton.gameObject.SetActive(false);
            if (rightButton != null) rightButton.gameObject.SetActive(false);
            if (upButton != null) upButton.gameObject.SetActive(false);
            if (downButton != null) downButton.gameObject.SetActive(true);
        }
        else
        {
            bool isFirst = currentIndex == 0;
            bool isLast = currentIndex == roomViews.Count - 1;

            if (leftButton != null) leftButton.gameObject.SetActive(isLooping || !isFirst);
            if (rightButton != null) rightButton.gameObject.SetActive(isLooping || !isLast);
            if (upButton != null) upButton.gameObject.SetActive(topView != null);
            if (downButton != null) downButton.gameObject.SetActive(bottomView != null);
        }
    }
}