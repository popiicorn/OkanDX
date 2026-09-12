using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class EpisodeSelectManager : MonoBehaviour
{
    [Header("エピソードデータ（順番通りにセット）")]
    [SerializeField] private List<EpisodeData> episodeDataList;

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
    private List<Image> dotImages = new List<Image>();

    private void Start()
    {
        activePanel = panelA;
        inactivePanel = panelB;

        activePanel.anchoredPosition = Vector2.zero;
        inactivePanel.anchoredPosition = new Vector2(1920, 0);

        GenerateDots();
        SetupPanelData(activePanel, currentPage);
        UpdateUIState();
    }

    /// <summary>
    /// 総ページ数に合わせてドット（〇）を生成する
    /// </summary>
    private void GenerateDots()
    {
        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE);

        foreach (Transform child in pageIndicatorContainer)
        {
            Destroy(child.gameObject);
        }
        dotImages.Clear();

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
                dotImages[i].transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            }
            else
            {
                dotImages[i].color = inactiveDotColor;
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
            int dataIndex = episodeNumber - 1; // List用インデックス

            if (episodeNumber <= totalEpisodes)
            {
                buttons[i].gameObject.SetActive(true);

                // EpisodeDataからタイトルを取得（データがある場合）
                TMP_Text btnText = buttons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    if (episodeDataList != null && dataIndex < episodeDataList.Count && episodeDataList[dataIndex] != null)
                    {
                        btnText.text = episodeDataList[dataIndex].FullTitleText;
                    }
                    else
                    {
                        btnText.text = $"エピソード{episodeNumber}";
                    }
                }

                bool isUnlocked = episodeNumber <= clearedIndex + 1;
                buttons[i].interactable = isUnlocked;

                // ★修正点：RemoveAllListeners() をやめ、プログラム登録分のみListenerを安全に登録
                EpisodeData targetData = (episodeDataList != null && dataIndex < episodeDataList.Count) ? episodeDataList[dataIndex] : null;
                int epNum = episodeNumber;

                // ページ切り替え時にクリックイベントが重複登録されるのを防ぐため、一旦この処理だけを外してから再登録
                buttons[i].onClick.RemoveListener(() => OnSelectEpisode(epNum, targetData));
                buttons[i].onClick.AddListener(() => OnSelectEpisode(epNum, targetData));
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnClickNextPage()
    {
        if (isAnimating) return;

        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE) - 1;

        AnimateButton(nextButton.transform);

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

    public void OnClickPrevPage()
    {
        if (isAnimating) return;

        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE) - 1;

        AnimateButton(prevButton.transform);

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

    private void AnimateButton(Transform buttonTransform)
    {
        buttonTransform.DOKill();
        buttonTransform.localScale = Vector3.one;

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

    private void UpdateUIState()
    {
        int maxPage = Mathf.CeilToInt((float)totalEpisodes / ITEMS_PER_PAGE) - 1;

        if (pageText != null) pageText.text = $"{currentPage + 1} / {maxPage + 1}";

        if (prevButton != null) prevButton.gameObject.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        UpdateDots();
    }

    private void OnSelectEpisode(int episodeNumber, EpisodeData data)
    {
        if (GameManager.Instance != null)
        {
            // GameManager側に EpisodeData を渡してエピソード開始
            if (data != null)
            {
                GameManager.Instance.StartEpisode(data);
            }
            else
            {
                GameManager.Instance.StartEpisode(episodeNumber);
            }
        }
    }
}