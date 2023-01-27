using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TerrainGenerator : MonoBehaviour
{
    static bool help = false;
    public ComputeShader noiseCompute3D, marchingCubesShader, listTextureTransfer;

    public int chunkResolution = 33;
    public int chunkResHeight = 257;

    public int seed;

    public int noiseScale;
    [Range(1, 6)]
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    [Range(0, 1)]
    public float marchingCubesSurfaceLevel;

    public Mesh generateMeshGPU(RenderTexture texture)
    {
        return MeshGenerator.getInstance().GenerateMeshGPU(texture, marchingCubesSurfaceLevel, ((float)Chunk.chunkSize / (chunkResolution - 1)), marchingCubesShader, 0).CreateMesh();
    }

    private void setupRenderTexture3D(ref RenderTexture texture, int width, int depth, int height)
    {
        if(texture != null)
        {
            texture.Release();
            texture.DiscardContents();
        }
        texture = new RenderTexture(width, depth, 0);
        texture.volumeDepth = height;
        texture.enableRandomWrite = true;
        texture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        texture.Create();
    }
    public void GenerateDensityMap(ref RenderTexture texture, Vector2 coord)
    {
        int groupsSides = Mathf.CeilToInt((chunkResolution + 2) / 8f);
        int groupsHeight = Mathf.CeilToInt(chunkResHeight / 16f);
        
        setupRenderTexture3D(ref texture, chunkResolution + 2, chunkResolution + 2, chunkResHeight);

        System.Random prng = new System.Random(seed);

        float[] offsetsArrayF = new float[6*4];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        for (int i = 0; i < octaves; i++)
        {
            offsetsArrayF[i*4] = coord.x * (chunkResolution - 1);
            offsetsArrayF[i*4 + 1] = coord.y * (chunkResolution - 1);
            offsetsArrayF[i*4 + 2] = 0f;
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }
        
        noiseCompute3D.SetInt("octaves", octaves);
        noiseCompute3D.SetFloat("persistance", persistance);
        noiseCompute3D.SetFloat("lacunarity", lacunarity);
        noiseCompute3D.SetFloat("noiseScale", noiseScale / 500f);

        int kernel = noiseCompute3D.FindKernel("GenerateNoise");

        noiseCompute3D.SetFloats("offsets", offsetsArrayF);
        noiseCompute3D.SetFloat("maxPossibleValue", maxPossibleHeight);

        noiseCompute3D.SetFloat("surfaceLevel", marchingCubesSurfaceLevel);

        noiseCompute3D.SetInt("noiseSides", chunkResolution + 2);
        noiseCompute3D.SetInt("noiseHeight", chunkResHeight);

        noiseCompute3D.SetTexture(kernel, "Result", texture);

        noiseCompute3D.Dispatch(kernel, groupsSides, groupsSides, groupsHeight);
    }

    public float[] RenderTextureToList(ref RenderTexture texture)
    {
        float[] list = new float[(chunkResolution + 2) * (chunkResolution + 2) * chunkResHeight];
        int groupsSides = Mathf.CeilToInt((chunkResolution + 2) / 8f);
        int groupsHeight = Mathf.CeilToInt(chunkResHeight / 16f);
        int kernel = listTextureTransfer.FindKernel("gpuToCpu");
        
        listTextureTransfer.SetInt("width", chunkResolution + 2);
        listTextureTransfer.SetInt("depth", chunkResolution + 2);
        listTextureTransfer.SetInt("height", chunkResHeight);
        
        listTextureTransfer.SetTexture(kernel, "rendTexture", texture);
        ComputeBuffer listBuffer = new ComputeBuffer(list.Length, sizeof(float));
        listTextureTransfer.SetBuffer(kernel, "list", listBuffer);

        listTextureTransfer.Dispatch(kernel, groupsSides, groupsSides, groupsHeight);

        listBuffer.GetData(list);

        if(!help && false)
        {
            for(int slice = 0; slice < chunkResolution + 2; slice++)
            {
                string listS = "";
                for(int z = 0; z < chunkResHeight; z++)
                {
                    listS += list[z * chunkResHeight + slice * (chunkResolution + 2) + 0] > marchingCubesSurfaceLevel ? "O" : "X";
                    for(int x = 1; x < chunkResolution + 2; x++)
                    {
                        listS += ", " + (list[z * chunkResHeight + slice * (chunkResolution + 2) + x] > marchingCubesSurfaceLevel ? "O" : "X");
                    }
                    listS += "\n";
                }
                Debug.Log(listS);
            }
            help = true;
        }

        listBuffer.Release();
        return list;
    }
    public void ListToRenderTexture(ref float[] list, ref RenderTexture texture)
    {
        setupRenderTexture3D(ref texture, chunkResolution + 2, chunkResHeight + 2, chunkResHeight);
        int groupsSides = Mathf.CeilToInt((chunkResolution + 2) / 8f);
        int groupsHeight = Mathf.CeilToInt(chunkResHeight / 16f);
        int kernel = listTextureTransfer.FindKernel("cpuToGpu");

        listTextureTransfer.SetInt("width", chunkResolution + 2);
        listTextureTransfer.SetInt("depth", chunkResolution + 2);
        listTextureTransfer.SetInt("height", chunkResHeight);
        
        listTextureTransfer.SetTexture(kernel, "rendTexture", texture);
        ComputeBuffer listBuffer = new ComputeBuffer(list.Length, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);

        NativeArray<float> listBufferInput = listBuffer.BeginWrite<float>(0, list.Length);
        listBufferInput.CopyFrom(list);
        for(int z = 0; z < chunkResHeight; z++)
        {
            for(int y = 0; y < chunkResolution + 2; y++)
            {
                for(int x = 1; x < chunkResolution + 2; x++)
                {
                    list[z * (chunkResolution + 2) + y * (chunkResolution + 2) + x] = z / (float)chunkResHeight;
                }
            }
        }
        if(!help)
        {
            for(int slice = 0; slice < chunkResolution + 2; slice++)
            {
                string listS = "";
                for(int z = 0; z < chunkResHeight; z++)
                {
                    listS += listBufferInput[z * chunkResHeight + slice * (chunkResolution + 2) + 0] > marchingCubesSurfaceLevel ? "O" : "X";
                    for(int x = 1; x < chunkResolution + 2; x++)
                    {
                        listS += ", " + (listBufferInput[z * chunkResHeight + slice * (chunkResolution + 2) + x] > marchingCubesSurfaceLevel ? "O" : "X");
                    }
                    listS += "\n";
                }
                Debug.Log(listS);
            }
            help = true;
        }
        listBuffer.EndWrite<float>(list.Length);

        listTextureTransfer.SetBuffer(kernel, "list", listBuffer);

        listTextureTransfer.Dispatch(kernel, groupsSides, groupsSides, groupsHeight);
        listBuffer.Release();
    }
}
