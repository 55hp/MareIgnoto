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
    /// <summary>Il meteo della Fase 1 (R-042, R-080–R-087), applicato dopo la rivelazione delle rotte e prima del movimento.</summary>
    internal static class WeatherFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            GameState state = ctx.State;

            // R-042: le scelte si chiedono nell'ordine di turno calcolato all'inizio del round.
            foreach (int id in state.TurnOrderList.ToList())
            {
                PlayerState player = state.PlayerById(id);

                // R-045: Svago, la nave è in porto e non subisce il meteo. R-082: isole e cornice non hanno zona.
                if (player.LeisureRound == state.Round) continue;
                int zone = state.Map.ZoneOf(player.Position);
                if (zone < 0) continue;

                // R-087
                if (player.TailwindRound == state.Round)
                {
                    ctx.Emit(new WeatherSkippedEvent(id, WeatherSkipReason.Tailwind));
                    continue;
                }

                WeatherState zoneState = state.ZoneStates[zone];
                WeatherState perceived = Perceived(zoneState, player, ctx.Config);
                ctx.Emit(new WeatherAppliedEvent(id, zone, zoneState, perceived));
                if (perceived == WeatherState.Normal) continue;

                // R-083/R-084: rotazione oraria di r scatti (8 = nessun cambio); R-086: il Timoniere sceglie.
                var rotation = new int[1];
                yield return Flow.Call(RollOrChoose(ctx, player, DiceReason.WeatherRotation, DecisionKind.WeatherRotation, rotation));
                Heading from = player.ChosenHeading.Value;
                Heading to = from.Rotate(rotation[0]);
                player.ChosenHeading = to;
                ctx.Emit(new HeadingRotatedEvent(id, rotation[0] % HeadingExtensions.Count, from, to));

                if (perceived == WeatherState.RoughSea)
                    yield return Flow.Call(LosePirateCards(ctx, player, ctx.Config.roughSeaPirateLoss));
                else
                    yield return Flow.Call(LoseBelowDeckCrew(ctx, player, ctx.Config.stormBelowDeckCrewLoss));
            }
        }

        /// <summary>R-085: ogni Navigatore sopra coperta (Jolly compreso, R-015) abbassa l'intensità di un livello.</summary>
        public static WeatherState Perceived(WeatherState zoneState, PlayerState player, RulesConfig config)
        {
            int reduction = CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Navigatore) * config.navigatorWeatherReduction;
            int level = (int)zoneState - reduction;
            return level <= (int)WeatherState.Normal ? WeatherState.Normal : (WeatherState)level;
        }

        /// <summary>
        /// Un risultato del d8 per <paramref name="player"/>: tirato, oppure scelto se ha il Timoniere sopra coperta
        /// (R-086, R-073b). Il risultato finisce in <paramref name="result"/>[0].
        /// </summary>
        public static IEnumerable<FlowStep> RollOrChoose(GameContext ctx, PlayerState player, DiceReason reason,
            DecisionKind choiceKind, int[] result)
        {
            if (!HasHelmsman(player))
            {
                result[0] = ctx.RollD8(player.Id, reason);
                yield break;
            }

            var options = new List<DecisionOption>();
            for (int value = 1; value <= HeadingExtensions.Count; value++) options.Add(new DieValueOption(value));
            AskStep ask = ctx.Ask(choiceKind, player.Id, false, options);
            yield return ask;
            result[0] = ask.Choice<DieValueOption>().Value;
            ctx.Emit(new DieChosenEvent(player.Id, result[0], reason));
        }

        public static bool HasHelmsman(PlayerState player) =>
            CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Timoniere) > 0;

        /// <summary>R-083: perde carte Pirateria dalla mano, a sua scelta, una alla volta (meno se non ne ha abbastanza).</summary>
        private static IEnumerable<FlowStep> LosePirateCards(GameContext ctx, PlayerState player, int count)
        {
            for (int i = 0; i < count && player.Hand.Count > 0; i++)
            {
                var options = Losses.DistinctHandChoices(player.Hand, 1)
                    .Select(choice => (DecisionOption)new PirateCardOption(choice[0])).ToList();
                AskStep ask = ctx.Ask(DecisionKind.WeatherPirateLoss, player.Id, true, options);
                yield return ask;
                Losses.LosePirateCards(ctx, player, new[] { ask.Choice<PirateCardOption>().Card });
            }
        }

        /// <summary>R-084: perde crew sotto coperta, se ne ha, a sua scelta. Il Medico protegge solo sopra coperta (R-023).</summary>
        private static IEnumerable<FlowStep> LoseBelowDeckCrew(GameContext ctx, PlayerState player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var options = player.Crew.BelowSlots().Where(s => player.Crew[s] != null)
                    .Select(s => (DecisionOption)new CrewSlotOption(s, player.Crew[s])).ToList();
                if (options.Count == 0) yield break;

                AskStep ask = ctx.Ask(DecisionKind.StormCrewLoss, player.Id, true, options);
                yield return ask;
                yield return Flow.Call(Losses.LoseCrew(ctx, player, ask.Choice<CrewSlotOption>().Slot, CrewLossCause.Storm));
            }
        }
    }
}
