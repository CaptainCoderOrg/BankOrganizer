using UnityEngine;
using UnityEngine.UI;

namespace BankOrganizer.UI
{
    public class UITitleText
    {
        private GameObject? _titleObject;

        public void Create(GameObject? panelParent)
        {
            if (panelParent == null) return;

            _titleObject = new GameObject("TitleText");
            _titleObject.transform.SetParent(panelParent.transform, false);

            RectTransform titleRect = _titleObject.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(580, 50);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -30); // 30 pixels from top

            Text titleText = _titleObject.AddComponent<Text>();
            titleText.text = "Bank Organizer";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
        }

        public void Destroy()
        {
            if (_titleObject != null)
            {
                GameObject.Destroy(_titleObject);
                _titleObject = null;
            }
        }
    }
}