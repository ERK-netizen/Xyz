"""Dünya 1936 yapılandırma üreteci (Adım 21): CShapes birimleri → egemen devlet, Natural Earth şehirleri → data/dunya1936.json."""
import json, math, re, sys, unicodedata, collections
from shapely.geometry import shape, Point
from shapely.ops import unary_union
from shapely.strtree import STRtree
# Kullanım: python gen_world1936.py <önbellek klasörü>  (içinde cshapes/CShapes-2.0.geojson ve ne/places.geojson)
# Natural Earth: https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_populated_places_simple.geojson
import os
S = sys.argv[1]
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
exec(open(os.path.join(HERE, 'world_countries.py'), encoding='utf-8').read())
cs = json.load(open(S + '/cshapes/CShapes-2.0.geojson'))
ne = json.load(open(S + '/ne/places.geojson'))
T = (1936, 3, 7)
unit_owner = {u: c[0] for c in COUNTRIES for u in c[4]}
units = []
for f in cs['features']:
    p = f['properties']
    if (p['gwsyear'], p['gwsmonth'], p['gwsday']) <= T <= (p['gweyear'], p['gwemonth'], p['gweday']):
        units.append(dict(name=p['cntry_name'], owner=unit_owner[p['cntry_name']], geom=shape(f['geometry']).buffer(0),
                          cap=p['capname'], caplon=p['caplong'], caplat=p['caplat']))
tree = STRtree([u['geom'] for u in units])
land = unary_union([u['geom'] for u in units])

def unit_at(lon, lat):
    pt = Point(lon, lat)
    best, bd = None, 0.35
    for i in tree.query(pt.buffer(0.35)):
        d = units[i]['geom'].distance(pt)
        if d < bd:
            best, bd = units[i], d
    return best

# 1936 adları ve Türkçe karşılıklar (Natural Earth adı → oyundaki ad).
RENAME = {
 'Milan':'Milano','Munich':'Münih','Turin':'Torino','Naples':'Napoli','Florence':'Floransa','Venice':'Venedik','Genoa':'Cenova',
 'Barcelona':'Barselona','Seville':'Sevilla','Valencia':'Valensiya','Marseille':'Marsilya','Cologne':'Köln','Nuremberg':'Nürnberg',
 'Hanover':'Hannover','Brunswick':'Braunschweig','Antwerp':'Anvers','The Hague':'La Haye','Rotterdam':'Rotterdam',
 'Gothenburg':'Göteborg','Krakow':'Krakov','Lodz':'Lodz','Poznan':'Posen','Bucharest':'Bükreş','Cluj-Napoca':'Kolojvar',
 'Iasi':'Yaş','Plovdiv':'Filibe','Varna':'Varna','Skopje':'Üsküp','Sarajevo':'Saraybosna','Tirana':'Tiran','Edinburgh':'Edinburg',
 'Lisbon':'Lizbon','Porto':'Porto','Geneva':'Cenevre','Zurich':'Zürih','Moscow':'Moskova','Minsk':'Minsk','Riga':'Riga',
 'Mosul':'Musul','Basra':'Basra','Aleppo':'Halep','Haifa':'Hayfa','Jeddah':'Cidde','Port Said':'Port Said','Tabriz':'Tebriz',
 'Mashhad':'Meşhed','Isfahan':'İsfahan','Shiraz':'Şiraz','Bursa':'Bursa','Adana':'Adana','Gaziantep':'Antep','Konya':'Konya',
 'Kayseri':'Kayseri','Samsun':'Samsun','Trabzon':'Trabzon','Erzurum':'Erzurum','Diyarbakir':'Diyarbakır','Eskisehir':'Eskişehir',
 'Tianjin':'Tiencin','Calcutta':'Kalküta','Bangalore':'Bangalor','Hyderabad':'Haydarabad','Rangoon':'Rangun','Hong Kong':'Hong Kong',
 'Washington, D.C.':'Vaşington','Washington':'Vaşington','Philadelphia':'Filadelfiya','New Orleans':'Yeni Orleans',

 'Istanbul':'İstanbul','London':'Londra','Paris':'Paris','Berlin':'Berlin','Moscow':'Moskova','Vienna':'Viyana','Rome':'Roma',
 'Athens':'Atina','Sofia':'Sofya','Belgrade':'Belgrad','Bucharest':'Bükreş','Budapest':'Budapeşte','Cairo':'Kahire','Baghdad':'Bağdat',
 'Tehran':'Tahran','Warsaw':'Varşova','Prague':'Prag','Brussels':'Brüksel','Lisbon':'Lizbon','Madrid':'Madrid','Bern':'Bern',
 'Copenhagen':'Kopenhag','Stockholm':'Stockholm','Oslo':'Oslo','Helsinki':'Helsinki','Dublin':'Dublin','Amsterdam':'Amsterdam',
 'Washington, D.C.':'Vaşington','New York':'New York','Ottawa':'Ottava','Mexico City':'Meksiko','Rio de Janeiro':'Rio de Janeiro',
 'Buenos Aires':'Buenos Aires','Santiago':'Santiago','Lima':'Lima','Bogota':'Bogota','Caracas':'Karakas','Tokyo':'Tokyo',
 'Beijing':'Pekin','Shanghai':'Şanghay','Nanjing':'Nankin','Hong Kong':'Hong Kong','Seoul':'Keijō (Seul)','Taipei':'Taihoku (Taipei)',
 'St. Petersburg':'Leningrad','Volgograd':'Stalingrad','Samara':'Kuybışev','Yekaterinburg':'Sverdlovsk','Nizhny Novgorod':'Gorki',
 'Kyiv':'Kiev','Kiev':'Kiev','Kharkiv':'Harkov','Odesa':'Odessa','Odessa':'Odessa','Tbilisi':'Tiflis','Yerevan':'Erivan','Baku':'Bakü',
 'Tashkent':'Taşkent','Almaty':'Alma-Ata','Bishkek':'Frunze','Dushanbe':'Stalinabad','Donetsk':'Stalino',
 'Jakarta':'Batavya','Mumbai':'Bombay','Chennai':'Madras','Kolkata':'Kalküta','New Delhi':'Yeni Delhi','Delhi':'Delhi',
 'Karachi':'Karaçi','Lahore':'Lahor','Dhaka':'Dakka','Yangon':'Rangun','Colombo':'Kolombo','Bangkok':'Bangkok','Hanoi':'Hanoi',
 'Ho Chi Minh City':'Saygon','Manila':'Manila','Singapore':'Singapur','Kuala Lumpur':'Kuala Lumpur','Kinshasa':'Leopoldville',
 'Harare':'Salisbury','Maputo':'Lourenço Marques','Lubumbashi':'Elisabethville','Ndjamena':'Fort-Lamy','N\'Djamena':'Fort-Lamy',
 'Algiers':'Cezayir','Tunis':'Tunus','Tripoli':'Trablusgarp','Benghazi':'Bingazi','Rabat':'Rabat','Casablanca':'Kazablanka',
 'Alexandria':'İskenderiye','Damascus':'Şam','Beirut':'Beyrut','Jerusalem':'Kudüs','Amman':'Amman','Riyadh':'Riyad','Mecca':'Mekke',
 'Medina':'Medine','Sanaa':'San\'a','Kabul':'Kabil','Addis Ababa':'Addis Ababa','Khartoum':'Hartum','Nairobi':'Nairobi',
 'Gdansk':'Danzig','Wroclaw':'Breslau','Szczecin':'Stettin','Kaliningrad':'Königsberg','Vilnius':'Wilno','Lviv':'Lwów',
 'Bratislava':'Bratislava','Ljubljana':'Lubyana','Zagreb':'Zagreb','Thessaloniki':'Selanik','Izmir':'İzmir','Ankara':'Ankara',
 'Chisinau':'Kişinev','Tallinn':'Tallinn','Riga':'Riga','Kaunas':'Kaunas','Reykjavik':'Reykjavik','Luxembourg':'Lüksemburg',
 'Ulaanbaatar':'Ulan Bator','Lhasa':'Lhasa','Kathmandu':'Katmandu','Thimphu':'Thimphu','Muscat':'Maskat',
 'Guangzhou':'Kanton','Chongqing':'Çungking','Wuhan':'Hankou','Tianjin':'Tiencin','Shenyang':'Mukden','Harbin':'Harbin',
 'Xian':'Şian','Lanzhou':'Lancou','Urumqi':'Dihua (Urumçi)','Pyongyang':'Heijō (Pyongyang)','Busan':'Fusan (Pusan)',
 'Osaka':'Osaka','Nagoya':'Nagoya','Sapporo':'Sapporo','Fukuoka':'Fukuoka','Hiroshima':'Hiroşima','Kyoto':'Kyoto',
 'Ho Chi Minh':'Saygon','Phnom Penh':'Pnom Pen','Vientiane':'Vientiane','Canberra':'Canberra','Sydney':'Sidney','Melbourne':'Melburn',
 'Wellington':'Wellington','Pretoria':'Pretorya','Cape Town':'Kap Şehri','Johannesburg':'Johannesburg','Windhoek':'Windhoek',
 'Luanda':'Luanda','Dakar':'Dakar','Lagos':'Lagos','Accra':'Akra','Abidjan':'Abidjan','Brazzaville':'Brazzaville',
 'Antananarivo':'Tananarive','Djibouti':'Cibuti','Mogadishu':'Mogadişu','Asmara':'Asmara','Havana':'Havana',
 'Montevideo':'Montevideo','Asuncion':'Asunción','La Paz':'La Paz','Quito':'Kito','Panama City':'Panama','San Jose':'San José',
}
SKIP_MODERN = {'Brasilia','Abuja','Islamabad','Canberra_x','Dodoma','Gaborone','Lilongwe','Yamoussoukro','Naypyidaw','Astana',
               'Putrajaya','Sri Jayawardenepura Kotte','Nouakchott','Lusaka_x','Kigali','Bujumbura','Juba','Ngerulmud','Palikir'}

def ascii_key(name):
    s = unicodedata.normalize('NFKD', name).encode('ascii', 'ignore').decode().lower()
    s = re.sub(r'[^a-z0-9]+', '', s)
    return s or 'sehir'

country_cfg = {c[0]: c for c in COUNTRIES}
cities = []
taken = []
keys = set()

def add_city(name, lon, lat, owner, tier, prio):
    for (x, y, o) in taken:
        # Başkentler ve zorunlu şehirler sıkı yerleşebilir (Kudüs–Amman, Singapur–Johor); diğerleri seyrek.
        limit = 0.3 if prio <= 1 else (0.9 if o == owner else 0.4)
        if abs(x - lon) < limit and abs(y - lat) < limit and math.hypot(x - lon, y - lat) < limit:
            return False
    key = ascii_key(name)
    base, n = key, 2
    while key in keys:
        key = base + str(n); n += 1
    keys.add(key)
    taken.append((lon, lat, owner))
    cities.append(dict(key=key, name=name, lat=round(lat, 4), lon=round(lon, 4), tier=tier, port=False, country=owner, prio=prio))
    return True

# 2) Zorunlu şehirler (tarihî önemi büyük, nüfus sırasında kaçabilenler).
MUST = [('Rodos', 28.22, 36.43, 2), ('Hong Kong', 114.17, 22.3, 3), ('Hanoi', 105.85, 21.03, 3), ('Saygon', 106.7, 10.78, 3),
        ('Singapur', 103.85, 1.29, 3), ('Kudüs', 35.22, 31.78, 3), ('Leningrad', 30.31, 59.94, 4), ('Cebelitarık', -5.35, 36.14, 2),
        ('Süveyş', 32.55, 29.97, 2), ('Aden', 45.03, 12.8, 2), ('Kolombo', 79.86, 6.93, 3), ('Dakar', -17.44, 14.69, 3)]
HK = ('Hong Kong', 'britanya')
for name, lon, lat, tier in MUST:
    u = unit_at(lon, lat)
    owner = 'britanya' if name in ('Hong Kong', 'Cebelitarık', 'Aden') else ('italya' if name == 'Rodos' else (u['owner'] if u else None))
    if owner:
        add_city(name, lon, lat, owner, tier, 1)

# 1) Her birimin başkenti (CShapes); egemen devlet başkenti kademe 5, sömürge/manda merkezi 3.
sovereign_cap = {}
for c in COUNTRIES:
    sovereign_cap[c[0]] = c[4][0]
places = ne['features']
def ne_name_near(lon, lat, fallback):
    best, bd = None, 0.6
    for f in places:
        p = f['properties']
        d = math.hypot(p['longitude'] - lon, p['latitude'] - lat)
        if d < bd:
            best, bd = p, d
    if best is None:
        return fallback
    return best['name']

for u in sorted(units, key=lambda u: (u['name'] != sovereign_cap[u['owner']], -u['geom'].area)):
    if u['cap'] is None or u['caplon'] is None:
        continue
    name = RENAME.get(u['cap'], None) or RENAME.get(ne_name_near(u['caplon'], u['caplat'], u['cap']), None) or u['cap']
    name = name.split('(')[0].strip() if name.startswith('(') is False and '(' in name and name not in RENAME.values() else name
    metro = u['name'] == sovereign_cap[u['owner']]
    lon, lat = u['caplon'], u['caplat']
    # 1936 başkentleri (CShapes güncel başkenti verirse)
    hist = {'Turkey (Ottoman Empire)': ('Ankara', 32.85, 39.93), 'Brazil': ('Rio de Janeiro', -43.2, -22.9), 'China': ('Nankin', 118.78, 32.06),
            'India': ('Yeni Delhi', 77.2, 28.6), 'Australia': ('Canberra', 149.13, -35.28), 'Nigeria': ('Lagos', 3.39, 6.45),
            'Tanzania (Tanganyika)': ('Darüsselam', 39.28, -6.8), 'Pakistan': ('Karaçi', 67.0, 24.86),
            'Cote D\'Ivoire': ('Bingerville', -3.89, 5.36), 'Mauritania': ('Saint-Louis', -16.5, 16.03),
            'Botswana': ('Mafeking', 25.64, -25.86), 'Malawi': ('Zomba', 35.32, -15.38), 'Lithuania': ('Kaunas', 23.9, 54.9),
            'Russia (Soviet Union)': ('Moskova', 37.62, 55.75), 'Cameroon': ('Yaoundé', 11.5, 3.87), 'Libya': ('Trablusgarp', 13.18, 32.89)}
    if u['name'] in hist:
        name, lon, lat = hist[u['name']]
    add_city(name, lon, lat, u['owner'], 5 if metro else 3, 0 if metro else 1)

# 3) Büyük şehirler: küresel nüfus sırasıyla (imparatorluklar kotaya takılmaz) toplam BUDGET'e kadar; aralık aynı ülkede
#    ≥ 0,9°, farklı ülkede ≥ 0,4°. 4) Kapsama: alanına göre seyrek kalan birimlere (Sibirya, Sahra, Kanada…) ek şehir.
BUDGET = 560
cands = []
for f in places:
    p = f['properties']
    if p['name'] in SKIP_MODERN or p['pop_max'] is None or p['pop_max'] < 100000:
        continue
    cands.append(p)
cands.sort(key=lambda p: (-p['pop_max'], p['name']))
def tier_of(pop):
    return 4 if pop >= 3_000_000 else 3 if pop >= 800_000 else 2
# 3a) Önce egemen devlet başına taban kota (alan ve nüfusla; Avrupa ve Türkiye gibi yoğun ülkeler küresel sırada
#     imparatorluk şehirlerine yenilmesin), 3b) sonra küresel sıra.
area = collections.Counter()
for u in units:
    area[u['owner']] += u['geom'].area
quota = {c[0]: min(1 + int(round(math.sqrt(area[c[0]]) / 2.6 + math.sqrt(c[3]) / 40)), 18) for c in COUNTRIES}
have0 = collections.Counter(x['country'] for x in cities)
for p in cands:
    u = unit_at(p['longitude'], p['latitude'])
    if u is None or u['name'] != sovereign_cap[u['owner']] or have0[u['owner']] >= quota[u['owner']]:
        continue
    if add_city(RENAME.get(p['name'], p['name']), p['longitude'], p['latitude'], u['owner'], tier_of(p['pop_max']), 2):
        have0[u['owner']] += 1
for p in cands:
    if len(cities) >= BUDGET - 25:
        break
    u = unit_at(p['longitude'], p['latitude'])
    if u is None:
        continue
    add_city(RENAME.get(p['name'], p['name']), p['longitude'], p['latitude'], u['owner'], tier_of(p['pop_max']), 2)
unit_count = collections.Counter()
for c in cities:
    u = unit_at(c['lon'], c['lat'])
    if u is not None:
        unit_count[u['name']] += 1
for u in sorted(units, key=lambda u: -u['geom'].area):
    want = int(math.sqrt(u['geom'].area) / 4.5)
    if unit_count[u['name']] >= want:
        continue
    for p in cands:
        if unit_count[u['name']] >= want or len(cities) >= BUDGET:
            break
        if not u['geom'].contains(Point(p['longitude'], p['latitude'])):
            continue
        if add_city(RENAME.get(p['name'], p['name']), p['longitude'], p['latitude'], u['owner'], tier_of(p['pop_max']), 2):
            unit_count[u['name']] += 1
have = collections.Counter(x['country'] for x in cities)

# Liman: kıyıya ≤ 0,15°.
coast = land.boundary
for c in cities:
    c['port'] = coast.distance(Point(c['lon'], c['lat'])) < 0.15
missing = [c[0] for c in COUNTRIES if have[c[0]] == 0 and not any(x['country'] == c[0] for x in cities)]
print('şehirsiz:', missing, file=sys.stderr)
print('şehir', len(cities), 'liman', sum(c['port'] for c in cities), file=sys.stderr)
print(sorted(collections.Counter(x['country'] for x in cities).items(), key=lambda t: -t[1])[:20], file=sys.stderr)
cities.sort(key=lambda c: (country_cfg[c['country']][1], c['prio'], -c['tier'], c['key']))
for c in cities:
    del c['prio']

cfg = json.load(open(os.path.join(REPO, 'Tools/ContentCompiler/data/1936_mvp.json'), encoding='utf-8'))
cfg['scenario'] = 'dunya1936'
cfg['title'] = '1936 Dünya'
cfg['keyPrefix'] = 'd1936'
cfg['date'] = [1936, 3, 7]
cfg['bbox'] = [-180.0, -56.0, 180.0, 84.0]
cfg['countries'] = [dict(key=c[0], id=c[1], name=c[2], sources=c[4], population=c[3]) for c in COUNTRIES]
cfg['cities'] = cities
cfg['rails'] = []
cfg['islands'] = []
cfg['straits'] = []
cfg['seaGridStep'] = 3.0
cfg['maxRoadKm'] = 1200
cfg['portLinkDegrees'] = 4.5
cfg['portLandCrossing'] = 1.5
cfg['corrections'] = [
 cfg['corrections'][0], cfg['corrections'][1],
 {"kind": "transfer", "from": "cin", "to": "britanya", "note": "Hong Kong 1842/1898'den beri İngiliz; CShapes Çin'de gösterir.", "polygon": [[113.82, 22.15], [114.45, 22.15], [114.45, 22.56], [113.82, 22.56]]},
 {"kind": "note", "note": "Dünya 1936: CShapes 2.0'ın 07.03.1936'da geçerli 170 birimi (bağımsız devletler, sömürgeler, mandalar, himayeler) yöneten egemen devlete bağlanır (Tools/ContentCompiler/data/dunya1936.json 'sources'). Dominyonlar (Kanada, Avustralya, Yeni Zelanda, Güney Afrika, İrlanda) ayrı devlettir; Danzig Serbest Şehri fiilen Nazi yönetiminde olduğundan Almanya'ya, Namibya mandası Güney Afrika'ya, Yeni Gine mandası Avustralya'ya, Filipinler (Commonwealth) ABD'ye bağlanır. Mançukuo ayrı tutulmaz (CShapes Çin'de gösterir)."},
 {"kind": "note", "note": "Şehirler: her CShapes biriminin başkenti (1936 başkentleriyle düzeltilmiş: Ankara, Rio de Janeiro, Nankin, Lagos…) ve Natural Earth 'populated places' (kamu malı) listesinden ülke kotasıyla en büyük şehirler (aralık ≥ 0,9°). Adlar 1936 kullanımı ve Türkçe karşılıklarla (Leningrad, Stalingrad, Batavya, Leopoldville…). Nüfus modern değerden kademe tahminidir."},
]
cfg['seaLinks'] = [
 {"name": "Boğazlar", "points": [[31.0, 41.6], [29.08, 41.2], [28.9, 40.85], [28.0, 40.7], [27.0, 40.55], [26.5, 40.25], [26.05, 40.0], [25.3, 39.8]]},
 {"name": "Cebelitarık", "points": [[-7.5, 35.9], [-5.6, 35.95], [-4.0, 36.1], [-2.0, 36.3]]},
 {"name": "Süveyş Kanalı", "points": [[32.3, 31.8], [32.35, 31.3], [32.45, 30.6], [32.55, 29.9], [32.9, 28.5], [34.0, 27.0]]},
 {"name": "Panama Kanalı", "points": [[-79.9, 9.6], [-79.9, 9.35], [-79.6, 9.0], [-79.5, 8.6], [-79.5, 7.5]]},
 {"name": "Danimarka boğazları", "points": [[11.0, 57.8], [11.2, 56.8], [11.0, 55.8], [11.3, 54.9], [13.0, 54.8], [15.0, 55.2]]},
 {"name": "Malakka", "points": [[98.0, 6.0], [99.5, 4.2], [101.0, 2.6], [103.0, 1.2], [104.5, 1.3], [105.5, 2.0]]},
 {"name": "Kerç", "points": [[36.4, 45.1], [36.55, 45.35], [36.8, 45.6]]},
 {"name": "Hürmüz", "points": [[52.0, 26.5], [55.0, 26.6], [56.4, 26.4], [57.5, 25.4], [58.5, 24.5]]},
 {"name": "Babülmendep", "points": [[40.5, 15.5], [42.5, 13.5], [43.4, 12.6], [44.5, 12.0], [46.0, 12.0]]},
 {"name": "Manş", "points": [[-5.5, 49.0], [-3.0, 49.8], [0.0, 50.2], [1.5, 51.0], [3.0, 51.7]]},
]
json.dump(cfg, open(os.path.join(REPO, 'Tools/ContentCompiler/data/dunya1936.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
