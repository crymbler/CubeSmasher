using System.Collections.Generic;
using UnityEngine;

public class BagRandomizer
{
    private readonly int _totalItems;
    private readonly List<int> _bag = new List<int>();
    private int _lastSpawnedIndex = -1;

    public BagRandomizer(int totalItems)
    {
        _totalItems = totalItems;
        RefillAndShuffle();
    }

    public int GetNext()
    {
        if (_bag.Count == 0)
        {
            RefillAndShuffle();
        }

        int nextItem = _bag[0];

        // Защита от дубликатов на стыке двух мешков
        if (nextItem == _lastSpawnedIndex && _bag.Count > 1)
        {
            nextItem = _bag[1];
            _bag.RemoveAt(1);
        }
        else
        {
            _bag.RemoveAt(0);
        }

        _lastSpawnedIndex = nextItem;
        return nextItem;
    }

    private void RefillAndShuffle()
    {
        _bag.Clear();
        for (int i = 0; i < _totalItems; i++)
        {
            _bag.Add(i);
        }

        // Классический алгоритм тасования Фишера-Йетса
        for (int i = 0; i < _bag.Count; i++)
        {
            int temp = _bag[i];
            int randomIndex = Random.Range(i, _bag.Count);
            _bag[i] = _bag[randomIndex];
            _bag[randomIndex] = temp;
        }
    }
}