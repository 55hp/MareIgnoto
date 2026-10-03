using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Config;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// L'avanzamento di una missione in mano (03_contenuti.md §3, colonna "Contatore"). Nasce quando il giocatore tiene
    /// la missione, quindi conta solo ciò che succede dopo (R-131).
    /// </summary>
    internal sealed class MissionProgress
    {
        public MissionCard Card { get; }
        public int Owner { get; }

        /// <summary>La condizione è diventata vera (anche se poi non lo è più): la missione si completa al prossimo punto sicuro.</summary>
        public bool Met { get; set; }

        /// <summary>Contatore generico: battaglie vinte o perse, perdite, Bordate, Svaghi, round consecutivi.</summary>
        public int Count { get; set; }

        /// <summary>Round a cui si riferisce <see cref="Count"/> per i contatori "nello stesso round" o "consecutivi"; -1 se nessuno.</summary>
        public int Round { get; set; } = -1;

        /// <summary>
        /// Missioni di stato (R-131): la condizione all'ultimo controllo. Si completano solo quando passa da falsa a vera,
        /// quindi se è già vera quando la missione si tiene deve prima smettere di esserlo.
        /// </summary>
        public bool StateWasTrue { get; set; }

        /// <summary>Insieme di chiavi: avversari battuti (Barbanera!), celle di arenamento (Gamba di legno).</summary>
        public HashSet<int> Keys { get; } = new HashSet<int>();

        public MissionProgress(MissionCard card, int owner)
        {
            Card = card;
            Owner = owner;
        }
    }

    /// <summary>
    /// Missioni Corsaro (R-130–R-133, 03 §3). I contatori si aggiornano osservando gli eventi (<see cref="Observe"/>, da
    /// <see cref="GameContext.Emit"/>); le missioni la cui condizione è vera si completano nei punti sicuri, cioè quando il
    /// motore si ferma per una decisione o a fine partita (<see cref="CompleteReady"/>). Così una missione immediata
    /// (R-132) si completa prima che chiunque possa rispondere a qualcosa, ma mai su uno stato a metà di un'operazione
    /// (uno scambio di slot fatto in due passi, per esempio).
    /// </summary>
    internal static class Missions
    {
        /// <summary>Il giocatore ha tenuto la missione: da qui conta (R-131).</summary>
        public static void Track(GameState state, PlayerState player, MissionCard card)
        {
            player.Missions.Add(card);
            var progress = new MissionProgress(card, player.Id);
            progress.StateWasTrue = StateCondition(state, player, card.Id) == true; // R-131: conta solo il passaggio a vero
            state.MissionProgress[card.Uid] = progress;
        }

        /// <summary>Aggiorna i contatori delle missioni in mano con un evento appena emesso.</summary>
        public static void Observe(GameState state, GameEvent e)
        {
            if (state.MissionProgress.Count == 0) return;
            RulesConfig cfg = state.Config;

            foreach (MissionProgress m in state.MissionProgress.Values)
            {
                PlayerState owner = state.PlayerById(m.Owner);
                int threshold = cfg.Mission(m.Card.Id).threshold;

                switch (m.Card.Id)
                {
                    case MissionId.Barbanera: // vinto almeno una battaglia contro ogni avversario
                        if (e is BattleWonEvent bw && bw.Winner == m.Owner)
                        {
                            m.Keys.Add(bw.Loser);
                            if (m.Keys.Count >= state.PlayerCount - 1) m.Met = true;
                        }
                        break;

                    case MissionId.Barbarossa: // battaglie vinte
                        if (e is BattleWonEvent won && won.Winner == m.Owner && ++m.Count >= threshold) m.Met = true;
                        break;

                    case MissionId.OlandeseVolante: // battaglie perse: si valuta solo a fine partita
                        if (e is BattleWonEvent lost && lost.Loser == m.Owner) m.Count++;
                        break;

                    case MissionId.MaledizionePirata: // perdite crew (R-021) nello stesso round
                        if (e is CrewLostEvent cl && cl.Player == m.Owner && CountInRound(m, state.Round) >= threshold) m.Met = true;
                        break;

                    case MissionId.NaveCorsara: // Bordate nello stesso round, gratuite comprese
                        if (e is CombatPlayEvent cp && cp.Player == m.Owner &&
                            (cp.Play == CombatPlay.Broadside || cp.Play == CombatPlay.FreeBroadside) &&
                            CountInRound(m, state.Round) >= threshold) m.Met = true;
                        break;

                    // Monete e mano: controllate a ogni evento, così conta anche un passaggio che dura un istante.
                    case MissionId.Avido:
                    case MissionId.Avidissimo:
                    case MissionId.Bancarotta:
                    case MissionId.DispersiInMare:
                        UpdateState(state, owner, m);
                        break;

                    case MissionId.SpugnaDiMare: // azioni Svago
                        if (e is PortActionEvent pa && pa.Player == m.Owner && pa.Action == PortAction.Leisure && ++m.Count >= threshold)
                            m.Met = true;
                        break;

                    case MissionId.Attaccabrighe: // battaglie (R-103/R-106, non Arrembaggio!) in round consecutivi
                        if (e is AttackStartedEvent attack && attack.Opening != AttackOpening.Arrembaggio &&
                            (attack.Attacker == m.Owner || attack.Defender == m.Owner) && m.Round != state.Round)
                        {
                            m.Count = m.Round == state.Round - 1 ? m.Count + 1 : 1;
                            m.Round = state.Round;
                            if (m.Count >= threshold) m.Met = true;
                        }
                        break;

                    case MissionId.LupoDiMare: // battaglia vinta con 0 carte sopra coperta
                        if (e is BattleWonEvent lone && lone.Winner == m.Owner && owner.CrewAbove.All(c => c == null)) m.Met = true;
                        break;

                    case MissionId.ParlareConIPesci: // perdite crew in mare (R-022)
                        if (e is CrewLostEvent sea && sea.Player == m.Owner && sea.AtSea && ++m.Count >= threshold) m.Met = true;
                        break;

                    case MissionId.GambaDiLegno: // arenamenti su celle di cornice diverse (R-069)
                        if (e is ShipStrandedEvent stranded && stranded.Player == m.Owner)
                        {
                            m.Keys.Add(stranded.Cell.Y * state.Map.Width + stranded.Cell.X);
                            if (m.Keys.Count >= threshold) m.Met = true;
                        }
                        break;

                    // Cacciatore di Taglie, Gemelli, Nave d'assalto: condizioni sullo stato della ciurma o delle missioni,
                    // valutate nei punti sicuri (IsMet).
                }
            }
        }

        /// <summary>
        /// Completa le missioni la cui condizione è vera (R-132): la carta si rivela e il giocatore riceve subito la
        /// ricompensa. Ripete finché ne completa, perché Cacciatore di Taglie conta le missioni completate.
        /// Olandese Volante si valuta solo a fine partita (<see cref="EvaluateAtGameEnd"/>).
        /// </summary>
        public static void CompleteReady(GameContext ctx)
        {
            GameState state = ctx.State;
            if (state.Phase == GamePhase.Ended) return;

            bool completed = true;
            while (completed)
            {
                completed = false;
                foreach (PlayerState player in state.Players)
                    foreach (MissionCard card in player.Missions.ToList())
                    {
                        MissionProgress m = state.MissionProgress[card.Uid];
                        if (card.Id == MissionId.OlandeseVolante || !IsMet(state, player, m)) continue;
                        Complete(ctx, player, m, false);
                        completed = true;
                    }
            }
        }

        /// <summary>
        /// Fine partita (R-132, R-133): ultime missioni immediate, poi Olandese Volante (nessuna battaglia persa da
        /// quando è stata pescata), poi di nuovo le immediate per Cacciatore di Taglie.
        /// </summary>
        public static void EvaluateAtGameEnd(GameContext ctx)
        {
            GameState state = ctx.State;
            CompleteReady(ctx);
            foreach (PlayerState player in state.Players)
                foreach (MissionCard card in player.Missions.Where(c => c.Id == MissionId.OlandeseVolante).ToList())
                {
                    MissionProgress m = state.MissionProgress[card.Uid];
                    if (m.Count == 0) Complete(ctx, player, m, true);
                }

            CompleteReady(ctx);
        }

        /// <summary>Ricompensa della missione per questo giocatore (Barbanera! per avversario, Gemelli coi due Jolly).</summary>
        public static int Reward(GameState state, PlayerState player, MissionCard card)
        {
            RulesConfig cfg = state.Config;
            int reward = cfg.Mission(card.Id).reward;
            switch (card.Id)
            {
                case MissionId.Barbanera: return reward * (state.PlayerCount - 1);
                case MissionId.Gemelli: return JokerTwins(player) ? cfg.twinsJokersReward : reward;
                default: return reward;
            }
        }

        private static bool IsMet(GameState state, PlayerState player, MissionProgress m)
        {
            if (m.Card.Id == MissionId.CacciatoreDiTaglie) // tutta la partita, eccezione a R-131
                return player.Completed.Count >= state.Config.Mission(m.Card.Id).threshold;
            UpdateState(state, player, m);
            return m.Met;
        }

        /// <summary>R-131: una missione di stato scatta quando la sua condizione passa da falsa a vera.</summary>
        private static void UpdateState(GameState state, PlayerState player, MissionProgress m)
        {
            bool? now = StateCondition(state, player, m.Card.Id);
            if (now == null) return;
            if (now.Value && !m.StateWasTrue) m.Met = true;
            m.StateWasTrue = now.Value;
        }

        /// <summary>La condizione delle missioni di stato (03 §3); null per le altre.</summary>
        private static bool? StateCondition(GameState state, PlayerState player, MissionId id)
        {
            switch (id)
            {
                case MissionId.Avido:
                case MissionId.Avidissimo: return player.Coins >= state.Config.Mission(id).threshold;
                case MissionId.Bancarotta: return player.Coins == 0;
                case MissionId.DispersiInMare: return player.Hand.Count == 0;
                case MissionId.Gemelli: return SameRankPair(player.CrewAbove);
                case MissionId.NaveDAssalto: return player.CrewAbove.Count(c => c != null && CrewCatalog.IsCourt(c.Rank)) >= 2;
                default: return null;
            }
        }

        private static void Complete(GameContext ctx, PlayerState player, MissionProgress m, bool atGameEnd)
        {
            int reward = Reward(ctx.State, player, m.Card);
            player.Missions.Remove(m.Card);
            player.Completed.Add(m.Card);
            ctx.State.MissionProgress.Remove(m.Card.Uid);
            ctx.Emit(new MissionCompletedEvent(player.Id, m.Card, reward, atGameEnd));
            if (reward != 0) ctx.ChangeBounty(player, reward, BountyReason.Mission);
        }

        /// <summary>Aggiorna il contatore "nello stesso round" e ne restituisce il valore.</summary>
        private static int CountInRound(MissionProgress m, int round)
        {
            if (m.Round != round)
            {
                m.Round = round;
                m.Count = 0;
            }

            return ++m.Count;
        }

        /// <summary>Gemelli: due carte sopra coperta dello stesso rango (i due Jolly hanno entrambi rango 0).</summary>
        private static bool SameRankPair(IReadOnlyList<CrewCard> above)
        {
            var cards = above.Where(c => c != null).ToList();
            return cards.GroupBy(c => c.Rank).Any(g => g.Count() >= 2);
        }

        private static bool JokerTwins(PlayerState player) => player.CrewAbove.Count(c => c != null && c.IsJoker) >= 2;
    }
}
