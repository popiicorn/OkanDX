using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class EpisodeSelectManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private RectTransform panelA;
    [SerializeField] private RectTransform panelB;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageText;

    [Header("ページインジケーター設定")]
    [Tooltip("ドットを並べる親オブジェクト")]
    [SerializeField] private Transform pageIndicatorContainer;
    [Tooltip("ドットのPrefab")]
    [SerializeField] private GameObject pageDotPrefab;
    [Tooltip("選択中のドットの色")]
    [SerializeField] private Color activeDotColor = Color.yellow;
    [Tooltip("非選択のドットの色")]
    [SerializeField] private Color inactiveDotColor = Color.white;

    [Header("設定")]
    [SerializeField] private int totalEpisodes = 30;
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    private const int ITEMS_PER_PAGE = 8;
    private int currentPage = 0;
    private bool isAnimating = false;

    private RectTransform activePanel;
    private RectTransform inactivePanel;
    private List<Image> dotImages = new List<Image>(); // 生成したドットのImage群

    private void Start()
    {
        activePanel = panelA;
        inactivePanel = panelB;

        activePanel.anchoredPosition = Vector2.zero;
        inactivePanel.anchoredPosition = new Vector2(1920, 0);

        GenerateDots(); // ドットの自動生成
        SetupPanelData(activePanel, currentPage);
        UpdateUIState();
    }

    /// <summary>
    /// 総ページ数に合わせてドット（〇）を生成する
    /// </summary>
    private void GenerateDots()
    {
        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE);

        // 既存のドットをクリア
        foreach (Transform child in pageIndicatorContainer)
        {
            Destroy(child.gameObject);
        }
        dotImages.Clear();

        // ページ数分だけドットを生成
        for (int i = 0; i < maxPage; i++)
        {
            GameObject dot = Instantiate(pageDotPrefab, pageIndicatorContainer);
            Image img = dot.GetComponent<Image>();
            if (img != null)
            {
                dotImages.Add(img);
            }
        }
    }

    /// <summary>
    /// ドットの色を現在のページに合わせて更新する
    /// </summary>
    private void UpdateDots()
    {
        for (int i = 0; i < dotImages.Count; i++)
        {
            if (i == currentPage)
            {
                dotImages[i].color = activeDotColor;
                // 0.2秒かけて100%のサイズになめらかに拡大
                dotImages[i].transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            }
            else
            {
                dotImages[i].color = inactiveDotColor;
                // 0.2秒かけて90%のサイズになめらかに縮小
                dotImages[i].transform.DOScale(new Vector3(0.9f, 0.9f, 1f), 0.2f);
            }
        }
    }

    private void SetupPanelData(RectTransform panel, int pageIndex)
    {
        int clearedIndex = GameManager.Instance != null ? GameManager.Instance.ClearedEpisodeIndex : 0;
        Button[] buttons = panel.GetComponentsInChildren<Button>();

        for (int i = 0; i < buttons.Length; i++)
        {
            int episodeNumber = (pageIndex * ITEMS_PER_PAGE) + i + 1;

            if (episodeNumber <= totalEpisodes)
            {
                buttons[i].gameObject.SetActive(true);

                TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = $"Ep.{episodeNumber:D3}";

                bool isUnlocked = episodeNumber <= clearedIndex + 1;
                buttons[i].interactable = isUnlocked;

                buttons[i].onClick.RemoveAllListeners();
                int epNum = episodeNumber;
                buttons[i].onClick.AddListener(() => OnSelectEpisode(epNum));
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 右ボタン（次へ）を押した時
    /// </summary>
    public void OnClickNextPage()
    {
        if (isAnimating) return;

        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE) - 1;

        AnimateButton(nextButton.transform);

        // 最後のページなら最初のページ(0)へ、それ以外は次のページへ
        if (currentPage >= maxPage)
        {
            currentPage = 0;
        }
        else
        {
            currentPage++;
        }

        SlidePanel(isNext: true);
    }

    /// <summary>
    /// 左ボタン（前へ）を押した時
    /// </summary>
    public void OnClickPrevPage()
    {
        if (isAnimating) return;

        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE) - 1;

        AnimateButton(prevButton.transform);

        // 最初のページ(0)なら最後のページへ、それ以外は前のページへ
        if (currentPage <= 0)
        {
            currentPage = maxPage;
        }
        else
        {
            currentPage--;
        }

        SlidePanel(isNext: false);
    }

    /// <summary>
    /// ボタンをクリックした時のポヨンとした拡縮アニメーション
    /// </summary>
    private void AnimateButton(Transform buttonTransform)
    {
        // 連打時のサイズ崩れを防ぐため一旦スケールをリセットしてTweenをキル
        buttonTransform.DOKill();
        buttonTransform.localScale = Vector3.one;

        // 一瞬 0.85倍 に縮んでから、少し反動をつけて 1.0倍 に戻る
        buttonTransform.DOScale(0.85f, 0.08f)
            .OnComplete(() =>
            {
                buttonTransform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
            });
    }

    private void SlidePanel(bool isNext)
    {
        isAnimating = true;

        UpdateUIState();

        SetupPanelData(inactivePanel, currentPage);

        float screenWidth = 1920f;
        Vector2 startPosInactive = new Vector2(isNext ? screenWidth : -screenWidth, 0);
        Vector2 endPosActive = new Vector2(isNext ? -screenWidth : screenWidth, 0);

        inactivePanel.anchoredPosition = startPosInactive;

        activePanel.DOAnchorPos(endPosActive, slideDuration).SetEase(slideEase);

        inactivePanel.DOAnchorPos(Vector2.zero, slideDuration)
            .SetEase(slideEase)
            .OnComplete(() =>
            {
                RectTransform temp = activePanel;
                activePanel = inactivePanel;
                inactivePanel = temp;

                isAnimating = false;
            });
    }

    /// <summary>
    /// UI状態の更新（矢印は常に表示）
    /// </summary>
    private void UpdateUIState()
    {
        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE) - 1;

        if (pageText != null) pageText.text = $"{currentPage + 1} / {maxPage + 1}";

        // ★ループ仕様のため、左右の矢印ボタンは常に表示（True）にする
        if (prevButton != null) prevButton.gameObject.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        // ドット表示の更新
        UpdateDots();
    }

    private void OnSelectEpisode(int episodeNumber)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEpisode(episodeNumber);
        }
    }
}