using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class ChunkManager : MonoBehaviour
{
    public int debugLevel;
    public GameObject target;
    [Range(1, 64)]
    public int renderDistance, editableDistance;
    public int maxEditableChunks;
    public int chunkLoadRate, maxQueuedChunks;
    public int deEditRate;

    public Material chunkMaterial;

    private Vector2 targetPosition;

    private Queue<Vector2Int> chunksToBeLoaded;
    private List<Chunk> seenLastFrame;

    private Dictionary<Vector2, Chunk> chunks;
    private Dictionary<Vector2, Chunk> inactiveChunks;
    private Stack<Chunk> deletedChunks;

    private List<Chunk> outOfRangeEditables;
    public bool clearingEditables;

    void Awake()
    {
        Chunk.terrainGenerator = GetComponent<TerrainGenerator>();
        Chunk.debugLevel = debugLevel;
    }

    void Start()
    {
        chunksToBeLoaded = new Queue<Vector2Int>();
        seenLastFrame = new List<Chunk>();
        chunks = new Dictionary<Vector2, Chunk>();
        inactiveChunks = new Dictionary<Vector2, Chunk>();
        deletedChunks = new Stack<Chunk>();
        outOfRangeEditables = new List<Chunk>();
        clearingEditables = false;
        for(int i = 0; i < 1000; i++)
        {
            deletedChunks.Push(new Chunk(this.transform, chunkMaterial));
        }
    }

    void Update()
    {
        targetPosition = new Vector2(target.transform.position.x, target.transform.position.z);
        Vector2Int targetCurrentChunk = Vector2Int.FloorToInt(targetPosition / Chunk.chunkSize);
        for(int j = targetCurrentChunk.y - renderDistance; j <= targetCurrentChunk.y + renderDistance; j++)
        {
            for(int i = targetCurrentChunk.x - renderDistance; i <= targetCurrentChunk.x + renderDistance; i++)
            {
                Vector2Int currentKey = new Vector2Int(i, j);
                if((currentKey-targetCurrentChunk).sqrMagnitude > renderDistance*renderDistance) continue;
                if(chunks.ContainsKey(currentKey))
                {
                    Chunk currentChunk = chunks[currentKey];
                    if((targetCurrentChunk - currentKey).sqrMagnitude < editableDistance*editableDistance)
                    {
                        if(!currentChunk.editable)
                        {
                            currentChunk.setEditable(true);
                        }
                        else
                        {
                            outOfRangeEditables.Remove(currentChunk);
                        }
                    }
                    else if(currentChunk.editable && !outOfRangeEditables.Contains(currentChunk))
                    {
                        outOfRangeEditables.Add(currentChunk);
                    }
                    currentChunk.setActive(true);
                    if(inactiveChunks.ContainsKey(currentKey))
                    {
                        inactiveChunks.Remove(currentKey);
                    }
                }
                else
                {
                    if(!chunksToBeLoaded.Contains(currentKey) && chunksToBeLoaded.Count < maxQueuedChunks)
                        chunksToBeLoaded.Enqueue(currentKey);
                }
            }
        }
        foreach(Chunk chunk in chunks.Values)
        {
            if(chunk.seenThisFrame)
            {
                chunk.seenThisFrame = false;
            }
            else
            {
                chunk.setActive(false);
                if(!inactiveChunks.ContainsKey(chunk.key))
                {
                    inactiveChunks.Add(chunk.key, chunk);
                }
            }
        }
        if(inactiveChunks.Count > 500)
        {
            Debug.Log("Chunk Cleanup");
            foreach(Chunk chunk in inactiveChunks.Values)
            {
                chunk.Destroy();
                if(chunk.editable && outOfRangeEditables.Contains(chunk)) outOfRangeEditables.Remove(chunk);
                chunks.Remove(chunk.key);
                deletedChunks.Push(chunk);
            }
            inactiveChunks.Clear();
        }
        if(Chunk.numEditables >= maxEditableChunks && !clearingEditables)
        {
            Debug.Log("Editables Cleanup");
            clearingEditables = true;
            ClearEditables();
        }
        for(int i = 0; i < chunkLoadRate && chunksToBeLoaded.Count != 0; i++)
        {
            Vector2Int currentKey = chunksToBeLoaded.Dequeue();
            if(deletedChunks.Count == 0) continue;
            Chunk temp = deletedChunks.Pop();
            bool createEditable = (targetCurrentChunk - currentKey).sqrMagnitude < editableDistance*editableDistance && Chunk.numEditables < maxEditableChunks;
            temp.Create(currentKey, createEditable);
            chunks.Add(currentKey, temp);
        }
    }

    private void ClearEditables()
    {
        foreach(Chunk c in outOfRangeEditables)
        {
            c.setEditable(false);
        }
        outOfRangeEditables.Clear();
        clearingEditables = false;
    }
}

public class Chunk
{
    public static int debugLevel;
    public static TerrainGenerator terrainGenerator;
    public const int chunkSize = 32;
    private bool valid;
    private GameObject go;
    private RenderTexture densityMapGPU;
    public bool editable;
    public static int numEditables;
    private float[] densityMapCPU;

    public bool seenThisFrame;
    public Vector2Int key;
    Material main;

    public Chunk(Transform parent, Material mat)
    {
        main = mat;
        go = new GameObject();
        go.transform.parent = null;
        if(debugLevel > 0) go.AddComponent<ChunkDataViewer>();
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Destroy();
    }

    public void Create(Vector2Int position, bool isEditable)
    {
        valid = true;
        seenThisFrame = true;
        editable = true;
        key = position;
        numEditables++;

        terrainGenerator.GenerateDensityMap(ref densityMapGPU, key);
        if(debugLevel > 0) go.GetComponent<ChunkDataViewer>().chunk = this;
        // setEditable(false);
        // setEditable(true);
        Mesh mesh = terrainGenerator.generateMeshGPU(densityMapGPU);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.transform.position = new Vector3(position.x, 0f, position.y) * chunkSize;
        go.name = position.ToString();
        setEditable(isEditable);
    }
    public void Destroy()
    {
        valid = false;
        if(editable)
        {
            numEditables--;
            if(densityMapGPU)
            {
                densityMapGPU.Release();
                densityMapGPU.DiscardContents();
                densityMapGPU = null;
            }
        }
        seenThisFrame = false;
        go.name = "Free Chunk";
        go.SetActive(false);
    }
    public void setActive(bool active)
    {
        if(!valid) return;
        if(active == true) seenThisFrame = true;
        go.SetActive(active);
    }
    public void setEditable(bool isEditable)
    {
        if(!editable && isEditable)
        {
            if(densityMapCPU != null)
            {
                terrainGenerator.ListToRenderTexture(ref densityMapCPU, ref densityMapGPU);
                densityMapCPU = null;
            }
            numEditables++;
            editable = true;
        }
        else if(editable && !isEditable)
        {
            if(densityMapGPU == null) Debug.Log("Null Texture on transfer at " + key);
            else
            {
                densityMapCPU = terrainGenerator.RenderTextureToList(ref densityMapGPU);
                densityMapGPU.Release();
                densityMapGPU.DiscardContents();
                
                densityMapGPU = null;
            }
            numEditables--;
            editable = false;
        }
    }

    public Texture getSlice(int slice)
    {
        {
            if(editable)
            {
                return terrainGenerator.getSlice(densityMapGPU, slice);
            }
            else
            {
                return terrainGenerator.getSlice(densityMapCPU, slice);
            }
        }
    }
}