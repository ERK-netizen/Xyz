using NUnit.Framework;
using TarihStrateji.Unity.Presentation.Map;
using UnityEngine;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Harita geometrisi: hücre merkezi ↔ dokunma seçimi, sarmalama, LOD ve parça ağları.</summary>
    public class HexMapGeometryTests
    {
        private const int Width = 24;
        private const int Height = 12;

        private static readonly Vector2[] Offsets =
        {
            Vector2.zero, new Vector2(0.7f, 0f), new Vector2(-0.7f, 0f), new Vector2(0f, 0.75f), new Vector2(0f, -0.75f),
            new Vector2(0.5f, 0.5f), new Vector2(-0.5f, -0.5f), new Vector2(0.55f, -0.45f), new Vector2(-0.55f, 0.45f),
        };

        [Test]
        public void Pick_ReturnsTheCellWhoseCenterWasTouched_IncludingNearEdges()
        {
            for (int row = 0; row < Height; row++)
            {
                for (int column = 0; column < Width; column++)
                {
                    int index = (row * Width) + column;
                    Vector2 center = HexLayout.CellCenter(column, row);
                    Assert.That(HexLayout.CellCenterOfIndex(index, Width), Is.EqualTo(center));
                    foreach (Vector2 offset in Offsets)
                    {
                        if ((row == 0 && offset.y > 0.5f) || (row == Height - 1 && offset.y < -0.5f))
                        {
                            continue;
                        }

                        Assert.That(HexLayout.Pick(center + offset, Width, Height), Is.EqualTo(index), $"({column},{row}) + {offset}");
                    }
                }
            }
        }

        [Test]
        public void Pick_WrapsAcrossTheAntimeridian()
        {
            float world = HexLayout.WorldWidth(Width);
            Vector2 center = HexLayout.CellCenter(3, 5);
            int index = (5 * Width) + 3;
            Assert.That(HexLayout.Pick(center + new Vector2(world, 0f), Width, Height), Is.EqualTo(index));
            Assert.That(HexLayout.Pick(center - new Vector2(world * 2f, 0f), Width, Height), Is.EqualTo(index));
            Assert.That(HexLayout.Pick(HexLayout.CellCenter(0, 4) - new Vector2(HexLayout.ColumnWidth, 0f), Width, Height), Is.EqualTo((4 * Width) + Width - 1));
        }

        [Test]
        public void Pick_OutsideThePolarRows_ReturnsMinusOne()
        {
            Assert.That(HexLayout.Pick(new Vector2(3f, 3f), Width, Height), Is.EqualTo(-1));
            Assert.That(HexLayout.Pick(new Vector2(3f, -HexLayout.RowHeight * (Height + 1)), Width, Height), Is.EqualTo(-1));
        }

        [Test]
        public void Neighbours_FollowOddRLayout_AtUnitHexDistance()
        {
            // Çift satırda kuzeydoğu (c, r−1), tek satırda (c+1, r−1); komşu merkezler √3 uzaklıkta.
            Assert.That((HexLayout.CellCenter(4, 2) - HexLayout.CellCenter(4, 1)).magnitude, Is.EqualTo(HexLayout.Sqrt3).Within(1e-4f));
            Assert.That((HexLayout.CellCenter(4, 3) - HexLayout.CellCenter(5, 2)).magnitude, Is.EqualTo(HexLayout.Sqrt3).Within(1e-4f));
            Assert.That((HexLayout.CellCenter(4, 3) - HexLayout.CellCenter(5, 3)).magnitude, Is.EqualTo(HexLayout.Sqrt3).Within(1e-4f));
            Assert.That(HexLayout.CellCenter(0, 1).y, Is.LessThan(HexLayout.CellCenter(0, 0).y), "satır 0 kuzeyde (yukarıda)");
        }

        [Test]
        public void WrapShift_MovesAnyCopyWithinHalfAWorldOfTheCamera()
        {
            float world = HexLayout.WorldWidth(Width);
            foreach (float x in new[] { -3f * world, -10f, 0f, 5f, world * 0.9f, world * 4.2f })
            {
                foreach (float camera in new[] { 0f, world * 0.5f, world * 0.99f })
                {
                    float placed = x + HexLayout.WrapShift(x, camera, world);
                    Assert.That(Mathf.Abs(placed - camera), Is.LessThanOrEqualTo((world * 0.5f) + 1e-3f));
                    Assert.That(Mathf.Repeat(placed - x, world), Is.EqualTo(0f).Within(1e-3f).Or.EqualTo(world).Within(1e-3f));
                }
            }
        }

        [Test]
        public void Lod_RisesWithZoomOut_AndHasHysteresis()
        {
            Assert.That(MapLod.LevelFor(10f, 0), Is.EqualTo(0));
            Assert.That(MapLod.LevelFor(500f, 0), Is.EqualTo(MapLod.MaxLevel));
            Assert.That(MapLod.LevelFor(10f, MapLod.MaxLevel), Is.EqualTo(0));
            Assert.That(MapLod.LevelFor(27f, 0), Is.EqualTo(0), "eşiğin hemen üstü: geçiş yok");
            Assert.That(MapLod.LevelFor(25f, 1), Is.EqualTo(1), "eşiğin hemen altı: geri dönüş yok");
            Assert.That(MapLod.LevelFor(40f, 0), Is.EqualTo(1));
            int previous = 0;
            for (float size = 4f; size < 400f; size *= 1.1f)
            {
                int level = MapLod.LevelFor(size, previous);
                Assert.That(level, Is.GreaterThanOrEqualTo(previous));
                previous = level;
            }

            Assert.That(MapLod.Step(0), Is.EqualTo(1));
            Assert.That(MapLod.Step(3), Is.EqualTo(8));
        }

        [Test]
        public void ChunkBuilder_Lod0_HasSevenVerticesPerCell_AndClipsAtMapEdge()
        {
            var builder = new MapChunkBuilder();
            Color32[] colors = Colors(20, 10);
            builder.Build(0, 0, 20, 10, 0, colors);
            Assert.That(builder.VertexCount, Is.EqualTo(16 * 10 * 7));
            Assert.That(builder.TriangleCount, Is.EqualTo(16 * 10 * 6));
            builder.Build(1, 0, 20, 10, 0, colors);
            Assert.That(builder.VertexCount, Is.EqualTo(4 * 10 * 7), "son parça kısmi");
        }

        [TestCase(1, 64)]
        [TestCase(2, 16)]
        [TestCase(3, 4)]
        public void ChunkBuilder_HigherLods_UseOneQuadPerBlock(int level, int quads)
        {
            var builder = new MapChunkBuilder();
            builder.Build(0, 0, 32, 32, level, Colors(32, 32));
            Assert.That(builder.VertexCount, Is.EqualTo(quads * 4));
            Assert.That(builder.TriangleCount, Is.EqualTo(quads * 2));
        }

        [Test]
        public void ChunkBuilder_AppliesToMeshWithBoundsInsideTheChunk()
        {
            var builder = new MapChunkBuilder();
            builder.Build(0, 0, 16, 16, 0, Colors(16, 16));
            var mesh = new Mesh();
            try
            {
                builder.Apply(mesh);
                Assert.That(mesh.vertexCount, Is.EqualTo(builder.VertexCount));
                Assert.That(mesh.colors32.Length, Is.EqualTo(builder.VertexCount));
                Assert.That(mesh.bounds.size.x, Is.EqualTo(HexLayout.ColumnWidth * 16.5f).Within(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        private static Color32[] Colors(int width, int height)
        {
            var colors = new Color32[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32((byte)(i % 251), 80, 120, 255);
            }

            return colors;
        }
    }
}
