using UnityEngine;

public class GrenadeManager : MonoBehaviour
{
    public static GrenadeManager Instance { get; private set; }

    [Tooltip("シーン内に存在できるグレネードの最大数")]
    public int maxAllowedGrenades = 10;

    private int currentActiveGrenades = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // シーンをまたいでマネージャーを維持する場合
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 新しいグレネードを生成（アクティブ化）できるか確認します。
    /// </summary>
    /// <returns>生成可能な場合はtrue、そうでない場合はfalse。</returns>
    public bool CanSpawnGrenade()
    {
        return currentActiveGrenades < maxAllowedGrenades;
    }

    /// <summary>
    /// 新たにグレネードが有効になった際に呼び出されます。
    /// 通常、グレネードのStart()またはAwake()から呼ばれます。
    /// </summary>
    public void RegisterGrenade()
    {
        currentActiveGrenades++;
        UpdateGrenadeCountUI(); // UI更新メソッドを呼び出す（UIがあれば）
    }

    /// <summary>
    /// グレネードが無効（破棄）になった際に呼び出されます。
    /// 通常、グレネードのOnDestroy()から呼ばれます。
    /// </summary>
    public void UnregisterGrenade()
    {
        if (currentActiveGrenades > 0)
        {
            currentActiveGrenades--;
        }
        UpdateGrenadeCountUI(); // UI更新メソッドを呼び出す（UIがあれば）
    }

    /// <summary>
    /// 現在アクティブなグレネードの数を取得します。
    /// </summary>
    /// <returns>アクティブなグレネードの数。</returns>
    public int GetCurrentGrenadeCount()
    {
        return currentActiveGrenades;
    }

    /// <summary>
    /// 許容されるグレネードの最大数を取得します。
    /// </summary>
    /// <returns>最大グレネード数。</returns>
    public int GetMaxAllowedGrenades()
    {
        return maxAllowedGrenades;
    }

    // UI更新のための仮メソッド（実際のUIシステムに応じて実装）
    private void UpdateGrenadeCountUI()
    {
        // Debug.Log($"Active Grenades: {currentActiveGrenades}/{maxAllowedGrenades}");
        // ここで実際のUI要素（例: TextMeshProのテキストなど）を更新するロジックを記述します。
        // 例: if (grenadeCountText != null) grenadeCountText.text = $"{currentActiveGrenades} / {maxAllowedGrenades}";
    }
}