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
    /// <summary>Attacco navale (02_regole.md §7.2): gittata, duello di Bordate, Falconet, Arrembaggio!, Quartiermastro.</summary>
    internal static class CombatFlow
    {
        /// <summary>R-100: base più Falconet, Saker e Culverin sopra coperta, per copia (Jolly compreso).</summary>
        public static int Range(PlayerState player, RulesConfig config) => Range(player.CrewAbove, config);

        /// <summary>La gittata dalle sole carte sopra coperta (pubbliche): la usano anche i bot (09_bot.md §3).</summary>
        public static int Range(IReadOnlyList<CrewCard> above, RulesConfig config)
        {
            return config.baseRange
                   + CrewEffects.EffectiveCount(above, CrewCardId.Falconet) * config.falconetRange
                   + CrewEffects.EffectiveCount(above, CrewCardId.Saker) * config.sakerRange
                   + CrewEffects.EffectiveCount(above, CrewCardId.Culverin) * config.culverinRange;
        }

        /// <summary>R-101, R-120: le navi avversarie in mare entro la gittata, se chi agisce è in mare; per id.</summary>
        public static List<PlayerState> Targets(GameContext ctx, PlayerState player)
        {
            GameMap map = ctx.State.Map;
            if (map.KindAt(player.Position) != CellKind.Sea) return new List<PlayerState>();
            int range = Range(player, ctx.Config);
            return ctx.State.Players
                .Where(p => p != player && map.KindAt(p.Position) == CellKind.Sea && Coord.Distance(p.Position, player.Position) <= range)
                .ToList();
        }

        /// <summary>
        /// Gli attacchi possibili (R-102, R-105, R-106, R-112) e l'alternativa del Quartiermastro (R-111).
        /// Col Falconet ogni attacco di Bordate è un Duello obbligatorio con il primo colpo gratuito.
        /// </summary>
        public static IEnumerable<DecisionOption> AttackOptions(GameContext ctx, PlayerState player, int cardsLeft)
        {
            List<PlayerState> targets = Targets(ctx, player);
            if (targets.Count == 0) yield break;

            IReadOnlyList<CrewCard> above = player.CrewAbove;
            bool falconet = CrewEffects.EffectiveCount(above, CrewCardId.Falconet) > 0;
            bool gunner = CrewEffects.EffectiveCount(above, CrewCardId.Cannoniere) > 0;
            bool broadside = player.Hand.Any(c => c.Id == PirateCardId.Bordata);
            bool boarding = player.Hand.Any(c => c.Id == PirateCardId.Arrembaggio);

            foreach (PlayerState target in targets)
            {
                if (falconet)
                {
                    yield return new AttackOption(target.Id, AttackOpening.FalconetDuel);
                }
                else
                {
                    if (broadside && cardsLeft > 0) yield return new AttackOption(target.Id, AttackOpening.HandBroadside);
                    if (gunner) yield return new AttackOption(target.Id, AttackOpening.FreeBroadside);
                }

                if (boarding && cardsLeft > 0) yield return new AttackOption(target.Id, AttackOpening.Arrembaggio);
            }

            if (CrewEffects.EffectiveCount(above, CrewCardId.Quartiermastro) > 0 && broadside && player.CrewAbove.Any(c => c != null))
                foreach (PlayerState target in targets.Where(t => t.CrewAbove.Any(c => c != null)))
                    yield return new QuartermasterOption(target.Id);
        }

        public static IEnumerable<FlowStep> Attack(GameContext ctx, PlayerState attacker, PlayerState defender, AttackOpening opening)
        {
            RulesConfig cfg = ctx.Config;
            int distance = Coord.Distance(attacker.Position, defender.Position);
            bool parleForbidden = CrewEffects.EffectiveCount(attacker.CrewAbove, CrewCardId.Culverin) > 0 &&
                                  distance >= cfg.culverinMinDistance; // R-108
            ctx.Emit(new AttackStartedEvent(attacker.Id, defender.Id, opening, distance, parleForbidden));

            if (opening == AttackOpening.Arrembaggio)
            {
                PlayFromHand(ctx, attacker, PirateCardId.Arrembaggio, true);
                yield return Flow.Call(Boarding(ctx, attacker, defender, parleForbidden));
                yield break;
            }

            // R-105: Bordate gratuite del Cannoniere per questo combattimento, per ciascun contendente.
            var free = new Dictionary<int, int>
            {
                [attacker.Id] = CrewEffects.EffectiveCount(attacker.CrewAbove, CrewCardId.Cannoniere) * cfg.gunnerFreeBroadsides,
                [defender.Id] = CrewEffects.EffectiveCount(defender.CrewAbove, CrewCardId.Cannoniere) * cfg.gunnerFreeBroadsides,
            };
            bool falconet = opening == AttackOpening.FalconetDuel;

            // Primo colpo.
            switch (opening)
            {
                case AttackOpening.HandBroadside:
                    PirateCard first = PlayFromHand(ctx, attacker, PirateCardId.Bordata, true);
                    ctx.Emit(new CombatPlayEvent(attacker.Id, CombatPlay.Broadside, first));
                    break;
                case AttackOpening.FreeBroadside:
                    free[attacker.Id]--;
                    ctx.Emit(new CombatPlayEvent(attacker.Id, CombatPlay.FreeBroadside, null));
                    break;
                default: // R-106: primo colpo gratuito del Falconet
                    ctx.Emit(new CombatPlayEvent(attacker.Id, CombatPlay.FreeBroadside, null));
                    break;
            }

            // R-103 / R-106: botta e risposta. Chi non risponde perde.
            PlayerState responder = defender, other = attacker;
            bool defenderParried = false;
            while (true)
            {
                bool parry = !falconet && responder == defender;
                var options = new List<DecisionOption>();
                if (parry)
                {
                    PirateCard parle = responder.Hand.FirstOrDefault(c => c.Id == PirateCardId.Parle);
                    if (parle != null && !parleForbidden) options.Add(CombatResponseOption.FromHand(CombatPlay.Parle, parle));
                }
                else
                {
                    PirateCard bordata = responder.Hand.FirstOrDefault(c => c.Id == PirateCardId.Bordata);
                    if (bordata != null) options.Add(CombatResponseOption.FromHand(CombatPlay.Broadside, bordata));
                    if (free[responder.Id] > 0) options.Add(CombatResponseOption.Free());
                }

                options.Add(CombatResponseOption.GiveUp());
                AskStep ask = ctx.Ask(DecisionKind.CombatResponse, responder.Id, true, options);
                yield return ask;
                CombatResponseOption response = ask.Choice<CombatResponseOption>();

                if (response.Yield)
                {
                    ctx.Emit(new CombatYieldedEvent(responder.Id));
                    break;
                }

                if (response.Card != null)
                {
                    responder.Hand.Remove(response.Card);
                    ctx.State.Pirate.Discard(response.Card);
                }
                else
                {
                    free[responder.Id]--;
                }

                ctx.Emit(new CombatPlayEvent(responder.Id, response.Play, response.Card));
                if (response.Play == CombatPlay.Parle) defenderParried = true;

                PlayerState swap = responder;
                responder = other;
                other = swap;
            }

            // Vince chi ha colpito per ultimo. Il Saker vale per l'attaccante vincitore contro un difensore senza Parlè! (R-107).
            yield return Flow.Call(Win(ctx, other, responder, attacker, other == attacker && !defenderParried));
        }

        /// <summary>R-104, R-107, R-109: segnalini, monete dal perdente, Saker e Nostromo del vincitore.</summary>
        private static IEnumerable<FlowStep> Win(GameContext ctx, PlayerState winner, PlayerState loser, PlayerState attacker, bool sakerEligible)
        {
            RulesConfig cfg = ctx.Config;
            int factor = 1;
            if (sakerEligible)
                for (int i = CrewEffects.EffectiveCount(winner.CrewAbove, CrewCardId.Saker); i > 0; i--) factor *= cfg.sakerGainMultiplier;

            int tokens = cfg.battleWinTokens * factor;
            ctx.ChangeBounty(winner, tokens, BountyReason.Battle);

            int coins = System.Math.Min(ctx.RollD8(winner.Id, DiceReason.BattleLoot) * factor, loser.Coins);
            if (coins > 0)
            {
                ctx.ChangeCoins(loser, -coins, CoinReason.BattleLoot);
                ctx.ChangeCoins(winner, coins, CoinReason.BattleLoot);
            }

            int stolen = 0;
            for (int i = CrewEffects.EffectiveCount(winner.CrewAbove, CrewCardId.Nostromo); i > 0 && loser.Hand.Count > 0; i--)
            {
                PirateCard card = loser.Hand[ctx.Random.Range(0, loser.Hand.Count)];
                loser.Hand.Remove(card);
                winner.Hand.Add(card);
                ctx.Emit(new CardStolenEvent(winner.Id, loser.Id, card));
                stolen++;
            }

            ctx.Emit(new BattleWonEvent(winner.Id, loser.Id, attacker.Id, tokens, coins, factor, stolen));
            yield break;
        }

        /// <summary>
        /// R-110: il difensore può giocare Parlè! (non contro il Culverin da lontano, R-108) o pagare all'attaccante;
        /// altrimenti l'attaccante prende una sua crew sopra coperta (Medico permettendo, R-023) e la riceve (R-020).
        /// Nessun segnalino né moneta, nessun botta e risposta.
        /// </summary>
        private static IEnumerable<FlowStep> Boarding(GameContext ctx, PlayerState attacker, PlayerState defender, bool parleForbidden)
        {
            int ransom = ctx.Config.boardingParryCoins;
            var options = new List<DecisionOption>();
            PirateCard parle = defender.Hand.FirstOrDefault(c => c.Id == PirateCardId.Parle);
            if (parle != null && !parleForbidden) options.Add(new BoardingDefenseOption(BoardingDefense.Parle, parle));
            if (defender.Coins >= ransom) options.Add(new BoardingDefenseOption(BoardingDefense.Paid));
            options.Add(new BoardingDefenseOption(BoardingDefense.None));

            AskStep ask = ctx.Ask(DecisionKind.BoardingDefense, defender.Id, true, options);
            yield return ask;
            BoardingDefenseOption defense = ask.Choice<BoardingDefenseOption>();
            ctx.Emit(new BoardingDefendedEvent(defender.Id, defense.Defense));

            switch (defense.Defense)
            {
                case BoardingDefense.Parle:
                    defender.Hand.Remove(defense.Card);
                    ctx.State.Pirate.Discard(defense.Card);
                    ctx.Emit(new CombatPlayEvent(defender.Id, CombatPlay.Parle, defense.Card));
                    yield break;
                case BoardingDefense.Paid:
                    // R-110: le monete vanno all'attaccante (lettura confermata da Franci, report della spec 0003).
                    ctx.ChangeCoins(defender, -ransom, CoinReason.BoardingRansom);
                    ctx.ChangeCoins(attacker, ransom, CoinReason.BoardingRansom);
                    yield break;
            }

            var slots = Enumerable.Range(0, defender.Crew.AboveCount).Where(s => defender.Crew[s] != null)
                .Select(s => (DecisionOption)new CrewSlotOption(s, defender.Crew[s])).ToList();
            if (slots.Count == 0) yield break;

            AskStep steal = ctx.Ask(DecisionKind.StealCrew, attacker.Id, false, slots);
            yield return steal;

            var taken = new List<CrewCard>();
            yield return Flow.Call(Losses.LoseCrew(ctx, defender, steal.Choice<CrewSlotOption>().Slot, CrewLossCause.Arrembaggio, taken));
            foreach (CrewCard card in taken)
            {
                attacker.InTransit.Add(card);
                yield return Flow.Call(CrewFlow.Receive(ctx, attacker, card));
            }
        }

        /// <summary>
        /// R-111: scartata una Bordata!, scambia una propria carta sopra coperta con una sopra coperta della nave bersaglio.
        /// Lo scambio non è una perdita (R-021) e porta solo carte sopra coperta (R-016 non c'entra).
        /// </summary>
        public static IEnumerable<FlowStep> Quartermaster(GameContext ctx, PlayerState player, PlayerState target)
        {
            PlayFromHand(ctx, player, PirateCardId.Bordata, false);

            var options = new List<DecisionOption>();
            for (int own = 0; own < player.Crew.AboveCount; own++)
                for (int theirs = 0; theirs < target.Crew.AboveCount; theirs++)
                    if (player.Crew[own] != null && target.Crew[theirs] != null)
                        options.Add(new QuartermasterSwapOption(own, theirs));

            AskStep ask = ctx.Ask(DecisionKind.QuartermasterSwap, player.Id, false, options);
            yield return ask;
            QuartermasterSwapOption swap = ask.Choice<QuartermasterSwapOption>();

            CrewCard mine = player.Crew[swap.OwnSlot];
            CrewCard theirsCard = target.Crew[swap.TargetSlot];
            ctx.Emit(new QuartermasterSwapEvent(player.Id, swap.OwnSlot, target.Id, swap.TargetSlot));
            ctx.PutCrew(player, swap.OwnSlot, theirsCard);
            ctx.PutCrew(target, swap.TargetSlot, mine);
        }

        /// <summary>Toglie dalla mano una carta dell'id dato, la scarta e lo annuncia.</summary>
        private static PirateCard PlayFromHand(GameContext ctx, PlayerState player, PirateCardId id, bool countsAsTurnCard)
        {
            PirateCard card = player.Hand.First(c => c.Id == id);
            player.Hand.Remove(card);
            ctx.State.Pirate.Discard(card);
            ctx.Emit(new CardPlayedEvent(player.Id, card, countsAsTurnCard));
            return card;
        }
    }
}
