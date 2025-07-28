using MelonLoader;
using System.Reflection;
using System.Runtime.Loader;
using UnityEngine;
using UnityEngine.UI;

namespace BankOrganizer;

public class BankOrganizer : MelonMod
{
    public const string ModVersion = "0.0.0";

    private GameObject? _uiCanvas;
    private GameObject? _mainPanel;
    private bool _isUIVisible = false;

    public override void OnInitializeMelon()
    {
        CreateUI();
    }

    public override void OnGUI()
    {
        
    }

    public override void OnApplicationQuit()
    {
        
    }

    public override void OnUpdate()
    {
        // Check for Ctrl+K input
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleUI();
            }
        }
    }

    private void CreateUI()
    {
        try
        {
            // Create Canvas GameObject
            _uiCanvas = new GameObject("BankOrganizerCanvas");
            GameObject.DontDestroyOnLoad(_uiCanvas);

            // Add Canvas component
            Canvas canvas = _uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order to appear on top

            // Add CanvasScaler for resolution independence
            CanvasScaler canvasScaler = _uiCanvas.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster for UI interactions
            _uiCanvas.AddComponent<GraphicRaycaster>();

            // Create main panel
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(_uiCanvas.transform, false);

            // Add RectTransform and configure size/position
            RectTransform panelRect = _mainPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(600, 600);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero; // Center of screen

            // Add background image
            Image panelImage = _mainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Dark semi-transparent background

            // Add a border/outline effect
            Outline outline = _mainPanel.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);

            // Create title text
            CreateTitleText();

            // Start with UI hidden
            _uiCanvas.SetActive(false);

            MelonLogger.Msg("Bank Organizer UI created successfully");
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Failed to create UI: {ex.Message}");
        }
    }

    private void CreateTitleText()
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(_mainPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(580, 50);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -30); // 30 pixels from top

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Bank Organizer";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
    }

    private void ToggleUI()
    {
        if (_uiCanvas == null) return;

        _isUIVisible = !_isUIVisible;
        _uiCanvas.SetActive(_isUIVisible);

        MelonLogger.Msg($"Bank Organizer UI {(_isUIVisible ? "opened" : "closed")}");
    }

    public override void OnDeinitializeMelon()
    {
        if (_uiCanvas != null)
        {
            GameObject.Destroy(_uiCanvas);
        }
    }
}