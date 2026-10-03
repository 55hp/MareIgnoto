using hp55games.MareIgnoto.Rules.Decisions;
using hp55games.MareIgnoto.Rules.State;

namespace hp55games.MareIgnoto.Rules.Bots
{
    /// <summary>
    /// Un giocatore automatico (09_bot.md §2): riceve la decisione in attesa e il punto di vista del proprio posto, e
    /// risponde con una delle opzioni legali. Non legge altro: né lo stato interno del motore né le carte nascoste degli altri.
    /// </summary>
    public interface IBot
    {
        DecisionAnswer Choose(PendingDecision decision, PlayerView view);
    }
}
