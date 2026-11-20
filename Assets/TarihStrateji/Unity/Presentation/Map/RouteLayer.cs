using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.UI;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Emir önizlemesinin rotası (Adım 16): <see cref="OrderFlow.Path"/> noktalarından kalın çizgi ve düğüm
    /// noktaları (tek dinamik ağ, işaretlerin altında). Renk emir türü ve riske göre: yürüyüş açık mavi, saldırı
    /// riskine göre yeşil/sarı/turuncu/kırmızı; geçersiz emirde rota çizilmez. Çizgi kalınlığı ekranda sabit
    /// kalacak şekilde kamera yakınlığına göre, yakınlaştırma adımlarına yuvarlanarak kurulur.
    /// </summary>
    public sealed class RouteLayer : MonoBehaviour
    {
        private const float LinePixels = 7f;
        private const float DotPixels = 9f;

        private static readonly Color32 MoveColor = new Color32(120, 200, 255, 255);
        private static readonly Color32 Outline = new Color32(15, 20, 30, 255);
        private static readonly Color32[] RiskColors =
        {
            new Color32(240, 240, 235, 255), // risk yok
            new Color32(90, 200, 110, 255),  // düşük
            new Color32(240, 200, 60, 255),  // orta
            new Color32(230, 70, 50, 255),   // yüksek
        };

        private readonly List<Vector3> _vertices = new List<Vector3>(512);
        private readonly List<Color32> _colors = new List<Color32>(512);
        private readonly List<int> _triangles = new List<int>(1024);

        private OrderFlow _flow;
        private Camera _camera;
        private Mesh _mesh;
        private int _builtVersion = -1;
        private float _builtWidth = -1f;

        public void Initialize(OrderFlow flow, Camera mapCamera, Material material)
        {
            _flow = flow;
            _camera = mapCamera;
            _mesh = new Mesh { name = "Rota" };
            _mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = 9;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        /// <summary>Emir türü ve riske göre rota rengi (testlerde de kullanılır).</summary>
        public static Color32 ColorFor(bool attack, int risk)
        {
            if (!attack)
            {
                return MoveColor;
            }

            return RiskColors[risk < 0 ? 0 : risk >= RiskColors.Length ? RiskColors.Length - 1 : risk];
        }

        private void LateUpdate()
        {
            if (_flow == null)
            {
                return;
            }

            float worldPerPixel = (_camera.orthographicSize * 2f) / Mathf.Max(1, Screen.height);
            float width = Mathf.Pow(2f, Mathf.Round(Mathf.Log(worldPerPixel * LinePixels, 2f) * 2f) / 2f);
            if (_flow.Version == _builtVersion && Mathf.Approximately(width, _builtWidth))
            {
                return;
            }

            _builtVersion = _flow.Version;
            _builtWidth = width;
            Rebuild(width);
        }

        private void Rebuild(float width)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            IReadOnlyList<GeoPoint> path = _flow.Path;
            if (_flow.IsOpen && _flow.Preview.IsValid && path.Count >= 2)
            {
                Color32 color = ColorFor(_flow.IsAttack, _flow.Preview.Risk);
                float dot = width * (DotPixels / LinePixels);
                for (int pass = 0; pass < 2; pass++)
                {
                    float w = pass == 0 ? width * 1.6f : width;
                    Color32 c = pass == 0 ? Outline : color;
                    for (int i = 1; i < path.Count; i++)
                    {
                        AddLine(MapProjection.ToWorld(path[i - 1]), MapProjection.ToWorld(path[i]), w, c);
                    }

                    for (int i = 1; i < path.Count; i++)
                    {
                        float size = i == path.Count - 1 ? dot * 1.6f : dot;
                        AddSquare(MapProjection.ToWorld(path[i]), (pass == 0 ? size * 1.3f : size) * 0.5f, c);
                    }
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0, true);
        }

        private void AddLine(Vector2 a, Vector2 b, float width, Color32 color)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector2 n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
            Vector2 e = d.normalized * (width * 0.5f);
            AddQuad(a - e + n, b + e + n, b + e - n, a - e - n, color);
        }

        private void AddSquare(Vector2 at, float half, Color32 color) =>
            AddQuad(at + new Vector2(-half, half), at + new Vector2(half, half), at + new Vector2(half, -half), at + new Vector2(-half, -half), color);

        private void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            int first = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
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
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
