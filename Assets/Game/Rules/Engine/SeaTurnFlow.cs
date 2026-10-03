using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Turno in mare (R-056): (A) pesca carte Pirateria e fine turno, oppure (B) nell'ordine che si vuole, ciascuna al
    /// massimo una volta: swap (R-057), attacco o Quartiermastro (§7.2, R-111), carte del turno (§7.3: una, più una per
    /// Bucaniere). L'apertura di un attacco con una carta della mano consuma una carta del turno (R-112).
    /// </summary>
    internal static class SeaTurnFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx, PlayerState player)
        {
            RulesConfig cfg = ctx.Config;
            int cardsLeft = 1 + CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Bucaniere) * cfg.buccaneerExtraCards;
            bool swapped = false, attacked = false, acted = false;

            while (true)
            {
                var options = new List<DecisionOption>();
                if (!acted) options.Add(new DrawCardsOption());
                if (!swapped) options.AddRange(SwapOptions(ctx, player));
                if (!attacked) options.AddRange(CombatFlow.AttackOptions(ctx, player, cardsLeft));
                if (cardsLeft > 0)
                    foreach (PirateCard card in Losses.DistinctHandChoices(player.Hand, 1).Select(c => c[0]))
                        if (IsPlayable(ctx, player, card)) options.Add(new PlayCardOption(card));
                options.Add(new EndTurnOption());

                AskStep ask = ctx.Ask(DecisionKind.SeaAction, player.Id, true, options);
                yield return ask;

                switch (ask.Chosen)
                {
                    case DrawCardsOption _: // R-056 (A)
                        ctx.DrawToHand(player, cfg.seaDrawCount);
                        yield break;
                    case SwapOption swap: // R-057
                        swapped = true;
                        ctx.Emit(new CrewSwappedEvent(player.Id, swap.From, swap.To));
                        ctx.Exchange(player, swap.From, swap.To);
                        break;
                    case AttackOption attack:
                        attacked = true;
                        if (attack.Opening == AttackOpening.HandBroadside || attack.Opening == AttackOpening.Arrembaggio) cardsLeft--; // R-112
                        yield return Flow.Call(CombatFlow.Attack(ctx, player, ctx.State.PlayerById(attack.Target), attack.Opening));
                        break;
                    case QuartermasterOption quartermaster: // R-111: usa l'azione di attacco del turno
                        attacked = true;
                        yield return Flow.Call(CombatFlow.Quartermaster(ctx, player, ctx.State.PlayerById(quartermaster.Target)));
                        break;
                    case PlayCardOption play:
                        cardsLeft--;
                        yield return Flow.Call(PlayCard(ctx, player, play.Card));
                        break;
                    default: // EndTurnOption
                        yield break;
                }

                acted = true;
            }
        }

        /// <summary>R-057: da uno slot occupato a qualsiasi altro (scambio se occupato, una opzione per coppia), mai un Nostromo bloccato sotto (R-016).</summary>
        private static IEnumerable<DecisionOption> SwapOptions(GameContext ctx, PlayerState player)
        {
            for (int from = 0; from < player.Crew.Count; from++)
            {
                if (player.Crew[from] == null) continue;
                for (int to = 0; to < player.Crew.Count; to++)
                {
                    if (to == from || (player.Crew[to] != null && to < from)) continue;
                    if (ctx.CanExchange(player, from, to)) yield return new SwapOption(from, to);
                }
            }
        }

        /// <summary>Le carte giocabili come carta del turno (§7.3). Bordata!/Parlè! mai (R-123), Arrembaggio! solo in attacco (R-102).</summary>
        private static bool IsPlayable(GameContext ctx, PlayerState player, PirateCard card)
        {
            switch (card.Id)
            {
                case PirateCardId.Bordata:
                case PirateCardId.Parle:
                case PirateCardId.Arrembaggio:
                    return false;
                case PirateCardId.UomoInMare:
                    return CombatFlow.Targets(ctx, player).Any(t => Losses.OccupiedSlots(t).Any());
                case PirateCardId.Spyglass:
                    return CombatFlow.Targets(ctx, player).Any(t => SpyglassPairs(ctx, t).Any());
                default:
                    return player.Coins >= ctx.Config.PirateCost(card.Id); // R-121 (le altre costano 0)
            }
        }

        private static IEnumerable<SpyglassOption> SpyglassPairs(GameContext ctx, PlayerState target)
        {
            List<int> occupied = Losses.OccupiedSlots(target).ToList();
            for (int i = 0; i < occupied.Count; i++)
                for (int j = i + 1; j < occupied.Count; j++)
                    if (ctx.CanExchange(target, occupied[i], occupied[j])) // R-016
                        yield return new SpyglassOption(target.Id, occupied[i], occupied[j]);
        }

        /// <summary>Una carta del turno (§7.3, 03 §2): si scarta, si paga il costo Meteo al Tesoro (R-121), poi l'effetto.</summary>
        private static IEnumerable<FlowStep> PlayCard(GameContext ctx, PlayerState player, PirateCard card)
        {
            RulesConfig cfg = ctx.Config;
            GameState state = ctx.State;
            player.Hand.Remove(card);
            state.Pirate.Discard(card);
            ctx.Emit(new CardPlayedEvent(player.Id, card, true));

            int cost = cfg.PirateCost(card.Id);
            if (cost > 0)
            {
                ctx.ChangeCoins(player, -cost, CoinReason.WeatherCard);
                // R-142: nel round di cortesia il Tesoro è già assegnato e le monete escono dal gioco.
                if (state.TreasureTakers.Count == 0) ctx.ChangeTreasure(cost);
            }

            // R-122: il Cuoco aggiunge una risorsa per copia alle carte pesca.
            int cook = CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Cuoco) * cfg.cookDrawBonus;

            switch (card.Id)
            {
                case PirateCardId.PescaFortunata:
                    ctx.ChangeCoins(player, cfg.fortunateCatchCoins + cook, CoinReason.FortunateCatch);
                    break;

                case PirateCardId.ReteAStrascico:
                    ctx.DrawToHand(player, cfg.trawlNetDrawCount + cook);
                    break;

                case PirateCardId.UomoInMare: // R-120: a tiro, nave in mare, nessuna difesa
                {
                    var targets = CombatFlow.Targets(ctx, player).Where(t => Losses.OccupiedSlots(t).Any())
                        .Select(t => (DecisionOption)new TargetPlayerOption(t.Id)).ToList();
                    AskStep target = ctx.Ask(DecisionKind.CardTarget, player.Id, false, targets);
                    yield return target;
                    PlayerState victim = state.PlayerById(target.Choice<TargetPlayerOption>().Target);

                    var slots = Losses.OccupiedSlots(victim).Select(s => (DecisionOption)new CrewSlotOption(s, victim.Crew[s])).ToList();
                    AskStep discard = ctx.Ask(DecisionKind.ManOverboardDiscard, victim.Id, true, slots);
                    yield return discard;
                    yield return Flow.Call(Losses.LoseCrew(ctx, victim, discard.Choice<CrewSlotOption>().Slot, CrewLossCause.ManOverboard));
                    break;
                }

                case PirateCardId.Spyglass: // R-120, R-016
                {
                    var pairs = CombatFlow.Targets(ctx, player).SelectMany(t => SpyglassPairs(ctx, t)).Cast<DecisionOption>().ToList();
                    AskStep ask = ctx.Ask(DecisionKind.CardTarget, player.Id, false, pairs);
                    yield return ask;
                    SpyglassOption pair = ask.Choice<SpyglassOption>();
                    PlayerState victim = state.PlayerById(pair.Target);
                    ctx.Emit(new SpyglassEvent(player.Id, victim.Id, pair.SlotA, pair.SlotB));
                    ctx.Exchange(victim, pair.SlotA, pair.SlotB);
                    break;
                }

                case PirateCardId.SupplicaGartya:
                {
                    var steps = new List<DecisionOption> { new WindStepOption(cfg.windStepSize), new WindStepOption(-cfg.windStepSize) };
                    AskStep ask = ctx.Ask(DecisionKind.CardTarget, player.Id, false, steps);
                    yield return ask;
                    state.Wind = state.Wind.Rotate(ask.Choice<WindStepOption>().Steps);
                    ctx.Emit(new WindChangedEvent(state.Wind));
                    break;
                }

                case PirateCardId.RafficaCanaglia:
                    state.Wind = state.Wind.Opposite();
                    ctx.Emit(new WindChangedEvent(state.Wind));
                    break;

                case PirateCardId.InvocazioneGartya:
                case PirateCardId.IraGartya:
                case PirateCardId.FavoreGartya:
                {
                    var zones = Enumerable.Range(0, state.ZoneLevels.Length)
                        .Select(z => (DecisionOption)new ZoneOption(z, state.Map.ZoneId(z))).ToList();
                    AskStep ask = ctx.Ask(DecisionKind.CardTarget, player.Id, false, zones);
                    yield return ask;
                    int zone = ask.Choice<ZoneOption>().Zone;
                    int from = state.ZoneLevels[zone];
                    state.ZoneLevels[zone] = LevelAfter(card.Id, from, cfg);
                    ctx.Emit(new ZoneChangedEvent(zone, state.Map.ZoneId(zone), from, state.ZoneLevels[zone]));
                    break;
                }

                case PirateCardId.VentoInPoppa: // R-087
                    player.TailwindRound = state.Round + 1;
                    break;
            }
        }

        /// <summary>
        /// R-088: Invocazione porta a <c>invocationLevel</c> e Ira a <c>wrathLevel</c>, solo da un livello più basso
        /// (altrimenti la carta è sprecata, ma pagata); Favore abbassa di <c>favorLevelDrop</c>, fino a 0.
        /// </summary>
        private static int LevelAfter(PirateCardId id, int level, RulesConfig cfg)
        {
            switch (id)
            {
                case PirateCardId.InvocazioneGartya: return level < cfg.invocationLevel ? cfg.invocationLevel : level;
                case PirateCardId.IraGartya: return level < cfg.wrathLevel ? cfg.wrathLevel : level;
                default: return System.Math.Max(0, level - cfg.favorLevelDrop); // Favore di Gartya
            }
        }
    }
}
