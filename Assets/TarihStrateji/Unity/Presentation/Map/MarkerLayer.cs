using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.UI;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Şehir, birlik ve seçim işaretleri: tek dinamik ağ (tek çizim çağrısı). Şehir kare (gelişim
    /// seviyesiyle büyür, kuşatmada kırmızı çerçeve), kara birliği eşkenar dörtgen, filo üçgen, hava kanadı
    /// ters üçgen; çerçeve rengi kendi (beyaz) / savaştaki düşman (kırmızı) / diğer (gri). Yalnız oyuncunun
    /// gördükleri çizilir (okuma modeli savaş sisini uygular). Konumlar gerçek koordinattır
    /// (<see cref="MapProjection"/>); işaret boyu ekranda sabit kalacak şekilde kamera yakınlığıyla ölçeklenir.
    /// </summary>
    public sealed class MarkerLayer : MonoBehaviour
    {
        private const int MaxUnitsDrawnPerPlace = 4;
        private const float ReferenceOrthographicSize = 6f;
        private static readonly Vector2[] UnitSlots = { new Vector2(-0.42f, -0.42f), new Vector2(0.42f, -0.42f), new Vector2(-0.42f, 0.42f), new Vector2(0.42f, 0.42f) };
        private static readonly Color32 TargetColor = new Color32(255, 140, 0, 255);

        private readonly List<Vector3> _vertices = new List<Vector3>(2048);
        private readonly List<Color32> _colors = new List<Color32>(2048);
        private readonly List<int> _triangles = new List<int>(4096);
        private readonly List<CityView> _cities = new List<CityView>(64);
        private readonly List<UnitView> _units = new List<UnitView>(256);
        private readonly Dictionary<GeoPoint, int> _unitsAtPlace = new Dictionary<GeoPoint, int>(256);

        private Mesh _mesh;
        private IGameReadModel _model;
        private SelectionModel _selection;
        private Camera _camera;
        private long _builtVersion = -1;
        private int _builtSelection = -1;
        private float _builtScale = -1f;

        public List<UnitView> VisibleUnits => _units;

        public List<CityView> Cities => _cities;

        /// <summary>Dokunma seçimi indeksi; görünür birlik ve şehirlerle birlikte yenilenir.</summary>
        public MapPicker Picker { get; } = new MapPicker();

        public void Initialize(IGameReadModel model, SelectionModel selection, Camera mapCamera, Material material)
        {
            _model = model;
            _selection = selection;
            _camera = mapCamera;
            _mesh = new Mesh { name = "Isaretler" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = 10;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        /// <summary>Görünür birlik listesini okuma modelinden tazeler (seçim uzlaştırması için de kullanılır).</summary>
        public void RefreshUnits()
        {
            _units.Clear();
            _model.GetVisibleUnits(_units);
            if (_cities.Count == 0)
            {
                _model.GetCities(_cities);
            }

            Picker.Rebuild(_units, _cities);
        }

        private void LateUpdate()
        {
            if (_model == null)
            {
                return;
            }

            float scale = Mathf.Pow(2f, Mathf.Round(Mathf.Log(_camera.orthographicSize / ReferenceOrthographicSize, 2f) * 2f) / 2f) * 0.12f;
            if (_builtVersion == _model.StateVersion && _builtSelection == _selection.Version && Mathf.Approximately(_builtScale, scale))
            {
                return;
            }

            if (_builtVersion != _model.StateVersion)
            {
                _cities.Clear();
                _model.GetCities(_cities);
                RefreshUnits();
            }

            _builtVersion = _model.StateVersion;
            _builtSelection = _selection.Version;
            _builtScale = scale;
            Rebuild(scale);
        }

        private void Rebuild(float k)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            _unitsAtPlace.Clear();

            for (int i = 0; i < _cities.Count; i++)
            {
                CityView city = _cities[i];
                Vector2 at = MapProjection.ToWorld(city.Location);
                float half = (0.16f + (0.03f * city.DevelopmentLevel)) * k * 4f;
                AddSquare(at, half + (0.08f * k * 4f), city.IsBesieged ? MapPalette.EnemyOutline : MapPalette.Outline);
                AddSquare(at, half, MapPalette.Country(city.Controller));
            }

            for (int i = 0; i < _units.Count; i++)
            {
                UnitView unit = _units[i];
                _unitsAtPlace.TryGetValue(unit.Location, out int slot);
                _unitsAtPlace[unit.Location] = slot + 1;
                if (slot >= MaxUnitsDrawnPerPlace)
                {
                    continue;
                }

                Vector2 at = MapProjection.ToWorld(unit.Location) + (UnitSlots[slot] * k * 4f);
                bool selected = unit.IsOwn && unit.Id == _selection.Unit && unit.Kind == UnitViewKind.Land;
                Color32 outline = unit.IsOwn ? MapPalette.Outline : _model.IsAtWarWith(unit.Owner) ? MapPalette.EnemyOutline : MapPalette.EstimateOutline;
                if (selected)
                {
                    AddShape(unit.Kind, at, 0.42f * k * 4f, MapPalette.Highlight);
                }

                AddShape(unit.Kind, at, 0.32f * k * 4f, outline);
                AddShape(unit.Kind, at, (unit.IsExact ? 0.24f : 0.18f) * k * 4f, MapPalette.Country(unit.Owner));
            }

            if (_selection.Focus.Kind != MapPickKind.None)
            {
                AddRing(MapProjection.ToWorld(_selection.Focus.Point), k * 5f, MapPalette.Highlight);
            }

            if (_selection.HasTarget && TryTargetPoint(_selection.Target, out Vector2 target))
            {
                AddRing(target, k * 5f, TargetColor);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0, true);
        }

        private bool TryTargetPoint(TargetRef target, out Vector2 at)
        {
            switch (target.Kind)
            {
                case TargetKind.Point:
                    at = MapProjection.ToWorld(target.Point);
                    return true;
                case TargetKind.City:
                    for (int i = 0; i < _cities.Count; i++)
                    {
                        if (_cities[i].Id.Value == target.EntityId)
                        {
                            at = MapProjection.ToWorld(_cities[i].Location);
                            return true;
                        }
                    }

                    break;
                case TargetKind.LandUnit:
                    for (int i = 0; i < _units.Count; i++)
                    {
                        if (_units[i].Id == target.EntityId && _units[i].Kind == UnitViewKind.Land)
                        {
                            at = MapProjection.ToWorld(_units[i].Location);
                            return true;
                        }
                    }

                    break;
            }

            at = default;
            return false;
        }

        private void AddShape(UnitViewKind kind, Vector2 at, float size, Color32 color)
        {
            switch (kind)
            {
                case UnitViewKind.Fleet:
                    AddTriangle(at + new Vector2(0f, size), at + new Vector2(size, -size * 0.8f), at + new Vector2(-size, -size * 0.8f), color);
                    break;
                case UnitViewKind.AirWing:
                    AddTriangle(at + new Vector2(-size, size * 0.8f), at + new Vector2(size, size * 0.8f), at + new Vector2(0f, -size), color);
                    break;
                default:
                    AddTriangle(at + new Vector2(0f, size), at + new Vector2(size, 0f), at + new Vector2(0f, -size), color);
                    AddTriangle(at + new Vector2(0f, size), at + new Vector2(0f, -size), at + new Vector2(-size, 0f), color);
                    break;
            }
        }

        private void AddSquare(Vector2 at, float half, Color32 color)
        {
            AddTriangle(at + new Vector2(-half, half), at + new Vector2(half, half), at + new Vector2(half, -half), color);
            AddTriangle(at + new Vector2(-half, half), at + new Vector2(half, -half), at + new Vector2(-half, -half), color);
        }

        private void AddRing(Vector2 center, float radius, Color32 color)
        {
            const int Sides = 16;
            for (int i = 0; i < Sides; i++)
            {
                float t0 = i * Mathf.PI * 2f / Sides;
                float t1 = (i + 1) * Mathf.PI * 2f / Sides;
                Vector2 a = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)) * radius;
                Vector2 b = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1)) * radius;
                Vector2 outerA = center + a;
                Vector2 outerB = center + b;
                Vector2 innerA = center + (a * 0.86f);
                Vector2 innerB = center + (b * 0.86f);
                AddTriangle(outerA, innerB, outerB, color);
                AddTriangle(outerA, innerA, innerB, color);
            }
        }

        private void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            int first = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
