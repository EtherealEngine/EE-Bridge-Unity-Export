using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DigitalOpus.MB;
using UnityEditor;
using DigitalOpus.MB.Core;
using System;
#if UNITY_EDITOR
using DigitalOpus.MB.MBEditor;

namespace XREngine
{
    [System.Serializable]
    public class MeshBakeResult
    {
        public GameObject[] originals;
        public GameObject[] combined;
    }

    public class MeshBake : MonoBehaviour
    {
        public enum Resolution
        {
            _512=512,
            _1024=1024,
            _2048=2048,
            _4096=4096
        }
        public enum Mode
        {
            BAKE_CHILDREN
        }

        public Mode mode;
        public Resolution resolution;

        public MeshBakeResult Bake(bool savePersistent = false)
        {
            var targets = new Dictionary<string, List<GameObject>>();

            switch (mode)
            {
                case Mode.BAKE_CHILDREN:
                    var frontier = new Queue<Transform>();
                    frontier.Enqueue(transform);
                    while(frontier.Count > 0)
                    {
                        var child = frontier.Dequeue();
                        if (!child.gameObject.activeInHierarchy)
                            continue;
                        var rend = child.GetComponent<MeshRenderer>(); 
                        if (rend != null && rend.enabled)
                        {
                            var mat = rend.sharedMaterial;

                            string rType = mat.GetTag("RenderType", false);

                            if (!targets.ContainsKey(rType))
                            {
                                targets[rType] = new List<GameObject>();
                            }
                            targets[rType].Add(child.gameObject);
                        }

                        var nuChildren = (Enumerable.Range(0, child.childCount).Select((i) =>
                        {
                            return child.GetChild(i);
                        }));
                        foreach(var nuChild in nuChildren)
                        {
                            frontier.Enqueue(nuChild);
                        }
                    }
                    break;
            }

            if(targets.Count > 0)
            {
                var allTargets = new List<GameObject>();
                var allCombined = new List<GameObject>();
                foreach(var targetKV in targets)
                {
                    if (targetKV.Key == "Fade") continue;

                    var theseTargets = targetKV.Value;

                    var theseMats = theseTargets.SelectMany((target) => target.GetComponent<MeshRenderer>().sharedMaterials).Distinct().ToArray();
                    foreach(var mat in theseMats)
                    {
                        Utilities.GLTFUtilities.BlitPropertiesIntoMaps(mat);
                        AssetDatabase.Refresh();
                    }

                    GameObject go = new GameObject("MeshBake-" + targetKV.Key + "-" + gameObject.name);

                    GameObject goChild = new GameObject("child", new[]
                    {
                        typeof(MeshRenderer),
                        typeof(MeshFilter)
                    });

                    goChild.transform.SetParent(go.transform);

                    var renderer = goChild.GetComponent<MeshRenderer>();
                    var filter = goChild.GetComponent<MeshFilter>();

                    var texBaker = go.AddComponent<MB3_TextureBaker>();
                    var meshBaker = go.AddComponent<MB3_MeshBaker>();
                    
                    texBaker.fixOutOfBoundsUVs = true;
                    texBaker.maxAtlasSize = (int)resolution;
                    texBaker.maxTilingBakeSize = (int)resolution / 2;

                    texBaker.customShaderProperties = GetShaderProps(renderer.sharedMaterial);

                    texBaker.objsToMesh = theseTargets;

                    meshBaker.objsToMesh = theseTargets;
                    meshBaker.meshCombiner.lightmapOption = PipelineSettings.preserveLightmapping ? 
                        MB2_LightmapOptions.preserve_current_lightmapping :
                        MB2_LightmapOptions.copy_UV2_unchanged_to_separate_rects;

                    meshBaker.meshCombiner.doTan = false;

                    string pathRoot = savePersistent ? PipelineSettings.PipelinePersistentFolder : PipelineSettings.PipelineAssetsFolder;
                    string matPath = pathRoot.Replace(Application.dataPath, "Assets") + gameObject.name + "-" + targetKV.Key + "_MeshBaker.asset";

                    MB3_TextureBakerEditorInternal.CreateCombinedMaterialAssets(texBaker, matPath);
                    texBaker.CreateAtlases(null, true, new MB3_EditorMethods());
                    EditorUtility.ClearProgressBar();
                    if (texBaker.textureBakeResults != null) EditorUtility.SetDirty(texBaker.textureBakeResults);

                    meshBaker.textureBakeResults = AssetDatabase.LoadAssetAtPath<MB2_TextureBakeResults>(matPath);
                    meshBaker.meshCombiner.resultSceneObject = go;

                    MB3_MeshBakerEditorInternal.bake(meshBaker);
                    goChild.isStatic = true;

                    allTargets.AddRange(theseTargets);
                    allCombined.Add(go);
                }
                return new MeshBakeResult
                {
                    originals = allTargets.ToArray(),
                    combined = allCombined.ToArray()
                };
            }

            return null;
        }

        private List<ShaderTextureProperty> GetShaderProps(Material sharedMaterial)
        {
            if (sharedMaterial == null)
                return new List<ShaderTextureProperty>();
            List<ShaderTextureProperty> result = new List<ShaderTextureProperty>();
            result.AddRange(sharedMaterial.GetTexturePropertyNames().Select((name) => new ShaderTextureProperty(name, name == "_BumpMap")));

            return result;
        }
    }
}
#endif

