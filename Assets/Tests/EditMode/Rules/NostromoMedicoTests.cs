using System.Collections.Generic;
using System.Linq;
using hp55games.MareIgnoto.Rules.Cards;
using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.Map;
using hp55games.MareIgnoto.Rules.State;
using NUnit.Framework;
using static hp55games.MareIgnoto.Rules.Tests.RoundScenario;

namespace hp55games.MareIgnoto.Rules.Tests
{
    /// <summary>
    /// Il Medico (R-023) e il blocco del Nostromo (R-016, 04 §6): il Medico interviene solo sulle perdite di carte sopra
    /// coperta, anche del Nostromo, ed è l'unica via per riportarlo sotto. p0 in (5,7), p1 in (6,7), navi ferme.
    /// </summary>
    public class NostromoMedicoTests
    {
        private static RoundScenario Table()
        {
            RoundScenario s = Create(2).At(0, 5, 7).At(1, 6, 7).Order(0, 1);
            s.P(0).Coins = 10;
            s.P(1).Coins = 0;
            return s;
        }

        [Test]
        public void TheMedicoSavesTheNostromoAndFreesItFromTheLock_R023_R016()
        {
            // Abbordaggio fortuito in (4,7): p0 sceglie di perdere il Nostromo e usa il Medico.
            RoundScenario s = Create(2).At(0, 3, 7).At(1, 5, 7).Order(0, 1);
            CrewCard nostromo = s.Crew(0, s.AboveSlot(0), CrewCardId.Nostromo);
            CrewCard medico = s.Crew(0, s.BelowSlot(0), CrewCardId.Medico);
            s.Random.Enqueue(1, 5);
            List<GameEvent> events = s.PlayRound(new[] { Heading.E, Heading.O }, Prefer((d, o) =>
                (o is BoardingLossOption l && l.CrewSlots.Contains(0)) || (o is MedicoOption m && m.Use)));

            Assert.AreEqual(1, s.Decisions.Count(d => d.Kind == DecisionKind.UseMedico), "il Medico è offerto anche per il Nostromo");
            Assert.AreSame(medico, s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreSame(nostromo, s.P(0).Crew[s.BelowSlot(0)], "l'unica via per riportarlo sotto");
            Assert.IsFalse(s.State.LockedNostromi.Contains(nostromo.Uid));
            Assert.IsTrue(s.State.NostromiFreedByMedico.Contains(nostromo.Uid));
            Assert.IsFalse(events.OfType<CrewLostEvent>().Any(), "non è una perdita (R-021)");
        }

        [Test]
        public void AFreedNostromoComesBackUpWithASwapAndIsLockedAgain_R016()
        {
            RoundScenario s = Table();
            CrewCard nostromo = s.Crew(1, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Crew(1, s.BelowSlot(0), CrewCardId.Medico);
            s.Hand(0, PirateCardId.UomoInMare);
            s.PlayTurns(Pick(Opt<PlayCardOption>(0, o => o.Card.Id == PirateCardId.UomoInMare),
                Opt<CrewSlotOption>(1, o => o.Slot == s.AboveSlot(0)),
                Opt<MedicoOption>(1, o => o.Use),
                Opt<SwapOption>(1, o => o.From == s.BelowSlot(0) && o.To == s.AboveSlot(1))));

            Assert.AreSame(nostromo, s.P(1).Crew[s.AboveSlot(1)], "risalito con lo swap del proprietario");
            Assert.IsTrue(s.State.LockedNostromi.Contains(nostromo.Uid), "di nuovo bloccato");
            Assert.IsFalse(s.State.NostromiFreedByMedico.Contains(nostromo.Uid));

            s.PlayTurns();
            var swaps = s.DecisionsOf(DecisionKind.SeaAction, 1).Last().Options.OfType<SwapOption>().ToList();
            Assert.IsFalse(swaps.Any(o => (o.From == s.AboveSlot(1) && o.To >= s.Config.slotsAbove) ||
                                          (o.To == s.AboveSlot(1) && o.From >= s.Config.slotsAbove)),
                "lo swap non lo riporta sotto");
        }

        [Test]
        public void TheMedicoBlocksArrembaggioOnTheNostromo_R110_R023()
        {
            RoundScenario s = Table();
            CrewCard nostromo = s.Crew(1, s.AboveSlot(0), CrewCardId.Nostromo);
            CrewCard medico = s.Crew(1, s.BelowSlot(0), CrewCardId.Medico);
            s.Hand(0, PirateCardId.Arrembaggio);
            s.PlayTurns(Pick(Opt<AttackOption>(0, o => o.Opening == AttackOpening.Arrembaggio), Opt<MedicoOption>(1, o => o.Use)));

            Assert.AreSame(medico, s.P(1).Crew[s.AboveSlot(0)]);
            Assert.AreSame(nostromo, s.P(1).Crew[s.BelowSlot(0)]);
            Assert.IsFalse(s.P(0).Crew.All().Any(), "l'attaccante non riceve nulla");
        }

        [Test]
        public void TheMedicoDoesNotCoverBelowDeckLosses_R023_R084()
        {
            // Tempesta: si perde una crew sotto coperta, il Medico non c'entra.
            RoundScenario storm = Create(2).At(0, 3, 7).At(1, 16, 16).Order(0, 1).ZoneAt(3, 7, WeatherState.Storm); // D7, nuvola N5
            storm.Crew(0, storm.BelowSlot(0), CrewCardId.Medico);
            storm.Crew(0, storm.BelowSlot(1), CrewCardId.Mozzo);
            storm.Random.Enqueue(8);
            List<GameEvent> stormEvents = storm.PlayRound(new[] { Heading.E, Heading.E },
                Prefer((d, o) => o is CrewSlotOption c && c.Slot == storm.BelowSlot(1)));
            Assert.IsFalse(storm.Decisions.Any(d => d.Kind == DecisionKind.UseMedico));
            Assert.AreEqual(CrewCardId.Mozzo, stormEvents.OfType<CrewLostEvent>().Single().Card.Kind);

            // Uomo in mare! su una carta sotto coperta: idem.
            RoundScenario below = Table();
            below.Crew(1, below.AboveSlot(0), CrewCardId.Mozzo);
            below.Crew(1, below.BelowSlot(0), CrewCardId.Medico);
            below.Crew(1, below.BelowSlot(1), CrewCardId.Cuoco);
            below.Hand(0, PirateCardId.UomoInMare);
            List<GameEvent> events = below.PlayTurns(Pick(Opt<PlayCardOption>(0, o => o.Card.Id == PirateCardId.UomoInMare),
                Opt<CrewSlotOption>(1, o => o.Slot == below.BelowSlot(1))));
            Assert.IsFalse(below.Decisions.Any(d => d.Kind == DecisionKind.UseMedico));
            Assert.AreEqual(CrewCardId.Cuoco, events.OfType<CrewLostEvent>().Single().Card.Kind);
        }

        [Test]
        public void SwapsDoNotCallTheMedico_R023_R111_R120()
        {
            RoundScenario spy = Table();
            spy.Crew(1, spy.AboveSlot(0), CrewCardId.Mozzo);
            spy.Crew(1, spy.BelowSlot(0), CrewCardId.Medico);
            spy.Crew(1, spy.BelowSlot(1), CrewCardId.Cuoco);
            spy.Hand(0, PirateCardId.Spyglass);
            spy.PlayTurns(Pick(Opt<PlayCardOption>(0, o => o.Card.Id == PirateCardId.Spyglass),
                Opt<SpyglassOption>(0, o => o.SlotA == spy.AboveSlot(0) && o.SlotB == spy.BelowSlot(1))));
            Assert.AreEqual(CrewCardId.Cuoco, spy.P(1).Crew[spy.AboveSlot(0)].Kind);
            Assert.IsFalse(spy.Decisions.Any(d => d.Kind == DecisionKind.UseMedico), "Spyglass! è uno scambio");

            RoundScenario quarter = Table();
            quarter.Crew(0, quarter.AboveSlot(0), CrewCardId.Quartiermastro);
            quarter.Crew(1, quarter.AboveSlot(0), CrewCardId.Timoniere);
            quarter.Crew(1, quarter.BelowSlot(0), CrewCardId.Medico);
            quarter.Hand(0, PirateCardId.Bordata);
            quarter.PlayTurns(Pick(Opt<QuartermasterOption>(0)));
            Assert.AreEqual(CrewCardId.Timoniere, quarter.P(0).Crew[quarter.AboveSlot(0)].Kind);
            Assert.IsFalse(quarter.Decisions.Any(d => d.Kind == DecisionKind.UseMedico), "il Quartiermastro è uno scambio");
        }

        [Test]
        public void TheQuartermasterMovesALockedNostromoOnlyAboveDeck_R111_R016()
        {
            RoundScenario s = Table();
            CrewCard quartermaster = s.Crew(0, s.AboveSlot(0), CrewCardId.Quartiermastro);
            CrewCard nostromo = s.Crew(1, s.AboveSlot(0), CrewCardId.Nostromo);
            s.Hand(0, PirateCardId.Bordata);
            s.PlayTurns(Pick(Opt<QuartermasterOption>(0)));

            Assert.AreSame(nostromo, s.P(0).Crew[s.AboveSlot(0)]);
            Assert.AreSame(quartermaster, s.P(1).Crew[s.AboveSlot(0)]);
            Assert.IsTrue(s.State.LockedNostromi.Contains(nostromo.Uid), "resta bloccato anche sulla nave nuova");
        }

        [Test]
        public void TheInvariantAllowsOnlyTheMedicoAsAWayDown_R016()
        {
            RoundScenario locked = Table();
            CrewCard nostromo = locked.Crew(0, locked.AboveSlot(0), CrewCardId.Nostromo);
            locked.P(0).Crew.Set(locked.AboveSlot(0), null);
            locked.P(0).Crew.Set(locked.BelowSlot(0), nostromo); // sotto senza Medico
            StringAssert.Contains("senza il Medico", string.Join("\n", locked.Session.CheckInvariants()));

            RoundScenario freed = Table();
            CrewCard other = freed.Crew(0, freed.BelowSlot(0), CrewCardId.Nostromo);
            freed.State.NostromiFreedByMedico.Add(other.Uid);
            freed.P(0).Crew.Set(freed.BelowSlot(0), null);
            freed.P(0).Crew.Set(freed.AboveSlot(0), other); // risalito senza tornare bloccato
            StringAssert.Contains("non è bloccato", string.Join("\n", freed.Session.CheckInvariants()));
        }
    }
}
