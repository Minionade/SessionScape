using System;

namespace SessionScape.Server.Simulation.World
{
    public interface IHeapItem<T> : IComparable<T>
    {
        public int HeapIndex { get; set; }
    }

    public class Heap<T> where T : IHeapItem<T>
    {
        private T[] items;
        private int itemCount;

        public int Count => itemCount;

        public Heap(int maxHeapSize)
        {
            items = new T[Math.Max(1, maxHeapSize)];
        }

        public void Add(T item)
        {
            if (itemCount >= items.Length)
            {
                Array.Resize(ref items, Math.Max(1, items.Length * 2));
            }

            item.HeapIndex = itemCount;
            items[itemCount] = item;

            itemCount++;

            SortUp(item);
        }

        public T RemoveFirst()
        {
            if (itemCount == 0)
                throw new InvalidOperationException("Cannot remove from an empty heap.");

            T firstItem = items[0];

            itemCount--;

            if (itemCount > 0)
            {
                items[0] = items[itemCount];
                items[0].HeapIndex = 0;
                items[itemCount] = default;

                SortDown(items[0]);
            }
            else
            {
                items[0] = default;
            }

            firstItem.HeapIndex = -1;

            return firstItem;
        }

        public void UpdateItem(T item)
        {
            SortUp(item);
            SortDown(item);
        }

        public bool Contains(T item)
        {
            if (item == null)
                return false;

            if (item.HeapIndex < 0 || item.HeapIndex >= itemCount)
                return false;

            return ReferenceEquals(items[item.HeapIndex], item);
        }

        private void SortDown(T item)
        {
            while (true)
            {
                int childLeftIndex = item.HeapIndex * 2 + 1;
                int childRightIndex = item.HeapIndex * 2 + 2;
                int swapIndex = -1;

                if (childLeftIndex >= itemCount)
                    return;

                swapIndex = childLeftIndex;

                if (childRightIndex < itemCount &&
                    items[childLeftIndex].CompareTo(items[childRightIndex]) < 0)
                {
                    swapIndex = childRightIndex;
                }

                if (item.CompareTo(items[swapIndex]) < 0)
                {
                    Swap(item, items[swapIndex]);
                }
                else
                {
                    return;
                }
            }
        }

        private void SortUp(T item)
        {
            while (item.HeapIndex > 0)
            {
                int parentIndex = (item.HeapIndex - 1) / 2;
                T parentItem = items[parentIndex];

                if (item.CompareTo(parentItem) <= 0)
                    return;

                Swap(item, parentItem);
            }
        }

        private void Swap(T itemA, T itemB)
        {
            int indexA = itemA.HeapIndex;
            int indexB = itemB.HeapIndex;

            items[indexA] = itemB;
            items[indexB] = itemA;

            itemA.HeapIndex = indexB;
            itemB.HeapIndex = indexA;
        }
    }
}