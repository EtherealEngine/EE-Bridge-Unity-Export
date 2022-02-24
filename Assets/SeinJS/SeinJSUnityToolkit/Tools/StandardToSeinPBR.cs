/**
 * @File   : StandardToSeinPBR.cs
 * @Author : dtysky (dtysky@outlook.com)
 * @Link   : dtysky.moe
 * @Date   : 2019/09/09 0:00:00PM
 */
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using XREngine;

namespace SeinJS
{
    public class StandardToSeinPBR : MonoBehaviour
    {
        static Dictionary<Material, string> backups;
        static Dictionary<Material, Material> gltfLinks;

        [MenuItem("Assets/Materials to SeinPBR", priority = 0)]
        public static void AssetsToSeinPBR()
        {
            backups = new Dictionary<Material, string>();
            var objects = Selection.objects;
            AssetsToSeinPBR(objects);

            AssetDatabase.Refresh();
        }

        private static void AssetsToSeinPBR(UnityEngine.Object[] objects)
        {
            var needBak = CheckNeedBackup();
            foreach (var obj in objects)
            {
                var type = obj.GetType();
                if (type == typeof(Material))
                {
                    ToSeinPBR(obj as Material, needBak);
                }
                else if (type == typeof(GameObject))
                {
                    HashSet<Material> materials = new HashSet<Material>();
                    List<Renderer> children = new List<Renderer>();
                    children.AddRange((obj as GameObject).GetComponentsInChildren<Renderer>());
                    children.AddRange((obj as GameObject).GetComponentsInChildren<SkinnedMeshRenderer>());

                    foreach (var mr in children)
                    {
                        var sms = mr.sharedMaterials;
                        foreach (var m in sms)
                        {
                            materials.Add(m);
                        }
                    }

                    foreach (var m in materials)
                    {
                        ToSeinPBR(m, needBak);
                    }
                }
                else
                {
                    //string selectionPath = AssetDatabase.GetAssetPath(obj); // relative path
                    //if (Directory.Exists(selectionPath))
                    //{

                    //}
                }
            }
        }

        [MenuItem("CONTEXT/Material/To SeinPBR", priority = 0)]
        private static void InspectorToSeinPBR(MenuCommand command)
        {
            var material = command.context as Material;
            ToSeinPBR(material);

            AssetDatabase.Refresh();
        }

        [MenuItem("GameObject/Sein/Materials to SeinPBR", priority = 11)]
        private static void GOToSeinPBR()
        {
            HashSet<System.Tuple<Renderer, Material>> materials = new HashSet<Tuple<Renderer, Material>>();
            var transforms = Selection.GetTransforms(SelectionMode.Deep);
            foreach (var tr in transforms)
            {
                Renderer mr = GetRenderer(tr);
                if (mr == null)
                {
                    continue;
                }

                var sms = mr.sharedMaterials;
                foreach (var m in sms)
                {
                    materials.Add(new Tuple<Renderer, Material>(mr, m));
                }
            }

            var needBak = CheckNeedBackup();
            foreach (var m in materials)
            {
                ToSeinPBR(m.Item2, needBak, m.Item1);
            }

            AssetDatabase.Refresh();
        }

        private static Renderer GetRenderer(Transform tr)
        {
            Renderer mr = tr.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                mr = tr.GetComponent<SkinnedMeshRenderer>();
            }
            return mr;
        }

        private static GameObject[] RecursiveGetGOs(GameObject root)
        {
            List<GameObject> result = new List<GameObject>();
            result.Add(root);
            int childCount = root.transform.childCount;
            if (childCount == 0)
                return result.ToArray();
            result.AddRange(Enumerable.Range(0, childCount).SelectMany((child) => RecursiveGetGOs(root.transform.GetChild(child).gameObject)));
            return result.ToArray();
        }

        [MenuItem("SeinJS/Materials to SeinPBR", priority = 4)]
        public static void AllToSeinPBR()
        {
            backups = new Dictionary<Material, string>();
            gltfLinks = new Dictionary<Material, Material>();
            Regex backupPattern = new Regex(".*_bak");
            var gos = Enumerable.Range(0,EditorSceneManager.sceneCount).Select((i) => 
                EditorSceneManager.GetSceneAt(i)).SelectMany((scene) => 
                    scene.GetRootGameObjects().SelectMany((root) => 
                        RecursiveGetGOs(root)));

            var materials = gos.Where((go) => go.activeInHierarchy)
                .SelectMany((go) => 
                    go.GetComponent<Renderer>() ? 
                    go.GetComponent<Renderer>().sharedMaterials : 
                    new Material[0]).Distinct().ToList();
            //var needBak = true;// CheckNeedBackup();
            
            for(int i = 0; i < materials.Count; i++)
            {
                Material mat = materials[i];
                BackupMaterial(ref mat);
            }

            AssetDatabase.Refresh();
            
            foreach (var m in materials)
            {
                ToSeinPBR(m, false);
            }

            AssetDatabase.Refresh();
        }

        private static bool CheckNeedBackup()
        {
            return EditorUtility.DisplayDialog(
                "Backup?",
                "Need to backup orginal materials?",
                "Yes",
                "No"
            );
        }
        private static Shader glShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.atteneder.gltfast/Runtime/Shader/Built-In/glTFPbrMetallicRoughness.shader");
        private static void ToSeinPBR(Material material, bool backup = true, Renderer renderer = null)
        {
            var shader = material.shader;
            var name = shader.name;

            if (!(name == "Standard" || name == "Autodesk Interactive" || name == "Standard (Specular setup)") && !(shader == glShader))
            {
                return;
            }

            Debug.Log("Converting: " + material.name);

            ConvertMaterial(material, renderer);
        }

        [MenuItem("SeinJS/Restore Materials")]
        public static void RestoreMaterials()
        {
            foreach(var material in backups.Keys)
            {
                Material backup = AssetDatabase.LoadAssetAtPath<Material>(backups[material]);
                material.shader = backup.shader;
                material.CopyPropertiesFromMaterial(backup);
            }
            /*
            var renderers = FindObjectsOfType<Renderer>();
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select((sharedMat) =>
                    gltfLinks.ContainsKey(sharedMat) ? gltfLinks[sharedMat] : sharedMat
                ).ToArray();
            }
            */
            
        }

        

        private static void BackupMaterial(ref Material material)
        {

            var origPath = AssetDatabase.GetAssetPath(material);
            
                
            
            var fname = Path.GetFileNameWithoutExtension(origPath) + "_" + material.name;
            var dir = Path.GetDirectoryName(origPath) + "/bak";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            Material nuMat = new Material(material);

            string nuPath = dir + "/" + fname + "_bak";
            SaveMaterial(nuMat, nuPath);

            backups[material] = nuPath + ".mat";
        }

        

        private static void ConvertMaterial(Material mat, Renderer rend)
        {
            var material = new Material(Shader.Find("Sein/PBR"));

            bool isMetal = mat.shader == glShader || mat.shader.name == "Standard" || mat.shader.name == "Autodesk Interactive";
            if (!isMetal)
            {
                material.SetInt("workflow", (int)SeinPBRShaderGUI.Workflow.Specular);
            }
            else
            {
                material.SetInt("workflow", (int)SeinPBRShaderGUI.Workflow.Metal);
            }

            var mode = GetBlendMode(mat);
            material.SetInt("_Mode", (int)mode);
            if (mode == SeinPBRShaderGUI.BlendMode.Cutout)
            {
                material.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
            }

            // Is smoothness defined by diffuse texture or PBR texture' alpha?
            if (mat.HasProperty("_SmoothnessTextureChannel") && Math.Abs(mat.GetFloat("_SmoothnessTextureChannel")) > 0.01)
                Debug.Log("Smoothness uses diffuse's alpha channel. Unsupported for now");

            bool hasPBRMap = (!isMetal && mat.GetTexture("_SpecGlossMap") != null) || (isMetal && mat.GetTexture("_MetallicGlossMap") != null);

            //Parse diffuse channel texture and color
            if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
            {
                var texture = (Texture2D)mat.GetTexture("_MainTex");
                ChangeSRGB(ref texture, true);
                material.SetTexture("_baseColorMap", texture);
            }

            if (mat.HasProperty("_Color"))
            {
                material.SetColor("_baseColor", mat.GetColor("_Color"));
            }

            //Parse PBR textures
            if (isMetal)
            {
                if (hasPBRMap) // No metallic factor if texture
                {

                    Texture2D metallicTexture = (Texture2D)mat.GetTexture("_MetallicGlossMap");
                    ChangeSRGB(ref metallicTexture, false);
                    Texture2D roughnessTexture = null;
                    if (mat.shader.name == "Autodesk Interactive")
                    {
                        roughnessTexture = (Texture2D)mat.GetTexture("_SpecGlossMap");
                    }
                    else if (mat.shader == glShader)
                    {
                        roughnessTexture = (Texture2D)mat.GetTexture("_MetallicGlossMap");
                    }
                    else
                    {
                        var channel = mat.GetInt("_SmoothnessTextureChannel");
                        if (channel == 0)
                        {
                            roughnessTexture = SplitRoughnessTexture(metallicTexture);
                        }
                        else
                        {
                            roughnessTexture = SplitRoughnessTexture((Texture2D)mat.GetTexture("_baseColorMap"));
                        }
                    }
                    if(roughnessTexture != null)
                        ChangeSRGB(ref roughnessTexture, false);

                    material.SetTexture("_metallicMap", metallicTexture);
                    material.SetTexture("_roughnessMap", roughnessTexture);
                }

                material.SetFloat("_metallic", hasPBRMap ? 1.0f : mat.GetFloat("_Metallic"));
                if (mat.shader == glShader)
                {
                    material.SetFloat("_roughness", mat.GetFloat("_Roughness"));
                }
                else
                {
                    
                    material.SetFloat("_roughness", mat.shader.name == "Autodesk Interactive" ? 1.0f : mat.GetFloat("_GlossMapScale"));
                }
                
            }
            else
            {
                if (hasPBRMap) // No metallic factor if texture
                {
                    var texture = (Texture2D)mat.GetTexture("_SpecGlossMap");
                    ChangeSRGB(ref texture, false);
                    material.SetTexture("__specularGlossinessMap", texture);
                }

                material.SetColor("__specular", hasPBRMap ? Color.white : mat.GetColor("_SpecColor"));
                material.SetFloat("_glossiness", hasPBRMap ? 1.0f : mat.GetFloat("_Glossiness"));
            }

            //BumpMap
            if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
            {
                Texture2D bumpTexture = mat.GetTexture("_BumpMap") as Texture2D;
                string texPath = AssetDatabase.GetAssetPath(bumpTexture);
                // Check if it's a normal or a bump map

                TextureImporter im = AssetImporter.GetAtPath(texPath) as TextureImporter;
                bool isBumpMap = im.convertToNormalmap;

                if (isBumpMap)
                {
                    Debug.LogWarning("Unsupported texture " + bumpTexture + " (normal maps generated from grayscale are not supported)");
                }
                else
                {
                    ChangeSRGB(ref bumpTexture, false);
                    material.SetTexture("_normalMap", bumpTexture);
                    material.SetFloat("_normalScale", mat.GetFloat("_BumpScale"));
                }
            }

            //Emissive
            if (mat.HasProperty("_EmissionMap") && mat.GetTexture("_EmissionMap") != null)
            {
                Texture2D emissiveTexture = mat.GetTexture("_EmissionMap") as Texture2D;
                ChangeSRGB(ref emissiveTexture, true);
                material.SetTexture("_emissionMap", emissiveTexture);
            }
            material.SetColor("_emission", mat.GetColor("_EmissionColor"));

            if (mat.HasProperty("_OcclusionMap") && mat.GetTexture("_OcclusionMap") != null)
            {
                var occlusionTexture = mat.GetTexture("_OcclusionMap") as Texture2D;
                ChangeSRGB(ref occlusionTexture, false);

                material.SetTexture("_occlusionMap", occlusionTexture);
                material.SetFloat("_occlusionStrength", mat.GetFloat("_OcclusionStrength"));
            }
            /*
            material.name = mat.name;
            string assetPath = PipelineSettings.PipelineAssetsFolder + material.name + DateTime.Now.Ticks + ".mat";
            AssetDatabase.CreateAsset(material, assetPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            rend.sharedMaterials = Enumerable.Range(0, rend.sharedMaterials.Length).Select((i) =>
            {
                if (rend.sharedMaterials[i] == mat)
                {
                    return material;
                }
                else return rend.sharedMaterials[i];
            }).ToArray();
            */
            ///*
            mat.shader = Shader.Find("Sein/PBR");
            mat.CopyPropertiesFromMaterial(material);
            //*/
        }

        
        private static void ChangeSRGB(ref Texture2D tex, bool isSRGB)
        {
            
            TextureImporter im = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
            if(im == null)
            {
                Debug.LogError("error importing texture " + tex);
                
            }
            if (im.sRGBTexture == isSRGB)
            {
                return;
            }

            im.sRGBTexture = isSRGB;
            im.SaveAndReimport();
        }

        private static SeinPBRShaderGUI.BlendMode GetBlendMode(Material mat)
        {
            if (!mat.HasProperty("_Mode"))
            {
                return SeinPBRShaderGUI.BlendMode.Opaque;
            }

            switch ((int)mat.GetFloat("_Mode"))
            {
                // Opaque
                case 0:
                    return SeinPBRShaderGUI.BlendMode.Opaque;
                // Cutout
                case 1:
                    return SeinPBRShaderGUI.BlendMode.Cutout;
                // Transparent
                case 2:
                case 3:
                    return SeinPBRShaderGUI.BlendMode.Transparent;
            }

            return SeinPBRShaderGUI.BlendMode.Opaque;
        }

        private static Texture2D SplitRoughnessTexture(Texture2D texture)
        {

            int width = texture.width;
            int height = texture.height;
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter im = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            im.isReadable = true;
            im.SaveAndReimport();
            var iColor = texture.GetPixels();
            im.isReadable = false;
            im.SaveAndReimport();

            // Let's consider that the three textures have the same resolution
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i += 1)
            {
                float a = 1 - iColor[i].a;

                colors[i] = new Color(a, a, a);
            }

            var res = new Texture2D(width, height);
            res.SetPixels(colors);

            string basename = Path.GetFileNameWithoutExtension(assetPath) + "_rg";
            string fullPath = Path.GetFullPath(Path.GetDirectoryName(assetPath)) + "/" + basename + ".png";

            string newAssetPath = GLTFTextureUtils.writeTextureOnDisk(res, fullPath, true);
            string projectPath = GLTFUtils.getPathProjectFromAbsolute(newAssetPath);
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(projectPath, typeof(Texture2D));

            return tex;
        }

        private static void SaveMaterial(Material material, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                File.Delete(path + ".meta");
                AssetDatabase.Refresh();
            }
            AssetDatabase.CreateAsset(material, path + ".mat");
        }
    }
}
