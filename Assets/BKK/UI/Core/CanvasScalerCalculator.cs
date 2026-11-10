using UnityEngine;
using UnityEngine.UI;

namespace BKK.UI
{
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class CanvasScalerCalculator : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasScaler canvasScaler;

        [SerializeField] private Vector2 defaultResolution = new Vector2(1920, 1080);
        
        private float match = 1;
        
        private void Start()
        {
            SetProperties();
            SetMatch();
        }

        private void Update()
        {
            if(Application.isPlaying) SetMatch();
        }

        private void OnValidate()
        {
            if(!Application.isPlaying) SetProperties();
        }

        private void SetProperties()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = defaultResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.referencePixelsPerUnit = 100;
        }

        public void SetMatch()
        {
            if (!canvasScaler) return;

            match = 1;

            var screenRatio = (float) Screen.width / (float) Screen.height;
            var scalerRatio = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;
        
            match = screenRatio > scalerRatio ? 1.0f : 0f;
         
            canvasScaler.matchWidthOrHeight = match;
        }
    }
}