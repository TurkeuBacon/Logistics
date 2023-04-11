using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Heap<T>
{
    private HeapEntry[] list;
    private int maxSize;
    public int size;
    public Heap(int maxSize)
    {
        this.maxSize = maxSize;
        list = new HeapEntry[maxSize+1];
        size = 0;
    }

    public T PopMin()
    {
        if(size <= 0) return list[0].data;
        T min = list[1].data;
        list[1] = list[size];
        size--;

        sink(1);

        return min;
    }

    public bool Insert(T data, float key)
    {
        Debug.Log("Insert: " + data);
        if(size < maxSize)
        {
            HeapEntry entry = new HeapEntry(data, key);

            size++;
            list[size] = entry;
            swim(size);

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Contains(T data)
    {
        for(int i = 1; i <= size; i++)
        {
            if(list[i].data.Equals(data)) 
            {
                return true;
            }
        }
        return false;
    }

    private void sink(int startI)
    {
        int i = startI;
        int minChild;
        while(i < size)
        {
            if(i*2 <= size)
            {
                if(i*2+1 > size)
                    minChild = i*2;
                else
                {
                    if(list[i*2].key < list[i*2+1].key)
                        minChild = i*2;
                    else
                        minChild = i*2+1;
                }
            }
            else if(i*2+1 <= size)
            {
                minChild = i*2+1;
            }
            else { break; }
            if(list[i].key <= list[minChild].key) { break; }
            HeapEntry temp = list[i];
            list[i] = list[minChild];
            list[minChild] = temp;
            i = minChild;
        }
    }
    private void swim(int startI)
    {
        int i = startI;
        while(i > 1)
        {
            if(list[i].key >= list[i/2].key) break;
            HeapEntry temp = list[i];
            list[i] = list[i/2];
            list[i/2] = temp;
            i /= 2;
        }
    }

    private struct HeapEntry
    {
        public T data;
        public float key;

        public HeapEntry(T data, float key)
        {
            this.data = data;
            this.key = key;
        }
    }
}