using System.Collections;
using hp55games.MareIgnoto.Rules.Events;
using hp55games.MareIgnoto.Rules.State;
using UnityEngine;

namespace hp55games.MareIgnoto.Unity.Views
{
    /// <summary>
    /// Una vista che riproduce gli eventi del motore (06_presentazione.md §4). <see cref="Flow.EventPlayer"/> chiama
    /// <see cref="Initialize"/> una volta con lo stato all'avvio, poi <see cref="Play"/> per ogni evento, in ordine.
    /// Le viste non modificano mai lo stato (CLAUDE.md, regola 3) e si basano sui dati dell'evento, non sullo stato
    /// corrente: quando un evento si riproduce, il motore è già più avanti.
    /// </summary>
    public abstract class GameView : MonoBehaviour
    {
        /// <summary>Costruisce la vista dallo stato iniziale. Con riferimenti mancanti logga l'errore e si disabilita.</summary>
        public abstract void Initialize(IReadOnlyGameState state);

        /// <summary>Reagisce all'evento in <paramref name="duration"/> secondi (0 = subito, animazioni saltate).</summary>
        public abstract IEnumerator Play(GameEvent gameEvent, float duration);

        /// <summary>CLAUDE.md, regola 7: riferimento nullo → errore con cosa manca e dove, poi il componente si disabilita.</summary>
        protected bool Require(Object reference, string field)
        {
            if (reference != null) return true;
            Debug.LogError("[" + GetType().Name + "] su '" + name + "': manca il riferimento '" + field + "' (Inspector).", this);
            enabled = false;
            return false;
        }

        /// <summary>Colora tutti i renderer di un oggetto generato da prefab senza toccare i materiali (URP Lit e standard).</summary>
        protected static void Tint(GameObject instance, Color color)
        {
            var block = new MaterialPropertyBlock();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
    }
}
