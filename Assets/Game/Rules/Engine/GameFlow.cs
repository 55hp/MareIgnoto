using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Il flusso principale della partita: preparazione, poi i round. Ogni parte della partita è un flusso
    /// (<see cref="FlowStep"/>) richiamato da qui; le specifiche successive aggiungono i passi mancanti
    /// (Fase 1 completa, Fase 2, fine partita) senza cambiare la struttura.
    /// </summary>
    internal static class GameFlow
    {
        public static IEnumerable<FlowStep> Run(GameContext ctx)
        {
            yield return Flow.Call(SetupFlow.Run(ctx));

            // TODO spec 0002–0004: ciclo dei round fino alla fine partita (R-140, R-142), con il limite maxRounds (R-150).
            // Per ora si gioca il solo primo round, fermandosi alla rivelazione delle rotte.
            ctx.State.Round = 1;
            ctx.Emit(new RoundStartedEvent(1));
            yield return Flow.Call(RoundFlow.Preparation(ctx));
        }
    }
}
