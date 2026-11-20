using System.Collections.Generic;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Basit çokgen üçgenleme (kulak kırpma, yalnız sunum). Halka yönü fark etmez; kapanış tekrarı
    /// olmadan verilir. Delikler bu katmanda çizilmez (bölge dolgusu üstüne komşu bölge çizilir).
    /// Dejenere girdi sonsuz döngüye girmez: kulak bulunamazsa kalan köşeler yelpaze olarak kapatılır.
    /// </summary>
    public static class PolygonTriangulator
    {
        public static void Triangulate(IReadOnlyList<Vector2> ring, int vertexOffset, List<int> triangles)
        {
            int n = ring.Count;
            if (n < 3)
            {
                return;
            }

            var index = new List<int>(n);
            bool ccw = SignedArea(ring) > 0f;
            for (int i = 0; i < n; i++)
            {
                index.Add(ccw ? i : n - 1 - i);
            }

            int guard = n * n;
            while (index.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < index.Count; i++)
                {
                    int a = index[(i + index.Count - 1) % index.Count];
                    int b = index[i];
                    int c = index[(i + 1) % index.Count];
                    if (!IsEar(ring, index, a, b, c))
                    {
                        continue;
                    }

                    triangles.Add(vertexOffset + a);
                    triangles.Add(vertexOffset + b);
                    triangles.Add(vertexOffset + c);
                    index.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    break;
                }
            }

            for (int i = 1; i + 1 < index.Count; i++)
            {
                triangles.Add(vertexOffset + index[0]);
                triangles.Add(vertexOffset + index[i]);
                triangles.Add(vertexOffset + index[i + 1]);
            }
        }

        public static float SignedArea(IReadOnlyList<Vector2> ring)
        {
            float area = 0f;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                area += (ring[j].x * ring[i].y) - (ring[i].x * ring[j].y);
            }

            return area * 0.5f;
        }

        private static bool IsEar(IReadOnlyList<Vector2> ring, List<int> index, int a, int b, int c)
        {
            Vector2 pa = ring[a];
            Vector2 pb = ring[b];
            Vector2 pc = ring[c];
            if (Cross(pb - pa, pc - pb) <= 0f)
            {
                return false;
            }

            for (int i = 0; i < index.Count; i++)
            {
                int k = index[i];
                if (k == a || k == b || k == c)
                {
                    continue;
                }

                if (Inside(ring[k], pa, pb, pc))
                {
                    return false;
                }
            }

            return true;
        }

        private static float Cross(Vector2 u, Vector2 v) => (u.x * v.y) - (u.y * v.x);

        private static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c) =>
            Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;
    }
}
