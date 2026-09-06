using UnityEngine;

public class BootLoader : MonoBehaviour
{
    private void Start()
    {
        // 起動したら即座にタイトルシーンへ
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToTitle();
        }
    }
}