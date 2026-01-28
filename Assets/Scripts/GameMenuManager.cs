using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameMenuManager : MonoBehaviour
{
    public static GameMenuManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject rhythmGameManagerPrefab;
    [SerializeField] private GameObject pianoGameManagerPrefab;
    [SerializeField] private FingerIndividuationGame fingerGame;

    [Header("UI - Will be created if not assigned")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button game1Button;
    [SerializeField] private Button game2Button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI game1Text;
    [SerializeField] private TextMeshProUGUI game2Text;

    public enum GameType
    {
        None,
        PracticePiano,  // Wait for correct press, no time limit
        GuitarHero      // Timed notes (future)
    }

    private GameType selectedGame = GameType.None;
    private bool menuActive = true;
    private bool isCalibrated = false;

    public void SetPrefabs(GameObject rhythmPrefab, GameObject pianoPrefab)
    {
        rhythmGameManagerPrefab = rhythmPrefab;
        pianoGameManagerPrefab = pianoPrefab;
    }

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
    }

    void Start()
    {
        CreateMenuUI();
        ShowMenu();

        // Find finger game if not assigned
        if (fingerGame == null)
        {
            fingerGame = FindObjectOfType<FingerIndividuationGame>();
        }
    }

    private void CreateMenuUI()
    {
        // Create Canvas
        if (menuCanvas == null)
        {
            GameObject canvasGO = new GameObject("MenuCanvas");
            canvasGO.transform.SetParent(transform);
            menuCanvas = canvasGO.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 200; // Above game UI

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create background panel
        if (menuPanel == null)
        {
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(menuCanvas.transform, false);

            Image panelImage = menuPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

            RectTransform panelRT = menuPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.sizeDelta = Vector2.zero;
        }

        // Create title
        if (titleText == null)
        {
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(menuPanel.transform, false);

            titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "FINGER PIANO";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            RectTransform rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.75f);
            rt.anchorMax = new Vector2(0.5f, 0.9f);
            rt.sizeDelta = new Vector2(800, 150);
            rt.anchoredPosition = Vector2.zero;
        }

        // Create subtitle
        GameObject subtitleGO = new GameObject("SubtitleText");
        subtitleGO.transform.SetParent(menuPanel.transform, false);

        TextMeshProUGUI subtitleText = subtitleGO.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Select a Game Mode";
        subtitleText.fontSize = 36;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.8f, 0.8f, 0.8f);

        RectTransform subRT = subtitleGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.65f);
        subRT.anchorMax = new Vector2(0.5f, 0.75f);
        subRT.sizeDelta = new Vector2(600, 80);
        subRT.anchoredPosition = Vector2.zero;

        // Create Game 1 Button (Practice Piano)
        CreateGameButton(
            "Game1Button",
            new Vector2(0.3f, 0.35f),
            new Vector2(0.3f, 0.55f),
            "GAME 1\n\nPractice Piano",
            "Learn songs at your own pace.\nNo time pressure - take your time\nto press the correct finger.",
            new Color(0.2f, 0.5f, 0.2f),
            ref game1Button,
            ref game1Text,
            () => SelectGame(GameType.PracticePiano)
        );

        // Create Game 2 Button (Guitar Hero - Coming Soon)
        CreateGameButton(
            "Game2Button",
            new Vector2(0.7f, 0.35f),
            new Vector2(0.7f, 0.55f),
            "GAME 2\n\nRhythm Challenge",
            "Coming Soon!\nNotes scroll toward you.\nHit them with perfect timing.",
            new Color(0.3f, 0.3f, 0.5f),
            ref game2Button,
            ref game2Text,
            () => SelectGame(GameType.GuitarHero)
        );

        // Disable Game 2 for now
        if (game2Button != null)
        {
            game2Button.interactable = false;
            var colors = game2Button.colors;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            game2Button.colors = colors;
        }

        // Create instructions text
        GameObject instrGO = new GameObject("InstructionsText");
        instrGO.transform.SetParent(menuPanel.transform, false);

        TextMeshProUGUI instrText = instrGO.AddComponent<TextMeshProUGUI>();
        instrText.text = "Position your hands above the Leap Motion sensor to begin";
        instrText.fontSize = 28;
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.color = new Color(0.7f, 0.7f, 0.7f);

        RectTransform instrRT = instrGO.GetComponent<RectTransform>();
        instrRT.anchorMin = new Vector2(0.5f, 0.1f);
        instrRT.anchorMax = new Vector2(0.5f, 0.2f);
        instrRT.sizeDelta = new Vector2(800, 80);
        instrRT.anchoredPosition = Vector2.zero;
    }

    private void CreateGameButton(string name, Vector2 anchorPos, Vector2 anchorPos2, string title, string description, Color bgColor, ref Button button, ref TextMeshProUGUI text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(menuPanel.transform, false);

        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = bgColor;

        button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var colors = button.colors;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor = bgColor * 0.8f;
        button.colors = colors;

        button.onClick.AddListener(onClick);

        RectTransform buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(anchorPos.x - 0.15f, anchorPos.y - 0.15f);
        buttonRT.anchorMax = new Vector2(anchorPos.x + 0.15f, anchorPos2.y + 0.05f);
        buttonRT.sizeDelta = Vector2.zero;

        // Title text
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = title;
        titleTMP.fontSize = 32;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;

        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.5f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.sizeDelta = Vector2.zero;
        titleRT.offsetMin = new Vector2(10, 0);
        titleRT.offsetMax = new Vector2(-10, -10);

        // Description text
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(buttonGO.transform, false);

        text = descGO.AddComponent<TextMeshProUGUI>();
        text.text = description;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.9f, 0.9f, 0.9f);

        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0);
        descRT.anchorMax = new Vector2(1, 0.5f);
        descRT.sizeDelta = Vector2.zero;
        descRT.offsetMin = new Vector2(10, 10);
        descRT.offsetMax = new Vector2(-10, 0);
    }

    public void ShowMenu()
    {
        menuActive = true;
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void HideMenu()
    {
        menuActive = false;
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void SelectGame(GameType gameType)
    {
        if (gameType == GameType.GuitarHero)
        {
            // Not implemented yet
            Debug.Log("Guitar Hero mode coming soon!");
            return;
        }

        selectedGame = gameType;
        Debug.Log($"Selected game: {gameType}");

        HideMenu();
        StartCoroutine(StartSelectedGame());
    }

    IEnumerator StartSelectedGame()
    {
        // Show calibration message
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowInstruction("CALIBRATION");
            GameUIManager.Instance.ShowCalibrationStatus("Position your hands flat above the sensor...");
        }

        yield return new WaitForSeconds(1f);

        // Start calibration
        if (fingerGame != null && !fingerGame.IsCalibrated)
        {
            fingerGame.StartCalibration();

            // Wait for calibration
            while (!fingerGame.IsCalibrated)
            {
                yield return null;
            }

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.HideCountdown();
                GameUIManager.Instance.HideProgress();
            }

            yield return new WaitForSeconds(1f);
        }

        // Start the selected game
        switch (selectedGame)
        {
            case GameType.PracticePiano:
                if (rhythmGameManagerPrefab != null)
                {
                    GameObject go = Instantiate(rhythmGameManagerPrefab);
                    // The RhythmGameManager will handle the rest
                }
                else
                {
                    Debug.LogError("RhythmGameManager prefab not assigned!");
                }
                break;

            case GameType.GuitarHero:
                // Future implementation
                Debug.Log("Guitar Hero mode not yet implemented");
                break;
        }
    }

    // Call this to return to menu
    public void ReturnToMenu()
    {
        // Stop any running games
        var rhythmManager = FindObjectOfType<RhythmGameManager>();
        if (rhythmManager != null)
        {
            Destroy(rhythmManager.gameObject);
        }

        var pianoManager = FindObjectOfType<PianoGameManager>();
        if (pianoManager != null)
        {
            Destroy(pianoManager.gameObject);
        }

        ShowMenu();
    }
}
