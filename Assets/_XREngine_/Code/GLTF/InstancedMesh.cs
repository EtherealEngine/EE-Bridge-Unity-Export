using GLTF.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;


namespace XREngine.XREngineProject
{
    public class InstancedMesh : MonoBehaviour
    {
        public bool applyToChildren;
        public UnityEngine.Mesh Mesh
        {
            get
            {
                return GetComponent<MeshFilter>() == null ? null : GetComponent<MeshFilter>().sharedMesh;
            }
            set
            {
                GetComponent<MeshFilter>().sharedMesh = value;
            }
        }
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
        static List<InstancedMesh> _generatedNodes;
        public static List<InstancedMesh> GeneratedNodes 
        { 
            get
            {
                if(_generatedNodes == null)
                {
                    _generatedNodes = new List<InstancedMesh>();
                }    
                return _generatedNodes;
            } 
        }
        static bool checkForDuplicates = true;
        static bool SameMesh(UnityEngine.Mesh mesh1, UnityEngine.Mesh mesh2)
        {

            if (mesh1 == null && mesh2 == null) return true;
            if (mesh1 == null || mesh2 == null) return false;
            bool result = true;
            result = result && mesh1.vertexCount == mesh2.vertexCount;
            result = result && mesh1.vertexAttributeCount == mesh2.vertexAttributeCount;
            result = result && mesh1.triangles.Length == mesh2.triangles.Length;
            result = result && mesh1.subMeshCount == mesh2.subMeshCount;
            return result;
        }
        public static InstanceMeshNode[] GenerateMeshNodes()
        {
        
            var iNodes = FindObjectsOfType<InstancedMesh>();
            var parents = iNodes.Where((iNode) => iNode.applyToChildren);
            foreach(var parent in parents)
            {
                foreach(var renderer in XREUnity.ChildComponents<MeshRenderer>(parent.transform))
                {
                    var iNode = renderer.gameObject.AddComponent<InstancedMesh>();
                    GeneratedNodes.Add(iNode);
                    iNodes.Append(iNode);
                }
            }
            
            if(checkForDuplicates)
            {
                var registry = new Dictionary<string, List<UnityEngine.Mesh>>();
                foreach (var iNode in iNodes)
                {
                    var mesh = iNode.Mesh;
                    if (mesh == null) continue;
                    string name = mesh.name;
                    string id = Regex.Match(name, @"^/w+").Value;
                    if(registry.ContainsKey(id))
                    {
                        var register = registry[id];
                        bool set = false;
                        for(int j = 0; j < register.Count; j++)
                        {
                            var regMesh = register[j];
                            if(SameMesh(mesh, regMesh))
                            {
                                iNode.Mesh = regMesh;
                                set = true;
                            }    
                        }
                        if(!set)
                        {
                            register.Add(mesh);
                        }
                    }
                }
            }

            var results = iNodes.Where((im) => im.gameObject.activeInHierarchy &&
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

        public static void CleanupGeneratedMeshNodes()
        {
            if(GeneratedNodes.Count > 0)
            {
                foreach(var iNode in GeneratedNodes)
                {
                    DestroyImmediate(iNode);
                }
                GeneratedNodes.Clear();
            }
        }
    }

}
