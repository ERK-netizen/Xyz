using System.Collections.Generic;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Dünya haritasında ±180° dikişi (Adım 21): kamera görüşü dikişi aşınca verilen kök nesnelerin altındaki tüm ağlar
    /// (bölge dolgusu, sınırlar, şehir/birlik işaretleri, rota) bir tur (<see cref="MapProjection.WrapWidth"/>) kaydırılmış
    /// kopya olarak da çizilir; böylece Pasifik bölünmeden görünür. Yalnız çizimdir: ağlar ve oyun durumu değişmez, kopyalar
    /// dokunmaya katılmaz (dokunma noktası <see cref="MapProjection.Canonical"/> ile asıl kopyaya indirgenir).
    /// </summary>
    public sealed class MapWrapRenderer : MonoBehaviour
    {
        private readonly List<MeshFilter> _filters = new List<MeshFilter>(16);
        private readonly List<MeshFilter> _scratch = new List<MeshFilter>(16);
        private readonly List<Material> _materials = new List<Material>(4);
        private Camera _camera;
        private Transform[] _roots;

        public void Initialize(Camera mapCamera, params Transform[] roots)
        {
            _camera = mapCamera;
            _roots = roots;
        }

        private void LateUpdate()
        {
            if (_camera == null || _roots == null)
            {
                return;
            }

            float halfWidth = _camera.orthographicSize * _camera.aspect;
            float x = _camera.transform.position.x;
            float seam = MapProjection.WrapWidth * 0.5f;
            bool right = x + halfWidth > seam;
            bool left = x - halfWidth < -seam;
            if (!right && !left)
            {
                return;
            }

            _filters.Clear();
            for (int r = 0; r < _roots.Length; r++)
            {
                if (_roots[r] == null)
                {
                    continue;
                }

                _roots[r].GetComponentsInChildren(false, _scratch);
                _filters.AddRange(_scratch);
            }

            for (int i = 0; i < _filters.Count; i++)
            {
                MeshFilter filter = _filters[i];
                Mesh mesh = filter.sharedMesh;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null || !renderer.enabled || mesh.vertexCount == 0)
                {
                    continue;
                }

                renderer.GetSharedMaterials(_materials);
                Matrix4x4 local = renderer.localToWorldMatrix;
                if (right)
                {
                    Draw(mesh, Matrix4x4.Translate(new Vector3(MapProjection.WrapWidth, 0f, 0f)) * local, filter.gameObject.layer);
                }

                if (left)
                {
                    Draw(mesh, Matrix4x4.Translate(new Vector3(-MapProjection.WrapWidth, 0f, 0f)) * local, filter.gameObject.layer);
                }
            }
        }

        private void Draw(Mesh mesh, Matrix4x4 matrix, int layer)
        {
            int count = Mathf.Min(mesh.subMeshCount, _materials.Count);
            for (int s = 0; s < count; s++)
            {
                Graphics.DrawMesh(mesh, matrix, _materials[s], layer, _camera, s);
            }
        }
    }
}
