using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ChunkDataViewer : MonoBehaviour
{
    public int slice = 0;
    private int lastSlice = 1;
    private const int sliceWidth = 200;

    public Chunk chunk;

    Texture sliceTexture;
    GUILayoutOption[] densityMapSliceOptions;

    public Texture getSlice()
    {
        if(slice != lastSlice)
        {
            if(slice >= Chunk.terrainGenerator.chunkResolution + 2 || slice < 0)
            {
                slice = lastSlice;
            }
            else
            {
                if(sliceTexture && sliceTexture.GetType() == typeof(RenderTexture))
                {
                    ((RenderTexture)sliceTexture).Release();
                }
                sliceTexture = chunk.getSlice(slice);
                densityMapSliceOptions = new GUILayoutOption[]{ GUILayout.Width(sliceWidth), GUILayout.Height(sliceWidth*3) };
            }
        }
        lastSlice = slice;
        return sliceTexture;
    }
    public GUILayoutOption[] GetLayoutOptions()
    {
        return densityMapSliceOptions;
    }
}

[CustomEditor(typeof(ChunkDataViewer))]
public class ChunkEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ChunkDataViewer dataViewer = (ChunkDataViewer)target;
        GUILayout.Label("", dataViewer.GetLayoutOptions());
        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), dataViewer.getSlice());
    }
}
