using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Kara ve deniz ulaşım grafı (§3.4, REG-015). Eklemede sahte komşuluk reddedilir:
    /// <list type="bullet">
    /// <item>kenar uzunluğu uç noktalar arası büyük çember mesafesinden kısa olamaz (ışınlanma yok);</item>
    /// <item>kara kenarı yalnız aynı bölgedeki veya <see cref="BorderGraph"/>'ta kara geçişine açık
    /// komşu bölgelerdeki kara düğümlerini bağlar (deniz üstünden yürüyüş yok, MAP-G05);</item>
    /// <item>boğaz geçişi iki kara düğümü arasında ve en çok <see cref="MaxStraitCrossingMeters"/>;</item>
    /// <item>deniz yolu yalnız liman ve deniz düğümlerini bağlar.</item>
    /// </list>
    /// Komşu kenar listeleri kenar kimliği sırasındadır (deterministik arama).
    /// </summary>
    public sealed class TransportGraph
    {
        public const long MaxStraitCrossingMeters = 30_000L;

        private static readonly List<TransportEdge> NoEdges = new List<TransportEdge>(0);
        private readonly Dictionary<TransportNodeId, TransportNode> _nodes = new Dictionary<TransportNodeId, TransportNode>();
        private readonly Dictionary<TransportEdgeId, TransportEdge> _edges = new Dictionary<TransportEdgeId, TransportEdge>();
        private readonly Dictionary<TransportNodeId, List<TransportEdge>> _adjacency = new Dictionary<TransportNodeId, List<TransportEdge>>();
        private readonly List<TransportNodeId> _nodeOrder = new List<TransportNodeId>();
        private readonly RegionAtlas _atlas;
        private readonly BorderGraph _borders;

        public TransportGraph(RegionAtlas atlas, BorderGraph borders)
        {
            _atlas = atlas;
            _borders = borders;
        }

        public int NodeCount => _nodes.Count;

        public int EdgeCount => _edges.Count;

        public ValidationResult AddNode(TransportNode node)
        {
            if (node == null || !node.Id.IsValid)
            {
                return ValidationResult.Fail(GeographyErrorKeys.DugumBulunamadi);
            }

            if (_nodes.ContainsKey(node.Id))
            {
                return ValidationResult.Fail(GeographyErrorKeys.DugumTekrar);
            }

            if (node.IsLand && (!node.Region.IsValid || !_atlas.Contains(node.Region, node.Location)))
            {
                return ValidationResult.Fail(GeographyErrorKeys.DugumBolgeDisinda);
            }

            _nodes.Add(node.Id, node);
            _nodeOrder.Insert(~_nodeOrder.BinarySearch(node.Id), node.Id);
            return ValidationResult.Ok;
        }

        public ValidationResult AddEdge(TransportEdge edge)
        {
            if (edge == null || !edge.Id.IsValid)
            {
                return ValidationResult.Fail(GeographyErrorKeys.KenarTekrar);
            }

            if (_edges.ContainsKey(edge.Id))
            {
                return ValidationResult.Fail(GeographyErrorKeys.KenarTekrar);
            }

            if (edge.A == edge.B)
            {
                return ValidationResult.Fail(GeographyErrorKeys.KenarKendisine);
            }

            if (!_nodes.TryGetValue(edge.A, out TransportNode a) || !_nodes.TryGetValue(edge.B, out TransportNode b))
            {
                return ValidationResult.Fail(GeographyErrorKeys.DugumBulunamadi);
            }

            if (edge.LengthMeters < GeoMath.DistanceMeters(a.Location, b.Location) || edge.DailyCapacity < 0)
            {
                return ValidationResult.Fail(GeographyErrorKeys.KenarUzunluguGecersiz);
            }

            switch (edge.Kind)
            {
                case TransportEdgeKind.SeaLane:
                    if (!a.IsNaval || !b.IsNaval)
                    {
                        return ValidationResult.Fail(GeographyErrorKeys.DenizKenariLimanDegil);
                    }

                    break;
                case TransportEdgeKind.StraitCrossing:
                    if (!a.IsLand || !b.IsLand || edge.LengthMeters > MaxStraitCrossingMeters)
                    {
                        return ValidationResult.Fail(GeographyErrorKeys.KaraKenariKomsuDegil);
                    }

                    break;
                default:
                    if (!a.IsLand || !b.IsLand || !_borders.LandConnected(a.Region, b.Region))
                    {
                        return ValidationResult.Fail(GeographyErrorKeys.KaraKenariKomsuDegil);
                    }

                    break;
            }

            _edges.Add(edge.Id, edge);
            Attach(edge.A, edge);
            Attach(edge.B, edge);
            return ValidationResult.Ok;
        }

        /// <summary>Düğümler kimlik sırasıyla.</summary>
        public IReadOnlyList<TransportNodeId> Nodes => _nodeOrder;

        public bool TryGetNode(TransportNodeId id, out TransportNode node) => _nodes.TryGetValue(id, out node);

        public bool TryGetEdge(TransportEdgeId id, out TransportEdge edge) => _edges.TryGetValue(id, out edge);

        /// <summary>Düğüme bağlı kenarlar (kenar kimliği sırasıyla).</summary>
        public IReadOnlyList<TransportEdge> EdgesOf(TransportNodeId node) => _adjacency.TryGetValue(node, out List<TransportEdge> list) ? list : NoEdges;

        private void Attach(TransportNodeId node, TransportEdge edge)
        {
            if (!_adjacency.TryGetValue(node, out List<TransportEdge> list))
            {
                list = new List<TransportEdge>(4);
                _adjacency.Add(node, list);
            }

            int index = 0;
            while (index < list.Count && list[index].Id < edge.Id)
            {
                index++;
            }

            list.Insert(index, edge);
        }
    }
}
