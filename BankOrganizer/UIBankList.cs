using UnityEngine;
using UnityEngine.UI;
using BankOrganizer.Models;
using MelonLoader;

namespace BankOrganizer.UI
{
    public class UIBankList
    {
        private GameObject? _scrollView;
        private GameObject? _itemListContent;

        public void Create(GameObject? panelParent)
        {
            if (panelParent == null) return;

            CreateScrollView(panelParent);
        }

        private void CreateScrollView(GameObject panelParent)
        {
            // Create scroll view container
            _scrollView = new GameObject("ScrollView");
            _scrollView.transform.SetParent(panelParent.transform, false);

            RectTransform scrollRect = _scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(10, 10); // 10px margin from edges
            scrollRect.offsetMax = new Vector2(-10, -70); // 10px margin, 70px from top for title

            // Add ScrollRect component
            ScrollRect scrollRectComponent = _scrollView.AddComponent<ScrollRect>();
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;

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
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.spacing = 2f;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);

            // Connect scroll view components
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.content = contentRect;
        }

        public void RefreshList()
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

                // Create header showing summary
                CreateSummaryHeader(bankResult.Entries.Count, bankResult.TotalSlots);

                // Create UI elements for each bank entry - use for loop instead of foreach
                for (int i = 0; i < bankResult.Entries.Count; i++)
                {
                    CreateItemListEntry(bankResult.Entries[i]);
                }

                MelonLogger.Msg($"Refreshed bank list with {bankResult.Entries.Count} unique items using {bankResult.TotalSlots} slots");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error refreshing bank list: {ex.Message}");
                MelonLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private void CreateSummaryHeader(int uniqueItems, int totalSlots)
        {
            GameObject headerEntry = new GameObject("SummaryHeader");
            headerEntry.transform.SetParent(_itemListContent.transform, false);

            // Add background with different color
            Image headerBackground = headerEntry.AddComponent<Image>();
            headerBackground.color = new Color(0.1f, 0.3f, 0.5f, 0.7f); // Blue tint

            // Create text for summary
            GameObject textObj = new GameObject("HeaderText");
            textObj.transform.SetParent(headerEntry.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 2);
            textRect.offsetMax = new Vector2(-5, -2);

            Text headerText = textObj.AddComponent<Text>();
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 12;
            headerText.color = Color.cyan;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.text = $"{uniqueItems} Unique Items • {totalSlots} Slots Used";

            // Add layout element
            LayoutElement layoutElement = headerEntry.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 25;
            layoutElement.minHeight = 25;
        }

        private void CreateItemListEntry(BankEntry entry)
        {
            GameObject itemEntry = new GameObject($"Item_{entry.ItemId}");
            itemEntry.transform.SetParent(_itemListContent.transform, false);

            // Add background
            Image entryBackground = itemEntry.AddComponent<Image>();
            entryBackground.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // Create text for item info
            GameObject textObj = new GameObject("ItemText");
            textObj.transform.SetParent(itemEntry.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 2);
            textRect.offsetMax = new Vector2(-5, -2);

            Text itemText = textObj.AddComponent<Text>();
            itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemText.fontSize = 14;
            itemText.color = Color.white;
            itemText.alignment = TextAnchor.MiddleLeft;

            // Format text using the BankEntry data
            string stackInfo = entry.SlotCount > 1 ? $" ({entry.SlotCount} stacks)" : "";
            itemText.text = $"{entry.ItemName} - Total: {entry.TotalQuantity}{stackInfo}";

            // Add layout element
            LayoutElement layoutElement = itemEntry.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 25;
            layoutElement.minHeight = 25;
        }

        public void Destroy()
        {
            if (_scrollView != null)
            {
                GameObject.Destroy(_scrollView);
                _scrollView = null;
            }
        }
    }
}