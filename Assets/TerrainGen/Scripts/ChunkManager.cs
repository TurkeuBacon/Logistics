using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public int debugLevel;
    public GameObject target, pleaseHold;
    [Range(1, 64)]
    public int renderDistance, editableDistance;
    private int lastRenderDistance; //!!!!!!!!!!!!DELETE THIS WHEN DONE TESTING!!!!!!!!!!!!
    private int sqrRenderDistance;
    public int maxEditableChunks;
    public bool clearingEditables;
    public int chunkLoadRate, maxQueuedChunks, maxInactiveChunks, chunkQueueCleanupFrequency;
    private const float
        chunkLoadCost = 7.5f,
        chunkDeleteCost = 3.35f,
        setEditableCost = 0.15f,
        setNonEditableCost = 1f;

    public Material chunkMaterial;

    private Vector2 targetPosition;
    public int minChunksForSpawn;
    private bool spawningTarget;

    private ChunkLoadQueue chunksToBeLoaded;
    private int chunkQueueCleanupTimer;

    private Dictionary<Vector2, Chunk> chunks;
    private Dictionary<Vector2, Chunk> inactiveChunks;
    private Stack<Chunk> deletedChunks;

    private List<Chunk> outOfRangeEditables;
    private List<Chunk> inRangeStatics;

    void Awake()
    {
        Chunk.terrainGenerator = GetComponent<TerrainGenerator>();
        Chunk.debugLevel = debugLevel;
    }

    void Start()
    {
        setRenderDistance(renderDistance);
        spawningTarget = false;
        chunks = new Dictionary<Vector2, Chunk>();
        inactiveChunks = new Dictionary<Vector2, Chunk>();
        deletedChunks = new Stack<Chunk>();
        outOfRangeEditables = new List<Chunk>();
        inRangeStatics = new List<Chunk>();
        clearingEditables = false;
        chunkQueueCleanupTimer = 0;
        for(int i = 0; i < 20000; i++)
        {
            deletedChunks.Push(new Chunk(this.transform, chunkMaterial));
        }
        FindObjectOfType<PauseMenuController>().gameplaySettingsApply += applySettings;
        //target.SetActive(false);
        //pleaseHold.SetActive(true);
    }

    //Vector2Int currChunkTest = Vector2Int.zero;
    void Update()
    {
        if(spawningTarget && (chunks.Count-inactiveChunks.Count) > minChunksForSpawn)
        {
            spawnTarget();
        }
        if(lastRenderDistance != renderDistance)
        {
            setRenderDistance(renderDistance);
        }
        targetPosition = new Vector2(target.transform.position.x, target.transform.position.z);
        Vector2Int targetCurrentChunk = Vector2Int.FloorToInt(targetPosition / Chunk.chunkSize);
        //Debug.Log("Target Chunk" + targetCurrentChunk + "-------------------");
        if(target.activeSelf && !chunks.ContainsKey(targetCurrentChunk))
        {
            target.SetActive(false);
            pleaseHold.SetActive(true);
        }
        for(int j = targetCurrentChunk.y - renderDistance; j <= targetCurrentChunk.y + renderDistance; j++)
        {
            for(int i = targetCurrentChunk.x - renderDistance; i <= targetCurrentChunk.x + renderDistance; i++)
            {
                Vector2Int currentKey = new Vector2Int(i, j);
                int sqrMagnitude = (currentKey - targetCurrentChunk).sqrMagnitude;
                if(sqrMagnitude > sqrRenderDistance) continue;
                if(chunks.ContainsKey(currentKey))
                {
                    Chunk currentChunk = chunks[currentKey];
                    // Chunk is in range to be active
                    currentChunk.setActive(true);
                    inactiveChunks.Remove(currentKey);
                    // In Range To Be Editable
                    if(sqrMagnitude < editableDistance*editableDistance)
                    {
                        if(currentChunk.editable)
                        {
                            // Player came back in range of editable chunk,
                            // The chunk can stay editable
                            outOfRangeEditables.Remove(currentChunk);
                        }
                        else if(!inRangeStatics.Contains(currentChunk))
                        {
                            // Player came into range of static chunk
                            inRangeStatics.Add(currentChunk);
                        }
                    }
                    // Out of Range to be Editable
                    else
                    {
                        if(currentChunk.editable)
                        {
                            // Player went out of range of editable chunk
                            if(!outOfRangeEditables.Contains(currentChunk))
                            {
                                outOfRangeEditables.Add(currentChunk);
                            }
                        }
                        else
                        {
                            // Player went back out of range of a static chunk
                            inRangeStatics.Remove(currentChunk);
                        }
                    }
                }
                else
                {
                    // Chunk does not exist yet
                    // Queued to be loaded
                    if(!chunksToBeLoaded.Contains(currentKey) /*&& chunksToBeLoaded.size < maxQueuedChunks*/)
                    {
                        bool success = chunksToBeLoaded.Enqueue(currentKey, (int)(targetCurrentChunk-currentKey).magnitude);
                    }
                }
            }
        }
        // Deactivate chunks outside of the render distance
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

        if(Chunk.numEditables > maxEditableChunks) clearingEditables = true;

        float availableCost = 16.5f;
        // Execute Queued Tasks. Kinda Scuffed
        if(inactiveChunks.Count > maxInactiveChunks && availableCost - chunkLoadRate*chunkLoadCost > 0)
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
            availableCost -= chunkDeleteCost;
        }
        // Load in the set ammount of chunks, if there is enough weight
        for(int i = 0; i < chunkLoadRate && chunksToBeLoaded.Count != 0 && availableCost > 0; i++)
        {
            Vector2Int currentKey = chunksToBeLoaded.Dequeue();
            if(chunkQueueCleanupTimer >= chunkQueueCleanupFrequency)
            {
                if(chunksToBeLoaded.rangeCheck(targetCurrentChunk))
                {
                    Debug.Log("Chunk Queue Cleanup");
                }
                chunkQueueCleanupTimer = 0;
            }
            else
            {
                chunkQueueCleanupTimer++;
            }
            if(Mathf.Abs((currentKey-targetCurrentChunk).x) > renderDistance || Mathf.Abs((currentKey-targetCurrentChunk).y) > renderDistance || chunks.ContainsKey(currentKey))
            {
                i--;
                continue;
            }
            if(deletedChunks.Count == 0) continue;
            Chunk temp = deletedChunks.Pop();
            bool createEditable = (targetCurrentChunk - currentKey).sqrMagnitude < editableDistance*editableDistance && Chunk.numEditables < maxEditableChunks;
            temp.Create(currentKey, createEditable);
            chunks.Add(currentKey, temp);
            if(currentKey == targetCurrentChunk)
            {
                spawningTarget = true;
            }
            availableCost -= chunkLoadCost;
        }
        // Set as many chunks editable as possible
        while(availableCost > 0 && inRangeStatics.Count > 0)
        {
            Chunk chunk = inRangeStatics[0];
            chunk.setEditable(true);
            inRangeStatics.RemoveAt(0);
            availableCost -= setEditableCost;
        }
        // Set as many chunks non editable as possible
        if(clearingEditables)
        {
            while(availableCost > 0 && outOfRangeEditables.Count > 0)
            {
                Chunk chunk = outOfRangeEditables[0];
                chunk.setEditable(false);
                outOfRangeEditables.RemoveAt(0);
                availableCost -= setNonEditableCost;
            }
            if(outOfRangeEditables.Count == 0)
            {
                clearingEditables = false;
            }
        }
    }

    public void spawnTarget()
    {
        RaycastHit spawnHeightHit;
        float chunkScale = Chunk.chunkSize / (FindObjectOfType<TerrainGenerator>().chunkResolution - 1);
        float trueChunkHeight = FindObjectOfType<TerrainGenerator>().chunkResHeight * chunkScale;
        Vector3 castPos = new Vector3(target.transform.position.x, 0f, target.transform.position.z) + Vector3.up * trueChunkHeight;
        if(Physics.Raycast(castPos, Vector3.down, out spawnHeightHit, trueChunkHeight))
        {
            target.transform.position = spawnHeightHit.point + Vector3.up * 1f;
            target.SetActive(true);
            pleaseHold.SetActive(false);
            spawningTarget = false;
        }
    }

    public void setRenderDistance(int rd)
    {
        renderDistance = rd;
        lastRenderDistance = renderDistance;
        sqrRenderDistance = renderDistance*renderDistance;
        chunksToBeLoaded = new ChunkLoadQueue(rd);
    }

    private void applySettings(params int[] settings)
    {
        setRenderDistance(settings[0]);
    }
}

public class Chunk
{
    public static int debugLevel;
    public static TerrainGenerator terrainGenerator;
    public const int chunkSize = 16;
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
        go.AddComponent<MeshCollider>();
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
        Mesh mesh = terrainGenerator.generateMeshGPU(densityMapGPU);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshCollider>().sharedMesh = mesh;
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
    public bool isActive()
    {
        return seenThisFrame;
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