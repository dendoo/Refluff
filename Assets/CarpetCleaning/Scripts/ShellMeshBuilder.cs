using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarpetCleaning
{
    /// <summary>Procedural meshes for the shell carpet, the water grid and the optional fins (section 3.9).</summary>
    public static class ShellMeshBuilder
    {
        /// <summary>ONE mesh with stacked grids. TEXCOORD0 = carpet UV, TEXCOORD1 = (t_i, 0), t_i = pow(i/(shells-1), 1.3).</summary>
        public static Mesh BuildShells(float sizeX, float sizeZ, int shells, int subdivX, int subdivZ, float pileM)
        {
            shells = Mathf.Max(1, shells); subdivX = Mathf.Max(1, subdivX); subdivZ = Mathf.Max(1, subdivZ);
            int vx = subdivX + 1, vz = subdivZ + 1;
            int perLayer = vx * vz;
            int vertCount = perLayer * shells;
            var pos = new Vector3[vertCount];
            var nrm = new Vector3[vertCount];
            var uv0 = new Vector2[vertCount];
            var uv1 = new Vector2[vertCount];
            var idx = new int[shells * subdivX * subdivZ * 6];

            float hx = sizeX * 0.5f, hz = sizeZ * 0.5f;
            int vi = 0, ii = 0;
            for (int layer = 0; layer < shells; layer++)
            {
                float t = shells > 1 ? Mathf.Pow(layer / (float)(shells - 1), 1.3f) : 0f;
                float y = t * pileM;
                int baseV = vi;
                for (int j = 0; j < vz; j++)
                {
                    float v = j / (float)subdivZ;
                    for (int i = 0; i < vx; i++)
                    {
                        float u = i / (float)subdivX;
                        pos[vi] = new Vector3(-hx + u * sizeX, y, -hz + v * sizeZ);
                        nrm[vi] = Vector3.up;
                        uv0[vi] = new Vector2(u, v);
                        uv1[vi] = new Vector2(t, 0f);
                        vi++;
                    }
                }
                for (int j = 0; j < subdivZ; j++)
                for (int i = 0; i < subdivX; i++)
                {
                    int a = baseV + j * vx + i, b = a + 1, c = a + vx, d = c + 1;
                    // front face seen from +Y (Unity clockwise winding): a, c, b / b, c, d
                    idx[ii++] = a; idx[ii++] = c; idx[ii++] = b;
                    idx[ii++] = b; idx[ii++] = c; idx[ii++] = d;
                }
            }

            var mesh = new Mesh { name = "CarpetShells", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(pos);
            mesh.SetNormals(nrm);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetIndices(idx, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(new Vector3(0f, pileM * 0.5f, 0f), new Vector3(sizeX, pileM + 0.2f, sizeZ));
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>Flat grid at y = 0 (water surface). TEXCOORD0 = carpet UV. Bounds expanded +0.1 in y.</summary>
        public static Mesh BuildGrid(float sizeX, float sizeZ, int subdivX, int subdivZ)
        {
            subdivX = Mathf.Max(1, subdivX); subdivZ = Mathf.Max(1, subdivZ);
            int vx = subdivX + 1, vz = subdivZ + 1;
            var pos = new Vector3[vx * vz];
            var nrm = new Vector3[vx * vz];
            var uv0 = new Vector2[vx * vz];
            var idx = new int[subdivX * subdivZ * 6];
            float hx = sizeX * 0.5f, hz = sizeZ * 0.5f;
            int vi = 0, ii = 0;
            for (int j = 0; j < vz; j++)
            {
                float v = j / (float)subdivZ;
                for (int i = 0; i < vx; i++)
                {
                    float u = i / (float)subdivX;
                    pos[vi] = new Vector3(-hx + u * sizeX, 0f, -hz + v * sizeZ);
                    nrm[vi] = Vector3.up;
                    uv0[vi] = new Vector2(u, v);
                    vi++;
                }
            }
            for (int j = 0; j < subdivZ; j++)
            for (int i = 0; i < subdivX; i++)
            {
                int a = j * vx + i, b = a + 1, c = a + vx, d = c + 1;
                idx[ii++] = a; idx[ii++] = c; idx[ii++] = b;
                idx[ii++] = b; idx[ii++] = c; idx[ii++] = d;
            }
            var mesh = new Mesh { name = "WaterGrid", indexFormat = vx * vz > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(pos);
            mesh.SetNormals(nrm);
            mesh.SetUVs(0, uv0);
            mesh.SetIndices(idx, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(sizeX, 0.3f, sizeZ));
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>Stretch (5.4): one 4 mm x 20 mm billboard per cell centre, TEXCOORD1 = (across 0..1, up 0..1).</summary>
        public static Mesh BuildFins(int nx, int nz, float sizeX, float sizeZ)
        {
            nx = Mathf.Max(1, nx); nz = Mathf.Max(1, nz);
            int cells = nx * nz;
            var pos = new Vector3[cells * 4];
            var uv0 = new Vector2[cells * 4];
            var uv1 = new Vector2[cells * 4];
            var nrm = new Vector3[cells * 4];
            var idx = new int[cells * 6];
            float hx = sizeX * 0.5f, hz = sizeZ * 0.5f;
            const float halfW = 0.002f, height = 0.02f;
            int vi = 0, ii = 0;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float u = (i + 0.5f) / nx, v = (j + 0.5f) / nz;
                var c = new Vector3(-hx + u * sizeX, 0f, -hz + v * sizeZ);
                int b = vi;
                pos[vi] = c + new Vector3(-halfW, 0f, 0f); uv1[vi] = new Vector2(0f, 0f); uv0[vi] = new Vector2(u, v); nrm[vi++] = Vector3.up;
                pos[vi] = c + new Vector3(halfW, 0f, 0f);  uv1[vi] = new Vector2(1f, 0f); uv0[vi] = new Vector2(u, v); nrm[vi++] = Vector3.up;
                pos[vi] = c + new Vector3(-halfW, height, 0f); uv1[vi] = new Vector2(0f, 1f); uv0[vi] = new Vector2(u, v); nrm[vi++] = Vector3.up;
                pos[vi] = c + new Vector3(halfW, height, 0f);  uv1[vi] = new Vector2(1f, 1f); uv0[vi] = new Vector2(u, v); nrm[vi++] = Vector3.up;
                idx[ii++] = b; idx[ii++] = b + 2; idx[ii++] = b + 1;
                idx[ii++] = b + 1; idx[ii++] = b + 2; idx[ii++] = b + 3;
            }
            var mesh = new Mesh { name = "CarpetFins", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(pos);
            mesh.SetNormals(nrm);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetIndices(idx, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(sizeX, 0.3f, sizeZ));
            mesh.UploadMeshData(false);
            return mesh;
        }

        /// <summary>1x1 quad in local XZ centred at origin, UV 0..1, facing +Y (for the tool cursor).</summary>
        public static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "CursorQuad" };
            mesh.SetVertices(new List<Vector3> {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetIndices(new[] { 0, 2, 1, 1, 2, 3 }, MeshTopology.Triangles, 0, true);
            return mesh;
        }
    }
}
