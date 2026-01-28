using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Leap;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetFingerText;
    [SerializeField] private Image progressBar;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color successColor = Color.green;

    private bool uiCreated = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateUI();
    }

    void Start()
    {
        // Subscribe to events
        FingerIndividuationGame.OnCalibrationStatus += ShowCalibrationStatus;
        FingerIndividuationGame.OnGestureSuccess += OnGestureSuccess;
        FingerIndividuationGame.OnCountdownTick += ShowCountdown;
        FingerIndividuationGame.OnCalibrationProgress += ShowProgress;
        FingerIndividuationGame.OnGamePaused += OnGamePaused;
        FingerIndividuationGame.OnGameResumed += OnGameResumed;
        FingerIndividuationGame.OnHandsLost += OnHandsLost;
        FingerIndividuationGame.OnHandsDrifted += OnHandsDrifted;
        FingerIndividuationGame.OnHandsRestored += OnHandsRestored;
    }

    void OnDestroy()
    {
        FingerIndividuationGame.OnCalibrationStatus -= ShowCalibrationStatus;
        FingerIndividuationGame.OnGestureSuccess -= OnGestureSuccess;
        FingerIndividuationGame.OnCountdownTick -= ShowCountdown;
        FingerIndividuationGame.OnCalibrationProgress -= ShowProgress;
        FingerIndividuationGame.OnGamePaused -= OnGamePaused;
        FingerIndividuationGame.OnGameResumed -= OnGameResumed;
        FingerIndividuationGame.OnHandsLost -= OnHandsLost;
        FingerIndividuationGame.OnHandsDrifted -= OnHandsDrifted;
        FingerIndividuationGame.OnHandsRestored -= OnHandsRestored;
    }

    private void OnGamePaused()
    {
        // Show warning panel - the message is set via OnCalibrationStatus
        ShowWarning("");
    }

    private void OnGameResumed()
    {
        HideWarning();
        HideCountdown();
    }

    private void OnHandsLost()
    {
        ShowWarning("HANDS NOT DETECTED\n\nPlease return your hands to the sensor.");
    }

    private void OnHandsDrifted()
    {
        ShowWarning("HANDS HAVE DRIFTED\n\nReturn to your baseline position.");
    }

    private void OnHandsRestored()
    {
        HideWarning();
    }

    private void CreateUI()
    {
        if (uiCreated) return;

        // Create Canvas if not assigned
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("GameUICanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;

            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create UI elements
        CreateInstructionText();
        CreateCountdownText();
        CreateStatusText();
        CreateScoreText();
        CreateTargetFingerText();
        CreateWarningPanel();
        CreateProgressBar();

        uiCreated = true;
        HideAllUI();
    }

    private void CreateInstructionText()
    {
        GameObject go = new GameObject("InstructionText");
        go.transform.SetParent(mainCanvas.transform, false);

        instructionText = go.AddComponent<TextMeshProUGUI>();
        instructionText.fontSize = 36;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.color = normalColor;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.85f);
        rt.anchorMax = new Vector2(0.5f, 0.95f);
        rt.sizeDelta = new Vector2(800, 100);
        rt.anchoredPosition = Vector2.zero;
    }

    private void CreateCountdownText()
    {
        GameObject go = new GameObject("CountdownText");
        go.transform.SetParent(mainCanvas.transform, false);

        countdownText = go.AddComponent<TextMeshProUGUI>();
        countdownText.fontSize = 120;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = normalColor;
        countdownText.fontStyle = FontStyles.Bold;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 200);
        rt.anchoredPosition = Vector2.zero;
    }

    private void CreateStatusText()
    {
        GameObject go = new GameObject("StatusText");
        go.transform.SetParent(mainCanvas.transform, false);

        statusText = go.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 28;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = normalColor;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.7f);
        rt.anchorMax = new Vector2(0.5f, 0.8f);
        rt.sizeDelta = new Vector2(600, 80);
        rt.anchoredPosition = Vector2.zero;
    }

    private void CreateScoreText()
    {
        GameObject go = new GameObject("ScoreText");
        go.transform.SetParent(mainCanvas.transform, false);

        scoreText = go.AddComponent<TextMeshProUGUI>();
        scoreText.fontSize = 48;
        scoreText.alignment = TextAlignmentOptions.TopRight;
        scoreText.color = normalColor;
        scoreText.text = "Score: 0";

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(300, 80);
        rt.anchoredPosition = new Vector2(-20, -20);
    }

    private void CreateTargetFingerText()
    {
        GameObject go = new GameObject("TargetFingerText");
        go.transform.SetParent(mainCanvas.transform, false);

        targetFingerText = go.AddComponent<TextMeshProUGUI>();
        targetFingerText.fontSize = 42;
        targetFingerText.alignment = TextAlignmentOptions.Center;
        targetFingerText.color = successColor;
        targetFingerText.fontStyle = FontStyles.Bold;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.15f);
        rt.anchorMax = new Vector2(0.5f, 0.25f);
        rt.sizeDelta = new Vector2(500, 80);
        rt.anchoredPosition = Vector2.zero;
    }

    private void CreateWarningPanel()
    {
        // Background panel
        GameObject panelGO = new GameObject("WarningPanel");
        panelGO.transform.SetParent(mainCanvas.transform, false);

        warningPanel = panelGO;
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;

        // Warning text
        GameObject textGO = new GameObject("WarningText");
        textGO.transform.SetParent(panelGO.transform, false);

        warningText = textGO.AddComponent<TextMeshProUGUI>();
        warningText.fontSize = 48;
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.color = warningColor;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.1f, 0.3f);
        textRT.anchorMax = new Vector2(0.9f, 0.7f);
        textRT.sizeDelta = Vector2.zero;

        warningPanel.SetActive(false);
    }

    private void CreateProgressBar()
    {
        // Background
        GameObject bgGO = new GameObject("ProgressBarBG");
        bgGO.transform.SetParent(mainCanvas.transform, false);

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.3f, 0.6f);
        bgRT.anchorMax = new Vector2(0.7f, 0.65f);
        bgRT.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillGO = new GameObject("ProgressBarFill");
        fillGO.transform.SetParent(bgGO.transform, false);

        progressBar = fillGO.AddComponent<Image>();
        progressBar.color = successColor;

        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);
        fillRT.sizeDelta = Vector2.zero;

        bgGO.SetActive(false);
    }

    // Public methods for other scripts to call

    public void ShowInstruction(string text)
    {
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = text;
            instructionText.color = normalColor;
        }
    }

    public void ShowCountdown(int seconds)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = seconds.ToString();

            // Color changes as countdown progresses
            if (seconds <= 3)
                countdownText.color = successColor;
            else if (seconds <= 5)
                countdownText.color = warningColor;
            else
                countdownText.color = normalColor;
        }
    }

    public void HideCountdown()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    public void ShowCalibrationStatus(string status)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = status;
        }
    }

    public void ShowProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.transform.parent.gameObject.SetActive(true);
            RectTransform rt = progressBar.GetComponent<RectTransform>();
            rt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
        }
    }

    public void HideProgress()
    {
        if (progressBar != null)
            progressBar.transform.parent.gameObject.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = $"Score: {score}";
        }
    }

    public void ShowTargetFinger(string handName, string fingerName)
    {
        if (targetFingerText != null)
        {
            targetFingerText.gameObject.SetActive(true);
            targetFingerText.text = $"Press: {handName} {fingerName}";
        }
    }

    public void HideTargetFinger()
    {
        if (targetFingerText != null)
            targetFingerText.gameObject.SetActive(false);
    }

    public void ShowWarning(string message)
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(true);
            warningText.text = message;
        }
    }

    public void HideWarning()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    public void ShowSuccess(string message)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
            statusText.color = successColor;
        }
    }

    public void ShowError(string message)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
            statusText.color = errorColor;
        }
    }

    public void HideAllUI()
    {
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (targetFingerText != null) targetFingerText.gameObject.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);
        HideProgress();
        // Keep score visible
    }

    private void OnGestureSuccess(Chirality hand, int fingerIndex)
    {
        string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
        ShowSuccess($"Nice! {hand} {fingerNames[fingerIndex]}");
    }
}
