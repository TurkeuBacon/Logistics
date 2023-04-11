using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoadQueue
{
    private Vector2Int[] list;
    private int[] sizes;
    private const float c = 6.5f;
    public int Count;

    public ChunkLoadQueue(int rd)
    {
        sizes = new int[rd];
        Count = 0;
        list = new Vector2Int[(int)(c*(rd+1)*(rd+2)/2)];
    }

    public bool Enqueue(Vector2Int key, int distFromPlayer)
    {
        if(distFromPlayer >= sizes.Length){ return false; }
        if(sizes[distFromPlayer] < c*(distFromPlayer + 1))
        {
            list[(int)(c*distFromPlayer*(distFromPlayer+1)/2)+sizes[distFromPlayer]] = key;
            sizes[distFromPlayer]++;
            Count++;
            return true;
        }
        else
        {
            return false;
        }
    }

    public Vector2Int Dequeue()
    {
        for(int i = 0; i < sizes.Length; i++)
        {
            if(sizes[i] > 0)
            {
                sizes[i]--;
                Count--;
                return list[(int)(c*i*(i+1)/2)+sizes[i]];
            }
        }
        throw new System.Exception("Empty Queue");
    }

    public bool Contains(Vector2Int key)
    {
        for(int j = 0; j < sizes.Length; j++)
        {
            int startIndex = (int)(c*j*(j+1)/2);
            for(int i = 0; i < sizes[j]; i++)
            {
                if(list[startIndex+i] == key)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool rangeCheck(Vector2Int playerChunk)
    {
        for(int j = 0; j < sizes.Length; j++)
        {
            int startIndex = (int)(c*j*(j+1)/2);
            if(sizes[j] == 0) continue;
            Vector2Int checkingChunk = list[startIndex];
            if(Mathf.Abs((playerChunk - checkingChunk).magnitude - j) > 3)
            {
                for(int i = 0; i < sizes.Length; i++)
                {
                    sizes[i] = 0;
                    Count = 0;
                }
                return true;
            }
        }
        return false;
    }
}
