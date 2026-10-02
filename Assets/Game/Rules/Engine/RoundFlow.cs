using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Un round (02_regole.md §4). Per ora arriva fino alla rivelazione delle rotte (R-040, R-041).</summary>
    internal static class RoundFlow
    {
        /// <summary>Fase 1 — Preparazione: rotte segrete scelte da ogni giocatore, poi rivelate insieme.</summary>
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

            // R-040: scelta segreta e obbligatoria, da confermare sempre. TODO R-045 (spec 0002): chi ha scelto Svago non sceglie.
            foreach (int id in state.TurnOrderList)
            {
                PlayerState player = state.PlayerById(id);
                var options = new List<DecisionOption>();
                for (int h = 0; h < HeadingExtensions.Count; h++) options.Add(new HeadingOption((Heading)h));

                AskStep ask = ctx.Ask(DecisionKind.ChooseHeading, id, true, options, null, requiresConfirmation: true);
                yield return ask;

                player.ChosenHeading = ask.Choice<HeadingOption>().Heading;
                ctx.Emit(new HeadingChosenEvent(id, player.ChosenHeading.Value));
            }

            // R-041
            foreach (PlayerState player in state.Players) player.HeadingRevealed = true;
            ctx.Emit(new HeadingsRevealedEvent(state.Players.Select(p => p.ChosenHeading.Value).ToArray()));

            // TODO R-042–R-045 (spec 0002): meteo, movimento cella per cella, Abbordaggi fortuiti.
        }
    }
}
