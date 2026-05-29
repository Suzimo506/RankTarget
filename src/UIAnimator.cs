using UnityEngine;
using System;

namespace Suzimo.MuseDashMods.RankTarget
{
    public class UIAnimator : MonoBehaviour
    {
        public UIAnimator(IntPtr ptr) : base(ptr) { }

        private const float AutoShowDelaySeconds = 2f;
        private const float ZoomSpeed = 2f;
        private const float Constants_SCORE_ZOOM_IN_Y = 530f;
        private const float Constants_SCORE_ZOOM_OUT_Y = 670f;
        
        private float _zoomProgress = 0f;
        private bool _isZooming = false;
        private bool _nativeZoomInCompleted = false;
        private float _autoShowEnableTime;
        private bool _allowNativeZoomFollow = false;
        
        private RectTransform? _rectTransform;
        private float _targetY;
        private float _startY;

        public Transform? OriginalScoreTransform;
        public UnityEngine.Font? CustomFont;
        private UnityEngine.UI.Text? _uiText;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _uiText = GetComponent<UnityEngine.UI.Text>();

            if (transform.parent != null)
            {
                OriginalScoreTransform = transform.parent.Find("Score");
            }

            if (_rectTransform != null)
            {
                _targetY = _rectTransform.anchoredPosition.y;
                _startY = _targetY - 200f;
                _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _startY);
            }
            
            _zoomProgress = 0f;
            _isZooming = false;
            _nativeZoomInCompleted = false;
            _allowNativeZoomFollow = false;
            
            // Info+ sets this immediately on init, not after native zoom completes!
            _autoShowEnableTime = Time.time + AutoShowDelaySeconds;
        }

        private float CurrentY => OriginalScoreTransform != null ? OriginalScoreTransform.localPosition.y : Constants_SCORE_ZOOM_OUT_Y;

        private void FixedUpdate()
        {
            if (_rectTransform == null) return;
            
            if (_uiText != null && CustomFont != null && _uiText.font != CustomFont)
            {
                _uiText.font = CustomFont;
            }

            if (!_nativeZoomInCompleted)
            {
                if (CurrentY <= Constants_SCORE_ZOOM_IN_Y + 2f)
                {
                    _nativeZoomInCompleted = true;
                }
            }

            if (!_allowNativeZoomFollow)
            {
                if (_nativeZoomInCompleted && Time.time >= _autoShowEnableTime)
                {
                    _allowNativeZoomFollow = true;
                    _isZooming = true;
                }
                else
                {
                    return;
                }
            }

            if (!_isZooming) return;

            _zoomProgress = Mathf.Min(_zoomProgress + Time.fixedDeltaTime * ZoomSpeed, 1f);
            
            // exactly from Info+
            float easedProgress = 1f - Mathf.Pow(1f - _zoomProgress, 3f);
            
            // Lerp from startY (hidden) up to targetY (normal)
            float currentY = Mathf.Lerp(_startY, _targetY, easedProgress);
            
            _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, currentY);

            if (_zoomProgress >= 1f)
            {
                _isZooming = false;
                _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, _targetY);
            }
        }
    }
}
