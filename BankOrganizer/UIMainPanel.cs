using UnityEngine;
using UnityEngine.UI;
using BankOrganizer.Camera;

namespace BankOrganizer.UI
{
    public class UIMainPanel
    {
        private GameObject? _panelObject;
        private UITitleText? _titleText;
        private UIBankList? _bankList;
        private GameObject? _resizeHandle;
        private GameObject? _controlsContainer;
        private UICheckbox? _blockCameraCheckbox;

        // Drag functionality
        private bool _isDragging = false;
        private Vector2 _lastMousePosition;

        // Resize functionality
        private bool _isResizing = false;
        private Vector2 _minSize = new Vector2(400, 300); // Minimum panel size
        private Vector2 _maxSize = new Vector2(1200, 900); // Maximum panel size

        // Auto camera blocking for interactions
        private bool _autoBlockingEnabled = false;
        private float _autoBlockingTimer = 0f;
        private const float AUTO_BLOCKING_DURATION = 0.5f; // How long to keep blocking after interaction stops

        public GameObject? PanelObject => _panelObject;

        public void Create(GameObject? canvasParent)
        {
            if (canvasParent == null) return;

            // Create main panel
            _panelObject = new GameObject("MainPanel");
            _panelObject.transform.SetParent(canvasParent.transform, false);

            // Add RectTransform and configure size/position
            RectTransform panelRect = _panelObject.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(600, 600);
            panelRect.anchorMin = new Vector2(0f, 1f); // Top-left anchor
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f); // Top-left pivot
            // Position it somewhat centered on canvas (accounting for top-left anchoring)
            // Using canvas reference resolution (1920x1080) instead of screen resolution
            panelRect.anchoredPosition = new Vector2(1920 * 0.5f - 300, -1080 * 0.5f + 300);

            // Add background image
            Image panelImage = _panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Dark semi-transparent background

            // Add a border/outline effect
            Outline outline = _panelObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);

            // Create resize handle
            CreateResizeHandle();

            // Create title text
            _titleText = new UITitleText();
            _titleText.Create(_panelObject);

            // Create controls container
            CreateControlsContainer();

            // Create bank list
            _bankList = new UIBankList();
            _bankList.Create(_panelObject);
        }

        private void OnBlockCameraToggled(bool isBlocked)
        {
            MelonLoader.MelonLogger.Msg($"Manual camera blocking toggled: {(isBlocked ? "ON" : "OFF")}");

            if (isBlocked)
            {
                // Enable camera blocking manually
                CameraBlocker.EnableBlocking();
            }
            else
            {
                // Only disable if auto-blocking isn't active
                if (!_autoBlockingEnabled)
                {
                    CameraBlocker.DisableBlocking();
                }
            }

            // Log the current blocking status
            MelonLoader.MelonLogger.Msg(CameraBlocker.GetBlockingStatus());
        }

        private void EnableCameraBlocking()
        {
            // TODO: Implement camera blocking - disable zoom and rotation
            MelonLoader.MelonLogger.Msg("Camera blocking enabled - zoom and rotation disabled");
        }

        private void DisableCameraBlocking()
        {
            // TODO: Implement camera unblocking - enable zoom and rotation
            MelonLoader.MelonLogger.Msg("Camera blocking disabled - zoom and rotation enabled");
        }

        private void CreateResizeHandle()
        {
            if (_panelObject == null) return;

            // Create resize handle in bottom-right corner
            _resizeHandle = new GameObject("ResizeHandle");
            _resizeHandle.transform.SetParent(_panelObject.transform, false);

            RectTransform handleRect = _resizeHandle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(1f, 0f); // Bottom-right anchor
            handleRect.anchorMax = new Vector2(1f, 0f);
            handleRect.sizeDelta = new Vector2(20, 20); // 20x20 pixel handle
            handleRect.anchoredPosition = new Vector2(0, 0); // Position at bottom-right corner

            // Add visual indicator for the resize handle
            Image handleImage = _resizeHandle.AddComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f); // Light gray, semi-transparent

            // Create diagonal lines to indicate resize functionality
            CreateResizeLines();
        }

        private void CreateResizeLines()
        {
            if (_resizeHandle == null) return;

            // Create 3 diagonal lines to show resize handle
            for (int i = 0; i < 3; i++)
            {
                GameObject line = new GameObject($"ResizeLine{i}");
                line.transform.SetParent(_resizeHandle.transform, false);

                RectTransform lineRect = line.AddComponent<RectTransform>();
                lineRect.sizeDelta = new Vector2(2, 15 - (i * 2)); // Varying lengths
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.anchoredPosition = new Vector2(-6 + (i * 3), 6 - (i * 3)); // Diagonal positioning
                lineRect.rotation = Quaternion.Euler(0, 0, 45); // 45-degree rotation

                Image lineImage = line.AddComponent<Image>();
                lineImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Dark gray lines
            }
        }

        private void CreateControlsContainer()
        {
            if (_panelObject == null) return;

            // Create controls container between title and bank list
            _controlsContainer = new GameObject("ControlsContainer");
            _controlsContainer.transform.SetParent(_panelObject.transform, false);

            RectTransform controlsRect = _controlsContainer.AddComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0f, 1f);
            controlsRect.anchorMax = new Vector2(1f, 1f);
            controlsRect.sizeDelta = new Vector2(-20, 35); // 20px margin on sides, 35px height
            controlsRect.anchoredPosition = new Vector2(0, -60); // Position below title (which is at -30)

            // Add background
            Image controlsBackground = _controlsContainer.AddComponent<Image>();
            controlsBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.8f); // Slightly darker than main panel

            // Add vertical layout group for controls
            VerticalLayoutGroup layoutGroup = _controlsContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.padding = new RectOffset(10, 10, 5, 5);
            layoutGroup.spacing = 5f;

            // Create camera blocking checkbox
            _blockCameraCheckbox = new UICheckbox();
            _blockCameraCheckbox.Create(_controlsContainer, "Block Camera Movement", false);
            _blockCameraCheckbox.OnValueChanged += OnBlockCameraToggled;
        }

        public void RefreshContent()
        {
            _bankList?.RefreshList();
        }

        public void HandleDragging()
        {
            if (_panelObject == null || !_panelObject.activeInHierarchy) return;

            Vector2 mousePosition = Input.mousePosition;
            RectTransform panelRect = _panelObject.GetComponent<RectTransform>();
            if (panelRect == null) return;

            // Handle checkbox clicks first
            _blockCameraCheckbox?.HandleClick();

            // Handle resize functionality (higher priority than dragging)
            HandleResize(mousePosition, panelRect);

            // Only handle dragging if not resizing
            if (!_isResizing)
            {
                HandlePanelDrag(mousePosition, panelRect);
            }

            // Handle scrolling detection
            HandleScrolling();

            // Update auto-blocking timer and state
            UpdateAutoBlocking();
        }

        private void HandleResize(Vector2 mousePosition, RectTransform panelRect)
        {
            if (_resizeHandle == null) return;

            RectTransform handleRect = _resizeHandle.GetComponent<RectTransform>();
            if (handleRect == null) return;

            // Check if we should start resizing
            if (Input.GetMouseButtonDown(0) && !_isResizing && !_isDragging)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(handleRect, mousePosition))
                {
                    _isResizing = true;
                    _lastMousePosition = mousePosition;
                    EnableAutoBlocking("resizing");
                }
            }

            // Continue resizing
            if (_isResizing && Input.GetMouseButton(0))
            {
                Vector2 mouseDelta = mousePosition - _lastMousePosition;

                // Calculate new size
                Vector2 currentSize = panelRect.sizeDelta;
                Vector2 newSize = currentSize + new Vector2(mouseDelta.x, -mouseDelta.y); // Negative Y because UI coordinates are inverted

                // Clamp to min/max size
                newSize.x = Mathf.Clamp(newSize.x, _minSize.x, _maxSize.x);
                newSize.y = Mathf.Clamp(newSize.y, _minSize.y, _maxSize.y);

                // Apply new size
                panelRect.sizeDelta = newSize;

                _lastMousePosition = mousePosition;
                RefreshAutoBlocking(); // Keep blocking active while resizing
            }

            // Stop resizing
            if (Input.GetMouseButtonUp(0))
            {
                if (_isResizing)
                {
                    _isResizing = false;
                    // Auto-blocking will timeout naturally
                }
            }
        }

        private void HandlePanelDrag(Vector2 mousePosition, RectTransform panelRect)
        {
            // Check if we should start dragging
            if (Input.GetMouseButtonDown(0) && !_isDragging)
            {
                // Check if mouse is over the panel background (not scroll area or resize handle)
                if (IsMouseOverPanelBackground(mousePosition))
                {
                    _isDragging = true;
                    _lastMousePosition = mousePosition;
                    EnableAutoBlocking("dragging");
                }
            }

            // Continue dragging
            if (_isDragging && Input.GetMouseButton(0))
            {
                Vector2 mouseDelta = mousePosition - _lastMousePosition;
                panelRect.position += new Vector3(mouseDelta.x, mouseDelta.y, 0);
                _lastMousePosition = mousePosition;
                RefreshAutoBlocking(); // Keep blocking active while dragging
            }

            // Stop dragging
            if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    // Auto-blocking will timeout naturally
                }
            }
        }

        private bool IsMouseOverPanelBackground(Vector2 mousePosition)
        {
            if (_panelObject == null) return false;

            RectTransform panelRect = _panelObject.GetComponent<RectTransform>();
            if (panelRect == null) return false;

            // Check if mouse is over the panel
            bool isOverPanel = RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePosition);
            if (!isOverPanel) return false;

            // Don't drag if over resize handle
            if (_resizeHandle != null)
            {
                RectTransform handleRect = _resizeHandle.GetComponent<RectTransform>();
                if (handleRect != null && RectTransformUtility.RectangleContainsScreenPoint(handleRect, mousePosition))
                {
                    return false;
                }
            }

            // Check if mouse is over the scroll area (which we don't want to drag from)
            GameObject? scrollView = _bankList?.GetScrollView();
            if (scrollView != null)
            {
                RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
                if (scrollRect != null && RectTransformUtility.RectangleContainsScreenPoint(scrollRect, mousePosition))
                {
                    return false; // Don't drag if over scroll area
                }
            }

            return true; // Mouse is over panel background, not scroll area or resize handle
        }

        private void HandleScrolling()
        {
            // Only handle scrolling if panel is visible and active
            if (_panelObject == null || !_panelObject.activeInHierarchy) return;

            // Check if mouse is over the scroll area and scrolling is happening
            if (_bankList?.GetScrollView() != null)
            {
                GameObject scrollView = _bankList.GetScrollView();
                RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
                Vector2 mousePosition = Input.mousePosition;

                if (scrollRect != null && RectTransformUtility.RectangleContainsScreenPoint(scrollRect, mousePosition))
                {
                    // Check for scroll wheel input
                    float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
                    if (Mathf.Abs(scrollDelta) > 0.01f) // Small threshold to avoid noise
                    {
                        EnableAutoBlocking("scrolling");
                    }
                }
            }
        }

        private void EnableAutoBlocking(string reason)
        {
            if (!_autoBlockingEnabled)
            {
                _autoBlockingEnabled = true;
                if (!CameraBlocker.IsBlocking)
                {
                    CameraBlocker.EnableBlocking();
                    MelonLoader.MelonLogger.Msg($"Auto camera blocking enabled for {reason}");
                }
            }
            _autoBlockingTimer = AUTO_BLOCKING_DURATION;
        }

        private void RefreshAutoBlocking()
        {
            if (_autoBlockingEnabled)
            {
                _autoBlockingTimer = AUTO_BLOCKING_DURATION;
            }
        }

        private void UpdateAutoBlocking()
        {
            if (_autoBlockingEnabled)
            {
                _autoBlockingTimer -= Time.deltaTime;

                if (_autoBlockingTimer <= 0f)
                {
                    _autoBlockingEnabled = false;

                    // Only disable camera blocking if the manual checkbox isn't checked
                    if (_blockCameraCheckbox != null && !_blockCameraCheckbox.IsChecked)
                    {
                        if (CameraBlocker.IsBlocking)
                        {
                            CameraBlocker.DisableBlocking();
                            MelonLoader.MelonLogger.Msg("Auto camera blocking disabled");
                        }
                    }
                }
            }
        }

        public bool IsDragging => _isDragging;
        public bool IsResizing => _isResizing;

        public void Destroy()
        {
            // Make sure to disable camera blocking when panel is destroyed
            if (CameraBlocker.IsBlocking)
            {
                CameraBlocker.DisableBlocking();
            }

            // Reset auto-blocking state
            _autoBlockingEnabled = false;
            _autoBlockingTimer = 0f;

            _titleText?.Destroy();
            _bankList?.Destroy();
            _blockCameraCheckbox?.Destroy();

            if (_controlsContainer != null)
            {
                GameObject.Destroy(_controlsContainer);
                _controlsContainer = null;
            }

            if (_resizeHandle != null)
            {
                GameObject.Destroy(_resizeHandle);
                _resizeHandle = null;
            }

            if (_panelObject != null)
            {
                GameObject.Destroy(_panelObject);
                _panelObject = null;
            }
        }
    }
}