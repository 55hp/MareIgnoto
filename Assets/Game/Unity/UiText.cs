using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Unity
{
    /// <summary>
    /// Tutti i testi mostrati al giocatore, in italiano (06_presentazione.md §5). Nessun componente o prefab scrive testo
    /// suo: lo chiede qui. Prima versione (spec 0005): nomi dei bot, vento, livelli delle zone, righe del log.
    /// </summary>
    public static class UiText
    {
        public static string BotName(int seat) => "Bot " + (seat + 1);

        /// <summary>
        /// I nomi dei posti per tipo di bot (spec 0011): "Bot Rush", "Bot Cacciatore", "Bot casuale 2". Se due posti hanno lo
        /// stesso profilo, il numero del posto li distingue ("Bot Rush 1", "Bot Rush 3").
        /// </summary>
        public static IReadOnlyList<string> BotNames(IReadOnlyList<SeatBot> seats)
        {
            var names = new List<string>();
            for (int seat = 0; seat < seats.Count; seat++)
            {
                SeatBot kind = seats[seat];
                bool duplicated = kind != SeatBot.Random && seats.Count(other => other == kind) > 1;
                names.Add(kind == SeatBot.Random || duplicated ? BotLabel(kind) + " " + (seat + 1) : BotLabel(kind));
            }

            return names;
        }

        private static string BotLabel(SeatBot kind)
        {
            switch (kind)
            {
                case SeatBot.Rush: return "Bot Rush";
                case SeatBot.Hunter: return "Bot Cacciatore";
                default: return "Bot casuale";
            }
        }

        public static string HeadingName(Heading heading)
        {
            switch (heading)
            {
                case Heading.N: return "Nord";
                case Heading.NE: return "Nord-Est";
                case Heading.E: return "Est";
                case Heading.SE: return "Sud-Est";
                case Heading.S: return "Sud";
                case Heading.SO: return "Sud-Ovest";
                case Heading.O: return "Ovest";
                default: return "Nord-Ovest";
            }
        }

        /// <summary>Etichetta della rosa dei venti (UI_TopBar).</summary>
        public static string Wind(Heading wind) => "Vento: " + HeadingName(wind);

        /// <summary>Il numero scritto su una zona meteo dal livello 1 in su (ZoneOverlayView).</summary>
        public static string ZoneLevel(int level) => level.ToString();

        public static string WeatherName(WeatherState weather)
        {
            switch (weather)
            {
                case WeatherState.RoughSea: return "Mare Mosso";
                case WeatherState.Storm: return "Tempesta";
                default: return "Normale";
            }
        }

        public static string PortActionName(PortAction action)
        {
            switch (action)
            {
                case PortAction.Plunder: return "Saccheggio";
                case PortAction.Leisure: return "Svago";
                case PortAction.RecruitOne: return "Reclutamento (tiene 1)";
                case PortAction.RecruitTwo: return "Reclutamento (tiene 2)";
                case PortAction.Mission: return "Missione";
                case PortAction.Commerce: return "Commercio";
                default: return "nessuna azione";
            }
        }

        /// <summary>
        /// La riga di log di un evento (06 §4: il log è sempre completo, anche senza animazioni), oppure null per gli eventi
        /// che non vanno nel log (i passi di movimento, le pescate, i cambi di slot...). <paramref name="state"/> serve solo
        /// per i dati fissi: nomi dei giocatori e id delle zone.
        /// </summary>
        public static string LogLine(GameEvent e, IReadOnlyGameState state)
        {
            string Name(int player) => player >= 0 && player < state.PlayerCount ? state.Player(player).Name : "?";
            string Names(IEnumerable<int> players) => string.Join(", ", players.Select(Name));

            switch (e)
            {
                case GameStartedEvent _: return "Inizia la partita.";
                case RoundStartedEvent r: return "— Round " + r.Round + " —";
                case PhaseStartedEvent p: return p.Phase == GamePhase.Preparation ? "Fase 1: rotte, meteo, movimento." : "Fase 2: i turni.";
                case OffersRevealedEvent o:
                    return "Offerte a Gartya: " + string.Join(", ", o.Amounts.Select((amount, player) => Name(player) + " " + amount)) + ".";
                case TurnOrderSetEvent t: return "Ordine di turno: " + Names(t.Order) + ".";
                case WindChangedEvent w: return "Il vento soffia verso " + HeadingName(w.Wind) + ".";
                case ZoneChangedEvent z:
                    return z.Changed
                        ? "La zona " + z.ZoneId + " passa dal livello " + z.FromLevel + " al " + z.ToLevel + "."
                        : "La zona " + z.ZoneId + " resta al livello " + z.ToLevel + ": carta sprecata.";
                case HeadingsRevealedEvent h:
                    return "Rotte: " + string.Join(", ", h.Headings.Select((heading, player) =>
                        Name(player) + " " + (heading.HasValue ? HeadingName(heading.Value) : "in Svago"))) + ".";
                case WeatherAppliedEvent w:
                    return Name(w.Player) + " è nella zona " + state.Map.ZoneId(w.Zone) + " (livello " + w.ZoneLevel + "): " +
                           WeatherName(w.Perceived) + ".";
                case HeadingRotatedEvent h: return Name(h.Player) + ": la rotta gira da " + HeadingName(h.From) + " a " + HeadingName(h.To) + ".";
                case ShipStoppedEvent s when s.Reason == StopReason.Collision:
                    return Name(s.Player) + " si ferma in " + s.Cell.Name + ": collisione.";
                case CrossingResolvedEvent c:
                    return Name(c.PlayerA) + " e " + Name(c.PlayerB) + " si incrociano: si fermano in " + c.Cell.Name + ".";
                case ShipStrandedEvent s: return Name(s.Player) + " si arena in " + s.Cell.Name + ".";
                case SacredIslandEnteredEvent s:
                    return Name(s.Player) + (s.ByBoarding ? ", spinto da un Abbordaggio," : "") + " raggiunge l'Isola Sacra!";
                case BoardingStartedEvent b: return "Abbordaggio in " + b.Cell.Name + ": " + Names(b.Players) + ".";
                case ShipRepositionedEvent r: return Name(r.Player) + " finisce in " + r.To.Name + ".";
                case CrewLostEvent c: return Name(c.Player) + " perde una carta crew.";
                case MedicoUsedEvent m: return Name(m.Player) + " usa il Medico.";
                case DieRolledEvent d: return (d.Player >= 0 ? Name(d.Player) + " tira il dado: " : "Dado: ") + d.Value + ".";
                case TurnStartedEvent t: return "Turno di " + Name(t.Player) + ".";
                case TurnSkippedEvent t: return Name(t.Player) + " salta il turno (" + SkipReason(t.Reason) + ").";
                case PortActionEvent p: return Name(p.Player) + " in porto: " + PortActionName(p.Action) + ".";
                case IslandMarkerPlacedEvent m: return Name(m.Player) + " mette il segnalino sull'isola " + m.IslandId + ".";
                case AttackStartedEvent a: return Name(a.Attacker) + " attacca " + Name(a.Defender) + ".";
                case BattleWonEvent b: return Name(b.Winner) + " vince la battaglia contro " + Name(b.Loser) + ".";
                case MissionCompletedEvent m: return Name(m.Player) + " completa una missione (+" + m.Reward + ").";
                case TreasureTakenEvent t: return Names(t.Players) + " prende il Tesoro.";
                case GameEndedEvent g: return "Fine della partita. Vince " + Names(g.Winners) + ".";
                default: return null;
            }
        }

        private static string SkipReason(TurnSkipReason reason)
        {
            switch (reason)
            {
                case TurnSkipReason.SacredIsland: return "è sull'Isola Sacra";
                case TurnSkipReason.OwnIslandMarker: return "è arrivato sull'isola del suo segnalino";
                default: return "è sulla cornice";
            }
        }
    }
}
