using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>Fine partita (02_regole.md §9): Tesoro dell'Isola Sacra, missioni di fine partita, punteggio, esito.</summary>
    internal static class EndFlow
    {
        /// <summary>
        /// R-140, R-141: dopo il movimento, il Tesoro va a chi è entrato nell'Isola Sacra nel passo più basso. A parità di
        /// passo ognuno prende il segnalino e le monete si dividono per difetto; il resto esce dal gioco.
        /// </summary>
        public static void TakeTreasure(GameContext ctx)
        {
            GameState state = ctx.State;
            if (state.SacredIslandArrivals.Count == 0 || state.TreasureTakers.Count > 0) return;

            int step = state.SacredIslandArrivals.Values.Min();
            List<int> takers = state.SacredIslandArrivals.Where(a => a.Value == step).Select(a => a.Key).OrderBy(id => id).ToList();
            state.TreasureTakers.AddRange(takers);

            int treasure = state.Treasure;
            int coinsEach = treasure / takers.Count;
            int lost = treasure - coinsEach * takers.Count;
            ctx.Emit(new TreasureTakenEvent(takers.ToArray(), step, ctx.Config.treasureTokens, coinsEach, lost));
            if (treasure > 0) ctx.ChangeTreasure(-treasure);

            foreach (int id in takers)
            {
                PlayerState player = state.PlayerById(id);
                ctx.ChangeBounty(player, ctx.Config.treasureTokens, BountyReason.Treasure);
                if (coinsEach > 0) ctx.ChangeCoins(player, coinsEach, CoinReason.Treasure);
            }
        }

        /// <summary>
        /// Chiude la partita: missioni di fine partita (R-132, R-133), taglia di ognuno (R-143–R-146), classifica e
        /// vincitori (R-147). Si chiama a fine round (R-142) o per il limite di R-150.
        /// </summary>
        public static void End(GameContext ctx, GameEndReason reason, int roundsPlayed)
        {
            GameState state = ctx.State;
            state.ActivePlayer = -1;
            Missions.EvaluateAtGameEnd(ctx);

            List<PlayerScore> ranking = Scoring.Rank(state);
            foreach (PlayerScore score in ranking) ctx.Emit(new FinalScoreEvent(score));

            state.Phase = GamePhase.Ended;
            state.Result = new GameResult(reason, roundsPlayed, ranking, state.TreasureTakers.ToArray());
            ctx.Emit(new GameEndedEvent(reason, roundsPlayed, state.Result.Winners));
        }
    }
}
