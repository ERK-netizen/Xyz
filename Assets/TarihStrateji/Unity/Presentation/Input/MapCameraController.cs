using System;
using TarihStrateji.Unity.Presentation.Map;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Input
{
    /// <summary>
    /// Ortografik harita kamerası: tek parmak/fare sürükleme kaydırır, iki parmak (ortası sabit
    /// kalarak) veya tekerlek yakınlaştırır, kısa dokunuş <see cref="Tapped"/> olayını dünya
    /// noktasıyla verir. Arayüz panelinin üstünde başlayan dokunuşlar haritaya geçmez
    /// (<see cref="IsOverUi"/>). Eski Input Manager kullanılır (ProjectSettings: activeInputHandler 0).
    /// </summary>
    public sealed class MapCameraController : MonoBehaviour
    {
        public const float MinOrthographicSize = 0.6f;
        public const float MaxOrthographicSize = 30f;

        private float _maxOrthographicSize = MaxOrthographicSize;

        private Camera _camera;
        private Rect _bounds;

        private bool _pointerActive;
        private bool _pointerBlocked;
        private bool _moved;
        private Vector2 _downPosition;
        private Vector2 _lastPosition;
        private float _downTime;
        private bool _pinching;
        private float _lastPinchDistance;

        /// <summary>Ekran noktası (piksel, alt-sol orijin) arayüz öğesinin üstünde mi.</summary>
        public Func<Vector2, bool> IsOverUi { get; set; }

        /// <summary>Dokunulan dünya noktası.</summary>
        public event Action<Vector2> Tapped;

        public Camera Camera => _camera;

        /// <summary><paramref name="worldBounds"/>: haritanın dünya birimindeki kutusu; kamera merkezi bunun içinde kalır.</summary>
        public void Initialize(Camera mapCamera, Rect worldBounds)
        {
            _camera = mapCamera;
            _bounds = worldBounds;
            // Dünya haritasında (Adım 21) tüm dünyaya uzaklaşılabilir; bölgesel haritalarda üst sınır değişmez.
            _maxOrthographicSize = Mathf.Max(MaxOrthographicSize, worldBounds.height * 0.55f);

            // Dokunuşlar ayrıca fare olayı üretmesin (çift dokunuş işlenmesini önler).
            UnityEngine.Input.simulateMouseWithTouches = false;
        }

        public void CenterOn(Vector2 world)
        {
            Vector3 p = _camera.transform.position;
            _camera.transform.position = new Vector3(world.x, world.y, p.z);
            ClampCamera();
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            if (UnityEngine.Input.touchSupported && UnityEngine.Input.touchCount > 0)
            {
                HandleTouches();
            }
            else
            {
                HandleMouse();
            }

            ClampCamera();
        }

        private void HandleTouches()
        {
            int count = UnityEngine.Input.touchCount;
            if (count >= 2)
            {
                Touch a = UnityEngine.Input.GetTouch(0);
                Touch b = UnityEngine.Input.GetTouch(1);
                float distance = (a.position - b.position).magnitude;
                Vector2 middle = (a.position + b.position) * 0.5f;
                if (!_pinching)
                {
                    _pinching = true;
                    _moved = true;
                    _lastPinchDistance = distance;
                    _lastPosition = middle;
                    return;
                }

                if (!_pointerBlocked)
                {
                    Pan(_lastPosition, middle);
                    ZoomAround(middle, GestureMath.PinchOrthographicSize(_camera.orthographicSize, _lastPinchDistance, distance, MinOrthographicSize, _maxOrthographicSize));
                }

                _lastPinchDistance = distance;
                _lastPosition = middle;
                return;
            }

            Touch touch = UnityEngine.Input.GetTouch(0);
            if (_pinching)
            {
                // İki parmaktan bire düşünce sıçramamak için referansı yenile.
                _pinching = false;
                _lastPosition = touch.position;
            }

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    PointerDown(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    PointerDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                    PointerUp(touch.position);
                    break;
                case TouchPhase.Canceled:
                    _pointerActive = false;
                    break;
            }
        }

        private void HandleMouse()
        {
            _pinching = false;
            Vector2 position = UnityEngine.Input.mousePosition;
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                PointerDown(position);
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                PointerDrag(position);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                PointerUp(position);
            }

            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && (IsOverUi == null || !IsOverUi(position)))
            {
                ZoomAround(position, GestureMath.ScrollOrthographicSize(_camera.orthographicSize, scroll, MinOrthographicSize, _maxOrthographicSize));
            }
        }

        private void PointerDown(Vector2 position)
        {
            _pointerActive = true;
            _pointerBlocked = IsOverUi != null && IsOverUi(position);
            _moved = false;
            _downPosition = position;
            _lastPosition = position;
            _downTime = Time.unscaledTime;
        }

        private void PointerDrag(Vector2 position)
        {
            if (!_pointerActive || _pointerBlocked)
            {
                return;
            }

            if ((position - _downPosition).magnitude > GestureMath.SlopPixels(Screen.dpi))
            {
                _moved = true;
            }

            if (_moved)
            {
                Pan(_lastPosition, position);
            }

            _lastPosition = position;
        }

        private void PointerUp(Vector2 position)
        {
            if (!_pointerActive)
            {
                return;
            }

            _pointerActive = false;
            if (_pointerBlocked)
            {
                return;
            }

            if (GestureMath.IsTap(_downPosition, position, Time.unscaledTime - _downTime, Screen.dpi, _moved))
            {
                Tapped?.Invoke(ScreenToWorld(position));
            }
        }

        private void Pan(Vector2 fromScreen, Vector2 toScreen)
        {
            Vector2 delta = ScreenToWorld(fromScreen) - ScreenToWorld(toScreen);
            _camera.transform.position += new Vector3(delta.x, delta.y, 0f);
        }

        private void ZoomAround(Vector2 screenPoint, float newSize)
        {
            Vector2 before = ScreenToWorld(screenPoint);
            _camera.orthographicSize = newSize;
            Vector2 after = ScreenToWorld(screenPoint);
            _camera.transform.position += new Vector3(before.x - after.x, before.y - after.y, 0f);
        }

        private Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            return new Vector2(world.x, world.y);
        }

        private void ClampCamera()
        {
            Vector3 p = _camera.transform.position;
            float x = Mathf.Clamp(p.x, _bounds.xMin, _bounds.xMax);
            float y = Mathf.Clamp(p.y, _bounds.yMin, _bounds.yMax);
            _camera.transform.position = new Vector3(x, y, p.z);
        }
    }
}
