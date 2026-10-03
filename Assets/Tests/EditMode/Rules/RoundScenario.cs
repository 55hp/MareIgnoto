using System;
using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Bots;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Engine;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.Random;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Sorgente casuale con tiri di d8 accodabili durante la partita: finché la coda è vuota usa quella seedata.
    /// Così uno scenario fissa i tiri di un round senza toccare quelli del setup.
    /// </summary>
    internal sealed class QueuedRandom : IRandomSource
    {
        private readonly Queue<int> rolls = new Queue<int>();
        private readonly SeededRandom fallback;

        public QueuedRandom(int seed)
        {
            fallback = new SeededRandom(seed);
        }

        public int Pending => rolls.Count;

        public void Enqueue(params int[] values)
        {
            foreach (int value in values) rolls.Enqueue(value);
        }

        public int RollD8() => rolls.Count > 0 ? rolls.Dequeue() : fallback.RollD8();

        public int Range(int minInclusive, int maxExclusive) => fallback.Range(minInclusive, maxExclusive);

        public void Shuffle<T>(IList<T> list) => fallback.Shuffle(list);
    }

    /// <summary>
    /// Uno scenario di round sulla mappa di <see cref="TestSupport.StandardMap"/>: dopo il setup (giocato dal bot) svuota
    /// ciurme e mani, poi il test mette navi, carte, vento e zone dove gli servono e gioca un round con rotte date.
    /// Le carte si prendono dai mazzi e tornano negli scarti, così la conservazione resta vera.
    /// Mappa: il layout v4 (LayoutV4, 05_mappa.md §6). Tutte le zone partono a livello 0, anello compreso.
    /// </summary>
    internal sealed class RoundScenario
    {
        public GameSession Session { get; }
        public QueuedRandom Random { get; }
        public RulesConfig Config { get; }
        public List<PendingDecision> Decisions { get; } = new List<PendingDecision>();

        internal GameState State => Session.InternalState;

        private RoundScenario(GameSession session, QueuedRandom random, RulesConfig config)
        {
            Session = session;
            Random = random;
            Config = config;
        }

        public static RoundScenario Create(int players, RulesConfig config = null, int seed = 1)
        {
            config = config ?? new RulesConfig();
            var random = new QueuedRandom(seed);
            GameSession session = TestSupport.Start(players, seed, config, random);
            TestSupport.PlayWithBot(session, seed);
            var scenario = new RoundScenario(session, random, config);

            GameState state = scenario.State;
            foreach (PlayerState player in state.Players)
            {
                for (int slot = 0; slot < player.Crew.Count; slot++) scenario.ClearSlot(player.Id, slot);
                foreach (PirateCard card in player.Hand.ToList()) state.Pirate.Discard(card);
                player.Hand.Clear();
                // Le missioni del setup escono: ogni test dà quelle che gli servono (Mission).
                foreach (MissionCard card in player.Missions.ToList()) state.Corsair.Discard(card);
                player.Missions.Clear();
            }

            state.MissionProgress.Clear();

            state.Wind = Heading.N;
            // Tutte le zone a livello 0, anello compreso: ogni test imposta il meteo che gli serve.
            for (int zone = 0; zone < state.ZoneLevels.Length; zone++) state.ZoneLevels[zone] = 0;
            return scenario;
        }

        internal PlayerState P(int id) => State.PlayerById(id);

        public RoundScenario At(int player, int x, int y)
        {
            P(player).Position = new Coord(x, y);
            return this;
        }

        public RoundScenario Order(params int[] order)
        {
            State.TurnOrderList.Clear();
            State.TurnOrderList.AddRange(order);
            return this;
        }

        public RoundScenario Wind(Heading wind)
        {
            State.Wind = wind;
            return this;
        }

        /// <summary>Il livello della zona che contiene la cella (R-081).</summary>
        public RoundScenario ZoneAt(int x, int y, int level)
        {
            int zone = State.Map.ZoneOf(new Coord(x, y));
            if (zone < 0) throw new InvalidOperationException(new Coord(x, y).Name + " non è in una zona meteo.");
            State.ZoneLevels[zone] = level;
            return this;
        }

        /// <summary>La zona che contiene la cella al livello più basso con quell'effetto (Normale 0, Mare Mosso, Tempesta).</summary>
        public RoundScenario ZoneAt(int x, int y, WeatherState weather) =>
            ZoneAt(x, y, weather == WeatherState.Storm ? Config.stormLevel : weather == WeatherState.RoughSea ? Config.roughSeaLevel : 0);

        /// <summary>Il livello di una zona per id (05_mappa.md §3).</summary>
        public RoundScenario Zone(string id, int level)
        {
            State.ZoneLevels[State.Map.ZoneIndex(id)] = level;
            return this;
        }

        /// <summary>Mette nello slot una carta crew del tipo dato, presa dal mazzo o dagli scarti Crew.</summary>
        public CrewCard Crew(int player, int slot, CrewCardId kind)
        {
            ClearSlot(player, slot);
            CrewCard card = State.Crew.DrawPile.Concat(State.Crew.DiscardPile).First(c => c.Kind == kind);
            State.Crew.Remove(card);
            P(player).Crew.Set(slot, card);
            if (kind == CrewCardId.Nostromo && P(player).Crew.IsAbove(slot)) State.LockedNostromi.Add(card.Uid); // R-016
            return card;
        }

        public void ClearSlot(int player, int slot)
        {
            CrewCard old = P(player).Crew.Take(slot);
            if (old == null) return;
            State.LockedNostromi.Remove(old.Uid); // fuori dalla nave il Nostromo non è bloccato (R-016)
            State.NostromiFreedByMedico.Remove(old.Uid);
            State.Crew.Discard(old);
        }

        /// <summary>
        /// Il giocatore tiene una missione dell'id dato, presa dal mazzo Corsaro o dagli scarti, come se l'avesse appena
        /// pescata (R-131: conta da qui).
        /// </summary>
        public MissionCard Mission(int player, MissionId id)
        {
            MissionCard card = State.Corsair.DrawPile.Concat(State.Corsair.DiscardPile).First(c => c.Id == id);
            State.Corsair.Remove(card);
            Missions.Track(State, P(player), card);
            return card;
        }

        /// <summary>Segnalini taglia già guadagnati, per fonte (tengono allineato il totale).</summary>
        public void Bounty(int player, BountyReason reason, int tokens)
        {
            P(player).BountyBySource[reason] = P(player).BountyFrom(reason) + tokens;
            P(player).BountyTokens += tokens;
        }

        /// <summary>Aggiunge alla mano carte Pirateria degli id dati.</summary>
        public RoundScenario Hand(int player, params PirateCardId[] ids)
        {
            foreach (PirateCardId id in ids)
            {
                PirateCard card = State.Pirate.DrawPile.Concat(State.Pirate.DiscardPile).First(c => c.Id == id);
                State.Pirate.Remove(card);
                P(player).Hand.Add(card);
            }

            return this;
        }

        public int AboveSlot(int index) => index;

        public int BelowSlot(int index) => Config.slotsAbove + index;

        /// <summary>
        /// Gioca il round in corso: rotte da <paramref name="headings"/> (per id), le altre decisioni con
        /// <paramref name="chooser"/> (default: la prima opzione). Si ferma alla prima decisione del round dopo.
        /// Restituisce gli eventi del round (fino a quella decisione).
        /// </summary>
        public List<GameEvent> PlayRound(Heading[] headings, Func<PendingDecision, DecisionAnswer> chooser = null)
        {
            chooser = chooser ?? Default;
            int round = Session.State.Round;
            var events = new List<GameEvent>();
            int guard = 0;
            while (Session.Pending != null && Session.State.Round == round)
            {
                if (guard++ > 1000) throw new InvalidOperationException("Il round non finisce.");
                PendingDecision pending = Session.Pending;
                Decisions.Add(pending);
                DecisionAnswer answer = pending.Kind == DecisionKind.ChooseHeading
                    ? pending.Choose(pending.Options.Cast<HeadingOption>().ToList().FindIndex(o => o.Heading == headings[pending.Player]))
                    : chooser(pending);
                events.AddRange(Session.Submit(answer));
            }

            TestSupport.AssertInvariants(Session);
            return events;
        }

        /// <summary>
        /// La risposta di default: nel turno in mare chiude il turno (così la Fase 2 non tocca mani e ciurme),
        /// altrimenti la prima opzione.
        /// </summary>
        public static DecisionAnswer Default(PendingDecision d)
        {
            int end = d.Options.ToList().FindIndex(o => o is EndTurnOption);
            return d.Choose(end >= 0 ? end : 0);
        }

        /// <summary>Risponde con la prima opzione che soddisfa il criterio, altrimenti con <see cref="Default"/>.</summary>
        public static Func<PendingDecision, DecisionAnswer> Prefer(Func<PendingDecision, DecisionOption, bool> wanted)
        {
            return d =>
            {
                for (int i = 0; i < d.Options.Count; i++)
                    if (wanted(d, d.Options[i])) return d.Choose(i);
                return Default(d);
            };
        }

        public static RandomBot Bot(int seed) => new RandomBot(new SeededRandom(seed));

        /// <summary>
        /// Gioca il round con tutte le rotte a Sud: col vento da Nord (default dello scenario) le navi in mare hanno
        /// velocità 0 e restano dove sono, così il round serve solo per la Fase 2.
        /// </summary>
        public List<GameEvent> PlayTurns(Func<PendingDecision, DecisionAnswer> chooser = null)
        {
            return PlayRound(Enumerable.Repeat(Heading.S, Session.State.PlayerCount).ToArray(), chooser);
        }

        /// <summary>Il primo criterio che trova un'opzione decide; altrimenti <see cref="Default"/>.</summary>
        public static Func<PendingDecision, DecisionAnswer> Pick(params Func<PendingDecision, DecisionOption, bool>[] wanted)
        {
            return d =>
            {
                foreach (Func<PendingDecision, DecisionOption, bool> rule in wanted)
                    for (int i = 0; i < d.Options.Count; i++)
                        if (rule(d, d.Options[i])) return d.Choose(i);
                return Default(d);
            };
        }

        /// <summary>Criterio: un'opzione di tipo <typeparamref name="T"/> per il giocatore dato, che soddisfa <paramref name="where"/>.</summary>
        public static Func<PendingDecision, DecisionOption, bool> Opt<T>(int player, Func<T, bool> where = null) where T : DecisionOption
        {
            return (d, o) => d.Player == player && o is T t && (where == null || where(t));
        }

        /// <summary>Le decisioni di un tipo, per un giocatore.</summary>
        public List<PendingDecision> DecisionsOf(DecisionKind kind, int player) =>
            Decisions.Where(d => d.Kind == kind && d.Player == player).ToList();
    }
}
