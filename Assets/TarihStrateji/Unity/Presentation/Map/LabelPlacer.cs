using System.Collections.Generic;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Şehir adı etiketlerinin çakışmasız yerleşimi (yalnız sunum). Açgözlü: adaylar önceliğe göre (yüksek önce,
    /// eşitlikte küçük kimlik) sıralanır; her etiket işaretin sağ, sol, üst ve alt yanına sırayla denenir ve
    /// yerleşmiş etiketlerle ya da herhangi bir şehir işaretiyle çakışmayan ilk konuma konur; hiçbiri olmazsa
    /// gizlenir. Koordinatlar panel pikselidir (y aşağı). Çakışma denetimi hücre boyu sabit bir ızgarayla
    /// yapılır; sonuç aynı girdi için her zaman aynıdır (sıralama kararlı, kimlik eşitlik bozucu).
    /// </summary>
    public sealed class LabelPlacer
    {
        /// <summary>Etiket ile işaret arasındaki boşluk (piksel).</summary>
        public const float Gap = 6f;

        private const float CellSize = 96f;

        public struct Candidate
        {
            public ulong Id;
            public Vector2 Anchor;
            public float Width;
            public float Height;
            public float MarkerRadius;
            public int Priority;

            public Candidate(ulong id, Vector2 anchor, float width, float height, float markerRadius, int priority)
            {
                Id = id;
                Anchor = anchor;
                Width = width;
                Height = height;
                MarkerRadius = markerRadius;
                Priority = priority;
            }
        }

        public struct Placement
        {
            public ulong Id;
            public float Left;
            public float Top;
            public float Width;
            public float Height;

            public Placement(ulong id, float left, float top, float width, float height)
            {
                Id = id;
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }

            public bool Overlaps(float left, float top, float width, float height) =>
                left < Left + Width && Left < left + width && top < Top + Height && Top < top + height;
        }

        private readonly List<Candidate> _order = new List<Candidate>(256);
        private readonly List<Placement> _boxes = new List<Placement>(512);
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>(512);
        private readonly Stack<List<int>> _pool = new Stack<List<int>>();

        /// <summary>
        /// Yerleşen etiketleri <paramref name="placed"/>'e (öncelik sırasıyla) yazar; ekran dışına taşan ya da
        /// yer bulamayan etiket yazılmaz. <paramref name="viewWidth"/>/<paramref name="viewHeight"/> panel boyudur.
        /// </summary>
        public void Place(IReadOnlyList<Candidate> candidates, float viewWidth, float viewHeight, List<Placement> placed)
        {
            placed.Clear();
            Reset();
            _order.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                _order.Add(candidates[i]);
            }

            _order.Sort(Compare);

            // Önce tüm işaretler engel olarak girer: bir etiket başka bir şehrin üstünü örtmez.
            for (int i = 0; i < _order.Count; i++)
            {
                Candidate c = _order[i];
                float r = c.MarkerRadius;
                Insert(new Placement(0UL, c.Anchor.x - r, c.Anchor.y - r, r * 2f, r * 2f), c.Id);
            }

            for (int i = 0; i < _order.Count; i++)
            {
                Candidate c = _order[i];
                for (int side = 0; side < 4; side++)
                {
                    Position(c, side, out float left, out float top);
                    if (left < 0f || top < 0f || left + c.Width > viewWidth || top + c.Height > viewHeight)
                    {
                        continue;
                    }

                    if (!Collides(left, top, c.Width, c.Height, c.Id))
                    {
                        var box = new Placement(c.Id, left, top, c.Width, c.Height);
                        Insert(box, ulong.MaxValue);
                        placed.Add(box);
                        break;
                    }
                }
            }
        }

        /// <summary>Kenar sırası: 0 sağ, 1 sol, 2 üst, 3 alt; dikeyde/yatayda işarete ortalanır.</summary>
        public static void Position(Candidate c, int side, out float left, out float top)
        {
            float offset = c.MarkerRadius + Gap;
            switch (side)
            {
                case 0:
                    left = c.Anchor.x + offset;
                    top = c.Anchor.y - (c.Height * 0.5f);
                    break;
                case 1:
                    left = c.Anchor.x - offset - c.Width;
                    top = c.Anchor.y - (c.Height * 0.5f);
                    break;
                case 2:
                    left = c.Anchor.x - (c.Width * 0.5f);
                    top = c.Anchor.y - offset - c.Height;
                    break;
                default:
                    left = c.Anchor.x - (c.Width * 0.5f);
                    top = c.Anchor.y + offset;
                    break;
            }
        }

        private static int Compare(Candidate a, Candidate b)
        {
            if (a.Priority != b.Priority)
            {
                return b.Priority.CompareTo(a.Priority);
            }

            return a.Id.CompareTo(b.Id);
        }

        /// <summary>Kendi işareti (aynı kimlik) etiketine engel değildir; yerleşmiş etiketler (MaxValue) herkese engeldir.</summary>
        private bool Collides(float left, float top, float width, float height, ulong self)
        {
            int x0 = Mathf.FloorToInt(left / CellSize);
            int x1 = Mathf.FloorToInt((left + width) / CellSize);
            int y0 = Mathf.FloorToInt(top / CellSize);
            int y1 = Mathf.FloorToInt((top + height) / CellSize);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (!_cells.TryGetValue(Key(x, y), out List<int> cell))
                    {
                        continue;
                    }

                    for (int i = 0; i < cell.Count; i++)
                    {
                        Placement box = _boxes[cell[i]];
                        if (box.Id != self && box.Overlaps(left, top, width, height))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>Kutuyu ızgaraya ekler; <paramref name="owner"/> işaret kutularında şehir kimliği, etikette MaxValue.</summary>
        private void Insert(Placement box, ulong owner)
        {
            int index = _boxes.Count;
            box.Id = owner;
            _boxes.Add(box);
            int x0 = Mathf.FloorToInt(box.Left / CellSize);
            int x1 = Mathf.FloorToInt((box.Left + box.Width) / CellSize);
            int y0 = Mathf.FloorToInt(box.Top / CellSize);
            int y1 = Mathf.FloorToInt((box.Top + box.Height) / CellSize);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    long key = Key(x, y);
                    if (!_cells.TryGetValue(key, out List<int> cell))
                    {
                        cell = _pool.Count > 0 ? _pool.Pop() : new List<int>(8);
                        _cells.Add(key, cell);
                    }

                    cell.Add(index);
                }
            }
        }

        private void Reset()
        {
            foreach (List<int> cell in _cells.Values)
            {
                cell.Clear();
                _pool.Push(cell);
            }

            _cells.Clear();
            _boxes.Clear();
        }

        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
