#!/usr/bin/env python3
"""ContentCompiler (TS-GDD-TDD-3.1 §22, ADR-011): tarihsel GeoJSON → oyunun coğrafi ikili paketi.

Hat:
  1. Kaynak GeoJSON indirilir (önbellek) ve SHA-256'sı yapılandırmadakiyle karşılaştırılır.
  2. Kaynak biçimi "cshapes" ise (CShapes 2.0) yalnız senaryo tarihinde geçerli kayıtlar alınır ve ad
     `cntry_name`'den okunur; aksi halde (historical-basemaps) ad `NAME`'dir. Deniz ızgarasının kara
     maskesi kaynağın tüm kayıtlarıdır (CShapes'te tüm yıllar: 1914'te devletsiz kara da kara sayılır).
     Kapsam kutusuna kırpılır; ülkeler kaynak adlarından birleştirilir; tarihî düzeltmeler
     (alan devri) uygulanır; adsız adalar en yakın ada kaydının ülkesine verilir.
  3. Her ülke kendi şehirlerinin Voronoi hücreleriyle idari bölgelere bölünür (bölgeler ülkeyi
     boşluksuz ve örtüşmesiz kaplar, komşu bölgeler aynı köşeleri paylaşır).
  4. Kara komşuluğu (BorderGraph), şehir düğümleri, Delaunay kara yolları (deniz üstünden geçmez,
     yalnız komşu bölgeler arası), 1914 demiryolları, sınır kavşakları, boğaz geçişleri, deniz
     ızgarası ve deniz yolları, limanların deniz bağlantısı (ulaşım grafı).
  5. Koordinatlar E6 tamsayıya yuvarlanır; her halka çekirdekteki GeometryValidator kurallarına
     göre denetlenir (en az 3 köşe, ardışık tekrar yok, sıfır alan yok, kendini kesme yok).
  6. Çekirdeğin CanonicalWriter biçimiyle (little-endian, uzunluk önekli UTF-8) ikili paket yazılır;
     sonuna yükün SHA-256'sı eklenir. Ayrıca tr.txt'deki otomatik ad bloğu güncellenir.

Kullanım:
  python3 Tools/ContentCompiler/compile_geo_package.py Tools/ContentCompiler/data/1914_mvp.json \
      --out Assets/TarihStrateji/Unity/Bootstrap/Resources/Senaryolar/1914_mvp.bytes \
      --tr Assets/TarihStrateji/Unity/Presentation/Resources/Localization/tr.txt

Çıktı determinist olmalıdır: aynı kaynak + aynı yapılandırma + aynı shapely/GEOS sürümü → aynı bayt.
"""
import argparse
import hashlib
import json
import math
import os
import struct
import sys
import urllib.request

from shapely import delaunay_triangles, voronoi_polygons
from shapely.geometry import LineString, MultiPoint, MultiPolygon, Point, Polygon, box, shape
from shapely.ops import nearest_points, unary_union

TAG = 0x50475354  # "TSGP"
VERSION = 1
E6 = 1_000_000

# Çekirdek sabitleri (World.Geography); değerleri değiştirmek kayıt/paket biçimini değiştirir.
NODE_JUNCTION, NODE_CITY, NODE_PORT, NODE_SEA = 1, 2, 3, 6
EDGE_TRACK, EDGE_ROAD, EDGE_RAIL, EDGE_SEA, EDGE_STRAIT = 1, 2, 3, 4, 5
TERRAIN_PLAINS = 2
EARTH_RADIUS_M = 6_371_009
MAX_STRAIT_M = 30_000


def log(*args):
    print(*args, file=sys.stderr)


# ---------------------------------------------------------------- yardımcılar

def haversine_m(lon1, lat1, lon2, lat2):
    p1, p2 = math.radians(lat1), math.radians(lat2)
    dp = p2 - p1
    dl = math.radians(lon2 - lon1)
    a = math.sin(dp / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * EARTH_RADIUS_M * math.asin(min(1.0, math.sqrt(a)))


def e6(v):
    return int(round(v * E6))


def polygons_of(geom):
    if geom.is_empty:
        return []
    if isinstance(geom, Polygon):
        return [geom]
    if isinstance(geom, MultiPolygon):
        return list(geom.geoms)
    out = []
    for g in getattr(geom, "geoms", []):
        out.extend(polygons_of(g))
    return out


def ring_e6(coords):
    """Halkayı E6'ya yuvarlar; kapanış tekrarını ve ardışık tekrarları atar."""
    pts = []
    for x, y in list(coords)[:-1]:
        p = (e6(y), e6(x))  # (enlem, boylam)
        if not pts or pts[-1] != p:
            pts.append(p)
    while len(pts) > 1 and pts[0] == pts[-1]:
        pts.pop()
    return pts


def signed_area2(pts):
    s = 0
    for i in range(len(pts)):
        la1, lo1 = pts[i - 1]
        la2, lo2 = pts[i]
        s += lo1 * la2 - lo2 * la1
    return s


def orient(ax, ay, bx, by, cx, cy):
    v = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax)
    return (v > 0) - (v < 0)


def segments_cross(p1, p2, q1, q2):
    o1 = orient(p1[1], p1[0], p2[1], p2[0], q1[1], q1[0])
    o2 = orient(p1[1], p1[0], p2[1], p2[0], q2[1], q2[0])
    o3 = orient(q1[1], q1[0], q2[1], q2[0], p1[1], p1[0])
    o4 = orient(q1[1], q1[0], q2[1], q2[0], p2[1], p2[0])
    if o1 != o2 and o3 != o4:
        return True
    return False


def ring_locate(pts, lat, lon):
    """GeoRing.Locate eşi (tamsayı): 1 içeride, 0 sınırda, -1 dışarıda."""
    inside = False
    n = len(pts)
    j = n - 1
    for i in range(n):
        ay, ax = pts[j]
        by, bx = pts[i]
        cross = (bx - ax) * (lat - ay) - (lon - ax) * (by - ay)
        if cross == 0 and min(ax, bx) <= lon <= max(ax, bx) and min(ay, by) <= lat <= max(ay, by):
            return 0
        if (ay > lat) != (by > lat):
            if (cross > 0) if by > ay else (cross < 0):
                inside = not inside
        j = i
    return 1 if inside else -1


def rings_contain(rings, lat, lon):
    """GeoMultiPolygon.Contains eşi: bir çokgenin dış halkasında (sınır dahil) ve deliklerinin içinde değil."""
    for outer, holes in rings:
        if ring_locate(outer, lat, lon) >= 0 and all(ring_locate(h, lat, lon) <= 0 for h in holes):
            return True
    return False


def settle_inside(region, lon, lat):
    """Noktayı E6'da bölgenin içine (çekirdeğin göreceği biçimde) yerleştirir; gerekirse temsilci noktaya doğru kaydırır."""
    rp = region["geom"].representative_point()
    for k in range(0, 41):
        t = k / 40
        x = lon + (rp.x - lon) * t
        y = lat + (rp.y - lat) * t
        if rings_contain(region["rings"], e6(y), e6(x)):
            return x, y
    raise SystemExit("Nokta bölgeye yerleştirilemedi: bölge " + str(region["id"]))


def ring_ok(pts):
    """GeometryValidator'ın tamsayı kurallarının özü (çekirdek ayrıca yüklemede denetler)."""
    n = len(pts)
    if n < 3 or signed_area2(pts) == 0 or len(set(pts)) != n:
        return False
    for i in range(n):
        a1, a2 = pts[i], pts[(i + 1) % n]
        for j in range(i + 2, n):
            if i == 0 and j == n - 1:
                continue
            b1, b2 = pts[j], pts[(j + 1) % n]
            if segments_cross(a1, a2, b1, b2):
                return False
    return True


# ---------------------------------------------------------------- ikili yazıcı (CanonicalWriter eşi)

class Writer:
    def __init__(self):
        self.parts = []

    def u32(self, v):
        self.parts.append(struct.pack("<I", v))

    def i32(self, v):
        self.parts.append(struct.pack("<i", v))

    def u64(self, v):
        self.parts.append(struct.pack("<Q", v))

    def i64(self, v):
        self.parts.append(struct.pack("<q", v))

    def byte(self, v):
        self.parts.append(struct.pack("<B", v))

    def boolean(self, v):
        self.byte(1 if v else 0)

    def string(self, s):
        b = s.encode("utf-8")
        self.i32(len(b))
        self.parts.append(b)

    def data(self):
        return b"".join(self.parts)


# ---------------------------------------------------------------- derleme

def load_source(cfg, cache_dir):
    src = cfg["source"]
    os.makedirs(cache_dir, exist_ok=True)
    path = os.path.join(cache_dir, os.path.basename(src["url"]))
    if not os.path.exists(path):
        log("indiriliyor:", src["url"])
        urllib.request.urlretrieve(src["url"], path)
    raw = open(path, "rb").read()
    digest = hashlib.sha256(raw).hexdigest()
    expected = src.get("sha256")
    if expected and expected != digest:
        raise SystemExit("Kaynak dosyanın SHA-256'sı beklenenden farklı: " + digest)
    src["sha256"] = digest
    return json.loads(raw.decode("utf-8"))


def feature_name(cfg, props):
    """Kaynak biçimine göre ülke adı; senaryo tarihinde geçerli olmayan kayıt için None."""
    if cfg["source"].get("format") == "cshapes":
        day = tuple(cfg["date"])
        start = (props["gwsyear"], props["gwsmonth"], props["gwsday"])
        end = (props["gweyear"], props["gwemonth"], props["gweday"])
        return props["cntry_name"] if start <= day <= end else None
    return props.get("NAME")


def feature_active(cfg, props):
    """historical-basemaps'te tüm kayıtlar tek tarihlidir; CShapes'te yalnız senaryo tarihindekiler."""
    return cfg["source"].get("format") != "cshapes" or feature_name(cfg, props) is not None


def build_countries(cfg, data):
    clip = box(*cfg["bbox"])
    by_source = {}
    for c in cfg["countries"]:
        for name in c["sources"]:
            by_source[name] = c["key"]
    parts = {c["key"]: [] for c in cfg["countries"]}
    unnamed = []
    all_land = []
    for f in data["features"]:
        active = feature_active(cfg, f["properties"])
        g = shape(f["geometry"]).buffer(0).intersection(clip)
        if g.is_empty:
            continue
        # Kara maskesi: CShapes'te tüm yılların kayıtları (1914'te devletsiz kara, ör. Necd, de kara sayılır).
        all_land.append(g)
        if not active:
            continue
        name = feature_name(cfg, f["properties"])
        if name in by_source:
            parts[by_source[name]].append(g)
        elif name is None:
            unnamed.extend(polygons_of(g))
    islands = cfg.get("islands", [])
    for poly in unnamed:
        c = poly.representative_point()
        best, dist = None, 1e9
        for isl in islands:
            d = math.hypot(isl["lon"] - c.x, isl["lat"] - c.y)
            if d < dist:
                best, dist = isl, d
        if best is not None and dist <= 1.2:
            parts[best["country"]].append(poly)
        else:
            log("uyarı: sahipsiz ada atlandı", round(c.x, 2), round(c.y, 2))
    countries = {k: unary_union(v) if v else Polygon() for k, v in parts.items()}
    for corr in cfg.get("corrections", []):
        if corr["kind"] != "transfer":
            continue
        area = Polygon(corr["polygon"])
        moved = countries[corr["from"]].intersection(area)
        countries[corr["from"]] = countries[corr["from"]].difference(area)
        countries[corr["to"]] = unary_union([countries[corr["to"]], moved])
    # Ülkeler arası örtüşmeyi kaynak sırasına göre kaldır (örtüşen alan önceki ülkede kalır).
    taken = Polygon()
    for c in cfg["countries"]:
        g = countries[c["key"]].difference(taken).buffer(0)
        countries[c["key"]] = g
        taken = unary_union([taken, g])
    # Kırıntıları at (≈ 2 km²'den küçük parçalar).
    for k, g in countries.items():
        countries[k] = MultiPolygon([p for p in polygons_of(g) if p.area > 2e-4]) if not g.is_empty else g
    # Tüm kara (oyuna girmeyen sahipsiz kara dahil): deniz ızgarası buraya düğüm koymaz.
    return countries, unary_union(all_land)


def snap_inside(poly, lon, lat):
    p = Point(lon, lat)
    if poly.covers(p) and poly.buffer(-0.005).contains(p):
        return lon, lat, False
    inner = poly.buffer(-0.03)
    target = inner if not inner.is_empty else poly
    q = nearest_points(target, p)[0]
    return q.x, q.y, True


def build_regions(cfg, countries):
    """Her ülkeyi şehirlerinin Voronoi hücrelerine böler. Dönüş: [(şehir, bölge geometrisi)] şehir sırasıyla."""
    cities_by_country = {}
    for city in cfg["cities"]:
        cities_by_country.setdefault(city["country"], []).append(city)
    result = []
    for c in cfg["countries"]:
        geom = countries[c["key"]]
        cities = cities_by_country.get(c["key"], [])
        if geom.is_empty or not cities:
            continue
        positions = []
        for city in cities:
            lon, lat, moved = snap_inside(geom, city["lon"], city["lat"])
            if moved:
                log("bilgi: şehir sınır içine kaydırıldı", city["key"], (city["lon"], city["lat"]), "→", (round(lon, 3), round(lat, 3)))
            city["glon"], city["glat"] = lon, lat
            positions.append(Point(lon, lat))
        if len(cities) == 1:
            result.append((cities[0], geom))
            continue
        cells = voronoi_polygons(MultiPoint(positions), extend_to=box(*cfg["bbox"]).buffer(5))
        cell_list = list(cells.geoms)
        for city, pt in zip(cities, positions):
            cell = next(cl for cl in cell_list if cl.covers(pt))
            region = geom.intersection(cell).buffer(0)
            result.append((city, region))
    return result


def to_rings(region):
    """Bölge geometrisi → [(dış halka, [delikler])], E6 ve geçerlilik denetimli; geçersiz parça atlanır."""
    out = []
    for poly in polygons_of(region):
        if poly.area < 2e-4:
            continue
        outer = ring_e6(poly.exterior.coords)
        if not ring_ok(outer):
            simplified = Polygon(poly.exterior).simplify(0.002, preserve_topology=True)
            outer = ring_e6(simplified.exterior.coords) if isinstance(simplified, Polygon) else []
            if not ring_ok(outer):
                log("uyarı: geçersiz halka atlandı (alan", round(poly.area, 4), ")")
                continue
        holes = []
        for interior in poly.interiors:
            h = ring_e6(interior.coords)
            if ring_ok(h):
                holes.append(h)
        out.append((outer, holes))
    return out


def compile_package(cfg, cache_dir):
    data = load_source(cfg, cache_dir)
    countries, all_land = build_countries(cfg, data)
    country_ids = {c["key"]: c["id"] for c in cfg["countries"]}
    pairs = build_regions(cfg, countries)

    regions = []  # (id, sahip, şehir, geometri, halkalar)
    for i, (city, geom) in enumerate(pairs):
        rings = to_rings(geom)
        if not rings:
            raise SystemExit("Bölge geometrisi boş: " + city["key"])
        regions.append({"id": i + 1, "owner": country_ids[city["country"]], "city": city, "geom": geom, "rings": rings})
    land = unary_union([r["geom"] for r in regions])

    # Kara komşuluğu: paylaşılan sınır uzunluğu (≈ metre).
    borders = []
    for i in range(len(regions)):
        for j in range(i + 1, len(regions)):
            a, b = regions[i], regions[j]
            if not a["geom"].buffer(0.01).intersects(b["geom"]):
                continue
            shared = a["geom"].boundary.intersection(b["geom"].buffer(0.005))
            length_deg = shared.length
            if length_deg < 0.01:
                continue
            lat = (a["city"]["glat"] + b["city"]["glat"]) / 2
            meters = int(length_deg * 111_000 * max(0.5, math.cos(math.radians(lat))))
            borders.append((a["id"], b["id"], max(1000, meters)))
    adjacency = {(a, b) for a, b, _ in borders} | {(b, a) for a, b, _ in borders}

    def land_connected(ra, rb):
        return ra == rb or (ra, rb) in adjacency

    def region_at(lon, lat):
        p = Point(lon, lat)
        for r in regions:
            if r["geom"].covers(p):
                return r["id"]
        return None

    nodes = []  # dict: id, lon, lat, region, kind
    edges = []  # dict: a, b, kind
    city_node = {}
    for r in regions:
        c = r["city"]
        node_id = len(nodes) + 1
        nodes.append({"id": node_id, "lon": c["glon"], "lat": c["glat"], "region": r["id"], "kind": NODE_PORT if c["port"] else NODE_CITY})
        city_node[c["key"]] = node_id
    city_region = {r["city"]["key"]: r["id"] for r in regions}
    key_of_node = {v: k for k, v in city_node.items()}

    def over_land(lon1, lat1, lon2, lat2, samples=12):
        inside = 0
        for k in range(1, samples):
            t = k / samples
            if land.covers(Point(lon1 + (lon2 - lon1) * t, lat1 + (lat2 - lat1) * t)):
                inside += 1
        return inside >= samples - 2

    rail_set = {tuple(sorted(p)) for p in cfg.get("rails", [])}
    edge_set = set()

    def add_edge(a, b, kind):
        key = (min(a, b), max(a, b))
        if key in edge_set or a == b:
            return False
        edge_set.add(key)
        edges.append({"a": a, "b": b, "kind": kind})
        return True

    # Delaunay kara yolları (sınır kavşaklı).
    pts = [Point(n["lon"], n["lat"]) for n in nodes]
    tri = delaunay_triangles(MultiPoint(pts), only_edges=True)
    coord_node = {(round(n["lon"], 9), round(n["lat"], 9)): n["id"] for n in nodes}
    candidates = []
    for line in tri.geoms:
        (x1, y1), (x2, y2) = line.coords
        a = coord_node[(round(x1, 9), round(y1, 9))]
        b = coord_node[(round(x2, 9), round(y2, 9))]
        candidates.append((min(a, b), max(a, b)))
    for a, b in sorted(set(candidates)):
        na, nb = nodes[a - 1], nodes[b - 1]
        if not over_land(na["lon"], na["lat"], nb["lon"], nb["lat"]):
            continue
        if haversine_m(na["lon"], na["lat"], nb["lon"], nb["lat"]) > 450_000:
            continue
        kind = EDGE_RAIL if tuple(sorted((key_of_node[a], key_of_node[b]))) in rail_set else EDGE_ROAD
        ra, rb = na["region"], nb["region"]
        if ra == rb:
            add_edge(a, b, kind)
            continue
        if not land_connected(ra, rb):
            continue
        # Sınır kavşağı: kenarın orta noktası, bulunduğu bölge iki uca da kara komşusuysa.
        mx, my = (na["lon"] + nb["lon"]) / 2, (na["lat"] + nb["lat"]) / 2
        rm = region_at(mx, my)
        if rm is not None and land_connected(rm, ra) and land_connected(rm, rb):
            j = len(nodes) + 1
            nodes.append({"id": j, "lon": mx, "lat": my, "region": rm, "kind": NODE_JUNCTION})
            add_edge(a, j, kind)
            add_edge(j, b, kind)
        else:
            add_edge(a, b, kind)

    # Delaunay dışında kalan 1914 demiryolları (uçlar komşu bölgedeyse doğrudan).
    for ka, kb in sorted(rail_set):
        if ka not in city_node or kb not in city_node:
            continue
        a, b = city_node[ka], city_node[kb]
        if (min(a, b), max(a, b)) in edge_set:
            continue
        na, nb = nodes[a - 1], nodes[b - 1]
        if land_connected(na["region"], nb["region"]) and over_land(na["lon"], na["lat"], nb["lon"], nb["lat"]):
            add_edge(a, b, EDGE_RAIL)

    # Boğaz geçişleri: iki kıyıda kavşak, aralarında boğaz kenarı, her kavşak bölgesinin şehrine yol.
    for s in cfg.get("straits", []):
        ends = []
        for lon, lat in (s["a"], s["b"]):
            rid = region_at(lon, lat)
            if rid is None:
                region = min(regions, key=lambda r: r["geom"].distance(Point(lon, lat)))
                lon, lat, _ = snap_inside(region["geom"], lon, lat)
                rid = region["id"]
            j = len(nodes) + 1
            nodes.append({"id": j, "lon": lon, "lat": lat, "region": rid, "kind": NODE_JUNCTION})
            city_key = next(r["city"]["key"] for r in regions if r["id"] == rid)
            add_edge(j, city_node[city_key], EDGE_ROAD)
            ends.append(j)
        a, b = nodes[ends[0] - 1], nodes[ends[1] - 1]
        if haversine_m(a["lon"], a["lat"], b["lon"], b["lat"]) * 1.1 <= MAX_STRAIT_M:
            add_edge(ends[0], ends[1], EDGE_STRAIT)
        else:
            log("uyarı: boğaz geçişi 30 km'yi aşıyor, atlandı:", s["name"])

    # Deniz ızgarası.
    step = cfg.get("seaGridStep", 0.75)
    lon0, lat0, lon1, lat1 = cfg["bbox"]
    coast = unary_union([land, all_land]).buffer(0.12)
    world_land = unary_union([land, all_land])
    grid = {}
    gy = 0
    lat = lat0 + step / 2
    while lat < lat1:
        gx = 0
        lon = lon0 + step / 2
        while lon < lon1:
            if not coast.covers(Point(lon, lat)):
                j = len(nodes) + 1
                nodes.append({"id": j, "lon": lon, "lat": lat, "region": 0, "kind": NODE_SEA})
                grid[(gx, gy)] = j
            gx += 1
            lon += step
        gy += 1
        lat += step
    for (gx, gy), j in sorted(grid.items()):
        for dx, dy in ((1, 0), (0, 1), (1, 1), (1, -1)):
            k = grid.get((gx + dx, gy + dy))
            if k is None:
                continue
            a, b = nodes[j - 1], nodes[k - 1]
            if not LineString([(a["lon"], a["lat"]), (b["lon"], b["lat"])]).intersects(world_land):
                add_edge(j, k, EDGE_SEA)
    # Elle deniz bağlantıları (dar boğazlar; kaba kıyı verisinde ızgara geçemez).
    sea_nodes = [n for n in nodes if n["kind"] == NODE_SEA]
    for link in cfg.get("seaLinks", []):
        chain = []
        for lon, lat in link["points"]:
            j = len(nodes) + 1
            nodes.append({"id": j, "lon": lon, "lat": lat, "region": 0, "kind": NODE_SEA})
            chain.append(j)
        for a, b in zip(chain, chain[1:]):
            add_edge(a, b, EDGE_SEA)
        for end in (chain[0], chain[-1]):
            e = nodes[end - 1]
            near = sorted(sea_nodes, key=lambda n: (n["lon"] - e["lon"]) ** 2 + (n["lat"] - e["lat"]) ** 2)[:2]
            for n in near:
                add_edge(end, n["id"], EDGE_SEA)
    sea_nodes = [n for n in nodes if n["kind"] == NODE_SEA]

    # Limanlar: en az kara geçen en yakın 2 deniz düğümüne bağlanır (körfez limanları kıyı verisi kaba
    # olduğundan ≤ 0,6° kara geçişine izin verilir); bağlanamayan liman şehir olur.
    for n in nodes:
        if n["kind"] != NODE_PORT:
            continue
        harbour = Point(n["lon"], n["lat"]).buffer(0.2)
        options = []
        for s in sea_nodes:
            dist = math.hypot(s["lon"] - n["lon"], s["lat"] - n["lat"])
            if dist > 2.2:
                continue
            line = LineString([(n["lon"], n["lat"]), (s["lon"], s["lat"])])
            crossing = line.difference(harbour).intersection(land).length
            if crossing <= 0.6:
                options.append((round(crossing, 6), round(dist, 6), s["id"]))
        options.sort()
        linked = 0
        for _, _, sid in options[:2]:
            if add_edge(n["id"], sid, EDGE_SEA):
                linked += 1
        if linked == 0:
            n["kind"] = NODE_CITY
            log("uyarı: liman deniz yoluna bağlanamadı, şehir yapıldı:", key_of_node[n["id"]])

    # Bağlantısız deniz düğümlerini at (yalnız kullanılanlar pakete girer).
    used = set()
    for e in edges:
        used.add(e["a"])
        used.add(e["b"])
    keep = [n for n in nodes if n["kind"] != NODE_SEA or n["id"] in used]
    remap = {n["id"]: i + 1 for i, n in enumerate(keep)}
    for n in keep:
        n["id"] = remap[n["id"]]
    for e in edges:
        e["a"], e["b"] = remap[e["a"]], remap[e["b"]]
    nodes = keep
    city_node = {k: remap[v] for k, v in city_node.items()}

    # Kara düğümleri ve şehirler çekirdeğin tamsayı nokta-çokgen denetiminden geçecek biçimde yerleşir.
    region_by_id = {r["id"]: r for r in regions}
    for n in nodes:
        if n["kind"] == NODE_SEA:
            continue
        n["lon"], n["lat"] = settle_inside(region_by_id[n["region"]], n["lon"], n["lat"])
    for r in regions:
        node = nodes[city_node[r["city"]["key"]] - 1]
        r["city"]["glon"], r["city"]["glat"] = node["lon"], node["lat"]

    # Kenar uzunluğu: büyük çember × kıvrım (çekirdek, büyük çemberden kısa kenarı reddeder).
    by_id = {n["id"]: n for n in nodes}
    winding = {EDGE_RAIL: 1.10, EDGE_ROAD: 1.20, EDGE_TRACK: 1.25, EDGE_SEA: 1.20, EDGE_STRAIT: 1.10}
    capacity = {EDGE_RAIL: 30, EDGE_ROAD: 10, EDGE_TRACK: 5, EDGE_SEA: 20, EDGE_STRAIT: 5}
    for e in edges:
        a, b = by_id[e["a"]], by_id[e["b"]]
        d = haversine_m(a["lon"], a["lat"], b["lon"], b["lat"])
        e["length"] = int(math.ceil(d * winding[e["kind"]])) + 50
        e["capacity"] = capacity[e["kind"]]

    # Nüfus: ülke toplamı bölgelere (kademe ağırlığı × alan kökü) dağıtılır; ölçek 1/5.
    totals = {c["key"]: c["population"] * 1000 // 5 for c in cfg["countries"]}
    weights = {}
    for r in regions:
        tier = r["city"]["tier"]
        w = (tier * tier) * max(0.2, math.sqrt(r["geom"].area))
        weights.setdefault(r["city"]["country"], []).append((r, w))
    for key, items in weights.items():
        total_w = sum(w for _, w in items)
        for r, w in items:
            r["population"] = max(20_000, int(totals[key] * w / total_w))

    return {"cfg": cfg, "regions": regions, "borders": borders, "nodes": nodes, "edges": edges, "city_node": city_node}


def write_package(pkg):
    cfg = pkg["cfg"]
    w = Writer()
    w.u32(TAG)
    w.i32(VERSION)
    w.string(cfg["scenario"])
    y, m, d = cfg["date"]
    w.i32(y)
    w.i32(m)
    w.i32(d)
    src = cfg["source"]
    w.i32(1)
    w.string(src["id"])
    w.string(src["license"])
    w.string(src["url"])
    w.string(src["sha256"])
    w.string(src.get("accuracy", ""))
    w.i32(len(cfg["countries"]))
    for c in cfg["countries"]:
        w.u64(c["id"])
        w.string("ulke.1914." + c["key"])
    regions = pkg["regions"]
    w.i32(len(regions))
    for r in regions:
        w.u64(r["id"])
        w.u64(r["owner"])
        w.string("bolge.1914." + r["city"]["key"])
        w.i64(r["population"])
        w.i32(len(r["rings"]))
        for outer, holes in r["rings"]:
            w.i32(1 + len(holes))
            for ring in [outer] + holes:
                w.i32(len(ring))
                for lat, lon in ring:
                    w.i32(lat)
                    w.i32(lon)
    w.i32(len(pkg["borders"]))
    for a, b, meters in pkg["borders"]:
        w.u64(a)
        w.u64(b)
        w.i64(meters)
        w.boolean(True)
    w.i32(len(pkg["nodes"]))
    for n in pkg["nodes"]:
        w.u64(n["id"])
        w.i32(e6(n["lat"]))
        w.i32(e6(n["lon"]))
        w.u64(n["region"])
        w.byte(n["kind"])
    w.i32(len(pkg["edges"]))
    for i, e in enumerate(pkg["edges"]):
        w.u64(i + 1)
        w.u64(e["a"])
        w.u64(e["b"])
        w.byte(e["kind"])
        w.i64(e["length"])
        w.i32(e["capacity"])
        w.byte(TERRAIN_PLAINS)
    w.i32(len(regions))
    for i, r in enumerate(regions):
        c = r["city"]
        w.u64(i + 1)
        w.string("sehir.1914." + c["key"])
        w.i32(e6(c["glat"]))
        w.i32(e6(c["glon"]))
        w.u64(r["id"])
        w.u64(pkg["city_node"][c["key"]])
        w.i32(c["tier"])
        w.boolean(c["port"])
        w.u64(r["owner"])
    payload = w.data()
    return payload + hashlib.sha256(payload).digest()


def update_tr(path, cfg):
    begin, end = "# <senaryo-1914: ContentCompiler üretir, elle düzenlemeyin>", "# </senaryo-1914>"
    text = open(path, encoding="utf-8").read()
    outside = text
    if begin in text:
        outside = text[: text.index(begin)] + text[text.index(end) + len(end):]
    lines = [begin]
    for c in cfg["countries"]:
        lines.append("ulke.1914.%s = %s" % (c["key"], c["name"]))
        # Genel ülke adı başka senaryoda tanımlıysa (ulke.1 Türkiye, ulke.2 Yunanistan) ezilmez.
        if "\nulke.%d = " % c["id"] not in outside:
            lines.append("ulke.%d = %s" % (c["id"], c["name"]))
    for c in cfg["cities"]:
        lines.append("sehir.1914.%s = %s" % (c["key"], c["name"]))
        lines.append("bolge.1914.%s = %s" % (c["key"], c["name"]))
    lines.append(end)
    if begin in text:
        head = text[: text.index(begin)]
        tail = text[text.index(end) + len(end):]
        text = head.rstrip("\n") + "\n" + "\n".join(lines) + tail
    else:
        text = text.rstrip("\n") + "\n" + "\n".join(lines) + "\n"
    open(path, "w", encoding="utf-8").write(text)


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("config")
    ap.add_argument("--out", required=True)
    ap.add_argument("--tr")
    ap.add_argument("--cache", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), ".cache"))
    args = ap.parse_args()
    cfg = json.load(open(args.config, encoding="utf-8"))
    pkg = compile_package(cfg, args.cache)
    data = write_package(pkg)
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    open(args.out, "wb").write(data)
    if args.tr:
        update_tr(args.tr, cfg)
    kinds = {}
    for e in pkg["edges"]:
        kinds[e["kind"]] = kinds.get(e["kind"], 0) + 1
    log("paket:", args.out, len(data), "bayt; bölge", len(pkg["regions"]), "sınır", len(pkg["borders"]),
        "düğüm", len(pkg["nodes"]), "kenar", len(pkg["edges"]), kinds, "kaynak sha256", cfg["source"]["sha256"][:16])


if __name__ == "__main__":
    main()
