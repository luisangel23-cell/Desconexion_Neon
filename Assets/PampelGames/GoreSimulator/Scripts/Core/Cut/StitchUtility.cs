// ----------------------------------------------------
// Gore Simulator
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using UnityEngine;

namespace PampelGames.GoreSimulator
{
    internal static class StitchUtility
    {
        public static Mesh StitchCutMesh(Transform smrTransform, ChunkClass chunkClass, List<Vector3> cutCenters)
        {
            
            var boundCenter = chunkClass.mesh.bounds.center;
            var chunkVertices = chunkClass.mesh.vertices;

            var stitchMeshes = new List<Mesh>();

            for (int i = 0; i < chunkClass.indexClasses.Count; i++)
            {
                var indexClass = chunkClass.indexClasses[i];
                if(indexClass.cutIndexes.Count == 0) continue;
                
                var stitchVertices = new List<Vector3>();

                for (int j = 0; j < indexClass.cutIndexes.Count; j++)
                {
                    stitchVertices.Add(chunkVertices[indexClass.cutIndexes[j]]);
                }
                
                var centerVertex = smrTransform.InverseTransformPoint(cutCenters[i]);

                var cutIndexPosition = chunkVertices[indexClass.cutIndexes[0]];
                Vector3 faceDirection = default;
                if (chunkClass.indexClasses.Count > 1)
                {
                    var oppositePositions = new List<Vector3>();
                    for (int j = 0; j < chunkClass.indexClasses.Count; j++)
                    {
                        if(j == i) continue;
                        if(chunkClass.indexClasses[j].cutIndexes.Count == 0) continue;
                        oppositePositions.Add(chunkVertices[chunkClass.indexClasses[j].cutIndexes[0]]);
                    }
                    if(oppositePositions.Count == 0) faceDirection = cutIndexPosition - boundCenter;
                    else faceDirection = cutIndexPosition - (oppositePositions.Aggregate((a, b) => a + b) / oppositePositions.Count);
                }
                else
                {
                    faceDirection = cutIndexPosition - boundCenter;   
                }
                stitchMeshes.Add(CreateStitchMesh(stitchVertices, centerVertex, faceDirection));
            }
            
            var combinedStitchMesh = PGMeshUtility.CombineMeshes(stitchMeshes.ToArray(), true); 
            var newMesh = PGMeshUtility.CombineMeshesManually(new[] {chunkClass.mesh, combinedStitchMesh});
            
            newMesh.RecalculateNormals();

            return newMesh;
        }

        private static Mesh CreateStitchMesh(List<Vector3> stitchVertices, Vector3 centerVertex, Vector3 faceDirection)
        {
            centerVertex += faceDirection.normalized * 0.01f; // Moving before the sew vertices (not perfect).
            
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertices = new List<Vector3> { centerVertex };
            normals.Add(Vector3.up);
            
            uvs.Add(new Vector2(0.5f, 0.5f)); // Center vertex
            
            for (int i = 0; i < stitchVertices.Count; i++)
            {
                vertices.Add(stitchVertices[i]);
                normals.Add(Vector3.up);

                var angle = i * Mathf.PI * 2 / stitchVertices.Count;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f;
                var uv = new Vector2(0.5f, 0.5f) + offset;
                uvs.Add(uv);
            }
            
            // Triangles
            // First checking which face direction is dominant, then using that for all.
            int dot = 0;
            for (int i = 0; i < stitchVertices.Count; i++)
            {
                int v0 = 0; // Index for the center vertex
                int v1 = i + 1; // Current stitch vertex index
                int v2 = i < stitchVertices.Count - 1 ? i + 2 : 1; // The next vertex index or, if it's the last vertex, wrap back to the first stitch vertex
                
                var faceNormal = Vector3.Cross(vertices[v1] - vertices[v0], vertices[v2] - vertices[v0]).normalized;
                if (PGTrigonometryUtility.IsSameDirection(faceNormal, faceDirection)) dot--;
                else dot++;
            }
            
            for (int i = 0; i < stitchVertices.Count; i++)
            {
                int v0 = 0; 
                int v1 = i + 1;
                int v2 = i < stitchVertices.Count - 1 ? i + 2 : 1;
                
                if (dot > 0) (v1, v2) = (v2, v1);
                triangles.AddRange(new[] { v0, v1, v2 });
            }
            
            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray(),
                normals = normals.ToArray(),
                tangents = new Vector4[vertices.Count]
            };
            
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}