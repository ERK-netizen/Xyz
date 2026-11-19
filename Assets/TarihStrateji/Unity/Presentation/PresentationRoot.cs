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
    /// uygular) arayüz üzerinden bulur ve harita kamerasını, parçalı haritayı, işaret katmanını,
    /// dokunmatik denetimi ve dikey arayüzü kurar. Sunum derlemesi yalnız Domain sözleşmelerine
    /// başvurur (CLAUDE.md §2.2); oyun kuralı yazmaz.
    /// </summary>
    public sealed class PresentationRoot : MonoBehaviour
    {
        public const string MapShaderResource = "Shaders/HaritaRenk";
        private const float StartOrthographicSize = 14f;

        private Material _material;

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
            IGameReadModel model = session.ReadModel;
            Camera mapCamera = CreateCamera();
            _material = CreateMaterial();
            var selection = new SelectionModel();
            Localizer text = Localizer.LoadTurkish();

            var map = new GameObject("Harita");
            map.transform.SetParent(transform, false);
            map.AddComponent<MapView>().Initialize(model, mapCamera, _material);

            var markers = new GameObject("Isaretler");
            markers.transform.SetParent(transform, false);
            markers.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            markers.AddComponent<MarkerLayer>().Initialize(model, selection, mapCamera, _material);

            var hudHost = new GameObject("Arayuz");
            hudHost.transform.SetParent(transform, false);
            HudController hud = hudHost.AddComponent<HudController>();
            hud.Initialize(session, selection, text);

            MapCameraController controller = mapCamera.gameObject.AddComponent<MapCameraController>();
            controller.Initialize(mapCamera, model.MapWidth, model.MapHeight);
            controller.IsOverUi = hud.IsOverUi;
            int width = model.MapWidth;
            int height = model.MapHeight;
            controller.Tapped += world => selection.Tap(HexLayout.Pick(world, width, height));
            controller.CenterOn(StartPoint(model));
        }

        private static Vector2 StartPoint(IGameReadModel model)
        {
            var cities = new List<CityView>();
            model.GetCities(cities);
            for (int i = 0; i < cities.Count; i++)
            {
                if (cities[i].Owner == model.PlayerCountry)
                {
                    return HexLayout.CellCenterOfIndex(cities[i].CellIndex, model.MapWidth);
                }
            }

            return cities.Count > 0
                ? HexLayout.CellCenterOfIndex(cities[0].CellIndex, model.MapWidth)
                : HexLayout.CellCenter(model.MapWidth / 2, model.MapHeight / 2);
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

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
