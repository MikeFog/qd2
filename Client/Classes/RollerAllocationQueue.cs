using System;
using System.Collections.Generic;
using System.Linq;

namespace Merlin.Classes
{
    // Список "ролик x количество" разворачивается в очередь единичных слотов и перемешивается один раз.
    // День за днём вызывающий код забирает порциями через TakeForToday — порция уже отсортирована
    // по убыванию длительности ролика (длинные ролики размещаются первыми, пока в дне больше свободных окон).
    // Используется и для линейной, и для веерной генерации по Шаблону 3.
    internal class RollerAllocationQueue
    {
        private readonly LinkedList<Roller> _queue;

        public RollerAllocationQueue(IEnumerable<(Roller Roller, int Quantity)> rollerQuantities)
        {
            var expanded = new List<Roller>();
            foreach (var (roller, quantity) in rollerQuantities)
                for (int i = 0; i < quantity; i++)
                    expanded.Add(roller);

            var rnd = new Random();
            for (int i = expanded.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (expanded[i], expanded[j]) = (expanded[j], expanded[i]);
            }

            _queue = new LinkedList<Roller>(expanded);
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
            return batch.OrderByDescending(r => r.Duration).ToList();
        }

        // Не хватило окон/слотов сегодня — вернуть остаток в начало очереди, попробовать в другой день
        public void PutBackToFront(List<Roller> rollers)
        {
            for (int i = rollers.Count - 1; i >= 0; i--)
                _queue.AddFirst(rollers[i]);
        }
    }
}
