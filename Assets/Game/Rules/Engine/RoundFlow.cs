using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Un round (02_regole.md §4): Fase 1 (rotte, meteo, movimento, Abbordaggi), poi Fase 2 a turno.</summary>
    internal static class RoundFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            GameState state = ctx.State;

            // R-042, R-050: dal round 2 l'ordine si calcola all'inizio del round e vale per le scelte della Fase 1.
            if (state.Round > 1) SetTurnOrder(ctx, TurnOrder.FromCrew(ctx));

            yield return Flow.Call(Preparation(ctx));
            yield return Flow.Call(Active(ctx));
        }

        /// <summary>Fase 1 — Preparazione (R-040–R-045). In Fase 1 non si gioca nessuna carta (R-044).</summary>
        public static IEnumerable<FlowStep> Preparation(GameContext ctx)
        {
            GameState state = ctx.State;
            state.Phase = GamePhase.Preparation;
            ctx.Emit(new PhaseStartedEvent(state.Round, GamePhase.Preparation));

            foreach (PlayerState player in state.Players)
            {
                player.ChosenHeading = null;
                player.HeadingRevealed = false;
            }

            // R-040: scelta segreta e obbligatoria, da confermare sempre. R-045: chi è in Svago non sceglie.
            foreach (int id in state.TurnOrderList.ToList())
            {
                PlayerState player = state.PlayerById(id);
                if (player.LeisureRound == state.Round) continue;

                var options = new List<DecisionOption>();
                for (int h = 0; h < HeadingExtensions.Count; h++) options.Add(new HeadingOption((Heading)h));

                AskStep ask = ctx.Ask(DecisionKind.ChooseHeading, id, true, options, null, requiresConfirmation: true);
                yield return ask;

                player.ChosenHeading = ask.Choice<HeadingOption>().Heading;
                ctx.Emit(new HeadingChosenEvent(id, player.ChosenHeading.Value));
            }

            // R-041
            foreach (PlayerState player in state.Players) player.HeadingRevealed = true;
            ctx.Emit(new HeadingsRevealedEvent(state.Players.Select(p => p.ChosenHeading).ToArray()));

            // R-042, R-043
            state.MovementInProgress = true;
            state.SharedSeaCellsAllowed.Clear();
            yield return Flow.Call(WeatherFlow.Run(ctx));
            yield return Flow.Call(MovementFlow.Run(ctx));
            yield return Flow.Call(BoardingFlow.Run(ctx));
            state.MovementInProgress = false;
        }

        /// <summary>
        /// Fase 2 — Attiva: ordine di turno (R-050, R-051; nel round 1 resta quello delle offerte, R-037), poi un turno
        /// a testa: cornice (R-053), Mozzo (R-052), poi porto o mare (<see cref="TurnFlow"/>).
        /// </summary>
        public static IEnumerable<FlowStep> Active(GameContext ctx)
        {
            GameState state = ctx.State;
            state.Phase = GamePhase.Active;
            ctx.Emit(new PhaseStartedEvent(state.Round, GamePhase.Active));

            // R-050: un Abbordaggio può aver cambiato la ciurma, quindi l'ordine si ricalcola.
            if (state.Round > 1) SetTurnOrder(ctx, TurnOrder.FromCrew(ctx));

            state.TurnsPlayed.Clear();
            foreach (int id in state.TurnOrderList.ToList())
            {
                PlayerState player = state.PlayerById(id);
                state.ActivePlayer = id;
                state.TurnsPlayed.Add(id);
                ctx.Emit(new TurnStartedEvent(state.Round, id));

                if (state.Map.KindAt(player.Position) == CellKind.Border)
                {
                    ctx.Emit(new TurnSkippedEvent(id)); // R-053, e niente Mozzo (R-052)
                }
                else
                {
                    int coins = CrewEffects.EffectiveCount(player.CrewAbove, CrewCardId.Mozzo) * ctx.Config.cabinBoyCoins;
                    if (coins > 0) ctx.ChangeCoins(player, coins, CoinReason.CabinBoy);
                    yield return Flow.Call(TurnFlow.Play(ctx, player));
                }

                ctx.Emit(new TurnEndedEvent(state.Round, id));
            }

            state.ActivePlayer = -1;
        }

        private static void SetTurnOrder(GameContext ctx, List<int> order)
        {
            ctx.State.TurnOrderList.Clear();
            ctx.State.TurnOrderList.AddRange(order);
            ctx.Emit(new TurnOrderSetEvent(ctx.State.Round, order.ToArray()));
        }
    }
}
