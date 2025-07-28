using UnityEngine;
using UnityEngine.UI;

namespace BankOrganizer.UI
{
    public class UIMainPanel
    {
        private GameObject? _panelObject;
        private UITitleText? _titleText;
        private UIBankList? _bankList;

        // Drag functionality
        private bool _isDragging = false;
        private Vector2 _lastMousePosition;

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
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero; // Center of screen

            // Add background image
            Image panelImage = _panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Dark semi-transparent background
            panelImage.raycastTarget = true; // Block raycasts to prevent camera interaction

            // Add a border/outline effect
            Outline outline = _panelObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);

            // Create title text
            _titleText = new UITitleText();
            _titleText.Create(_panelObject);

            // Create bank list
            _bankList = new UIBankList();
            _bankList.Create(_panelObject);
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

            // Check if we should start dragging
            if (Input.GetMouseButtonDown(0) && !_isDragging)
            {
                // Check if mouse is over the panel background (not scroll area)
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

            return true; // Mouse is over panel background, not scroll area
        }

        public bool IsDragging => _isDragging;

        public void Destroy()
        {
            _titleText?.Destroy();
            _bankList?.Destroy();

            if (_panelObject != null)
            {
                GameObject.Destroy(_panelObject);
                _panelObject = null;
            }
        }
    }
}