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
    [SerializeField] private Rigidbody2D cartRigidbody;
    [SerializeField] private float minX = -400f;
    [SerializeField] private float maxX = 400f;
    [SerializeField] private float cartMoveSpeed = 25f; // カート追従スピード
    [SerializeField] private Collider2D cartInnerArea; // カート内側の判定エリア (Trigger)

    [Header("■ トイレットペーパー基本設定")]
    [SerializeField] private GameObject normalPaperPrefab;  // 通常トイレットペーパー
    [SerializeField] private GameObject goldenPaperPrefab;  // ゴールデントイレットペーパー
    [Range(0, 100)]
    [SerializeField] private float goldenSpawnChance = 15f; // ゴールデンが出現する確率（％）

    [SerializeField] private RectTransform spawnAreaCenter;
    [SerializeField] private Transform canvasParent;
    [SerializeField] private float spawnInterval = 0.8f;

    [Header("■ 物理・発射パラメータ（インスペクター調整用）")]
    [SerializeField] private float minForceY = 1000f;
    [SerializeField] private float maxForceY = 1500f;
    [SerializeField] private float minForceX = -200f;
    [SerializeField] private float maxForceX = 200f;
    [SerializeField] private float torqueAmount = 50f;

    private float currentTimer;
    private bool isGameActive = false;
    private Canvas parentCanvas;
    private List<ToiletPaperItem> spawnedPapers = new List<ToiletPaperItem>();

    public static int TotalHesokuriMoney { get; private set; } = 0;

    private void Start()
    {
        if (cartTransform != null)
        {
            parentCanvas = cartTransform.GetComponentInParent<Canvas>();
            if (cartRigidbody == null) cartRigidbody = cartTransform.GetComponent<Rigidbody2D>();
        }

        // 初期状態ではまだゲームを開始せず（タイマー停止）、説明ダイアログを開く
        PrepareMiniGame();
    }

    /// <summary>
    /// ミニゲームの初期化と説明ダイアログの表示
    /// </summary>
    public void PrepareMiniGame()
    {
        isGameActive = false; // まだタイマーもカート操作もスポーンも行わない
        currentTimer = gameTime;

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(gameTime).ToString() + "秒";
        }

        // 説明ダイアログが存在する場合は開く
        if (TPInstructionModalUI.Instance != null)
        {
            TPInstructionModalUI.Instance.Show(this);
        }
        else
        {
            // ダイアログが無い場合は直接スタート
            StartMiniGame();
        }
    }

    /// <summary>
    /// 説明ダイアログが閉じられた後に呼ばれてゲームを実際にスタートする
    /// </summary>
    public void OnInstructionClosedAndStartGame()
    {
        StartMiniGame();
    }

    public void StartMiniGame()
    {
        spawnedPapers.Clear();
        currentTimer = gameTime;
        isGameActive = true; // ここからゲーム＆カウントダウン開始！

        StartCoroutine(SpawnRoutine());
    }

    private void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, currentTimer)).ToString();
        }

        if (currentTimer <= 0)
        {
            EndMiniGame();
            return;
        }

        HandleCartInput();
    }

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
                float targetX = Mathf.Clamp(localPoint.x, minX, maxX);

                if (cartRigidbody != null)
                {
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

            if (!isGameActive) break;

            // 出現確率によって通常かゴールデンかをランダム選択
            GameObject prefabToSpawn = normalPaperPrefab;
            if (goldenPaperPrefab != null && Random.Range(0f, 100f) <= goldenSpawnChance)
            {
                prefabToSpawn = goldenPaperPrefab;
            }

            if (prefabToSpawn == null) continue;

            Transform parent = canvasParent != null ? canvasParent : transform;
            GameObject tp = Instantiate(prefabToSpawn, parent);

            Vector2 startPos = spawnAreaCenter != null ? spawnAreaCenter.anchoredPosition : Vector2.zero;
            float halfWidth = spawnAreaCenter != null ? spawnAreaCenter.rect.width * 0.5f : 100f;
            startPos.x += Random.Range(-halfWidth, halfWidth);

            RectTransform tpRect = tp.GetComponent<RectTransform>();
            if (tpRect != null) tpRect.anchoredPosition = startPos;

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

        // カート内にある各ペーパーの金額と数を集計
        int totalGainedMoney = CalculateTotalMoneyInCart(out int totalCount, out int goldenCount);

        // 本編のへそくりマネージャーへ加算
        if (HesokuriManager.Instance != null)
        {
            HesokuriManager.Instance.AddHesokuri(totalGainedMoney);
        }
        else
        {
            TotalHesokuriMoney += totalGainedMoney;
        }

        int currentTotalMoney = HesokuriManager.Instance != null
            ? HesokuriManager.Instance.CurrentHesokuri
            : TotalHesokuriMoney;

        // モーダルUIを使ってリザルトを表示
        if (TPResultModalUI.Instance != null)
        {
            TPResultModalUI.Instance.Show(totalCount, goldenCount, totalGainedMoney, currentTotalMoney, this);
        }
    }

    /// <summary>
    /// カート内にあるトイレットペーパーの個別の金額（PaperValue）を合計するメソッド
    /// </summary>
    private int CalculateTotalMoneyInCart(out int totalCount, out int goldenCount)
    {
        int totalMoney = 0;
        totalCount = 0;
        goldenCount = 0;

        foreach (var paper in spawnedPapers)
        {
            if (paper != null && cartInnerArea != null)
            {
                Collider2D paperCol = paper.GetComponent<Collider2D>();
                bool isInCart = false;

                if (paperCol != null)
                {
                    // コライダー範囲（Bounds）が重なっているか判定
                    isInCart = cartInnerArea.bounds.Intersects(paperCol.bounds);
                }
                else
                {
                    isInCart = cartInnerArea.OverlapPoint(paper.transform.position);
                }

                if (isInCart)
                {
                    totalMoney += paper.PaperValue; // 各ペーパーが持つ個別金額を加算
                    totalCount++;

                    // 100円を超える高額ペーパー（金）を個別にカウント
                    if (paper.PaperValue > 100)
                    {
                        goldenCount++;
                    }
                }
            }
        }
        return totalMoney;
    }

    /// <summary>
    /// リザルトの閉じるボタン押下時に呼ばれる（本編復帰処理）
    /// </summary>
    public void OnCloseResultAndReturnMain()
    {
        // 画面に残っているペーパーのクリア
        foreach (var paper in spawnedPapers)
        {
            if (paper != null) Destroy(paper.gameObject);
        }
        spawnedPapers.Clear();

        // ミニゲームUI全体の非アクティブ化
        gameObject.SetActive(false);
    }
}