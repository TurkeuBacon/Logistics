using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
MIT License

Copyright (c) 2021 Sebastian Lague

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
/*
MIT License

Copyright (c) 2016 Sebastian

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

public class MeshGenerator
{
    public bool setup;
    private static MeshGenerator instance;
    public static MeshGenerator getInstance()
    {
        if (instance == null)
            instance = new MeshGenerator();
        return instance;
    }
    public static void destroyInstance()
    {
        instance = null;
    }
    private MeshGenerator()
    {
        setup = false;
        //SetupMarchingCubesShader();
    }
    ~MeshGenerator()
    {
        Debug.Log("Destroying MeshGenerator");
        if (setup)
        {
            DisposeMarchingCubesShader();
        }
    }

    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;

    void SetupMarchingCubesShader(int gridSide, int gridHeight)
    {
        int numCubes = (gridSide + 1) * (gridSide + 1) * (gridHeight + 1);
        int maxTris = numCubes * 5; // Max 5 Tris per cube, 3 verts per tri

        int vector3Size = sizeof(float) * 3;
        int VertexStructSize = vector3Size * 2;
        int TriangleStructSize = VertexStructSize * 3 + sizeof(int) * 3;

        triangleBuffer = new ComputeBuffer(maxTris, TriangleStructSize, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        setup = true;
    }
    void DisposeMarchingCubesShader()
    {
        Debug.Log("Disposing of Compute Buffers");
        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
        setup = false;
    }

    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    }

    struct Triangle
    {
        public Vector3Int index;
        public Vertex vert1;
        public Vertex vert2;
        public Vertex vert3;
    };

    public MeshData GenerateMeshGPU(RenderTexture noiseRendTex, float surfaceLevel, float scale, ComputeShader shader, int lod)
    {
        MeshData meshData = new MeshData();
        int mapSize = noiseRendTex.width - 3;
        int mapHeight = noiseRendTex.volumeDepth - 1;
        //Debug.Log("Compute Sizes: " + mapSize + " x " + mapSize + " x " + mapHeight);

        if(!setup)
            SetupMarchingCubesShader(mapSize, mapHeight);

        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(shader.FindKernel("GenerateMarchingCube"), "triangles", triangleBuffer);

        //Texture3D texture = TextureGenerator.texture3DFromNoiseMap(mapData.noiseMap);
        shader.SetTexture(shader.FindKernel("GenerateMarchingCube"), "noiseMapTexture", noiseRendTex);

        //Texture3D gridToWorld = TextureGenerator.textureGridPoints(mapData.gridPoints);
        //shader.SetTexture(shader.FindKernel("GenerateMarchingCube"), "gridToWorld", gridToWorld);

        shader.SetFloat("surfaceLevel", surfaceLevel);
        shader.SetFloat("scale", scale);
        shader.SetInt("noiseMapSize", noiseRendTex.width);
        shader.SetInt("incrementAmmount", (int) Mathf.Pow(2, lod));

        shader.Dispatch(shader.FindKernel("GenerateMarchingCube"), mapSize / 8, mapSize / 8, mapHeight / 16);

        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] counter = new int[1] { 0 };
        triCountBuffer.GetData(counter);

        int count = counter[0];
        int vector3Size = sizeof(float) * 3;
        int VertexStructSize = vector3Size * 2;

        Triangle[] tris = new Triangle[count];
        triangleBuffer.GetData(tris);

        Dictionary<Vector3, int> usedVerts = new Dictionary<Vector3, int>();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        Vector3[] vertTemp;
        Vector3[] normalTemp;
        int yeeted = 0;

        for (int i = 0; i < count; i++)
        {
            vertTemp = new Vector3[3] { tris[i].vert1.position,tris[i].vert2.position, tris[i].vert3.position };
            normalTemp = new Vector3[3] { tris[i].vert1.normal, tris[i].vert2.normal, tris[i].vert3.normal };
            for (int s = 0; s < 3; s++)
            {
                Vector3 currentVert = vertTemp[s];
                if (usedVerts.ContainsKey(currentVert))
                {
                    yeeted++;
                    triangles.Add(usedVerts[currentVert]);
                }
                else
                {
                    usedVerts.Add(currentVert, vertices.Count);
                    normals.Add(normalTemp[s]);
                    triangles.Add(vertices.Count);
                    vertices.Add(currentVert);
                }
            }
        }

        meshData.vertices = vertices;
        meshData.triangles = triangles;
        meshData.normals = normals.ToArray();
        //meshData.RecalculateNormals();
        //Debug.Log(randNormal + ": " + meshData.normals[randNormal]);
        //Debug.Log(randNormal + ": " + meshData.normals[randNormal]);

        return meshData;
    }
}

public class MeshData
{
    public Dictionary<Vector3, int> usedVerts;
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector2> uv;
    public Vector3[] normals;
    public int triCount;

    public MeshData()
    {
        triCount = 0;
        usedVerts = new Dictionary<Vector3, int>();
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uv = new List<Vector2>();
        normals = null;
    }

    public void RecalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Count];
        int triangleCount = triangles.Count / 3;
        for(int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }
        for(int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        normals = vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = vertices[indexA];
        Vector3 pointB = vertices[indexB];
        Vector3 pointC = vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
    private Vector3 GetPoint(int x, int y, int z, Vector3[,,] gridPoints, Vector3Int basicPos)
    {
        return gridPoints[x + basicPos.x - 1, y + basicPos.z - 1, z + basicPos.y];
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals;
        return mesh;
    }
}
