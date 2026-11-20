using System.Collections.Generic;
using System.Text;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Dikey telefon arayüzü (UI Toolkit, koddan kurulur): üstte tarih, oyun hızı ve ülke özeti; altta
    /// odaklanan şehir/bölgenin bilgisi, oradaki görünür birlikler ve emir düğmeleri. Düğmeler emri
    /// <see cref="IGameCommandGateway.Validate"/> ile önceden doğrular (geçersizse pasif); basınca
    /// <see cref="IGameCommandGateway.Submit"/> sonucu Türkçe bildirim olarak görünür. Durumu
    /// değiştirmez; yalnız emir gönderir (CLAUDE.md §2.1). Dokunma hedefleri ≥ 112 referans pikseli
    /// (1080 genişlikte ≈ 48 dp); güvenli alan iç boşluk olarak uygulanır.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        public const string ThemeResource = "UI/TarihTema";
        private const float RefreshInterval = 0.2f;
        private const float ToastSeconds = 2.5f;
        private const int TouchTarget = 112;

        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.12f, 0.9f);
        private static readonly Color ButtonColor = new Color(0.17f, 0.27f, 0.42f, 1f);
        private static readonly Color ActiveButtonColor = new Color(0.85f, 0.62f, 0.15f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.95f, 0.92f, 1f);

        private readonly List<UnitView> _units = new List<UnitView>(256);
        private readonly List<CityView> _cities = new List<CityView>(64);
        private readonly List<RegionView> _regions = new List<RegionView>(64);
        private CountryId _focusCountry;
        private CityId _focusCity;
        private readonly StringBuilder _builder = new StringBuilder(256);

        private IGameSession _session;
        private SelectionModel _selection;
        private Localizer _text;
        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _spacer;
        private VisualElement _top;
        private VisualElement _bottom;
        private Label _date;
        private Label _summary;
        private Label _title;
        private Label _info;
        private Label _toast;
        private VisualElement _unitList;
        private readonly Button[] _speedButtons = new Button[4];
        private static readonly int[] Speeds = { 0, 1, 2, 4 };
        private Button _move;
        private Button _attack;
        private Button _hold;
        private Button _deselect;
        private Button _recruit;
        private Button _declareWar;
        private Button _proposePeace;

        private bool _dirty = true;
        private float _nextRefresh;
        private float _toastUntil;
        private Rect _appliedSafeArea;
        private Vector2 _appliedPanelSize;

        public void Initialize(IGameSession session, SelectionModel selection, Localizer text)
        {
            _session = session;
            _selection = selection;
            _text = text;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "TarihStrateji.Panel";
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1080, 1920);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0f;
            if (settings.themeStyleSheet == null)
            {
                Debug.LogError("Arayüz teması bulunamadı: Resources/" + ThemeResource);
            }

            // UIDocument etkin olmadan panel ayarı atanır; böylece ilk OnEnable doğru panele bağlanır.
            gameObject.SetActive(false);
            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = settings;
            gameObject.SetActive(true);

            Build(_document.rootVisualElement);
            _session.ReadModel.StateChanged += OnStateChanged;
            _selection.Changed += MarkDirty;
        }

        /// <summary>Ekran noktası (piksel, alt-sol orijin) bir panelin üstünde mi (harita dokunuşunu engeller).</summary>
        public bool IsOverUi(Vector2 screenPosition)
        {
            IPanel panel = _root?.panel;
            if (panel == null)
            {
                return false;
            }

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            VisualElement picked = panel.Pick(panelPosition);
            return picked != null && picked != panel.visualTree && picked != _document.rootVisualElement && picked != _root && picked != _spacer;
        }

        public void ShowMessage(string key)
        {
            _toast.text = _text.Get(key);
            _toast.style.display = DisplayStyle.Flex;
            _toastUntil = Time.unscaledTime + ToastSeconds;
        }

        private void OnStateChanged(long version) => _dirty = true;

        private void MarkDirty()
        {
            _dirty = true;
            _nextRefresh = 0f;
        }

        private void Build(VisualElement documentRoot)
        {
            documentRoot.pickingMode = PickingMode.Ignore;
            _root = new VisualElement { name = "kok", pickingMode = PickingMode.Ignore };
            _root.style.flexGrow = 1;
            _root.style.flexDirection = FlexDirection.Column;
            documentRoot.Add(_root);

            _top = Panel("ust");
            var firstRow = Row();
            _date = MakeLabel(44, true);
            _date.style.flexGrow = 1;
            firstRow.Add(_date);
            string[] speedKeys = { "arayuz.duraklat", "arayuz.hiz1", "arayuz.hiz2", "arayuz.hiz4" };
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                int speed = Speeds[i];
                _speedButtons[i] = MakeButton(speedKeys[i], () => SetSpeed(speed));
                _speedButtons[i].style.minWidth = 140;
                firstRow.Add(_speedButtons[i]);
            }

            _top.Add(firstRow);
            _summary = MakeLabel(32, false);
            _summary.style.marginTop = 8;
            _top.Add(_summary);
            _root.Add(_top);

            _spacer = new VisualElement { name = "harita", pickingMode = PickingMode.Ignore };
            _spacer.style.flexGrow = 1;
            _spacer.style.justifyContent = Justify.FlexEnd;
            _spacer.style.alignItems = Align.Center;
            _toast = MakeLabel(34, true);
            _toast.pickingMode = PickingMode.Ignore;
            _toast.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            _toast.style.paddingLeft = 28;
            _toast.style.paddingRight = 28;
            _toast.style.paddingTop = 14;
            _toast.style.paddingBottom = 14;
            _toast.style.marginBottom = 20;
            SetRadius(_toast, 16);
            _toast.style.display = DisplayStyle.None;
            _spacer.Add(_toast);
            _root.Add(_spacer);

            _bottom = Panel("alt");
            _title = MakeLabel(38, true);
            _bottom.Add(_title);
            _info = MakeLabel(30, false);
            _info.style.marginTop = 6;
            _bottom.Add(_info);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.maxHeight = 380;
            scroll.style.marginTop = 10;
            _unitList = scroll.contentContainer;
            _bottom.Add(scroll);

            var actions = Row();
            actions.style.flexWrap = Wrap.Wrap;
            actions.style.marginTop = 10;
            _move = AddAction(actions, "arayuz.tasi", OnMove);
            _attack = AddAction(actions, "arayuz.saldir", OnAttack);
            _hold = AddAction(actions, "arayuz.bekle", OnHold);
            _deselect = AddAction(actions, "arayuz.secimiKaldir", () => _selection.ClearUnit());
            _recruit = AddAction(actions, "arayuz.topla", OnRecruit);
            _declareWar = AddAction(actions, "arayuz.savasIlan", OnDeclareWar);
            _proposePeace = AddAction(actions, "arayuz.barisOner", OnProposePeace);
            _bottom.Add(actions);
            _root.Add(_bottom);
        }

        private void LateUpdate()
        {
            if (_session == null)
            {
                return;
            }

            ApplySafeArea();
            if (_toast.style.display == DisplayStyle.Flex && Time.unscaledTime > _toastUntil)
            {
                _toast.style.display = DisplayStyle.None;
            }

            if (_dirty && Time.unscaledTime >= _nextRefresh)
            {
                _dirty = false;
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                Refresh();
            }
        }

        private void ApplySafeArea()
        {
            Rect area = Screen.safeArea;
            Vector2 panelSize = new Vector2(_root.layout.width, _root.layout.height);
            if (float.IsNaN(panelSize.x) || panelSize.x <= 0f || (area == _appliedSafeArea && panelSize == _appliedPanelSize))
            {
                return;
            }

            _appliedSafeArea = area;
            _appliedPanelSize = panelSize;
            Vector4 insets = SafeAreaMath.Insets(area, Screen.width, Screen.height, panelSize.x, panelSize.y);
            _top.style.paddingTop = 16 + insets.x;
            _bottom.style.paddingBottom = 16 + insets.y;
            _root.style.paddingLeft = insets.z;
            _root.style.paddingRight = insets.w;
        }

        private void Refresh()
        {
            IGameReadModel model = _session.ReadModel;
            _units.Clear();
            model.GetVisibleUnits(_units);
            _selection.Reconcile(_units);

            _date.text = DisplayFormat.Date(model.CurrentDate, _text);
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                _speedButtons[i].style.backgroundColor = Speeds[i] == _session.DaysPerSecond ? ActiveButtonColor : ButtonColor;
            }

            RefreshSummary(model);
            RefreshSelection(model);
        }

        private void RefreshSummary(IGameReadModel model)
        {
            if (!model.TryGetCountry(model.PlayerCountry, out CountryView country))
            {
                _summary.text = _text.Get("arayuz.gozlemci");
                return;
            }

            _builder.Clear();
            _builder.Append(CountryName(country.Id)).Append("  ·  ")
                .Append(_text.Get("arayuz.hazine")).Append(' ').Append(DisplayFormat.Whole(country.Treasury)).Append("  ·  ")
                .Append(_text.Get("arayuz.istikrar")).Append(' ').Append(DisplayFormat.Percent(country.Stability)).Append('\n')
                .Append(_text.Get("arayuz.nufus")).Append(' ').Append(DisplayFormat.Thousands(country.Population)).Append("  ·  ")
                .Append(_text.Get("arayuz.tumen")).Append(' ').Append(country.Divisions).Append("  ·  ")
                .Append(_text.Get(country.AtWar ? "arayuz.savasta" : "arayuz.barista"));
            _summary.text = _builder.ToString();
        }

        private void RefreshSelection(IGameReadModel model)
        {
            _unitList.Clear();
            MapPick focus = _selection.Focus;
            if (focus.Kind == MapPickKind.None)
            {
                _title.text = _text.Get("arayuz.secimYok");
                _info.text = string.Empty;
                HideAllActions();
                return;
            }

            _cities.Clear();
            model.GetCities(_cities);
            _regions.Clear();
            model.GetRegions(_regions);
            CityView city = default;
            bool hasCity = false;
            CityId cityId = focus.City;
            if (!cityId.IsValid && focus.Kind == MapPickKind.Unit)
            {
                cityId = CityAt(focus.Point);
            }

            for (int i = 0; i < _cities.Count && cityId.IsValid; i++)
            {
                if (_cities[i].Id == cityId)
                {
                    city = _cities[i];
                    hasCity = true;
                    break;
                }
            }

            RegionView region = default;
            bool hasRegion = false;
            for (int i = 0; i < _regions.Count && focus.Region.IsValid; i++)
            {
                if (_regions[i].Id == focus.Region)
                {
                    region = _regions[i];
                    hasRegion = true;
                    break;
                }
            }

            _builder.Clear();
            if (hasCity)
            {
                _title.text = _text.Get(city.NameKey);
                _builder.Append(_text.Get("arayuz.gelisim")).Append(": ").Append(city.DevelopmentLevel)
                    .Append("  ·  ").Append(_text.Get("arayuz.sahip")).Append(": ").Append(CountryName(city.Owner))
                    .Append("  ·  ").Append(_text.Get("arayuz.kontrol")).Append(": ").Append(CountryName(city.Controller));
                if (city.IsPort)
                {
                    _builder.Append("  ·  ").Append(_text.Get("arayuz.liman"));
                }

                if (city.IsBesieged)
                {
                    _builder.Append('\n').Append(_text.Get("arayuz.kusatma"));
                }
            }
            else if (hasRegion)
            {
                _title.text = CountryName(region.Owner);
                _builder.Append(_text.Get("arayuz.kontrol")).Append(": ").Append(CountryName(region.Controller));
                if (region.IsOccupied)
                {
                    _builder.Append("  ·  ").Append(_text.Get("arayuz.isgal"));
                }
            }
            else
            {
                _title.text = _text.Get("arayuz.deniz");
            }

            _info.text = _builder.ToString();
            FillUnits(focus, hasCity ? city.Location : focus.Point);
            RefreshActions(model, hasCity ? city : default, hasCity, hasRegion ? region : default, hasRegion);
        }

        private CityId CityAt(GeoPoint point)
        {
            for (int i = 0; i < _cities.Count; i++)
            {
                if (_cities[i].Location == point)
                {
                    return _cities[i].Id;
                }
            }

            return CityId.None;
        }

        /// <summary>Odaktaki yerde (aynı konumda) görünen birlikler.</summary>
        private void FillUnits(MapPick focus, GeoPoint place)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                UnitView unit = _units[i];
                bool here = focus.Kind == MapPickKind.Unit ? unit.Location == focus.Point : unit.Location == place;
                if (!here)
                {
                    continue;
                }

                _builder.Clear();
                _builder.Append(CountryName(unit.Owner)).Append(' ').Append(_text.Get(KindKey(unit.Kind))).Append(' ')
                    .Append(DisplayFormat.Percent(unit.Strength));
                if (!unit.IsExact)
                {
                    _builder.Append(" (").Append(_text.Get("arayuz.tahmini")).Append(')');
                }

                if (unit.IsOwn && unit.Kind == UnitViewKind.Land)
                {
                    ulong id = unit.Id;
                    bool selected = _selection.Unit == id;
                    Button select = MakeButton(null, () => _selection.SelectUnit(id));
                    select.text = (selected ? "▶ " : string.Empty) + _builder + "  —  " + _text.Get("arayuz.birlikSec");
                    select.style.backgroundColor = selected ? ActiveButtonColor : ButtonColor;
                    select.style.unityTextAlign = TextAnchor.MiddleLeft;
                    _unitList.Add(select);
                }
                else
                {
                    Label line = MakeLabel(30, false);
                    line.text = _builder.ToString();
                    line.style.marginTop = 6;
                    _unitList.Add(line);
                }
            }
        }

        private void RefreshActions(IGameReadModel model, CityView city, bool hasCity, RegionView region, bool hasRegion)
        {
            IGameCommandFactory orders = _session.Orders;
            bool unit = _selection.HasUnit;
            bool target = unit && _selection.HasTarget;
            Show(_move, target, target ? orders.MoveUnit(_selection.Unit, _selection.Target) : null);
            Show(_attack, target, target ? orders.Attack(_selection.Unit, _selection.Target) : null);
            Show(_hold, unit, unit ? orders.HoldPosition(_selection.Unit) : null);
            _deselect.style.display = unit ? DisplayStyle.Flex : DisplayStyle.None;

            CountryId player = model.PlayerCountry;
            bool ownCity = player.IsValid && hasCity && city.Owner == player && city.Controller == player;
            Show(_recruit, ownCity, ownCity ? orders.RecruitInfantry(city.Id) : null);

            CountryId other = TargetCountry(player, city, hasCity, region, hasRegion);
            bool foreign = player.IsValid && other.IsValid && other != player;
            bool atWar = foreign && model.IsAtWarWith(other);
            Show(_declareWar, foreign && !atWar, foreign && !atWar ? orders.DeclareWar(other) : null);
            Show(_proposePeace, atWar, atWar ? orders.ProposeWhitePeace(other) : null);
            _focusCountry = other;
            _focusCity = hasCity ? city.Id : CityId.None;
        }

        private static CountryId TargetCountry(CountryId player, CityView city, bool hasCity, RegionView region, bool hasRegion)
        {
            if (hasCity)
            {
                return city.Controller.IsValid && city.Controller != player ? city.Controller : city.Owner;
            }

            if (hasRegion)
            {
                return region.Controller.IsValid && region.Controller != player ? region.Controller : region.Owner;
            }

            return CountryId.None;
        }

        private void Show(Button button, bool visible, GameCommand command)
        {
            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible && command != null)
            {
                button.SetEnabled(_session.Commands.Validate(command).IsValid);
            }
        }

        private void HideAllActions()
        {
            _move.style.display = DisplayStyle.None;
            _attack.style.display = DisplayStyle.None;
            _hold.style.display = DisplayStyle.None;
            _deselect.style.display = DisplayStyle.None;
            _recruit.style.display = DisplayStyle.None;
            _declareWar.style.display = DisplayStyle.None;
            _proposePeace.style.display = DisplayStyle.None;
        }

        private void OnMove() => Submit(_session.Orders.MoveUnit(_selection.Unit, _selection.Target));

        private void OnAttack() => Submit(_session.Orders.Attack(_selection.Unit, _selection.Target));

        private void OnHold() => Submit(_session.Orders.HoldPosition(_selection.Unit));

        private void OnRecruit()
        {
            if (_focusCity.IsValid)
            {
                Submit(_session.Orders.RecruitInfantry(_focusCity));
            }
        }

        private void OnDeclareWar()
        {
            CountryId target = TargetCountry();
            if (target.IsValid)
            {
                Submit(_session.Orders.DeclareWar(target));
            }
        }

        private void OnProposePeace()
        {
            CountryId target = TargetCountry();
            if (target.IsValid)
            {
                Submit(_session.Orders.ProposeWhitePeace(target));
            }
        }

        private CountryId TargetCountry() => _focusCountry;

        private void Submit(GameCommand command)
        {
            CommandReceipt receipt = _session.Commands.Submit(command);
            ShowMessage(receipt.Accepted ? "arayuz.emirKabul" : receipt.Validation.ErrorKey);
            MarkDirty();
        }

        private void SetSpeed(int daysPerSecond)
        {
            _session.SetDaysPerSecond(daysPerSecond);
            MarkDirty();
        }

        private string CountryName(CountryId country) => country.IsValid ? _text.Get("ulke." + country.Value) : "—";

        private static string KindKey(UnitViewKind kind)
        {
            switch (kind)
            {
                case UnitViewKind.Fleet:
                    return "arayuz.filo";
                case UnitViewKind.AirWing:
                    return "arayuz.havaKanadi";
                default:
                    return "arayuz.karaBirligi";
            }
        }

        private Button AddAction(VisualElement row, string key, System.Action onClick)
        {
            Button button = MakeButton(key, onClick);
            button.style.display = DisplayStyle.None;
            row.Add(button);
            return button;
        }

        private Button MakeButton(string key, System.Action onClick)
        {
            var button = new Button(onClick) { text = key == null ? string.Empty : _text.Get(key) };
            button.style.minHeight = TouchTarget;
            button.style.fontSize = 32;
            button.style.color = TextColor;
            button.style.backgroundColor = ButtonColor;
            button.style.marginLeft = 6;
            button.style.marginRight = 6;
            button.style.marginTop = 6;
            button.style.marginBottom = 6;
            button.style.paddingLeft = 24;
            button.style.paddingRight = 24;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.whiteSpace = WhiteSpace.Normal;
            SetRadius(button, 14);
            return button;
        }

        private static Label MakeLabel(int size, bool bold)
        {
            var label = new Label();
            label.style.fontSize = size;
            label.style.color = TextColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            if (bold)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            return label;
        }

        private static VisualElement Panel(string name)
        {
            var panel = new VisualElement { name = name, pickingMode = PickingMode.Position };
            panel.style.backgroundColor = PanelColor;
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 16;
            panel.style.paddingBottom = 16;
            return panel;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.ReadModel.StateChanged -= OnStateChanged;
            }

            if (_selection != null)
            {
                _selection.Changed -= MarkDirty;
            }
        }
    }
}
