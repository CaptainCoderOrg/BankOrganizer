using UnityEngine;
using UnityEngine.UI;

namespace BankOrganizer.UI
{
    public class UIMainPanel
    {
        private GameObject? _panelObject;
        private UITitleText? _titleText;
        private UIBankList? _bankList;

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