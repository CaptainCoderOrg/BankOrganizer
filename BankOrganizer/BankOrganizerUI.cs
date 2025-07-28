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

                MelonLogger.Msg("Bank Organizer UI created successfully");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to create UI: {ex.Message}");
            }
        }

        public void HandleInput()
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

        public void ToggleUI()
        {
            _isVisible = !_isVisible;
            SetVisible(_isVisible);

            // Log bank information when opening the UI
            if (_isVisible)
            {
                LogBankInformation();
            }

            MelonLogger.Msg($"Bank Organizer UI {(_isVisible ? "opened" : "closed")}");
        }

        private void LogBankInformation()
        {
            try
            {
                var bankResult = BankEntry.BuildBankEntries();

                MelonLogger.Msg("=== BANK INVENTORY REPORT ===");
                MelonLogger.Msg($"Total Slots Occupied: {bankResult.TotalSlots}");
                MelonLogger.Msg($"Unique Items: {bankResult.Entries.Count}");
                MelonLogger.Msg("");

                if (bankResult.Entries.Count == 0)
                {
                    MelonLogger.Msg("No items found in bank.");
                    return;
                }

                foreach (var entry in bankResult.Entries)
                {
                    string stackInfo = entry.SlotCount > 1 ? $" ({entry.SlotCount} stacks)" : "";
                    MelonLogger.Msg($"{entry.ItemName}: {entry.TotalQuantity} total{stackInfo}");

                    // Show additional details if the item could be better organized
                    int wastedSlots = entry.GetWastedSlots();
                    if (wastedSlots > 0)
                    {
                        MelonLogger.Msg($"  -> Could save {wastedSlots} slot{(wastedSlots > 1 ? "s" : "")} with better stacking");
                    }
                }

                MelonLogger.Msg("=== END BANK REPORT ===");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error logging bank information: {ex.Message}");
            }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            _canvas?.SetActive(visible);
        }

        public void Cleanup()
        {
            _mainPanel?.Destroy();
            _canvas?.Destroy();
        }
    }
}