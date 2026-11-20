using System;
using System.Collections.Generic;
using TarihStrateji.Core.Simulation.Commands;
using TarihStrateji.Core.Simulation.Phases;

using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>
    /// Çekirdeğin bileşimi: emir işleyicileri, faz sistemleri, değişmez denetimleri ve otomatik
    /// kayıt. Bootstrap katmanı (ve testler) kurar. Aynı kurulum + aynı durum + aynı emirler →
    /// aynı sonuç.
    /// </summary>
    public sealed class SimulationSetup
    {
        private readonly List<ISimulationSystem> _systems = new List<ISimulationSystem>();
        private readonly List<IInvariantCheck> _invariants = new List<IInvariantCheck>();

        public SimulationSetup()
        {
            Commands = new CommandRegistry();
            Commands.Register(new SandboxSetCityDevelopmentHandler());
        }

        public CommandRegistry Commands { get; }

        public IReadOnlyList<ISimulationSystem> Systems => _systems;

        public IReadOnlyList<IInvariantCheck> Invariants => _invariants;

        public IAutosaveSink AutosaveSink { get; set; }

        /// <summary>Kaç günde bir otomatik kayıt; 0 = kapalı.</summary>
        public int AutosaveIntervalDays { get; set; }

        /// <summary>Kaç günde bir durum hash'i hesaplanır; 0 = kapalı. Varsayılan her gün.</summary>
        public int HashIntervalDays { get; set; } = 1;

        /// <summary>Kurulu tanım verisi (<see cref="StandardRules.Install"/> atar); okuma modeli birlik güçleri için kullanır.</summary>
        public Content.GameContent Content { get; set; }

        /// <summary>
        /// Coğrafi kayıt yüklerken değişmez dünyayı (çokgenler, şehirler, ulaşım grafı) aynı içerikten kuran
        /// üretici (ADR-011, §16.4); kayıtta yalnız geometrinin parmak izi vardır.
        /// </summary>
        public System.Func<World.Geography.GeoWorld> GeoWorldTemplate { get; set; }

        /// <summary>Senaryonun ülke ad anahtarları (sunum içindir; durum değildir). Boşsa <c>ulke.N</c>.</summary>
        public System.Func<CountryId, string> CountryNameKeys { get; set; }

        /// <summary>Senaryonun başkent tablosu (tanım verisidir, durum değildir): ülke → başkent şehri. Boşsa başkent yok.</summary>
        public System.Func<CountryId, CityId> Capitals { get; set; }

        public SimulationSetup AddSystem(ISimulationSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (!SimulationPhases.IsDefined(system.Phase))
            {
                throw new ArgumentOutOfRangeException(nameof(system), "Tanımsız faz.");
            }

            _systems.Add(system);
            return this;
        }

        public SimulationSetup AddInvariant(IInvariantCheck check)
        {
            _invariants.Add(check ?? throw new ArgumentNullException(nameof(check)));
            return this;
        }
    }
}
