using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Ülke haritası (ADR-011, altıgensiz): bölge çokgenleri bir kez üçgenlenir ve tek ağda çizilir; her
    /// bölge fiilî kontrolcünün rengini alır (işgalde sahibin rengiyle karışık, koyu). Üstte yalnız ülke
    /// sınırları ve kıyılar çizgi olarak çizilir (<see cref="CountryBorders"/>); idari bölge sınırları
    /// görünmez. Renkler durum değişince en çok 4 Hz yenilenir; sınırlar yalnız sahiplik değişince yeniden
    /// kurulur. Sınır kenarları zincirlere birleştirilir ve ayrıntı düzeyine göre (<see cref="MapLod"/>,
    /// 4 düzey, histerezisli) Douglas–Peucker ile sadeleştirilir; çizgi kalınlığı kamera yakınlığıyla ölçeklenir.
    /// </summary>
    public sealed class CountryMapView : MonoBehaviour
    {
        private const float ColorRefreshInterval = 0.25f;
        private const float BorderPixels = 2.5f;

        private readonly List<RegionView> _regions = new List<RegionView>(64);
        private readonly List<List<RegionRingView>> _rings = new List<List<RegionRingView>>(64);
        private readonly List<int> _firstVertex = new List<int>(64);
        private readonly List<int> _vertexCount = new List<int>(64);
        private readonly List<CountryBorders.Segment> _segments = new List<CountryBorders.Segment>(512);
        private readonly List<BorderChains.Chain> _chains = new List<BorderChains.Chain>(128);
        private readonly List<Vector2> _chainWorld = new List<Vector2>(256);
        private readonly List<Vector2> _chainSimple = new List<Vector2>(256);
        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<Color32> _colors = new List<Color32>(4096);
        private readonly List<int> _triangles = new List<int>(8192);

        private IGameReadModel _model;
        private Camera _camera;
        private Mesh _fill;
        private Mesh _borders;
        private Color32[] _fillColors;
        private string _ownerSignature = string.Empty;
        private float _builtLineWidth = -1f;
        private int _builtLevel = -1;
        private int _level = -1;
        private bool _stateDirty = true;
        private float _nextRefresh;

        /// <summary>Geçerli ayrıntı düzeyi (<see cref="MapLod"/>); etiket katmanı da okur.</summary>
        public int Level => _level < 0 ? 0 : _level;

        /// <summary>Bölgelerin dünya birimindeki sınırlayıcı kutusu (kamera sınırı için).</summary>
        public Rect Bounds { get; private set; }

        public void Initialize(IGameReadModel model, Camera mapCamera, Material material)
        {
            _model = model;
            _camera = mapCamera;
            _fill = CreateLayer("Ulkeler", material, 0, 0f);
            _borders = CreateLayer("Sinirlar", material, 5, -0.05f);
            BuildFill();
            RefreshColors();
            _model.StateChanged += OnStateChanged;
        }

        private Mesh CreateLayer(string name, Material material, int order, float z)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            host.transform.localPosition = new Vector3(0f, 0f, z);
            var mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            host.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = host.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = order;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return mesh;
        }

        private void OnStateChanged(long version) => _stateDirty = true;

        /// <summary>Değişmez geometri: her bölgenin dış halkaları üçgenlenir (bölge başına köşe aralığı saklanır).</summary>
        private void BuildFill()
        {
            _model.GetRegions(_regions);
            _vertices.Clear();
            _triangles.Clear();
            var ring2d = new List<Vector2>(128);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int r = 0; r < _regions.Count; r++)
            {
                var rings = new List<RegionRingView>();
                _model.GetRegionRings(_regions[r].Id, rings);
                _rings.Add(rings);
                _firstVertex.Add(_vertices.Count);
                for (int k = 0; k < rings.Count; k++)
                {
                    if (rings[k].IsHole)
                    {
                        continue;
                    }

                    ring2d.Clear();
                    int offset = _vertices.Count;
                    for (int i = 0; i < rings[k].Points.Length; i++)
                    {
                        Vector2 p = MapProjection.ToWorld(rings[k].Points[i]);
                        ring2d.Add(p);
                        _vertices.Add(p);
                        minX = Mathf.Min(minX, p.x);
                        minY = Mathf.Min(minY, p.y);
                        maxX = Mathf.Max(maxX, p.x);
                        maxY = Mathf.Max(maxY, p.y);
                    }

                    PolygonTriangulator.Triangulate(ring2d, offset, _triangles);
                }

                _vertexCount.Add(_vertices.Count - _firstVertex[r]);
            }

            Bounds = _regions.Count == 0 ? new Rect(-1f, -1f, 2f, 2f) : Rect.MinMaxRect(minX, minY, maxX, maxY);
            _fillColors = new Color32[_vertices.Count];
            _fill.Clear();
            _fill.indexFormat = _vertices.Count > 65_000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            _fill.SetVertices(_vertices);
            _fill.SetTriangles(_triangles, 0, true);
        }

        private void LateUpdate()
        {
            if (_model == null)
            {
                return;
            }

            if (_stateDirty && Time.unscaledTime >= _nextRefresh)
            {
                _stateDirty = false;
                _nextRefresh = Time.unscaledTime + ColorRefreshInterval;
                RefreshColors();
            }

            _level = MapLod.LevelFor(_camera.orthographicSize, _level);
            float lineWidth = LineWidth();
            if (!Mathf.Approximately(lineWidth, _builtLineWidth) || _level != _builtLevel)
            {
                BuildBorders(lineWidth, _level);
            }
        }

        private float LineWidth()
        {
            float worldPerPixel = (_camera.orthographicSize * 2f) / Mathf.Max(1, Screen.height);
            // Yakınlaştırma adımlarına yuvarlanır: her karede ağ kurulmaz.
            return Mathf.Pow(2f, Mathf.Round(Mathf.Log(worldPerPixel * BorderPixels, 2f) * 2f) / 2f);
        }

        private void RefreshColors()
        {
            _regions.Clear();
            _model.GetRegions(_regions);
            var signature = new System.Text.StringBuilder(_regions.Count * 4);
            for (int r = 0; r < _regions.Count && r < _firstVertex.Count; r++)
            {
                RegionView region = _regions[r];
                Color32 color = MapPalette.Region(region.Owner, region.Controller);
                for (int i = 0; i < _vertexCount[r]; i++)
                {
                    _fillColors[_firstVertex[r] + i] = color;
                }

                signature.Append(region.Owner.Value).Append(',');
            }

            _fill.colors32 = _fillColors;
            string owners = signature.ToString();
            if (!string.Equals(owners, _ownerSignature, System.StringComparison.Ordinal))
            {
                _ownerSignature = owners;
                CountryBorders.Compute(_regions, _rings, _segments);
                BorderChains.Build(_segments, _chains);
                _builtLineWidth = -1f;
            }
        }

        /// <summary>Sınır ve kıyı zincirleri düzeyin toleransıyla sadeleştirilip şerit olarak çizilir.</summary>
        private void BuildBorders(float width, int level)
        {
            _builtLineWidth = width;
            _builtLevel = level;
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            float tolerance = MapLod.SimplifyTolerance(level);
            for (int c = 0; c < _chains.Count; c++)
            {
                BorderChains.Chain chain = _chains[c];
                _chainWorld.Clear();
                for (int i = 0; i < chain.Points.Count; i++)
                {
                    _chainWorld.Add(MapProjection.ToWorld(chain.Points[i]));
                }

                PolylineSimplifier.Simplify(_chainWorld, tolerance, _chainSimple);
                float w = chain.IsCoast ? width * 0.6f : width;
                Color32 color = chain.IsCoast ? MapPalette.Coast : MapPalette.Border;
                for (int i = 0; i + 1 < _chainSimple.Count; i++)
                {
                    AddLine(_chainSimple[i], _chainSimple[i + 1], w, color);
                }
            }

            _borders.Clear();
            _borders.indexFormat = _vertices.Count > 65_000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            _borders.SetVertices(_vertices);
            _borders.SetColors(_colors);
            _borders.SetTriangles(_triangles, 0, true);
        }

        private void AddLine(Vector2 a, Vector2 b, float width, Color32 color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector2 n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
            Vector2 e = d.normalized * (width * 0.5f); // uçları uzatarak köşelerde boşluk kalmaz
            int first = _vertices.Count;
            _vertices.Add(a - e + n);
            _vertices.Add(b + e + n);
            _vertices.Add(b + e - n);
            _vertices.Add(a - e - n);
            for (int i = 0; i < 4; i++)
            {
                _colors.Add(color);
            }

            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
            _triangles.Add(first);
            _triangles.Add(first + 2);
            _triangles.Add(first + 3);
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.StateChanged -= OnStateChanged;
            }

            if (_fill != null)
            {
                Destroy(_fill);
            }

            if (_borders != null)
            {
                Destroy(_borders);
            }
        }
    }
}
