using UnityEngine;
using UnityEngine.UI;
using System;

namespace BankOrganizer.UI
{
    public class UICheckbox
    {
        private GameObject? _checkboxObject;
        private Button? _button;
        private Image? _checkmarkImage;
        private Text? _labelText;
        private bool _isChecked = false;

        public event Action<bool>? OnValueChanged;
        public bool IsChecked => _isChecked;

        public void Create(GameObject? parent, string labelText, bool initialValue = false)
        {
            if (parent == null) return;

            _isChecked = initialValue;

            // Create main checkbox container
            _checkboxObject = new GameObject("Checkbox");
            _checkboxObject.transform.SetParent(parent.transform, false);

            RectTransform checkboxRect = _checkboxObject.AddComponent<RectTransform>();
            checkboxRect.sizeDelta = new Vector2(200, 25);

            // Add horizontal layout group for checkbox and label
            HorizontalLayoutGroup layoutGroup = _checkboxObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 5f;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;

            // Create the custom checkbox button
            CreateCheckboxButton();

            // Create the label
            CreateLabel(labelText);

            // Add layout element to control sizing
            LayoutElement layoutElement = _checkboxObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 25;
            layoutElement.minHeight = 25;
        }

        private void CreateCheckboxButton()
        {
            if (_checkboxObject == null) return;

            GameObject buttonObj = new GameObject("CheckboxButton");
            buttonObj.transform.SetParent(_checkboxObject.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(20, 20);

            _button = buttonObj.AddComponent<Button>();

            // Create background
            Image backgroundImage = buttonObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            _button.targetGraphic = backgroundImage;

            // Create checkmark container
            GameObject checkmarkContainer = new GameObject("Checkmark");
            checkmarkContainer.transform.SetParent(buttonObj.transform, false);

            RectTransform checkmarkRect = checkmarkContainer.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(2, 2);
            checkmarkRect.offsetMax = new Vector2(-2, -2);

            // Create checkmark image
            _checkmarkImage = checkmarkContainer.AddComponent<Image>();
            _checkmarkImage.color = new Color(0f, 1f, 0f, 1f); // Green checkmark

            // Set initial visibility
            _checkmarkImage.gameObject.SetActive(_isChecked);

            // Add layout element
            LayoutElement buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
            buttonLayoutElement.preferredWidth = 20;
            buttonLayoutElement.preferredHeight = 20;
        }

        private void CreateLabel(string labelText)
        {
            if (_checkboxObject == null) return;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(_checkboxObject.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(175, 20);

            _labelText = labelObj.AddComponent<Text>();
            _labelText.text = labelText;
            _labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _labelText.fontSize = 12;
            _labelText.color = Color.white;
            _labelText.alignment = TextAnchor.MiddleLeft;

            // Add layout element
            LayoutElement labelLayoutElement = labelObj.AddComponent<LayoutElement>();
            labelLayoutElement.preferredWidth = 175;
            labelLayoutElement.preferredHeight = 20;
        }

        public void HandleClick()
        {
            // This method will be called from the main panel's Update loop
            if (_button != null && _button.targetGraphic != null)
            {
                // Check if mouse is over button and clicked
                Vector2 mousePosition = Input.mousePosition;
                RectTransform buttonRect = _button.GetComponent<RectTransform>();

                if (buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(buttonRect, mousePosition))
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        ToggleValue();
                    }
                }
            }
        }

        private void ToggleValue()
        {
            _isChecked = !_isChecked;

            // Update visual state
            if (_checkmarkImage != null)
            {
                _checkmarkImage.gameObject.SetActive(_isChecked);
            }

            // Trigger event
            OnValueChanged?.Invoke(_isChecked);
        }

        public void SetValue(bool value)
        {
            if (_isChecked != value)
            {
                _isChecked = value;

                // Update visual state
                if (_checkmarkImage != null)
                {
                    _checkmarkImage.gameObject.SetActive(_isChecked);
                }
            }
        }

        public void SetLabel(string text)
        {
            if (_labelText != null)
            {
                _labelText.text = text;
            }
        }

        public void Destroy()
        {
            if (_checkboxObject != null)
            {
                GameObject.Destroy(_checkboxObject);
                _checkboxObject = null;
            }
        }
    }
}