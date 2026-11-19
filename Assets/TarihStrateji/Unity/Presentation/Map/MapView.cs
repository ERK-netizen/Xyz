using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Parçalı (chunk) dünya haritası çizicisi. Harita <see cref="MapChunkBuilder.ChunkSize"/>'lık
    /// parçalara bölünür; her parça kendi ağını taşır ve Unity'nin görüş konisi ayıklaması dışarıda
    /// kalanı çizmez. Durum değişince hücre renkleri (en çok 4 Hz) yeniden hesaplanır, yalnız rengi
    /// değişen parçalar kirli işaretlenir; LOD değişince hepsi. Kirli parçalar karede en çok
    /// <see cref="MaxRebuildsPerFrame"/> tane, önce görünenler olmak üzere yeniden kurulur (ani
    /// duraksama yok). Parçalar kameraya en yakın harita kopyasına kaydırılır (±180° dikişi).
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        private const int MaxRebuildsPerFrame = 12;
        private const float ColorRefreshInterval = 0.25f;

        private IGameReadModel _model;
        private Camera _camera;
        private readonly MapChunkBuilder _builder = new MapChunkBuilder();
        private Color32[] _cellColors;
        private Mesh[] _meshes;
        private Transform[] _chunks;
        private bool[] _dirty;
        private int _chunksAcross;
        private int _chunksDown;
        private int _level;
        private float _worldWidth;
        private bool _stateDirty = true;
        private float _nextColorRefresh;

        public int Level => _level;

        public void Initialize(IGameReadModel model, Camera mapCamera, Material material)
        {
            _model = model;
            _camera = mapCamera;
            _worldWidth = HexLayout.WorldWidth(model.MapWidth);
            _chunksAcross = MapChunkBuilder.ChunksAcross(model.MapWidth);
            _chunksDown = MapChunkBuilder.ChunksDown(model.MapHeight);
            int count = _chunksAcross * _chunksDown;
            _cellColors = new Color32[model.MapWidth * model.MapHeight];
            _meshes = new Mesh[count];
            _chunks = new Transform[count];
            _dirty = new bool[count];
            _level = MapLod.LevelFor(mapCamera.orthographicSize, 0);

            for (int y = 0; y < _chunksDown; y++)
            {
                for (int x = 0; x < _chunksAcross; x++)
                {
                    int i = (y * _chunksAcross) + x;
                    var chunk = new GameObject("Parca_" + x + "_" + y);
                    chunk.transform.SetParent(transform, false);
                    var mesh = new Mesh { name = chunk.name };
                    mesh.MarkDynamic();
                    chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = material;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;
                    _meshes[i] = mesh;
                    _chunks[i] = chunk.transform;
                }
            }

            RefreshColors(true);
            _model.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(long version) => _stateDirty = true;

        private void LateUpdate()
        {
            if (_model == null)
            {
                return;
            }

            if (_stateDirty && Time.unscaledTime >= _nextColorRefresh)
            {
                _stateDirty = false;
                _nextColorRefresh = Time.unscaledTime + ColorRefreshInterval;
                RefreshColors(false);
            }

            int level = MapLod.LevelFor(_camera.orthographicSize, _level);
            if (level != _level)
            {
                _level = level;
                for (int i = 0; i < _dirty.Length; i++)
                {
                    _dirty[i] = true;
                }
            }

            PositionChunks();
            RebuildDirty();
        }

        private void RefreshColors(bool all)
        {
            int width = _model.MapWidth;
            int height = _model.MapHeight;
            for (int row = 0; row < height; row++)
            {
                int chunkRow = (row / MapChunkBuilder.ChunkSize) * _chunksAcross;
                for (int column = 0; column < width; column++)
                {
                    int index = (row * width) + column;
                    CellView cell = _model.GetCell(index);
                    Color32 color = MapPalette.Cell(cell, _model.IsCellVisible(index));
                    Color32 old = _cellColors[index];
                    if (all || old.r != color.r || old.g != color.g || old.b != color.b || old.a != color.a)
                    {
                        _cellColors[index] = color;
                        _dirty[chunkRow + (column / MapChunkBuilder.ChunkSize)] = true;
                    }
                }
            }
        }

        private void PositionChunks()
        {
            float cameraX = _camera.transform.position.x;
            float halfChunk = HexLayout.ColumnWidth * MapChunkBuilder.ChunkSize * 0.5f;
            for (int y = 0; y < _chunksDown; y++)
            {
                for (int x = 0; x < _chunksAcross; x++)
                {
                    Vector2 origin = MapChunkBuilder.ChunkOrigin(x, y);
                    float shift = HexLayout.WrapShift(origin.x + halfChunk, cameraX, _worldWidth);
                    _chunks[(y * _chunksAcross) + x].localPosition = new Vector3(origin.x + shift, origin.y, 0f);
                }
            }
        }

        private void RebuildDirty()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector3 cameraPosition = _camera.transform.position;
            float chunkWidth = HexLayout.ColumnWidth * MapChunkBuilder.ChunkSize;
            float chunkHeight = HexLayout.RowHeight * MapChunkBuilder.ChunkSize;
            int budget = MaxRebuildsPerFrame;

            // Önce görünen parçalar, sonra kalanlar (bütçe izin verdikçe).
            for (int pass = 0; pass < 2 && budget > 0; pass++)
            {
                for (int i = 0; i < _dirty.Length && budget > 0; i++)
                {
                    if (!_dirty[i])
                    {
                        continue;
                    }

                    Vector3 p = _chunks[i].localPosition;
                    bool visible = p.x - halfWidth - chunkWidth <= cameraPosition.x && cameraPosition.x <= p.x + chunkWidth + halfWidth
                        && p.y - chunkHeight - halfHeight <= cameraPosition.y && cameraPosition.y <= p.y + halfHeight + 1f;
                    if (pass == 0 && !visible)
                    {
                        continue;
                    }

                    _builder.Build(i % _chunksAcross, i / _chunksAcross, _model.MapWidth, _model.MapHeight, _level, _cellColors);
                    _builder.Apply(_meshes[i]);
                    _dirty[i] = false;
                    budget--;
                }
            }
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.StateChanged -= OnStateChanged;
            }

            if (_meshes == null)
            {
                return;
            }

            for (int i = 0; i < _meshes.Length; i++)
            {
                if (_meshes[i] != null)
                {
                    Destroy(_meshes[i]);
                }
            }
        }
    }
}
