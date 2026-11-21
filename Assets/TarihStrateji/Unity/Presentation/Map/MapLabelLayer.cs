using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Harita yazıları (yalnız sunum): şehir adları ve birlik yığını sayıları, arayüz panelinin arkasındaki
    /// dokunmayı geçiren katmanda (<see cref="HudController.MapOverlay"/>) UI Toolkit etiketleri olarak çizilir.
    /// Şehir adı LOD düzeyinin en küçük gelişim seviyesini geçen ve ekranda olan şehirlere,
    /// <see cref="LabelPlacer"/> ile çakışmasız konur (öncelik: gelişim, sonra başkent, sonra oyuncunun kendi şehri). Aynı
    /// yerde birden çok görünür birlik varsa sayısı işaretin sağ üstüne rozet olarak yazılır (ağ en çok
    /// <see cref="MarkerLayer.MaxUnitsDrawnPerPlace"/> şekil çizer; rozet gerçek sayıyı söyler). Etiketler
    /// havuzdan gelir; kamera, işaret katmanının kurulduğu durum sürümü veya panel boyu değişmedikçe yeniden yerleştirilmez.
    /// </summary>
    public sealed class MapLabelLayer : MonoBehaviour
    {
        public const int CityFontSize = 26;
        public const int BadgeFontSize = 22;

        /// <summary>Genişlik tahmini: karakter başına yazı boyunun bu oranı (ölçüm yerine; kararlı ve ucuz).</summary>
        private const float CharWidthRatio = 0.56f;
        private const float LabelPadding = 8f;
        private const float MarkerRadiusPixels = 14f;

        private static readonly Color TextColor = new Color(0.97f, 0.97f, 0.94f, 1f);
        private static readonly Color OwnTextColor = new Color(1f, 0.88f, 0.45f, 1f);
        private static readonly Color OutlineColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        private static readonly Color BadgeColor = new Color(0.08f, 0.1f, 0.16f, 0.88f);

        private readonly LabelPlacer _placer = new LabelPlacer();
        private readonly List<LabelPlacer.Candidate> _candidates = new List<LabelPlacer.Candidate>(256);
        private readonly List<LabelPlacer.Placement> _placed = new List<LabelPlacer.Placement>(256);
        private readonly Dictionary<ulong, CityView> _cityById = new Dictionary<ulong, CityView>(256);
        private readonly Dictionary<GeoPoint, int> _stackCount = new Dictionary<GeoPoint, int>(256);
        private readonly List<GeoPoint> _stackPlaces = new List<GeoPoint>(256);
        private readonly List<Label> _cityLabels = new List<Label>(128);
        private readonly List<Label> _badges = new List<Label>(64);

        private IGameReadModel _model;
        private MarkerLayer _markers;
        private CountryMapView _countries;
        private Camera _camera;
        private Localizer _text;
        private VisualElement _overlay;

        private Vector3 _builtCameraPosition;
        private float _builtCameraSize = -1f;
        private long _builtVersion = -1;
        private int _builtLevel = -1;
        private Vector2 _builtPanelSize;

        public void Initialize(IGameReadModel model, MarkerLayer markers, CountryMapView countries, Camera mapCamera, Localizer text, VisualElement overlay)
        {
            _model = model;
            _markers = markers;
            _countries = countries;
            _camera = mapCamera;
            _text = text;
            _overlay = overlay;
        }

        private void LateUpdate()
        {
            if (_model == null || _overlay == null || _overlay.panel == null)
            {
                return;
            }

            Vector2 panelSize = new Vector2(_overlay.layout.width, _overlay.layout.height);
            if (float.IsNaN(panelSize.x) || panelSize.x <= 0f || panelSize.y <= 0f)
            {
                return;
            }

            int level = _countries.Level;
            Vector3 position = _camera.transform.position;
            if (_builtVersion == _markers.BuiltVersion && _builtLevel == level && position == _builtCameraPosition
                && Mathf.Approximately(_builtCameraSize, _camera.orthographicSize) && panelSize == _builtPanelSize)
            {
                return;
            }

            _builtVersion = _markers.BuiltVersion;
            _builtLevel = level;
            _builtCameraPosition = position;
            _builtCameraSize = _camera.orthographicSize;
            _builtPanelSize = panelSize;
            PlaceCityLabels(level, panelSize);
            PlaceBadges(panelSize);
        }

        /// <summary>Yazı genişliği tahmini (panel pikseli).</summary>
        public static float EstimateWidth(string text, int fontSize) => (text.Length * fontSize * CharWidthRatio) + LabelPadding;

        /// <summary>Etiket önceliği: gelişim seviyesi baskındır; aynı seviyede başkent, sonra oyuncunun kendi şehri önce.</summary>
        public static int Priority(CityView city, bool own) => (city.DevelopmentLevel * 4) + (city.IsCapital ? 2 : 0) + (own ? 1 : 0);

        private void PlaceCityLabels(int level, Vector2 panelSize)
        {
            int minDevelopment = MapLod.MinLabelDevelopment(level);
            List<CityView> cities = _markers.Cities;
            _candidates.Clear();
            _cityById.Clear();
            for (int i = 0; i < cities.Count; i++)
            {
                CityView city = cities[i];
                if ((city.DevelopmentLevel < minDevelopment && !city.IsCapital) || !ToPanel(city.Location, panelSize, out Vector2 anchor))
                {
                    continue;
                }

                string name = _text.Get(city.NameKey);
                bool own = city.Controller == _model.PlayerCountry;
                _cityById[city.Id.Value] = city;
                _candidates.Add(new LabelPlacer.Candidate(city.Id.Value, anchor, EstimateWidth(name, CityFontSize), CityFontSize + 6f,
                    MarkerRadiusPixels, Priority(city, own)));
            }

            _placer.Place(_candidates, panelSize.x, panelSize.y, _placed);
            for (int i = 0; i < _placed.Count; i++)
            {
                LabelPlacer.Placement box = _placed[i];
                CityView city = _cityById[box.Id];
                Label label = Pooled(_cityLabels, i, CityFontSize, false);
                label.text = _text.Get(city.NameKey);
                label.style.color = city.Controller == _model.PlayerCountry ? OwnTextColor : TextColor;
                label.style.left = box.Left;
                label.style.top = box.Top;
            }

            Hide(_cityLabels, _placed.Count);
        }

        private void PlaceBadges(Vector2 panelSize)
        {
            _stackCount.Clear();
            _stackPlaces.Clear();
            List<UnitView> units = _markers.VisibleUnits;
            for (int i = 0; i < units.Count; i++)
            {
                GeoPoint at = units[i].Location;
                if (_stackCount.TryGetValue(at, out int n))
                {
                    _stackCount[at] = n + 1;
                }
                else
                {
                    _stackCount.Add(at, 1);
                    _stackPlaces.Add(at);
                }
            }

            int shown = 0;
            for (int i = 0; i < _stackPlaces.Count; i++)
            {
                GeoPoint at = _stackPlaces[i];
                int count = _stackCount[at];
                if (count < 2 || !ToPanel(at, panelSize, out Vector2 anchor))
                {
                    continue;
                }

                Label badge = Pooled(_badges, shown, BadgeFontSize, true);
                badge.text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                badge.style.left = anchor.x + MarkerRadiusPixels;
                badge.style.top = anchor.y - MarkerRadiusPixels - BadgeFontSize;
                shown++;
            }

            Hide(_badges, shown);
        }

        /// <summary>Dünya noktası → panel pikseli (y aşağı); ekran dışındaysa false.</summary>
        private bool ToPanel(GeoPoint point, Vector2 panelSize, out Vector2 at)
        {
            Vector2 world = MapProjection.ToWorld(point);
            world.x = MapProjection.NearestCopy(world.x, _camera.transform.position.x);
            Vector2 panel = RuntimePanelUtils.CameraTransformWorldToPanel(_overlay.panel, new Vector3(world.x, world.y, 0f), _camera);
            Rect bounds = _overlay.worldBound;
            at = new Vector2(panel.x - bounds.x, panel.y - bounds.y);
            return at.x >= 0f && at.y >= 0f && at.x <= panelSize.x && at.y <= panelSize.y;
        }

        private Label Pooled(List<Label> pool, int index, int fontSize, bool badge)
        {
            while (pool.Count <= index)
            {
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.style.position = Position.Absolute;
                label.style.fontSize = fontSize;
                label.style.color = TextColor;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.unityTextOutlineWidth = 1.5f;
                label.style.unityTextOutlineColor = OutlineColor;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.paddingLeft = 0;
                label.style.paddingRight = 0;
                label.style.marginLeft = 0;
                label.style.marginTop = 0;
                if (badge)
                {
                    label.style.backgroundColor = BadgeColor;
                    label.style.paddingLeft = 8;
                    label.style.paddingRight = 8;
                    label.style.borderTopLeftRadius = 10;
                    label.style.borderTopRightRadius = 10;
                    label.style.borderBottomLeftRadius = 10;
                    label.style.borderBottomRightRadius = 10;
                }

                _overlay.Add(label);
                pool.Add(label);
            }

            Label result = pool[index];
            result.style.display = DisplayStyle.Flex;
            return result;
        }

        private static void Hide(List<Label> pool, int from)
        {
            for (int i = from; i < pool.Count; i++)
            {
                pool[i].style.display = DisplayStyle.None;
            }
        }
    }
}
