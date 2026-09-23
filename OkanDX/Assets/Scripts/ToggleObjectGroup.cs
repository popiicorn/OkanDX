using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleObjectGroup : MonoBehaviour
{
    public enum ToggleMode
    {
        MutualExclusion, // パターン①：切り替え方式（Aが表示の時B非表示 ＆ Bが表示の時A非表示）
        OneWayAtoB       // パターン②：一方のみ（Aが表示の時B非表示）
    }

    [System.Serializable]
    public class TogglePair
    {
        [Tooltip("ペアの識別名（インスペクター管理用・任意）")]
        public string pairName = "New Pair";

        [Tooltip("連動パターン")]
        public ToggleMode mode = ToggleMode.MutualExclusion;

        [Header("■ オブジェクトA群")]
        public GameObject[] objectsA;

        [Header("■ オブジェクトB群")]
        public GameObject[] objectsB;

        [HideInInspector] public bool lastStateA = false;
        [HideInInspector] public bool lastStateB = false;
    }

    [Header("■ 連動ペアのリスト（任意にいくらでも追加可能）")]
    [SerializeField] private List<TogglePair> togglePairs = new List<TogglePair>();

    private void Update()
    {
        if (togglePairs == null || togglePairs.Count == 0) return;

        foreach (var pair in togglePairs)
        {
            if (pair == null) continue;

            bool isAActive = IsAnyActive(pair.objectsA);
            bool isBActive = IsAnyActive(pair.objectsB);

            switch (pair.mode)
            {
                case ToggleMode.MutualExclusion:
                    // パターン①：切り替え方式
                    if (isAActive && isAActive != pair.lastStateA)
                    {
                        // Aが表示されたらBを非表示
                        SetAllActive(pair.objectsB, false);
                        pair.lastStateB = false;
                    }
                    else if (isBActive && isBActive != pair.lastStateB)
                    {
                        // Bが表示されたらAを非表示
                        SetAllActive(pair.objectsA, false);
                        pair.lastStateA = false;
                    }
                    break;

                case ToggleMode.OneWayAtoB:
                    // パターン②：一方のみ
                    if (isAActive && isAActive != pair.lastStateA)
                    {
                        // Aが表示されたらBを非表示
                        SetAllActive(pair.objectsB, false);
                        pair.lastStateB = false;
                    }
                    break;
            }

            pair.lastStateA = IsAnyActive(pair.objectsA);
            pair.lastStateB = IsAnyActive(pair.objectsB);
        }
    }

    private bool IsAnyActive(GameObject[] objects)
    {
        if (objects == null || objects.Length == 0) return false;
        foreach (var obj in objects)
        {
            if (obj != null && obj.activeSelf) return true;
        }
        return false;
    }

    private void SetAllActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (var obj in objects)
        {
            if (obj != null && obj.activeSelf != active)
            {
                obj.SetActive(active);
            }
        }
    }
}