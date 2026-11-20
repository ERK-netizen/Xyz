using System.Collections.Generic;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Douglas–Peucker çizgi sadeleştirme (yalnız sunum, sınır LOD'u). Uç noktalar her zaman korunur; böylece
    /// komşu zincirler kavşaklarda kopmaz. Kapalı halka (ilk = son) için ilk nokta iki uç sayılır.
    /// Özyineleme yerine açık yığın kullanılır (uzun kıyılarda yığın taşması yok).
    /// </summary>
    public static class PolylineSimplifier
    {
        public static void Simplify(IReadOnlyList<Vector2> points, float tolerance, List<Vector2> into)
        {
            into.Clear();
            int n = points.Count;
            if (n <= 2 || tolerance <= 0f)
            {
                for (int i = 0; i < n; i++)
                {
                    into.Add(points[i]);
                }

                return;
            }

            var keep = new bool[n];
            keep[0] = true;
            keep[n - 1] = true;
            var stack = new Stack<(int, int)>();
            stack.Push((0, n - 1));
            float tolerance2 = tolerance * tolerance;
            while (stack.Count > 0)
            {
                (int first, int last) = stack.Pop();
                float worst = -1f;
                int worstIndex = -1;
                for (int i = first + 1; i < last; i++)
                {
                    float d = SegmentDistance2(points[i], points[first], points[last]);
                    if (d > worst)
                    {
                        worst = d;
                        worstIndex = i;
                    }
                }

                if (worstIndex >= 0 && worst > tolerance2)
                {
                    keep[worstIndex] = true;
                    stack.Push((first, worstIndex));
                    stack.Push((worstIndex, last));
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (keep[i])
                {
                    into.Add(points[i]);
                }
            }
        }

        /// <summary>Noktanın [a, b] doğru parçasına uzaklığının karesi.</summary>
        public static float SegmentDistance2(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float length2 = ab.sqrMagnitude;
            if (length2 <= 0f)
            {
                return (p - a).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / length2);
            return (p - (a + (ab * t))).sqrMagnitude;
        }
    }
}
