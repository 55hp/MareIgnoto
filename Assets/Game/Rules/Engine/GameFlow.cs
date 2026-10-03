using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Il flusso principale della partita: preparazione, poi i round finché una nave non entra nell'Isola Sacra (R-140);
    /// quel round si completa (round di cortesia, R-142) e poi si fa il conteggio (<see cref="EndFlow"/>).
    /// Ogni parte della partita è un flusso (<see cref="FlowStep"/>) richiamato da qui.
    /// </summary>
    internal static class GameFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            GameState state = ctx.State;
            yield return Flow.Call(SetupFlow.Run(ctx));

            for (int round = 1; ; round++)
            {
                // R-150: solo per le simulazioni.
                if (ctx.Config.maxRounds > 0 && round > ctx.Config.maxRounds)
                {
                    EndFlow.End(ctx, GameEndReason.RoundLimit, round - 1);
                    yield break;
                }

                state.Round = round;
                ctx.Emit(new RoundStartedEvent(round));
                yield return Flow.Call(RoundFlow.Run(ctx));

                // R-142: qualcuno è entrato nell'Isola Sacra in questo round, che ora è completo.
                if (state.SacredIslandArrivals.Count > 0)
                {
                    EndFlow.End(ctx, GameEndReason.SacredIsland, round);
                    yield break;
                }
            }
        }
    }
}
