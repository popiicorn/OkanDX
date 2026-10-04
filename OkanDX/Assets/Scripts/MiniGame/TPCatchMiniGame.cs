using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TPCatchMiniGame : MonoBehaviour
{
    [Header("■ 制限時間設定")]
    [SerializeField] private float gameTime = 30f;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("■ 買い物カート設定")]
    [SerializeField] private RectTransform cartTransform;
    [SerializeField] private Rigidbody2D cartRigidbody; // カートのRigidbody2D
    [SerializeField] private float minX = -400f;
    [SerializeField] private float maxX = 400f;
    [SerializeField] private Collider2D cartInnerArea; // ⑧ 計測用：カート内側のTriggerエリア

    [Header("■ トイレットペーパー基本設定")]
    [SerializeField] private GameObject toiletPaperPrefab;
    [SerializeField] private RectTransform spawnAreaCenter;
    [SerializeField] private Transform canvasParent;
    [SerializeField] private float spawnInterval = 0.8f;

    [Header("■ 物理・発射パラメータ（インスペクター調整用）")]
    [Tooltip("上方向（Y軸）へ飛ばす力")]
    [SerializeField] private float minForceY = 300f;
    [SerializeField] private float maxForceY = 500f;

    [Tooltip("左右（X軸）へ散らばせる力")]
    [SerializeField] private float minForceX = -100f;
    [SerializeField] private float maxForceX = 100f;

    [Tooltip("回転させる力")]
    [SerializeField] private float torqueAmount = 20f;

    [Header("■ リザルト表示・へそくり（円）設定")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private int pricePerPaper = 100; // ペーパー1個あたりのへそくり額（円）

    private float currentTimer;
    private bool isGameActive = false;
    private Canvas parentCanvas;
    private List<ToiletPaperItem> spawnedPapers = new List<ToiletPaperItem>();

    // ⑨ 脱出ゲーム本編へ引き継ぐへそくり合計額（他スクリプトから参照可能）
    public static int TotalHesokuriMoney { get; private set; } = 0;

    private void Start()
    {
        if (cartTransform != null)
        {
            parentCanvas = cartTransform.GetComponentInParent<Canvas>();
            if (cartRigidbody == null) cartRigidbody = cartTransform.GetComponent<Rigidbody2D>();
        }
        StartMiniGame();
    }

    public void StartMiniGame()
    {
        spawnedPapers.Clear();
        currentTimer = gameTime;
        isGameActive = true;

        if (resultPanel != null) resultPanel.SetActive(false);

        StartCoroutine(SpawnRoutine());
    }

    private void Update()
    {
        if (!isGameActive) return;

        // タイマーカウントダウン
        currentTimer -= Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, currentTimer)).ToString() + "秒";
        }

        if (currentTimer <= 0)
        {
            EndMiniGame();
            return;
        }

        // ② カートの物理移動（ドラッグ処理）
        HandleCartInput();
    }

    [Header("■ カート追従の滑らかさ")]
    [SerializeField] private float cartMoveSpeed = 25f; // 追従スピード（値が大きいほど俊敏）

    private void HandleCartInput()
    {
        if (cartTransform == null || parentCanvas == null) return;

        if (Input.GetMouseButton(0))
        {
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            if (canvasRect == null) return;

            Camera uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            if (uiCamera == null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = Camera.main;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                uiCamera,
                out Vector2 localPoint))
            {
                // 目標のX座標
                float targetX = Mathf.Clamp(localPoint.x, minX, maxX);

                if (cartRigidbody != null)
                {
                    // ★ 物理移動（MovePosition）の急激なワープを防ぐため、Lerpで滑らかに追従
                    Vector2 targetWorldPos = canvasRect.TransformPoint(new Vector3(targetX, cartTransform.anchoredPosition.y, 0));
                    Vector2 nextPos = Vector2.Lerp(cartRigidbody.position, targetWorldPos, Time.deltaTime * cartMoveSpeed);

                    cartRigidbody.MovePosition(nextPos);
                }
                else
                {
                    Vector2 newPos = cartTransform.anchoredPosition;
                    newPos.x = targetX;
                    cartTransform.anchoredPosition = newPos;
                }
            }
        }
        else
        {
            // マウスを離している時はカートの物理速度を完全にリセット（残存慣性で弾けるのを防ぐ）
            if (cartRigidbody != null)
            {
                cartRigidbody.linearVelocity = Vector2.zero;
            }
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isGameActive || toiletPaperPrefab == null) break;

            // ① スポーン生成
            Transform parent = canvasParent != null ? canvasParent : transform;
            GameObject tp = Instantiate(toiletPaperPrefab, parent);
            //tp.transform.SetAsLastSibling();

            // 生成位置（SpawnAreaCenterから少しランダムズレ）
            Vector2 startPos = spawnAreaCenter != null ? spawnAreaCenter.anchoredPosition : Vector2.zero;
            float halfWidth = spawnAreaCenter != null ? spawnAreaCenter.rect.width * 0.5f : 100f;
            startPos.x += Random.Range(-halfWidth, halfWidth);

            RectTransform tpRect = tp.GetComponent<RectTransform>();
            if (tpRect != null) tpRect.anchoredPosition = startPos;

            // 物理的な放り投げ力を計算
            Vector2 force = new Vector2(
                Random.Range(minForceX, maxForceX),
                Random.Range(minForceY, maxForceY)
            );
            float torque = Random.Range(-torqueAmount, torqueAmount);

            var tpScript = tp.GetComponent<ToiletPaperItem>();
            if (tpScript != null)
            {
                tpScript.LaunchPhysics(force, torque);
                spawnedPapers.Add(tpScript);
            }
        }
    }

    private void EndMiniGame()
    {
        isGameActive = false;

        // ⑧ タイムアップ時、カート内エリアに残っている数を集計
        int finalInCartCount = CountPapersInCart();

        // ⑨ へそくり額を計算して合算
        int gainedMoney = finalInCartCount * pricePerPaper;
        TotalHesokuriMoney += gainedMoney;

        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultScoreText != null)
        {
            resultScoreText.text = $"カートに入った数: {finalInCartCount}個\n獲得へそくり: {gainedMoney}円！\n(総へそくり: {TotalHesokuriMoney}円)";
        }
    }

    /// <summary>
    /// カートのTriggerエリア（cartInnerArea）内に留まっているペーパーの数を判定
    /// </summary>
    private int CountPapersInCart()
    {
        int count = 0;
        foreach (var paper in spawnedPapers)
        {
            if (paper != null && cartInnerArea != null)
            {
                // ペーパーの位置がカート内エリアの中にあるかチェック
                if (cartInnerArea.OverlapPoint(paper.transform.position))
                {
                    count++;
                }
            }
        }
        return count;
    }
}