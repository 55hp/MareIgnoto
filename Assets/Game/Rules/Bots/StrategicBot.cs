using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Bots
{
    /// <summary>
    /// Bot che gioca con un <see cref="BotProfile"/> (09_bot.md §2, §4). Legge solo la decisione e il proprio
    /// <see cref="PlayerView"/>; le decisioni che il profilo non copre si risolvono come <see cref="RandomBot"/>.
    /// Ha una casualità propria, separata da quella della partita (<see cref="SeedFor"/>), per le parità.
    /// </summary>
    public sealed class StrategicBot : IBot
    {
        private readonly IRandomSource random;

        public BotProfile Profile { get; }

        public StrategicBot(BotProfile profile, IRandomSource random)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Il seed della casualità del bot, derivato dal seed della partita e dal posto (09 §1).</summary>
        public static int SeedFor(int gameSeed, int seat) => unchecked(gameSeed * 7919 + (seat + 1) * 104729);

        public DecisionAnswer Choose(PendingDecision decision, PlayerView view)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (view == null) throw new ArgumentNullException(nameof(view));

            DecisionOption choice = Policy(decision, view) ?? BotTools.Pick(decision.Options, random);
            return decision.Choose(choice);
        }

        /// <summary>L'opzione del profilo per questa decisione, oppure null per "a caso".</summary>
        private DecisionOption Policy(PendingDecision d, PlayerView view)
        {
            switch (d.Kind)
            {
                case DecisionKind.GartyaOffer: return Offer(d);
                case DecisionKind.KeepMissions: return KeepOneMission(d);
                case DecisionKind.ChooseHeading: return Route(d, view);
                case DecisionKind.WeatherPirateLoss: return LowestPirate(d);
                case DecisionKind.StormCrewLoss:
                case DecisionKind.ManOverboardDiscard: return LowestCrew(d);
                case DecisionKind.SeaAction: return SeaAction(d, view);
                case DecisionKind.PortAction: return Port(d, view);
                case DecisionKind.LookoutChoice: return Lookout(d, view);
                case DecisionKind.CombatResponse: return Combat(d);
                case DecisionKind.BoardingDefense: return Boarding(d, view);
                case DecisionKind.ReceiveCrew: return ReceiveCrew(d, view);
                case DecisionKind.CardTarget: return CardTarget(d, view);
                default: return null;
            }
        }

        // ---- Preparazione ----

        private DecisionOption Offer(PendingDecision d)
        {
            var offers = d.Options.OfType<OfferOption>().ToList();
            if (offers.Count == 0) return null;
            return Profile.OffersEverything ? offers.OrderByDescending(o => o.Amount).First() : offers.OrderBy(o => o.Amount).First();
        }

        /// <summary>Ne tiene una sola, a caso (o il minimo consentito, se è di più).</summary>
        private DecisionOption KeepOneMission(PendingDecision d)
        {
            var keeps = d.Options.OfType<KeepMissionsOption>().ToList();
            if (keeps.Count == 0) return null;
            int fewest = keeps.Min(k => k.Kept.Count);
            return BotTools.Pick(keeps.Where(k => k.Kept.Count == fewest).ToList(), random);
        }

        // ---- Rotta (09 §3) ----

        private DecisionOption Route(PendingDecision d, PlayerView view)
        {
            GameMap map = view.State.Map;
            List<Coord> goals;
            List<Coord> forbidden = null;
            if (Profile.Goal == BotGoal.SacredIsland)
            {
                goals = BotTools.SacredIslandCells(map);
                if (Profile.CautiousNearSacredIsland && BotTools.WouldLoseTakingTreasure(view))
                {
                    // Modalità cauta: l'Isola Sacra si evita, la meta è il porto più vicino senza il proprio segnalino.
                    forbidden = goals;
                    goals = BotTools.PortCells(map, view.Self.IslandMarker);
                }
            }
            else
            {
                List<int> ships = BotTools.ShipsAtSea(view);
                if (ships.Count > 0)
                {
                    Coord own = view.Self.Position;
                    int nearest = ships.Min(p => Coord.Distance(view.State.Player(p).Position, own));
                    int target = BotTools.ChooseTarget(view, ships.Where(p => Coord.Distance(view.State.Player(p).Position, own) == nearest), random);
                    goals = new List<Coord> { view.State.Player(target).Position };
                }
                else
                {
                    goals = BotTools.PortCells(map, view.Self.IslandMarker);
                }
            }

            Heading heading = BotTools.ChooseHeading(view, goals, Profile, random, forbidden);
            return d.Options.OfType<HeadingOption>().FirstOrDefault(o => o.Heading == heading);
        }

        // ---- Perdite ----

        private DecisionOption LowestPirate(PendingDecision d)
        {
            var cards = d.Options.OfType<PirateCardOption>().ToList();
            if (cards.Count == 0) return null;
            int worst = cards.Max(o => BotTools.PirateRank(o.Card, Profile));
            return BotTools.Pick(cards.Where(o => BotTools.PirateRank(o.Card, Profile) == worst).ToList(), random);
        }

        private DecisionOption LowestCrew(PendingDecision d)
        {
            var slots = d.Options.OfType<CrewSlotOption>().ToList();
            if (slots.Count == 0) return null;
            int worst = slots.Max(o => BotTools.CrewRank(o.Card, Profile));
            return BotTools.Pick(slots.Where(o => BotTools.CrewRank(o.Card, Profile) == worst).ToList(), random);
        }

        // ---- Turno ----

        /// <summary>
        /// Turno in mare (09 §4). Cacciatore: attacco al bersaglio, poi Uomo in mare! e Spyglass!. Poi, per tutti, uno swap
        /// utile, poi la pesca. Pescare è possibile solo come prima azione (R-056): dopo uno swap il turno finisce.
        /// </summary>
        private DecisionOption SeaAction(PendingDecision d, PlayerView view)
        {
            if (Profile.Attacks)
            {
                var attacks = d.Options.OfType<AttackOption>().ToList();
                if (attacks.Count > 0)
                {
                    int target = BotTools.ChooseTarget(view, attacks.Select(a => a.Target).Distinct(), random);
                    foreach (AttackOpening opening in new[] { AttackOpening.FalconetDuel, AttackOpening.FreeBroadside, AttackOpening.HandBroadside, AttackOpening.Arrembaggio })
                    {
                        AttackOption attack = attacks.FirstOrDefault(a => a.Target == target && a.Opening == opening);
                        if (attack != null) return attack;
                    }
                }

                PlayCardOption play = d.Options.OfType<PlayCardOption>()
                    .FirstOrDefault(o => o.Card.Id == PirateCardId.UomoInMare || o.Card.Id == PirateCardId.Spyglass);
                if (play != null) return play;
            }

            SwapOption swap = BotTools.BestSwap(view, d.Options.OfType<SwapOption>(), Profile, random);
            if (swap != null) return swap;

            // TODO 09 §4: "swap utile, poi pesca 2" non si può fare nello stesso turno (R-056: pescare chiude il turno ed è
            // solo la prima azione). Il bot fa lo swap e chiude; altrimenti pesca. Domanda nel report della spec 0011.
            return (DecisionOption)d.Options.OfType<DrawCardsOption>().FirstOrDefault() ?? d.Options.OfType<EndTurnOption>().FirstOrDefault();
        }

        private DecisionOption Port(PendingDecision d, PlayerView view)
        {
            var actions = d.Options.OfType<PortActionOption>().ToList();
            if (Profile.RecruitsWhenUnarmed && view.Self.Coins >= Profile.RecruitMinCoins &&
                !view.Self.CrewAbove.Any(c => c != null && Profile.CombatCrew.Contains(c.Kind)))
            {
                PortActionOption recruit = actions.FirstOrDefault(a => a.Action == PortAction.RecruitOne);
                if (recruit != null) return recruit;
            }

            return actions.FirstOrDefault(a => a.Action == PortAction.Plunder);
        }

        private DecisionOption Lookout(PendingDecision d, PlayerView view)
        {
            bool port = Profile.LookoutPortWithoutTarget && BotTools.TargetsInRange(view).Count == 0;
            return d.Options.OfType<TurnKindOption>().FirstOrDefault(o => o.Kind == (port ? TurnKind.Port : TurnKind.Sea));
        }

        // ---- Combattimento ----

        /// <summary>
        /// Rush: Parlè! se ce l'ha, altrimenti cede. Cacciatore: continua finché ha carte (prima le Bordate gratuite).
        /// </summary>
        private DecisionOption Combat(PendingDecision d)
        {
            var responses = d.Options.OfType<CombatResponseOption>().ToList();
            CombatResponseOption giveUp = responses.FirstOrDefault(r => r.Yield);
            if (Profile.KeepsFighting)
                return responses.Where(r => !r.Yield).OrderBy(r => r.Card == null ? 0 : 1).FirstOrDefault() ?? giveUp;
            return responses.FirstOrDefault(r => !r.Yield && r.Play == CombatPlay.Parle) ?? giveUp;
        }

        private DecisionOption Boarding(PendingDecision d, PlayerView view)
        {
            var defenses = d.Options.OfType<BoardingDefenseOption>().ToList();
            BoardingDefenseOption parle = defenses.FirstOrDefault(o => o.Defense == BoardingDefense.Parle);
            if (parle != null) return parle;
            BoardingDefenseOption pay = defenses.FirstOrDefault(o => o.Defense == BoardingDefense.Paid);
            if (pay != null && view.Self.Coins >= Profile.RansomMinCoins) return pay;
            return defenses.FirstOrDefault(o => o.Defense == BoardingDefense.None);
        }

        // ---- Ciurma ricevuta (09 §3) ----

        /// <summary>
        /// In uno slot vuoto: sopra coperta se la carta è nella priorità del profilo, altrimenti sotto. Con gli slot pieni
        /// sostituisce la carta di priorità più bassa, oppure scarta quella in arrivo se è lei la più bassa.
        /// </summary>
        private DecisionOption ReceiveCrew(PendingDecision d, PlayerView view)
        {
            var options = d.Options.OfType<ReceiveCrewOption>().ToList();
            var card = d.Cards.OfType<CrewCard>().FirstOrDefault();
            if (options.Count == 0 || card == null) return null;
            int above = view.Self.CrewAbove.Count;
            int incoming = BotTools.CrewRank(card, Profile);

            var placed = options.Where(o => o.Outcome == ReceiveOutcome.Placed).ToList();
            if (placed.Count > 0)
            {
                bool wantsAbove = incoming < Profile.CrewPriority.Count;
                var preferred = placed.Where(o => (o.Slot < above) == wantsAbove).ToList();
                return BotTools.Pick(preferred.Count > 0 ? preferred : placed, random);
            }

            var replaced = options.Where(o => o.Outcome == ReceiveOutcome.Replaced).ToList();
            ReceiveCrewOption discard = options.FirstOrDefault(o => o.Outcome == ReceiveOutcome.Discarded);
            if (replaced.Count == 0) return discard;
            int worst = replaced.Max(o => BotTools.CrewRank(BotTools.OwnCrewAt(view, o.Slot), Profile));
            if (discard != null && incoming >= worst) return discard;
            return BotTools.Pick(replaced.Where(o => BotTools.CrewRank(BotTools.OwnCrewAt(view, o.Slot), Profile) == worst).ToList(), random);
        }

        // ---- Bersagli delle carte ----

        private DecisionOption CardTarget(PendingDecision d, PlayerView view)
        {
            var players = d.Options.OfType<TargetPlayerOption>().ToList();
            if (players.Count > 0)
            {
                int target = BotTools.ChooseTarget(view, players.Select(o => o.Target), random);
                return players.First(o => o.Target == target);
            }

            var pairs = d.Options.OfType<SpyglassOption>().ToList();
            if (pairs.Count > 0)
            {
                int target = BotTools.ChooseTarget(view, pairs.Select(o => o.Target).Distinct(), random);
                return BotTools.Pick(pairs.Where(o => o.Target == target).ToList(), random);
            }

            return null;
        }
    }
}
