using System;

// 힙에 들어갈 아이템이 구현해야 하는 인터페이스
// HeapIndex를 아이템 스스로 들고 있어 Contains/UpdateItem을 O(1)/O(log n)에 처리할 수 있다
public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}

// 배열 기반 이진 힙 (최댓값 우선 = CompareTo가 클수록 우선순위가 높음)
public class Heap<T> where T : IHeapItem<T>
{
    private readonly T[] items;
    private int currentItemCount;

    public int Count => currentItemCount;

    public Heap(int maxHeapSize)
    {
        items = new T[maxHeapSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst()
    {
        T firstItem = items[0];
        currentItemCount--;

        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);

        return firstItem;
    }

    // 아이템의 우선순위가 더 좋아졌을 때(예: gCost 갱신) 위치를 재조정
    public void UpdateItem(T item)
    {
        SortUp(item);
    }

    public bool Contains(T item)
    {
        return item.HeapIndex < currentItemCount && Equals(items[item.HeapIndex], item);
    }

    private void SortDown(T item)
    {
        while (true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;

            if (childIndexLeft >= currentItemCount) { return; }

            int swapIndex = childIndexLeft;

            if (childIndexRight < currentItemCount &&
                items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
            {
                swapIndex = childIndexRight;
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
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (parentIndex >= 0)
        {
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) <= 0) { break; }

            Swap(item, parentItem);
            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    private void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;

        int tempIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = tempIndex;
    }
}
