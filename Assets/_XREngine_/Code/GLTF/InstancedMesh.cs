using GLTF.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XREngine.XREngineProject
{
    public class InstancedMesh : MonoBehaviour
    {
        public UnityEngine.Mesh Mesh => GetComponent<MeshFilter>().sharedMesh;
        public UnityEngine.Material Material => GetComponent<MeshRenderer>().sharedMaterial;
    }
    public class InstanceMeshNode : MonoBehaviour
    {
        public UnityEngine.Mesh mesh;
        public UnityEngine.Material material;

        public Transform[] xforms;
        struct MeshRend
        {
            public UnityEngine.Mesh mesh;
            public UnityEngine.Material material;
        }
        public static InstanceMeshNode[] GenerateMeshNodes()
        {
            var results = FindObjectsOfType<InstancedMesh>().Where((im) => im.gameObject.activeInHierarchy &&
                                                                           im.GetComponent<MeshRenderer>() != null &&
                                                                           im.GetComponent<MeshRenderer>().enabled).GroupBy((x) => new MeshRend { material = x.Material, mesh = x.Mesh })
                .Select((entry) =>
                {
                    GameObject go = new GameObject(entry.Key.mesh.name + "_instanced", new[] { typeof(InstanceMeshNode), typeof(MeshFilter), typeof(MeshRenderer) });
                    var iNode = go.GetComponent<InstanceMeshNode>();
                    
                    iNode.mesh = entry.Key.mesh;
                    iNode.material = entry.Key.material;
                    iNode.xforms = entry.Select((inode) => inode.GetComponent<Transform>()).ToArray();
                    
                    go.GetComponent<MeshFilter>().sharedMesh = iNode.mesh;
                    go.GetComponent<MeshRenderer>().sharedMaterial = iNode.material;

                    return iNode;
                }).ToArray();
            return results;
        }
    }

}
