using UnityEngine;
using UnityEngine.UI;

namespace BankOrganizer.UI
{
    public class UIMainPanel
    {
        private GameObject? _panelObject;
        private UITitleText? _titleText;
        private UIBankList? _bankList;
        private GameObject? _resizeHandle;

        // Drag functionality
        private bool _isDragging = false;
        private Vector2 _lastMousePosition;

        // Resize functionality
        private bool _isResizing = false;
        private Vector2 _minSize = new Vector2(400, 300); // Minimum panel size
        private Vector2 _maxSize = new Vector2(1200, 900); // Maximum panel size

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
            panelImage.raycastTarget = true; // Block raycasts to prevent camera interaction

            // Add a border/outline effect
            Outline outline = _panelObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);

            // Create resize handle
            CreateResizeHandle();

            // Create title text
            _titleText = new UITitleText();
            _titleText.Create(_panelObject);

            // Create bank list
            _bankList = new UIBankList();
            _bankList.Create(_panelObject);
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
            handleImage.raycastTarget = true; // Enable mouse interaction

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
                lineImage.raycastTarget = false; // Don't block input events
            }
        }

        public void RefreshContent()
        {
            _bankList?.RefreshList();
        }

        public void HandleDragging()
        {
            if (_panelObject == null) return;

            Vector2 mousePosition = Input.mousePosition;
            RectTransform panelRect = _panelObject.GetComponent<RectTransform>();
            if (panelRect == null) return;

            // Handle resize functionality first (higher priority)
            HandleResize(mousePosition, panelRect);

            // Only handle dragging if not resizing
            if (!_isResizing)
            {
                HandlePanelDrag(mousePosition, panelRect);
            }
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
            }

            // Stop resizing
            if (Input.GetMouseButtonUp(0))
            {
                _isResizing = false;
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
                }
            }

            // Continue dragging
            if (_isDragging && Input.GetMouseButton(0))
            {
                Vector2 mouseDelta = mousePosition - _lastMousePosition;
                panelRect.position += new Vector3(mouseDelta.x, mouseDelta.y, 0);
                _lastMousePosition = mousePosition;
            }

            // Stop dragging
            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
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

        public bool IsDragging => _isDragging;
        public bool IsResizing => _isResizing;

        public void Destroy()
        {
            _titleText?.Destroy();
            _bankList?.Destroy();

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