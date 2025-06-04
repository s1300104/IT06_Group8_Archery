using UnityEngine;
using TMPro; // TextMeshProを使うために必要 (3Dテキストもこれに含まれる)

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References (Floating Panel)")]
    // フローティングパネルのUIテキストへの参照（もし使用する場合）
    public TextMeshProUGUI floatingScoreText;
    public TextMeshProUGUI floatingTimerText;

    [Header("UI References (TV Screen)")]
    // TV画面のUIテキストへの参照
    public TextMeshProUGUI tvScoreText;
    public TextMeshProUGUI tvTimerText;

    [Header("UI References (3D Objects)")] // ★ここを追加★
    // 3Dオブジェクトのテキストへの参照
    public TextMeshPro scoreText3D; // ★ TextMeshProUGUI ではなく TextMeshPro に変更 ★
    public TextMeshPro timerText3D; // ★ TextMeshProUGUI ではなく TextMeshPro に変更 ★


    [Header("Game Settings")]
    public int initialScore = 0;
    public float initialTime = 30f;

    private int _currentScore;
    private float _currentTime;
    private bool _isGameActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        _currentScore = initialScore;
        _currentTime = initialTime;
        _isGameActive = true;

        UpdateScoreUI();
        UpdateTimerUI();

        Debug.Log("GameManager initialized. Game Started!");
    }

    void Update()
    {
        if (_isGameActive)
        {
            _currentTime -= Time.deltaTime;
            UpdateTimerUI();
            AddScore();

            if (_currentTime <= 0)
            {
                _currentTime = 0;
                _isGameActive = false;
                UpdateTimerUI();
                Debug.Log("Game Over!");

                if (tvScoreText != null) tvScoreText.text = "GAME OVER";
                if (tvTimerText != null) tvTimerText.text = "";

                // ★ 3Dテキストもゲームオーバー表示に更新 ★
                if (scoreText3D != null) scoreText3D.text = "GAME OVER";
                if (timerText3D != null) timerText3D.text = "";
            }
        }
    }

    public void AddScore()
    {
        if (_isGameActive)
        {
            int count = TargetPoolManager.Instance.getDefeatCount();
            _currentScore = count;
            UpdateScoreUI();
            Debug.Log($"Score updated: {_currentScore}");
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting Game...");
        _currentScore = initialScore;
        _currentTime = initialTime;
        _isGameActive = true;
        UpdateScoreUI();
        UpdateTimerUI();
    }

    // スコア表示UIを更新するメソッド
    private void UpdateScoreUI()
    {
        string scoreString = $"Score: {_currentScore}";

        if (floatingScoreText != null)
        {
            floatingScoreText.text = scoreString;
        }
        if (tvScoreText != null)
        {
            tvScoreText.text = scoreString;
        }
        // ★ 3Dテキストのスコアを更新 ★
        if (scoreText3D != null)
        {
            scoreText3D.text = scoreString;
        }
    }

    // タイマー表示UIを更新するメソッド
    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(_currentTime / 60);
        int seconds = Mathf.FloorToInt(_currentTime % 60);
        string timerString = $"Time: {minutes:00}:{seconds:00}";

        if (floatingTimerText != null)
        {
            floatingTimerText.text = timerString;
        }
        if (tvTimerText != null)
        {
            tvTimerText.text = timerString;
        }
        // ★ 3Dテキストのタイマーを更新 ★
        if (timerText3D != null)
        {
            timerText3D.text = timerString;
        }
    }

    public bool IsGameActive
    {
        get { return _isGameActive; }
    }

    public int CurrentScore
    {
        get { return _currentScore; }
    }

    public float CurrentTime
    {
        get { return _currentTime; }
    }
}