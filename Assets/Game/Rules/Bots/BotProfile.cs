using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Cards;

namespace hp55games.MareIgnoto.Rules.Bots
{
    /// <summary>La meta della rotta di un profilo (09_bot.md §4).</summary>
    public enum BotGoal
    {
        /// <summary>L'Isola Sacra (Rush), salvo la modalità cauta.</summary>
        SacredIsland,
        /// <summary>La nave avversaria in mare (Cacciatore); senza navi in mare, il porto più vicino.</summary>
        Hunt,
    }

    /// <summary>
    /// Un profilo di bot strategico (09_bot.md §2, §4): dati immutabili. Le politiche le applica <see cref="StrategicBot"/>.
    /// I profili della versione 1 sono <see cref="Rush"/> e <see cref="Hunter"/>.
    /// </summary>
    public sealed class BotProfile
    {
        /// <summary>Nome descrittivo, per report e UI ("Rush", "Cacciatore").</summary>
        public string Name { get; private set; }
        public BotGoal Goal { get; private set; }

        /// <summary>Offerta a Gartya: tutte le monete (vero) o nessuna (falso).</summary>
        public bool OffersEverything { get; private set; }

        /// <summary>Priorità delle carte crew sopra coperta, dalla più alta. Le carte non elencate valgono come uno slot vuoto.</summary>
        public IReadOnlyList<CrewCardId> CrewPriority { get; private set; }

        /// <summary>Carte Pirateria da tenere, dalla più utile; si perde prima quella più in basso (le non elencate per prime).</summary>
        public IReadOnlyList<PirateCardId> PirateKeepPriority { get; private set; }

        /// <summary>Rush: non entra nell'Isola Sacra se perderebbe contro il minimo noto di un avversario (09 §4).</summary>
        public bool CautiousNearSacredIsland { get; private set; }

        /// <summary>Attacca un bersaglio a tiro e gioca Uomo in mare! e Spyglass! su di lui.</summary>
        public bool Attacks { get; private set; }

        /// <summary>Con la Vedetta sceglie il porto solo se non ha un bersaglio a tiro (altrimenti sceglie sempre il mare).</summary>
        public bool LookoutPortWithoutTarget { get; private set; }

        /// <summary>Nel botta e risposta continua finché ha carte (vero) o difende solo con Parlè! (falso).</summary>
        public bool KeepsFighting { get; private set; }

        /// <summary>In porto recluta se ha almeno <see cref="RecruitMinCoins"/> monete e nessuna carta di <see cref="CombatCrew"/> sopra coperta.</summary>
        public bool RecruitsWhenUnarmed { get; private set; }
        public int RecruitMinCoins { get; private set; }
        public IReadOnlyList<CrewCardId> CombatCrew { get; private set; }

        /// <summary>Contro Arrembaggio! paga il riscatto solo con almeno questo numero di monete.</summary>
        public int RansomMinCoins { get; private set; }

        /// <summary>Penalità di rotta per la cella d'arrivo in Tempesta o in Mare Mosso (09 §3).</summary>
        public int StormPenalty { get; private set; }
        public int RoughSeaPenalty { get; private set; }

        private BotProfile()
        {
        }

        /// <summary>Rosso — Rush (09 §4): dritto all'Isola Sacra, offre tutto, non attacca.</summary>
        public static readonly BotProfile Rush = new BotProfile
        {
            Name = "Rush",
            Goal = BotGoal.SacredIsland,
            OffersEverything = true,
            CrewPriority = new[] { CrewCardId.Timoniere, CrewCardId.Navigatore, CrewCardId.Vedetta, CrewCardId.Mozzo, CrewCardId.Jolly },
            // "La meno utile in difesa" si perde per prima: Parlè! è la più utile.
            PirateKeepPriority = new[]
            {
                PirateCardId.Parle, PirateCardId.VentoInPoppa, PirateCardId.FavoreGartya, PirateCardId.PescaFortunata,
                PirateCardId.ReteAStrascico,
            },
            CautiousNearSacredIsland = true,
            CombatCrew = new CrewCardId[0],
            RansomMinCoins = 8,
            StormPenalty = 4,
            RoughSeaPenalty = 1,
        };

        /// <summary>Giallo — Cacciatore (09 §4): insegue le navi avversarie e le attacca, offre 0.</summary>
        public static readonly BotProfile Hunter = new BotProfile
        {
            Name = "Cacciatore",
            Goal = BotGoal.Hunt,
            OffersEverything = false,
            CrewPriority = new[]
            {
                CrewCardId.Culverin, CrewCardId.Falconet, CrewCardId.Cannoniere, CrewCardId.Saker, CrewCardId.Nostromo,
                CrewCardId.Bucaniere,
            },
            PirateKeepPriority = new[]
            {
                PirateCardId.Bordata, PirateCardId.Arrembaggio, PirateCardId.UomoInMare, PirateCardId.Spyglass, PirateCardId.Parle,
                PirateCardId.PescaFortunata, PirateCardId.ReteAStrascico,
            },
            Attacks = true,
            LookoutPortWithoutTarget = true,
            KeepsFighting = true,
            RecruitsWhenUnarmed = true,
            RecruitMinCoins = 4,
            CombatCrew = new[] { CrewCardId.Culverin, CrewCardId.Falconet, CrewCardId.Cannoniere, CrewCardId.Saker },
            RansomMinCoins = 8,
            StormPenalty = 4,
            RoughSeaPenalty = 1,
        };

        public override string ToString() => Name;
    }
}
