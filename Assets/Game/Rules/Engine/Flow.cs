using System;
using System.Collections.Generic;
using hp55games.MareIgnoto.Rules.Decisions;

namespace hp55games.MareIgnoto.Rules.Engine
{
    /// <summary>
    /// Un passo di un flusso di gioco. I flussi sono metodi iteratori che scrivono la partita come codice
    /// sequenziale: dove serve una scelta del giocatore fanno <c>yield return ctx.Ask(...)</c>, e quando
    /// riprendono la scelta è in <see cref="AskStep.Chosen"/>; per eseguire un sotto-flusso fanno
    /// <c>yield return Flow.Call(...)</c>. Il <see cref="FlowRunner"/> li pilota.
    /// Così una regola nuova (un effetto di carta, un'azione di porto) è un metodo che chiede ciò che gli serve,
    /// senza macchine a stati da tenere sincronizzate.
    /// </summary>
    internal abstract class FlowStep
    {
    }

    /// <summary>Chiede una decisione e sospende il flusso fino alla risposta.</summary>
    internal sealed class AskStep : FlowStep
    {
        public PendingDecision Decision { get; }

        /// <summary>L'opzione scelta; valorizzata quando il flusso riprende.</summary>
        public DecisionOption Chosen { get; internal set; }

        public AskStep(PendingDecision decision)
        {
            Decision = decision;
        }

        /// <summary>La scelta come tipo di opzione atteso.</summary>
        public T Choice<T>() where T : DecisionOption => (T)Chosen;
    }

    /// <summary>Esegue un sotto-flusso fino in fondo, poi il chiamante riprende.</summary>
    internal sealed class CallStep : FlowStep
    {
        public IEnumerator<FlowStep> Flow { get; }

        public CallStep(IEnumerator<FlowStep> flow)
        {
            Flow = flow;
        }
    }

    internal static class Flow
    {
        public static CallStep Call(IEnumerable<FlowStep> flow) => new CallStep(flow.GetEnumerator());
    }

    /// <summary>
    /// Pilota una pila di flussi: avanza fino alla prossima decisione che richiede un giocatore. Prima di fermarsi chiama
    /// <c>beforePause</c>: è il punto sicuro in cui lo stato è coerente (le missioni immediate si completano lì).
    /// </summary>
    internal sealed class FlowRunner
    {
        private readonly Stack<IEnumerator<FlowStep>> stack = new Stack<IEnumerator<FlowStep>>();
        private readonly Action beforePause;
        private AskStep current;

        /// <summary>La decisione in attesa, null se il flusso è terminato.</summary>
        public PendingDecision Pending => current?.Decision;

        public bool IsFinished => stack.Count == 0;

        public FlowRunner(IEnumerable<FlowStep> root, Action beforePause = null)
        {
            stack.Push(root.GetEnumerator());
            this.beforePause = beforePause;
        }

        /// <summary>Avanza il flusso fino alla prossima decisione o alla fine.</summary>
        public void Run()
        {
            while (stack.Count > 0)
            {
                IEnumerator<FlowStep> top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                switch (top.Current)
                {
                    case CallStep call:
                        stack.Push(call.Flow);
                        break;
                    case AskStep ask:
                        // Una sola opzione legale: la sceglie il motore, salvo le decisioni da confermare.
                        if (ask.Decision.Options.Count == 1 && !ask.Decision.RequiresConfirmation)
                        {
                            ask.Chosen = ask.Decision.Options[0];
                            break;
                        }

                        beforePause?.Invoke();
                        current = ask;
                        return;
                    default:
                        throw new InvalidOperationException("Passo di flusso sconosciuto: " + top.Current);
                }
            }

            current = null;
        }

        /// <summary>Consegna la risposta alla decisione in attesa e avanza.</summary>
        public void Answer(DecisionOption chosen)
        {
            if (current == null) throw new InvalidOperationException("Nessuna decisione in attesa.");
            current.Chosen = chosen;
            current = null;
            Run();
        }
    }
}
