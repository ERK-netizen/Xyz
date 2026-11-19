using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.UI;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Şehir, birlik ve seçim işaretleri: tek dinamik ağ (tek çizim çağrısı). Şehir kare (kademeyle
    /// büyür, kuşatmada kırmızı çerçeve), kara birliği eşkenar dörtgen, filo üçgen, hava kanadı ters
    /// üçgen; çerçeve rengi kendi (beyaz) / savaştaki düşman (kırmızı) / diğer (gri). Yalnız oyuncunun
    /// gördükleri çizilir (okuma modeli savaş sisini uygular). İşaretler kameraya en yakın harita
    /// kopyasına yerleşir.
    /// </summary>
    public sealed class MarkerLayer : MonoBehaviour
    {
        private const int MaxUnitsDrawnPerCell = 4;
        private static readonly Vector2[] UnitSlots = { new Vector2(-0.42f, -0.42f), new Vector2(0.42f, -0.42f), new Vector2(-0.42f, 0.42f), new Vector2(0.42f, 0.42f) };
        private static readonly Color32 TargetColor = new Color32(255, 140, 0, 255);

        private readonly List<Vector3> _vertices = new List<Vector3>(2048);
        private readonly List<Color32> _colors = new List<Color32>(2048);
        private readonly List<int> _triangles = new List<int>(4096);
        private readonly List<CityView> _cities = new List<CityView>(64);
        private readonly List<UnitView> _units = new List<UnitView>(256);
        private readonly Dictionary<int, int> _unitsInCell = new Dictionary<int, int>(256);

        private Mesh _mesh;
        private IGameReadModel _model;
        private SelectionModel _selection;
        private Camera _camera;
        private long _builtVersion = -1;
        private int _builtSelection = -1;
        private int _builtBucket = int.MinValue;

        public List<UnitView> VisibleUnits => _units;

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
        }

        private void LateUpdate()
        {
            if (_model == null)
            {
                return;
            }

            float worldWidth = HexLayout.WorldWidth(_model.MapWidth);
            int bucket = Mathf.FloorToInt(_camera.transform.position.x / (worldWidth * 0.25f));
            if (_builtVersion == _model.StateVersion && _builtSelection == _selection.Version && _builtBucket == bucket)
            {
                return;
            }

            if (_builtVersion != _model.StateVersion)
            {
                RefreshUnits();
                _cities.Clear();
                _model.GetCities(_cities);
            }

            _builtVersion = _model.StateVersion;
            _builtSelection = _selection.Version;
            _builtBucket = bucket;
            Rebuild(_camera.transform.position.x, worldWidth);
        }

        private void Rebuild(float cameraX, float worldWidth)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            _unitsInCell.Clear();
            int width = _model.MapWidth;

            for (int i = 0; i < _cities.Count; i++)
            {
                CityView city = _cities[i];
                Vector2 at = Place(city.CellIndex, width, cameraX, worldWidth);
                float half = 0.2f + (0.06f * city.Tier);
                AddSquare(at, half + 0.08f, city.IsBesieged ? MapPalette.EnemyOutline : MapPalette.Outline);
                AddSquare(at, half, MapPalette.Country(city.Controller));
            }

            for (int i = 0; i < _units.Count; i++)
            {
                UnitView unit = _units[i];
                _unitsInCell.TryGetValue(unit.CellIndex, out int slot);
                _unitsInCell[unit.CellIndex] = slot + 1;
                if (slot >= MaxUnitsDrawnPerCell)
                {
                    continue;
                }

                Vector2 at = Place(unit.CellIndex, width, cameraX, worldWidth) + UnitSlots[slot];
                bool selected = unit.IsOwn && unit.Id == _selection.Unit && unit.Kind == UnitViewKind.Land;
                Color32 outline = unit.IsOwn ? MapPalette.Outline : _model.IsAtWarWith(unit.Owner) ? MapPalette.EnemyOutline : MapPalette.EstimateOutline;
                if (selected)
                {
                    AddShape(unit.Kind, at, 0.42f, MapPalette.Highlight);
                }

                AddShape(unit.Kind, at, 0.32f, outline);
                AddShape(unit.Kind, at, unit.IsExact ? 0.24f : 0.18f, MapPalette.Country(unit.Owner));
            }

            if (_selection.Cell >= 0)
            {
                AddRing(Place(_selection.Cell, width, cameraX, worldWidth), MapPalette.Highlight);
            }

            if (_selection.Target >= 0)
            {
                AddRing(Place(_selection.Target, width, cameraX, worldWidth), TargetColor);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0, true);
        }

        private static Vector2 Place(int cellIndex, int width, float cameraX, float worldWidth)
        {
            Vector2 at = HexLayout.CellCenterOfIndex(cellIndex, width);
            at.x += HexLayout.WrapShift(at.x, cameraX, worldWidth);
            return at;
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

        private void AddRing(Vector2 center, Color32 color)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 a = HexLayout.Corner(i);
                Vector2 b = HexLayout.Corner((i + 1) % 6);
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
