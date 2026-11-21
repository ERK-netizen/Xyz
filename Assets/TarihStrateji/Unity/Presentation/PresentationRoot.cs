using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.Input;
using TarihStrateji.Unity.Presentation.Map;
using TarihStrateji.Unity.Presentation.UI;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation
{
    /// <summary>
    /// Sunum katmanının kökü: sahne yüklendikten sonra oturumu (<see cref="IGameSession"/>, Bootstrap
    /// uygular) arayüz üzerinden bulur ve harita kamerasını, ülke haritasını (bölge çokgenleri, yalnız ülke
    /// sınırları), şehir/birlik işaretlerini, emir önizlemesinin rotasını, şehir adı ve yığın sayısı yazılarını, dokunmatik denetimi ve dikey arayüzü kurar. Sunum derlemesi yalnız Domain sözleşmelerine
    /// başvurur (CLAUDE.md §2.2); oyun kuralı yazmaz.
    /// </summary>
    public sealed class PresentationRoot : MonoBehaviour
    {
        public const string MapShaderResource = "Shaders/HaritaRenk";
        private const float StartOrthographicSize = 6f;
        private const float TapTolerancePixels = 40f;

        private Material _material;
        private IGameSession _session;
        private MapCameraController _controller;
        private bool _rebuildPending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnStartup()
        {
            if (FindAnyObjectByType<PresentationRoot>() != null)
            {
                return;
            }

            IGameSession session = FindSession();
            if (session == null)
            {
                Debug.LogError("Oyun oturumu (IGameSession) bulunamadı; sunum kurulmadı.");
                return;
            }

            var host = new GameObject("TarihStrateji.Sunum");
            DontDestroyOnLoad(host);
            host.AddComponent<PresentationRoot>().Initialize(session);
        }

        private static IGameSession FindSession()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IGameSession session)
                {
                    return session;
                }
            }

            return null;
        }

        public void Initialize(IGameSession session)
        {
            if (_session == null)
            {
                _session = session;
                session.SessionReplaced += () => _rebuildPending = true;
            }

            IGameReadModel model = session.ReadModel;
            Camera mapCamera = CreateCamera();
            _material = CreateMaterial();
            var selection = new SelectionModel();
            var flow = new OrderFlow();
            Localizer text = Localizer.LoadTurkish();

            var map = new GameObject("Harita");
            map.transform.SetParent(transform, false);
            CountryMapView countries = map.AddComponent<CountryMapView>();
            countries.Initialize(model, mapCamera, _material);

            var markers = new GameObject("Isaretler");
            markers.transform.SetParent(transform, false);
            markers.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            MarkerLayer markerLayer = markers.AddComponent<MarkerLayer>();
            markerLayer.Initialize(model, selection, mapCamera, _material);

            var hudHost = new GameObject("Arayuz");
            hudHost.transform.SetParent(transform, false);
            HudController hud = hudHost.AddComponent<HudController>();
            hud.Initialize(session, selection, flow, text);

            var route = new GameObject("Rota");
            route.transform.SetParent(transform, false);
            route.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            route.AddComponent<RouteLayer>().Initialize(flow, mapCamera, _material);

            var labels = new GameObject("HaritaYazilari");
            labels.transform.SetParent(transform, false);
            labels.AddComponent<MapLabelLayer>().Initialize(model, markerLayer, countries, mapCamera, text, hud.MapOverlay);

            MapCameraController controller = mapCamera.gameObject.AddComponent<MapCameraController>();
            _controller = controller;
            controller.Initialize(mapCamera, countries.Bounds);
            if (MapProjection.IsWorldWide(countries.Bounds))
            {
                // Dünya haritası (Adım 21): kamera ±180°'de sarmalanır, harita/işaret/rota ağları dikişin iki yanında kopyalanır.
                controller.WrapsHorizontally = true;
                gameObject.AddComponent<MapWrapRenderer>().Initialize(mapCamera, map.transform, markers.transform, route.transform);
            }

            controller.IsOverUi = hud.IsOverUi;
            controller.Tapped += world =>
            {
                float tolerance = (mapCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height)) * TapTolerancePixels;
                selection.Tap(markerLayer.Picker.Pick(model, new Vector2(MapProjection.Canonical(world.x), world.y), tolerance));
            };
            controller.CenterOn(StartPoint(model, countries.Bounds));
        }

        private static Vector2 StartPoint(IGameReadModel model, Rect bounds)
        {
            var cities = new List<CityView>();
            model.GetCities(cities);
            for (int i = 0; i < cities.Count; i++)
            {
                if (cities[i].Owner == model.PlayerCountry)
                {
                    return MapProjection.ToWorld(cities[i].Location);
                }
            }

            return bounds.center;
        }

        private static Camera CreateCamera()
        {
            Camera mapCamera = Camera.main;
            if (mapCamera == null)
            {
                var host = new GameObject("HaritaKamerasi") { tag = "MainCamera" };
                DontDestroyOnLoad(host);
                mapCamera = host.AddComponent<Camera>();
            }

            mapCamera.orthographic = true;
            mapCamera.orthographicSize = StartOrthographicSize;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = MapPalette.Background;
            mapCamera.nearClipPlane = 0.1f;
            mapCamera.farClipPlane = 50f;
            mapCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            return mapCamera;
        }

        private static Material CreateMaterial()
        {
            Shader shader = Resources.Load<Shader>(MapShaderResource);
            if (shader == null)
            {
                Debug.LogError("Harita gölgelendiricisi bulunamadı: Resources/" + MapShaderResource);
                shader = Shader.Find("Sprites/Default");
            }

            return new Material(shader) { name = "HaritaRenk" };
        }

        /// <summary>
        /// Yeni oyun (başka senaryo/harita): tüm katmanlar ve kamera denetimi yıkılıp aynı oturumla yeniden kurulur.
        /// Olay bir düğme geri çağrısından gelebileceği için yıkım bir sonraki karede yapılır.
        /// </summary>
        private void LateUpdate()
        {
            if (!_rebuildPending)
            {
                return;
            }

            _rebuildPending = false;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            if (_controller != null)
            {
                Destroy(_controller);
            }

            if (_material != null)
            {
                Destroy(_material);
            }

            Initialize(_session);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
