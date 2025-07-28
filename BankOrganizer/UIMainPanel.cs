using System;
using System.Collections;
using System.Collections.Generic;
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

        // Search functionality
        private InputField? _searchInputField;
        private float _searchDebounceTimer = 0f;
        private string _pendingSearchText = "";
        private bool _searchPending = false;
        private const float SEARCH_DEBOUNCE_DELAY = 0.3f;

        // Filter functionality
        private GameObject? _filtersPanel;
        private Button? _filtersButton;
        private Dictionary<Il2Cpp.EquipSlotTypeFlag, Text> _filterToggleTexts = new Dictionary<Il2Cpp.EquipSlotTypeFlag, Text>();
        private HashSet<Il2Cpp.EquipSlotTypeFlag> _activeFilters = new HashSet<Il2Cpp.EquipSlotTypeFlag>();
        private bool _filtersPanelVisible = false;

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
            panelRect.sizeDelta = new Vector2(400, 600);
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

            // Add horizontal layout group for controls (search + filters button)
            HorizontalLayoutGroup layoutGroup = _controlsContainer.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.padding = new RectOffset(10, 10, 5, 5);
            layoutGroup.spacing = 5f;

            // Create search box
            CreateSearchBox();
            
            // Create filters button (but defer panel creation until after main panel is fully set up)
            CreateFiltersButton();
        }

        private void CreateSearchBox()
        {
            if (_controlsContainer == null) return;

            // Create search container
            GameObject searchContainer = new GameObject("SearchContainer");
            searchContainer.transform.SetParent(_controlsContainer.transform, false);

            RectTransform searchRect = searchContainer.AddComponent<RectTransform>();
            searchRect.sizeDelta = new Vector2(0, 25); // Height of 25px, width controlled by layout group

            // Add layout element to ensure proper sizing
            LayoutElement searchLayoutElement = searchContainer.AddComponent<LayoutElement>();
            searchLayoutElement.preferredHeight = 25;
            searchLayoutElement.flexibleWidth = 1; // Take most of the space

            // Add background for search box
            Image searchBackground = searchContainer.AddComponent<Image>();
            searchBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Darker than controls container

            // Create InputField
            GameObject inputFieldObject = new GameObject("SearchInputField");
            inputFieldObject.transform.SetParent(searchContainer.transform, false);

            RectTransform inputRect = inputFieldObject.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.sizeDelta = Vector2.zero;
            inputRect.anchoredPosition = Vector2.zero;

            _searchInputField = inputFieldObject.AddComponent<InputField>();

            // Create text component for InputField
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(inputFieldObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0); // Left padding
            textRect.offsetMax = new Vector2(-10, 0); // Right padding

            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = "";
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;

            // Create placeholder text
            GameObject placeholderObject = new GameObject("Placeholder");
            placeholderObject.transform.SetParent(inputFieldObject.transform, false);

            RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;
            placeholderRect.offsetMin = new Vector2(10, 0); // Left padding
            placeholderRect.offsetMax = new Vector2(-10, 0); // Right padding

            Text placeholderText = placeholderObject.AddComponent<Text>();
            placeholderText.text = "Search items...";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Light gray
            placeholderText.alignment = TextAnchor.MiddleLeft;

            // Configure InputField
            _searchInputField.textComponent = textComponent;
            _searchInputField.placeholder = placeholderText;
            _searchInputField.targetGraphic = searchBackground;

            // Add event listener for text changes
            _searchInputField.onValueChanged.AddListener(new System.Action<string>(OnSearchTextChanged));
        }

        private void CreateFiltersButton()
        {
            if (_controlsContainer == null) return;

            // Create filters button
            GameObject filtersButtonObject = new GameObject("FiltersButton");
            filtersButtonObject.transform.SetParent(_controlsContainer.transform, false);

            RectTransform buttonRect = filtersButtonObject.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(60, 25); // Fixed width button

            // Add layout element
            LayoutElement buttonLayoutElement = filtersButtonObject.AddComponent<LayoutElement>();
            buttonLayoutElement.preferredWidth = 60;
            buttonLayoutElement.preferredHeight = 25;

            // Add button component
            _filtersButton = filtersButtonObject.AddComponent<Button>();

            // Add background image
            Image buttonBackground = filtersButtonObject.AddComponent<Image>();
            buttonBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Create button text
            GameObject buttonTextObject = new GameObject("Text");
            buttonTextObject.transform.SetParent(filtersButtonObject.transform, false);

            RectTransform textRect = buttonTextObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            Text buttonText = buttonTextObject.AddComponent<Text>();
            buttonText.text = "Filters";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;

            // Set button target graphic
            _filtersButton.targetGraphic = buttonBackground;

            // Add click handler using System.Action implicit conversion for Il2Cpp
            System.Action clickAction = ToggleFiltersPanel;
            _filtersButton.onClick.AddListener(clickAction);
        }

        private void CreateFiltersPanel()
        {
            if (_panelObject == null || _filtersPanel != null) return; // Don't create if already exists

            // Create filters panel
            _filtersPanel = new GameObject("FiltersPanel");
            _filtersPanel.transform.SetParent(_panelObject.transform, false);

            RectTransform panelRect = _filtersPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(300, 400); // Fixed size panel
            panelRect.anchorMin = new Vector2(1f, 1f); // Top-right anchor
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0f, 1f); // Top-left pivot
            panelRect.anchoredPosition = new Vector2(10, 0); // Position to the right of main panel

            // Add background
            Image panelBackground = _filtersPanel.AddComponent<Image>();
            panelBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // Add border
            Outline panelOutline = _filtersPanel.AddComponent<Outline>();
            panelOutline.effectColor = Color.white;
            panelOutline.effectDistance = new Vector2(2, 2);

            // Create title
            CreateFiltersPanelTitle();

            // Create scroll view for filters
            CreateFiltersPanelContent();

            // Initially hide the panel
            _filtersPanel.SetActive(false);
        }

        private void CreateFiltersPanelTitle()
        {
            if (_filtersPanel == null) return;

            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(_filtersPanel.transform, false);

            RectTransform titleRect = titleObject.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(-20, 30);
            titleRect.anchoredPosition = new Vector2(0, -15);

            Text titleText = titleObject.AddComponent<Text>();
            titleText.text = "Equipment Slot Filters";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
        }

        private void CreateFiltersPanelContent()
        {
            if (_filtersPanel == null) return;

            // Create scroll view
            GameObject scrollViewObject = new GameObject("ScrollView");
            scrollViewObject.transform.SetParent(_filtersPanel.transform, false);

            RectTransform scrollRect = scrollViewObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -50); // Leave space for title

            ScrollRect scrollRectComponent = scrollViewObject.AddComponent<ScrollRect>();
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;

            // Create viewport
            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(scrollViewObject.transform, false);

            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0.1f);
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Create content
            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            // Add ContentSizeFitter and GridLayoutGroup
            ContentSizeFitter contentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GridLayoutGroup gridLayout = contentObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 25);
            gridLayout.spacing = new Vector2(5, 5);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2; // 2 columns
            gridLayout.padding = new RectOffset(5, 5, 5, 5);
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            // Configure scroll view
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.content = contentRect;

            // Create filter toggles
            CreateFilterToggles(contentObject);
        }

        private void CreateFilterToggles(GameObject parent)
        {
            // Get all EquipSlotTypeFlag enum values
            var enumValues = Enum.GetValues(typeof(Il2Cpp.EquipSlotTypeFlag));
            
            foreach (Il2Cpp.EquipSlotTypeFlag flag in enumValues)
            {
                // Skip "All" as it's not a specific equipment slot
                if (flag == Il2Cpp.EquipSlotTypeFlag.All) continue;

                CreateSingleToggle(flag, parent);
            }
        }

        private void CreateSingleToggle(Il2Cpp.EquipSlotTypeFlag equipSlotFlag, GameObject parent)
        {
            // Create toggle container
            GameObject toggleObject = new GameObject($"Toggle_{equipSlotFlag}");
            toggleObject.transform.SetParent(parent.transform, false);

            // Add RectTransform for proper UI layout
            RectTransform toggleRect = toggleObject.AddComponent<RectTransform>();

            // Add background
            Image toggleBackground = toggleObject.AddComponent<Image>();
            toggleBackground.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // Create text GameObject as child
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(toggleObject.transform, false);
            
            // Add RectTransform to text object
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            // Create text component on the text object
            Text toggleText = textObject.AddComponent<Text>();
            toggleText.text = equipSlotFlag.ToString();
            toggleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toggleText.fontSize = 11;
            toggleText.color = Color.black; // Start as inactive (black)
            toggleText.alignment = TextAnchor.MiddleCenter;

            // Store reference to the text component
            _filterToggleTexts[equipSlotFlag] = toggleText;

            // Add Button component for click handling
            Button toggleButton = toggleObject.AddComponent<Button>();
            toggleButton.targetGraphic = toggleBackground;

            // Add click handler
            CreateToggleClickHandler(toggleButton, equipSlotFlag);
        }

        private void CreateToggleClickHandler(Button button, Il2Cpp.EquipSlotTypeFlag equipSlotFlag)
        {
            // Create a System.Action and use implicit conversion for Il2Cpp
            System.Action action = () => OnFilterToggleClicked(equipSlotFlag);
            button.onClick.AddListener(action);
        }

        private void OnFilterToggleClicked(Il2Cpp.EquipSlotTypeFlag equipSlotFlag)
        {
            // Toggle the filter state
            if (_activeFilters.Contains(equipSlotFlag))
            {
                // Remove from active filters
                _activeFilters.Remove(equipSlotFlag);
                // Set text to black (inactive)
                if (_filterToggleTexts.TryGetValue(equipSlotFlag, out Text? text))
                {
                    text.color = Color.black;
                }
            }
            else
            {
                // Add to active filters
                _activeFilters.Add(equipSlotFlag);
                // Set text to green (active)
                if (_filterToggleTexts.TryGetValue(equipSlotFlag, out Text? text))
                {
                    text.color = Color.green;
                }
            }

            // TODO: Apply filter (will be implemented later)
            // For now, just refresh the list with current search text
            string currentSearchText = _searchInputField?.text ?? "";
            ExecuteSearch(currentSearchText);
        }

        private void ToggleFiltersPanel()
        {
            // Create the panel if it doesn't exist yet
            if (_filtersPanel == null)
            {
                CreateFiltersPanel();
            }
            
            if (_filtersPanel == null) return; // Still null, something went wrong

            _filtersPanelVisible = !_filtersPanelVisible;
            _filtersPanel.SetActive(_filtersPanelVisible);
        }

        private void OnSearchTextChanged(string searchText)
        {
            // Set up debounce timer
            _pendingSearchText = searchText;
            _searchDebounceTimer = SEARCH_DEBOUNCE_DELAY;
            _searchPending = true;
        }

        private void UpdateSearchDebounce()
        {
            if (_searchPending)
            {
                _searchDebounceTimer -= Time.deltaTime;
                if (_searchDebounceTimer <= 0f)
                {
                    ExecuteSearch(_pendingSearchText);
                    _searchPending = false;
                }
            }
        }

        private void ExecuteSearch(string searchText)
        {
            // Execute search by calling RefreshList with search text
            _bankList?.RefreshList(searchText);
        }

        public void RefreshContent()
        {
            // Start listening for changes when panel becomes visible
            _bankList?.StartListeningForChanges();
            _bankList?.RefreshList();
        }

        public void OnPanelHidden()
        {
            // Stop listening for changes when panel is hidden
            _bankList?.StopListeningForChanges();
        }

        public void HandleDragging()
        {
            if (_panelObject == null || !_panelObject.activeInHierarchy) return;

            Vector2 mousePosition = Input.mousePosition;
            RectTransform panelRect = _panelObject.GetComponent<RectTransform>();
            if (panelRect == null) return;


            // Handle resize functionality (higher priority than dragging)
            HandleResize(mousePosition, panelRect);

            // Only handle dragging if not resizing
            if (!_isResizing)
            {
                HandlePanelDrag(mousePosition, panelRect);
            }

            // Handle scrolling detection
            HandleScrolling();

            // Update search debounce timer
            UpdateSearchDebounce();

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

                    if (CameraBlocker.IsBlocking)
                    {
                        CameraBlocker.DisableBlocking();
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

            // Clean up search components
            _searchPending = false;
            _searchDebounceTimer = 0f;
            _pendingSearchText = "";
            _searchInputField = null;

            _titleText?.Destroy();
            _bankList?.Destroy();

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