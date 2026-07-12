using System;
using System.Collections.Generic;
using System.Linq;

namespace Merlin.Classes
{
    // Список "ролик x количество" раскладывается в очередь round-robin по раундам (см. конструктор).
    // День за днём вызывающий код забирает порциями через TakeForToday — внутри порции ролики
    // дополнительно распределены так, чтобы один и тот же ролик не шёл подряд (см. ArrangeNoAdjacentRepeats) —
    // это подстраховка на случай, когда дневная порция не совпадает ровно с границей раунда.
    // Используется и для линейной, и для веерной генерации по Шаблону 3.
    internal class RollerAllocationQueue
    {
        private readonly LinkedList<Roller> _queue;

        // Round-robin по раундам вместо одного общего shuffle: раунд N берёт по одному экземпляру
        // каждого ролика, у которого ещё осталась квота (>N уже выданных), порядок внутри раунда
        // перемешивается отдельно. При равных квотах, совпадающих с дневной нормой, это даёт
        // каждому дню все разные ролики. Ролики с большей квотой неизбежно достаются последним
        // раундам почти в одиночку — это ожидаемое поведение, не дефект.
        public RollerAllocationQueue(IEnumerable<(Roller Roller, int Quantity)> rollerQuantities)
        {
            // rollerQuantities — IEnumerable, обходим Max() и Where() многократно, поэтому
            // материализуем один раз, иначе можно словить повторный (и потенциально другой) запрос к источнику.
            var quantities = rollerQuantities.Where(rq => rq.Quantity > 0).ToList();

            var pool = new List<Roller>(quantities.Sum(rq => rq.Quantity));
            var rnd = new Random();

            if (quantities.Count > 0)
            {
                int maxQuantity = quantities.Max(rq => rq.Quantity);

                for (int round = 0; round < maxQuantity; round++)
                {
                    var roundItems = quantities
                        .Where(rq => rq.Quantity > round)
                        .Select(rq => rq.Roller)
                        .ToList();

                    for (int i = roundItems.Count - 1; i > 0; i--)
                    {
                        int j = rnd.Next(i + 1);
                        (roundItems[i], roundItems[j]) = (roundItems[j], roundItems[i]);
                    }

                    pool.AddRange(roundItems);
                }
            }

            _queue = new LinkedList<Roller>(pool);
        }

        public int Count => _queue.Count;

        public List<Roller> TakeForToday(int count)
        {
            var batch = new List<Roller>();
            for (int i = 0; i < count && _queue.Count > 0; i++)
            {
                batch.Add(_queue.First.Value);
                _queue.RemoveFirst();
            }
            return ArrangeNoAdjacentRepeats(batch);
        }

        // Распределяет порцию так, чтобы один и тот же ролик не шёл подряд: на каждом шаге
        // берём ролик, которого в остатке порции больше всего, но не совпадающий с только что
        // поставленным; среди равных по количеству — более длинный (сохраняет исходную идею
        // "длинные ролики сначала, пока в дне больше свободных окон"). Если один ролик занимает
        // больше половины порции, подряд-повтор неизбежен чисто арифметически — тогда алгоритм
        // просто минимизирует их количество, а не гарантирует ноль.
        private static List<Roller> ArrangeNoAdjacentRepeats(List<Roller> batch)
        {
            var groups = batch
                .GroupBy(r => r.RollerId)
                .Select(g => new Queue<Roller>(g))
                .ToList();

            var result = new List<Roller>(batch.Count);
            Roller last = null;

            while (result.Count < batch.Count)
            {
                Queue<Roller> next = groups
                    .Where(g => g.Count > 0)
                    .OrderByDescending(g => g.Count)
                    .ThenByDescending(g => g.Peek().Duration)
                    .FirstOrDefault(g => last == null || g.Peek().RollerId != last.RollerId)
                    ?? groups.First(g => g.Count > 0);

                last = next.Dequeue();
                result.Add(last);
            }

            return result;
        }

        // Не хватило окон/слотов сегодня — вернуть остаток в начало очереди, попробовать в другой день
        public void PutBackToFront(List<Roller> rollers)
        {
            for (int i = rollers.Count - 1; i >= 0; i--)
                _queue.AddFirst(rollers[i]);
        }
    }
}
