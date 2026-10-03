using System;
using System.Collections.Generic;
using System.Linq;

namespace hp55games.MareIgnoto.Rules.Map
{
    /// <summary>
    /// Geometria pura di un gruppo di celle, per disegnare le zone meteo (06_presentazione.md §2, ZoneOverlayView):
    /// contorno lungo il perimetro delle celle e cella su cui scrivere il livello. Le coordinate dei vertici sono gli
    /// spigoli della griglia: lo spigolo (i, j) è l'angolo in basso a sinistra della cella (i, j), quindi la cella (x, y)
    /// va da (x, y) a (x + 1, y + 1). La vista li porta nel mondo (centro cella in (x, 0, y): spigolo in (i - 0.5, 0, j - 0.5)).
    /// </summary>
    public static class ZoneGeometry
    {
        /// <summary>
        /// I contorni chiusi delle celle: uno per il bordo esterno di ogni gruppo connesso, più uno per ogni buco. Ogni
        /// contorno è una lista di spigoli senza ripetere il primo alla fine, percorsa con le celle a sinistra (antiorario
        /// all'esterno, orario attorno ai buchi), senza vertici intermedi sui lati dritti.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<Coord>> Outline(IEnumerable<Coord> cells)
        {
            var set = new HashSet<Coord>(cells ?? throw new ArgumentNullException(nameof(cells)));

            // Lati di confine, orientati con la cella a sinistra, indicizzati per spigolo di partenza.
            var outgoing = new Dictionary<Coord, List<Coord>>();
            void Edge(Coord from, Coord to)
            {
                if (!outgoing.TryGetValue(from, out List<Coord> list)) outgoing[from] = list = new List<Coord>();
                list.Add(to);
            }

            foreach (Coord c in set.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                if (!set.Contains(new Coord(c.X, c.Y - 1))) Edge(new Coord(c.X, c.Y), new Coord(c.X + 1, c.Y));             // sotto
                if (!set.Contains(new Coord(c.X + 1, c.Y))) Edge(new Coord(c.X + 1, c.Y), new Coord(c.X + 1, c.Y + 1));     // destra
                if (!set.Contains(new Coord(c.X, c.Y + 1))) Edge(new Coord(c.X + 1, c.Y + 1), new Coord(c.X, c.Y + 1));     // sopra
                if (!set.Contains(new Coord(c.X - 1, c.Y))) Edge(new Coord(c.X, c.Y + 1), new Coord(c.X, c.Y));             // sinistra
            }

            var loops = new List<IReadOnlyList<Coord>>();
            foreach (Coord start in outgoing.Keys.OrderBy(c => c.Y).ThenBy(c => c.X).ToList())
            {
                while (outgoing.TryGetValue(start, out List<Coord> first) && first.Count > 0)
                {
                    var loop = new List<Coord> { start };
                    Coord previous = start, current = Take(outgoing, start, null);
                    while (current != start)
                    {
                        loop.Add(current);
                        Coord next = Take(outgoing, current, Direction(previous, current));
                        previous = current;
                        current = next;
                    }

                    loops.Add(Simplify(loop));
                }
            }

            return loops;
        }

        /// <summary>
        /// La cella del gruppo più vicina al baricentro, dove scrivere il livello: sta sempre dentro la zona, anche per forme
        /// curve. A parità, la più in basso e poi la più a sinistra.
        /// </summary>
        public static Coord LabelCell(IEnumerable<Coord> cells)
        {
            List<Coord> list = (cells ?? throw new ArgumentNullException(nameof(cells))).ToList();
            if (list.Count == 0) throw new ArgumentException("Servono celle.", nameof(cells));
            double cx = list.Average(c => c.X), cy = list.Average(c => c.Y);
            return list.OrderBy(c => (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy)).ThenBy(c => c.Y).ThenBy(c => c.X).First();
        }

        /// <summary>
        /// Toglie e restituisce un lato che parte da <paramref name="from"/>. Dove due lati partono dallo stesso spigolo (celle
        /// che si toccano per uno spigolo) si gira a sinistra rispetto alla direzione di arrivo, così i contorni non si incrociano.
        /// </summary>
        private static Coord Take(Dictionary<Coord, List<Coord>> outgoing, Coord from, Coord? arrivalDirection)
        {
            List<Coord> options = outgoing[from];
            int index = 0;
            if (options.Count > 1 && arrivalDirection.HasValue)
            {
                Coord left = new Coord(-arrivalDirection.Value.Y, arrivalDirection.Value.X);
                int found = options.FindIndex(to => Direction(from, to) == left);
                if (found >= 0) index = found;
            }

            Coord to = options[index];
            options.RemoveAt(index);
            return to;
        }

        private static Coord Direction(Coord from, Coord to) => new Coord(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

        /// <summary>Toglie i vertici in mezzo a un lato dritto.</summary>
        private static IReadOnlyList<Coord> Simplify(List<Coord> loop)
        {
            var result = new List<Coord>();
            for (int i = 0; i < loop.Count; i++)
            {
                Coord prev = loop[(i + loop.Count - 1) % loop.Count], here = loop[i], next = loop[(i + 1) % loop.Count];
                if (Direction(prev, here) != Direction(here, next)) result.Add(here);
            }

            return result;
        }
    }
}
