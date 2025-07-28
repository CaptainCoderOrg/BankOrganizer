using UnityEngine;
using UnityEngine.UI;

namespace BankOrganizer.UI
{
    public class UICanvas
    {
        private GameObject? _canvasObject;

        public GameObject? CanvasObject => _canvasObject;

        public void Create()
        {
            // Create Canvas GameObject
            _canvasObject = new GameObject("BankOrganizerCanvas");
            GameObject.DontDestroyOnLoad(_canvasObject);

            // Add Canvas component
            Canvas canvas = _canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order to appear on top

            // Add CanvasScaler for resolution independence
            CanvasScaler canvasScaler = _canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster for UI interactions
            _canvasObject.AddComponent<GraphicRaycaster>();
        }

        public void SetActive(bool active)
        {
            _canvasObject?.SetActive(active);
        }

        public void Destroy()
        {
            if (_canvasObject != null)
            {
                GameObject.Destroy(_canvasObject);
                _canvasObject = null;
            }
        }
    }
}