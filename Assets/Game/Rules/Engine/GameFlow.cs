using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Il flusso principale della partita: preparazione, poi i round. Ogni parte della partita è un flusso
    /// (<see cref="FlowStep"/>) richiamato da qui; le specifiche successive aggiungono i passi mancanti
    /// (Fase 2, fine partita) senza cambiare la struttura.
    /// </summary>
    internal static class GameFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            GameState state = ctx.State;
            yield return Flow.Call(SetupFlow.Run(ctx));

            // TODO R-140/R-142 (spec 0004): la partita finisce con l'Isola Sacra. Per ora solo il limite di R-150.
            for (int round = 1; ; round++)
            {
                if (ctx.Config.maxRounds > 0 && round > ctx.Config.maxRounds)
                {
                    End(ctx, GameEndReason.RoundLimit, round - 1);
                    yield break;
                }

                state.Round = round;
                ctx.Emit(new RoundStartedEvent(round));
                yield return Flow.Call(RoundFlow.Run(ctx));
            }
        }

        private static void End(GameContext ctx, GameEndReason reason, int roundsPlayed)
        {
            ctx.State.Phase = GamePhase.Ended;
            ctx.State.Result = new GameResult(reason == GameEndReason.RoundLimit, roundsPlayed);
            ctx.Emit(new GameEndedEvent(reason, roundsPlayed));
        }
    }
}
