using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using BankOrganizer.Models;

namespace BankOrganizer.UI
{
    public class BankOrganizerUI
    {
        private UICanvas? _canvas;
        private UIMainPanel? _mainPanel;
        private bool _isVisible = false;

        public void Initialize()
        {
            try
            {
                _canvas = new UICanvas();
                _canvas.Create();

                _mainPanel = new UIMainPanel();
                _mainPanel.Create(_canvas.CanvasObject);

                // Start with UI hidden
                SetVisible(false);

            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to create UI: {ex.Message}");
            }
        }

        public void HandleInput()
        {
            // Handle dragging
            _mainPanel?.HandleDragging();

            // Check for Ctrl+K input
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.K))
                {
                    ToggleUI();
                }
            }
        }

        public void ToggleUI()
        {
            _isVisible = !_isVisible;
            SetVisible(_isVisible);
        }


        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            _canvas?.SetActive(visible);
            
            // Control event listening based on visibility
            if (visible)
            {
                _mainPanel?.RefreshContent();
            }
            else
            {
                _mainPanel?.OnPanelHidden();
            }
        }

        public void Cleanup()
        {
            _mainPanel?.Destroy();
            _canvas?.Destroy();
        }
    }
}