using UnityEngine;
using UnityEngine.UI;
using BankOrganizer.Models;
using BankOrganizer.Hooks;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BankOrganizer.UI
{
    public class UIBankList
    {
        private GameObject? _scrollView;
        private GameObject? _itemListContent;
        private GameObject? _statusBar;
        private Text? _statusText;
        private bool _isListeningForChanges = false;

        public void Create(GameObject? panelParent)
        {
            if (panelParent == null) return;

            CreateScrollView(panelParent);
            CreateStatusBar(panelParent);
            
            // Don't subscribe to events immediately - wait until panel is visible
        }

        public void StartListeningForChanges()
        {
            if (!_isListeningForChanges)
            {
                BankContainerManager.Instance.OnBankDataChanged += OnBankDataChanged;
                _isListeningForChanges = true;
            }
        }

        public void StopListeningForChanges()
        {
            if (_isListeningForChanges)
            {
                BankContainerManager.Instance.OnBankDataChanged -= OnBankDataChanged;
                _isListeningForChanges = false;
            }
        }

        private void OnBankDataChanged()
        {
            RefreshList();
        }

        private void CreateScrollView(GameObject panelParent)
        {
            // Create scroll view container
            _scrollView = new GameObject("ScrollView");
            _scrollView.transform.SetParent(panelParent.transform, false);

            RectTransform scrollRect = _scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(10, 45); // 10px margin from edges, 45px from bottom for status bar with spacing
            scrollRect.offsetMax = new Vector2(-10, -150); // 10px margin, start right after controls (controls at -85 with 60px height = -145, plus 5px margin)

            // Add ScrollRect component
            ScrollRect scrollRectComponent = _scrollView.AddComponent<ScrollRect>();
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            scrollRectComponent.scrollSensitivity = 30f; // Increase scroll speed (default is usually around 10)
            scrollRectComponent.inertia = true; // Enable inertia for smooth scrolling
            scrollRectComponent.decelerationRate = 0.135f; // How quickly scrolling slows down

            // Add invisible image
            Image scrollViewBlocker = _scrollView.AddComponent<Image>();
            scrollViewBlocker.color = new Color(0, 0, 0, 0); // Completely transparent

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(_scrollView.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // Add mask to viewport
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.1f); // Very transparent background
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Create content container
            _itemListContent = new GameObject("Content");
            _itemListContent.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = _itemListContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, 0);
            contentRect.pivot = new Vector2(0.5f, 1f);

            // Add ContentSizeFitter to automatically adjust content height
            ContentSizeFitter contentSizeFitter = _itemListContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add VerticalLayoutGroup for automatic item positioning
            VerticalLayoutGroup layoutGroup = _itemListContent.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlHeight = true; // Changed to false for dynamic heights
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 2f;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);

            // Connect scroll view components
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.content = contentRect;
        }

        public void RefreshList(string searchText = "", HashSet<Il2Cpp.EquipSlotTypeFlag>? equipSlotFilters = null)
        {
            if (_itemListContent == null) return;

            try
            {
                // Clear existing items - use while loop instead of foreach
                while (_itemListContent.transform.childCount > 0)
                {
                    Transform child = _itemListContent.transform.GetChild(0);
                    child.SetParent(null);
                    GameObject.Destroy(child.gameObject);
                }

                // Get bank data using our new model
                var bankResult = BankEntry.BuildBankEntries();

                // Calculate total available slots and used slots
                int totalAvailableSlots = CalculateTotalAvailableSlots();
                int usedSlots = bankResult.TotalSlots;

                // Apply search filter if search text is provided
                var filteredEntries = ApplySearchFilter(bankResult.Entries, searchText);
                
                // Apply equipment slot filter if filters are active
                filteredEntries = ApplyEquipSlotFilter(filteredEntries, equipSlotFilters);

                // Determine if any filters are active
                bool hasActiveFilters = !string.IsNullOrWhiteSpace(searchText) || (equipSlotFilters != null && equipSlotFilters.Count > 0);

                // Update status bar with slot usage
                UpdateStatusBar(usedSlots, totalAvailableSlots);

                // Create UI elements for each filtered bank entry - use for loop instead of foreach
                for (int i = 0; i < filteredEntries.Count; i++)
                {
                    CreateItemListEntry(filteredEntries[i]);
                }

            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error refreshing bank list: {ex.Message}");
                MelonLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Calculate the total number of available slots across all bank containers
        /// </summary>
        private int CalculateTotalAvailableSlots()
        {
            try
            {
                int totalSlots = 0;
                var allContainers = BankContainerManager.Instance.GetAllContainers();
                
                foreach (var containerKvp in allContainers)
                {
                    var container = containerKvp.Value;
                    var slots = container.GetAllSlots();
                    totalSlots += slots.Count;
                }
                
                return totalSlots;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error calculating total available slots: {ex.Message}");
                return 0;
            }
        }

        private void CreateStatusBar(GameObject panelParent)
        {
            // Create status bar at bottom of panel
            _statusBar = new GameObject("StatusBar");
            _statusBar.transform.SetParent(panelParent.transform, false);

            RectTransform statusRect = _statusBar.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.sizeDelta = new Vector2(-20, 25); // 20px margin on sides, 25px height
            statusRect.anchoredPosition = new Vector2(0, 20f); // Position at bottom with more margin for spacing

            // Add transparent background (no visible background)
            Image statusBackground = _statusBar.AddComponent<Image>();
            statusBackground.color = new Color(0, 0, 0, 0); // Completely transparent

            // Create status text
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(_statusBar.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            _statusText = textObj.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 14;
            _statusText.color = Color.yellow;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.fontStyle = FontStyle.Bold;
            _statusText.text = "0 / 0 Slots";
        }

        private void UpdateStatusBar(int usedSlots, int totalAvailableSlots)
        {
            if (_statusText != null)
            {
                _statusText.text = $"{usedSlots} / {totalAvailableSlots} Slots";
            }
        }

        /// <summary>
        /// Apply search filter to the list of bank entries
        /// </summary>
        private List<BankEntry> ApplySearchFilter(List<BankEntry> entries, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return entries; // No filter, return all

            string[] searchTerms = searchText.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return entries.Where(entry =>
                MatchesSearchTerms(entry.ItemName, searchTerms)).ToList();
        }

        /// <summary>
        /// Check if an item name matches all search terms (case-insensitive)
        /// </summary>
        private static bool MatchesSearchTerms(string itemName, string[] searchTerms)
        {
            if (searchTerms.Length == 0) return true;
            
            string lowerItemName = itemName.ToLowerInvariant();
            
            foreach (string term in searchTerms)
            {
                if (!lowerItemName.Contains(term.ToLowerInvariant()))
                {
                    return false; // All terms must match
                }
            }
            return true;
        }

        /// <summary>
        /// Apply equipment slot filter to the list of bank entries
        /// </summary>
        private List<BankEntry> ApplyEquipSlotFilter(List<BankEntry> entries, HashSet<Il2Cpp.EquipSlotTypeFlag>? equipSlotFilters)
        {
            // If no filters are active, return all entries
            if (equipSlotFilters == null || equipSlotFilters.Count == 0)
                return entries;

            return entries.Where(entry => MatchesEquipSlotFilter(entry, equipSlotFilters)).ToList();
        }

        /// <summary>
        /// Check if a bank entry matches any of the selected equipment slot filters
        /// </summary>
        private static bool MatchesEquipSlotFilter(BankEntry entry, HashSet<Il2Cpp.EquipSlotTypeFlag> equipSlotFilters)
        {
            // Check each ItemDataReference in the entry
            foreach (var itemRef in entry.ItemReferences)
            {
                // Check if the item's AllowedLocations has any of the selected flags
                foreach (var selectedFlag in equipSlotFilters)
                {
                    // Use bitwise AND to check if the flag is set (since it's a [Flags] enum)
                    if ((itemRef.AllowedLocations & selectedFlag) == selectedFlag)
                    {
                        return true; // Item matches at least one selected filter
                    }
                }
            }
            
            return false; // Item doesn't match any selected filters
        }

        private void CreateItemListEntry(BankEntry entry)
        {
            // Create entry container
            GameObject itemEntry = new GameObject($"Item_{entry.ItemId}");
            itemEntry.transform.SetParent(_itemListContent.transform, false);

            // Add background
            Image entryBackground = itemEntry.AddComponent<Image>();
            entryBackground.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // Add ContentSizeFitter for dynamic height based on stack count
            ContentSizeFitter entrySizeFitter = itemEntry.AddComponent<ContentSizeFitter>();
            entrySizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add vertical layout group (name on top, stacks below)
            VerticalLayoutGroup verticalLayout = itemEntry.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childControlHeight = true;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.spacing = 5f;
            verticalLayout.padding = new RectOffset(10, 10, 5, 5);

            // Create item name container (top)
            CreateItemNameContainer(entry, itemEntry);

            // Create stacks container (bottom)
            CreateStacksContainer(entry, itemEntry);
        }

        private void CreateItemNameContainer(BankEntry entry, GameObject parent)
        {
            GameObject nameContainer = new GameObject("ItemName");
            nameContainer.transform.SetParent(parent.transform, false);

            // Add layout element for preferred height
            LayoutElement nameLayout = nameContainer.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 30f;
            nameLayout.minHeight = 20f;

            // Create item name text
            Text nameText = nameContainer.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            
            // Simple format: Item Name (Total Quantity)
            nameText.text = $"{entry.ItemName} ({entry.TotalQuantity})";
        }

        private void CreateStacksContainer(BankEntry entry, GameObject parent)
        {
            GameObject stacksContainer = new GameObject("StacksContainer");
            stacksContainer.transform.SetParent(parent.transform, false);

            // Add ContentSizeFitter for dynamic height
            ContentSizeFitter stacksSizeFitter = stacksContainer.AddComponent<ContentSizeFitter>();
            stacksSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add GridLayoutGroup for stack elements
            GridLayoutGroup stacksGrid = stacksContainer.AddComponent<GridLayoutGroup>();
            stacksGrid.cellSize = new Vector2(64, 32);
            stacksGrid.spacing = new Vector2(4, 4);
            stacksGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            
            // Calculate column count based on available width (more generous now)
            float availableWidth = 500f; // More space since we're using full width
            int columnCount = Mathf.Max(1, (int)(availableWidth / (64 + 4))); // 64 cell width + 4 spacing
            stacksGrid.constraintCount = columnCount;

            // Populate stack elements
            foreach (var itemRef in entry.ItemReferences)
            {
                if (itemRef.StackSize > 0)
                {
                    CreateStackElement(itemRef, stacksContainer);
                }
            }
        }

        private void CreateStackElement(ItemDataReference itemRef, GameObject parent)
        {
            GameObject stackElement = new GameObject($"Stack_{itemRef.StackSize}");
            stackElement.transform.SetParent(parent.transform, false);

            // Add background
            Image stackBackground = stackElement.AddComponent<Image>();
            stackBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            // Add Button component for click functionality
            Button stackButton = stackElement.AddComponent<Button>();
            stackButton.targetGraphic = stackBackground;
            stackButton.onClick.AddListener((System.Action)itemRef.OnClickBankStack);

            // Create icon container
            GameObject iconObj = new GameObject("StackIcon");
            iconObj.transform.SetParent(stackElement.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.6f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image stackIcon = iconObj.AddComponent<Image>();
            stackIcon.color = Color.white;

            // Try to set the sprite
            if (!TrySetSprite(stackIcon, itemRef.Sprite))
            {
                stackIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            // Create quantity text
            GameObject quantityObj = new GameObject("StackQuantity");
            quantityObj.transform.SetParent(stackElement.transform, false);

            RectTransform quantityRect = quantityObj.AddComponent<RectTransform>();
            quantityRect.anchorMin = new Vector2(0.6f, 0.1f);
            quantityRect.anchorMax = new Vector2(0.9f, 0.9f);
            quantityRect.offsetMin = Vector2.zero;
            quantityRect.offsetMax = Vector2.zero;

            Text quantityText = quantityObj.AddComponent<Text>();
            quantityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            quantityText.fontSize = 10;
            quantityText.color = Color.white;
            quantityText.alignment = TextAnchor.MiddleRight;
            quantityText.text = $"x{itemRef.StackSize}";
        }

        private bool TrySetSprite(Image imageComponent, UnityEngine.Sprite sprite)
        {
            if (sprite == null || sprite.WasCollected)
            {
                imageComponent.sprite = null;
                return false;
            }

            try
            {
                imageComponent.sprite = sprite;
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"Failed to set sprite: {ex.Message}");
                imageComponent.sprite = null;
                return false;
            }
        }

        public void Destroy()
        {
            // Stop listening for changes and unsubscribe from events
            StopListeningForChanges();
            
            if (_scrollView != null)
            {
                GameObject.Destroy(_scrollView);
                _scrollView = null;
            }
            
            if (_statusBar != null)
            {
                GameObject.Destroy(_statusBar);
                _statusBar = null;
            }
            
            _statusText = null;
        }

        public GameObject? GetScrollView()
        {
            return _scrollView;
        }
    }
}