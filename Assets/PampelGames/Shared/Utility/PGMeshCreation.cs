// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PampelGames.Shared.Utility
{
    public static class PGMeshCreation
    {
        /// <summary>
        ///     forwardAxis = Z is equal to the Unity plane.
        /// </summary>
        /// <param name="uvX">Default is Vector2(0,1).</param>
        /// <param name="uvY">Default is Vector2(0,1).</param>
        public static void Rectangle(Mesh mesh, float width, float length, PGEnums.Axis forwardAxis, Vector2 uvX = default, Vector2 uvY = default)
        {
            RectangleInternal(mesh, width, length, forwardAxis, uvX, uvY);
        }

        /// <summary>
        ///     Creates a circle.
        /// </summary>
        /// <param name="resolution">Amount of edgeloops, where one loop covers the radius (not diameter).</param>
        /// <param name="degrees">Starting at forward direction and going clockwise.</param>
        /// <param name="uvRadius">Starting at UV(0.5, 1) and going clockwise.</param>
        /// <param name="rotationY">Y-Rotation in degrees.</param>
        public static void Circle(Mesh mesh, float radius, int resolution, float degrees = 360f, float uvRadius = 0.5f, float rotationY = 0f)
        {
            CircleInternal(mesh, radius, resolution, degrees, uvRadius, rotationY);
        }


        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private static void RectangleInternal(Mesh mesh, float width, float length, PGEnums.Axis forwardAxis, Vector2 uvX = default,
            Vector2 uvY = default)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var uv01 = new Vector2(0f, 0f);
            var uv02 = new Vector2(1f, 0f);
            var uv03 = new Vector2(0f, 1f);
            var uv04 = new Vector2(1f, 1f);

            if (uvX != Vector2.zero || uvY != Vector2.zero)
            {
                uv01 = new Vector2(uvX.x, uvY.x);
                uv02 = new Vector2(uvX.y, uvY.x);
                uv03 = new Vector2(uvX.x, uvY.y);
                uv04 = new Vector2(uvX.y, uvY.y);
            }

            if (forwardAxis == PGEnums.Axis.Z)
            {
                triangles.AddRange(new[] {2, 1, 0, 1, 2, 3});
                vertices.AddRange(new Vector3[]
                {
                    new(-width / 2, 0, -length / 2),
                    new(width / 2, 0, -length / 2),
                    new(-width / 2, 0, length / 2),
                    new(width / 2, 0, length / 2)
                });
                normals.AddRange(new[] {Vector3.up, Vector3.up, Vector3.up, Vector3.up});
                uvs.AddRange(new[] {uv01, uv02, uv03, uv04});
            }
            else if (forwardAxis == PGEnums.Axis.Y)
            {
                triangles.AddRange(new[] {0, 1, 2, 3, 2, 1});
                vertices.AddRange(new Vector3[]
                {
                    new(-width / 2, -length / 2, 0),
                    new(width / 2, -length / 2, 0),
                    new(-width / 2, length / 2, 0),
                    new(width / 2, length / 2, 0)
                });
                normals.AddRange(new[] {Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward});
                uvs.AddRange(new[] {uv01, uv02, uv03, uv04});
            }
            else
            {
                triangles.AddRange(new[] {2, 1, 0, 1, 2, 3});
                vertices.AddRange(new Vector3[]
                {
                    new(0, -width / 2, -length / 2),
                    new(0, width / 2, -length / 2),
                    new(0, -width / 2, length / 2),
                    new(0, width / 2, length / 2)
                });
                normals.AddRange(new[] {Vector3.right, Vector3.right, Vector3.right, Vector3.right});
                uvs.AddRange(new[] {uv01, uv02, uv03, uv04});
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            PGMeshUtility.RecalculateMeshData(mesh, false);
        }

        private static void CircleInternal(Mesh mesh, float radius, int resolution, float degrees = 360f, float uvRadius = 0.5f,
            float rotationY = 0f)
        {
            resolution++;
            var totalPoints = Mathf.FloorToInt(resolution * (degrees / 360f)) + 1;
            var radStartRotation = rotationY * Mathf.Deg2Rad;

            var vertices = new Vector3[totalPoints];
            var uv = new Vector2[totalPoints];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);

            for (var i = 0; i < totalPoints - 1; i++)
            {
                var ratio = (float) i / (totalPoints - 2);
                var rad = ratio * Mathf.Deg2Rad * degrees + radStartRotation;
                var cos = Mathf.Cos(rad);
                var sin = Mathf.Sin(rad);

                vertices[i + 1] = new Vector3(sin * radius, 0, cos * radius);
                uv[i + 1] = new Vector2(sin * uvRadius + 0.5f, cos * uvRadius + 0.5f);
            }

            var triangles = new int[(totalPoints - 2) * 3];

            for (var i = 0; i < totalPoints - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            PGMeshUtility.RecalculateMeshData(mesh, true);
        }
    }
}