using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class TerrainGenerator : MonoBehaviour
{
    public ComputeShader noiseCompute3D, marchingCubesShader, listTextureTransfer, slicer;

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
        ComputeBuffer listBuffer = new ComputeBuffer(list.Length, sizeof(float));
        listBuffer.SetData(list);

        listTextureTransfer.SetBuffer(kernel, "list", listBuffer);

        listTextureTransfer.Dispatch(kernel, groupsSides, groupsSides, groupsHeight);
        listBuffer.Release();
    }

    public RenderTexture getSlice(RenderTexture densityMap, int slice)
    {
        RenderTexture sliceTexture = new RenderTexture(densityMap.width, densityMap.volumeDepth, 1);
        sliceTexture.format = RenderTextureFormat.ARGBFloat;
        sliceTexture.enableRandomWrite = true;

        int kernel = slicer.FindKernel("getSlice");
        int width = sliceTexture.width;
        int height = sliceTexture.height;

        slicer.SetInt("width", width);
        slicer.SetInt("height", height);
        slicer.SetInt("slice", slice);
        slicer.SetTexture(kernel, "densityMap", densityMap);
        slicer.SetTexture(kernel, "sliceTexture", sliceTexture);

        slicer.Dispatch(kernel, Mathf.CeilToInt(width/32f), Mathf.CeilToInt(height/32f), 1);

        return sliceTexture;
    }

    public Texture2D getSlice(float[] densityMap, int slice)
    {
        int width = chunkResolution+2;
        int height = chunkResHeight;
        Texture2D sliceTexture = new Texture2D(width, height);
        Color[] colors = new Color[width*height];
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                float value = densityMap[(y*width + slice)*width+x];
                colors[y*width+x] = new Color(value, 0f, 0f, 1f);
            }
        }
        sliceTexture.SetPixels(colors);
        sliceTexture.Apply();
        return sliceTexture;
    }
}
