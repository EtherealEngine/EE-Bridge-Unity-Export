using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace XREngine
{
    public class ReverseUV
    {
        public Mesh mesh;
        int nTris;

        public ReverseUV(Mesh _mesh)
        {
            mesh = _mesh;
            nTris = mesh.triangles.Length / 3;
            
        }

        //--/from https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle
        float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = Sign(pt, v1, v2);
            d2 = Sign(pt, v2, v3);
            d3 = Sign(pt, v3, v1);

            has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(has_neg && has_pos);
        }
        //--/



        public Vector2 UVAt(Vector2 pt, int lookIndex = 0, int outputIndex = 0)
        {
            int triIndex = 0;
            var tris = mesh.triangles;
            while(triIndex < nTris)
            {
                int i1 = tris[triIndex * 3];
                int i2 = tris[triIndex * 3 + 1];
                int i3 = tris[triIndex * 3 + 2];

                List<Vector2> lookUVs = new List<Vector2>();
                List<Vector2> outUVs = new List<Vector2>();

                mesh.GetUVs(lookIndex, lookUVs);
                mesh.GetUVs(outputIndex, outUVs);

                Vector2 v1 = lookUVs[i1];
                Vector2 v2 = lookUVs[i2];
                Vector2 v3 = lookUVs[i3];
                if (PointInTriangle(pt, v1, v2, v3))
                {
                    //barycentric coordinate weighting
                    float w1 = ((v2.y - v3.y) * (pt.x - v3.x) + (v3.x - v2.x) * (pt.y - v3.y)) /
                               ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
                    float w2 = ((v3.y - v1.y) * (pt.x - v3.x) + (v3.x - v2.x) * (pt.y - v3.y)) /
                               ((v2.y - v3.y) * (v1.x - v3.x) + (v3.x - v2.x) * (v1.y - v3.y));
                    float w3 = 1 - w1 - w2;

                    if(i1 >= outUVs.Count)
                    {
                        Debug.Log("uh oh");
                    }
                    Vector2 vOut1 = outUVs[i1];
                    Vector2 vOut2 = outUVs[i2];
                    Vector2 vOut3 = outUVs[i3];

                    Vector2 result = vOut1 * w1 + vOut2 * w2 + vOut3 * w3;
                    return result;
                }
                triIndex++;
            }
            return Vector2.negativeInfinity;
        }
        
        
    }

}
