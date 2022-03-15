using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

//using Unity.EditorCoroutines.Editor;
using UnityEngine;
using SeinJS;
using System.Text.RegularExpressions;
using System.Diagnostics;
using XREngine.XREngineProject;
using System.Linq;
using System;
using XREngine.Utilities;
using XREngine.XREngineProject;

namespace XREngine
{
    public class ExportScene : EditorWindow
    {
        public static ExportScene instance;

        public enum State
        {
            INITIAL,
            PRE_EXPORT,
            EXPORTING,
            POST_EXPORT,
            RESTORING,
            ERROR
        }

        State state;
        bool doDebug;
        string defaultMatPath = @"Assets/_XREngine_/Content/Materials/Block.mat";
        string nullMatPath = @"Assets/_XREngine_/Content/Materials/Null.mat";

        [MenuItem("XREngine/Export Scene")]
        static void Init()
        {
            ExportScene window = (ExportScene)EditorWindow.GetWindow(typeof(ExportScene));
            window.state = State.INITIAL;
            instance = window;
            EditorSceneManager.activeSceneChanged += (x1, x2) => OnChangeMainScene();
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                OnChangeMainScene();
            };
            window.Show();
        }

        private static void OnChangeMainScene()
        {
            UnityEngine.Debug.Log("on change scene");
            var data = new PipelineSettings.Data();
            data.Set();
            if(!PipelineSettings.ReadSettingsFromConfig())
            {
                data.Apply();
            }
        }

        Material _targetMat;

        Exporter exporter;

        public string ConversionPath => Path.Combine(PipelineSettings.ConversionFolder, PipelineSettings.GLTFName);
        
        public string ExportPath
        {
            get
            {
                string exportFolder = PipelineSettings.XREProjectFolder + "/assets/";

                return exportFolder;
            }
        }
        
        private void OnFocus()
        {
            if(exporter == null)
            {
                exporter = new Exporter();
            }

            Config.Load();


            ExtensionManager.Init();
        }

        Texture2D targTex, result;
        bool showAdvancedOptions;
        bool savePersistentSelected;
        private void OnGUI()
        {
            #region Initial Menu
            switch (state)
            {

                case State.INITIAL:
                    PipelineSettings.GLTFName = EditorGUILayout.TextField("Name:", PipelineSettings.GLTFName);

                    if (GUILayout.Button("Set Output Directory"))
                    {
                        PipelineSettings.XREProjectFolder = EditorUtility.SaveFolderPanel("Output Directory", PipelineSettings.XREProjectFolder, "");
                    }
                    GUILayout.Space(8);
                    GUILayout.Label("Export Components:");
                    PipelineSettings.ExportLights = EditorGUILayout.Toggle("Lights", PipelineSettings.ExportLights);
                    PipelineSettings.ExportColliders = EditorGUILayout.Toggle("Colliders", PipelineSettings.ExportColliders);
                    PipelineSettings.ExportSkybox = EditorGUILayout.Toggle("Skybox", PipelineSettings.ExportSkybox);
                    //PipelineSettings.ExportEnvmap = EditorGUILayout.Toggle("Envmap", PipelineSettings.ExportEnvmap);
                    GUILayout.Space(8);
                    PipelineSettings.meshMode = (MeshExportMode)EditorGUILayout.EnumPopup("Mesh Export Options", PipelineSettings.meshMode);
                    PipelineSettings.lightmapMode = (LightmapMode)EditorGUILayout.EnumPopup("Lightmap Mode", PipelineSettings.lightmapMode);
                    GUILayout.Space(8);
                    PipelineSettings.CombinedTextureResolution = EditorGUILayout.IntField("Max Texture Resolution", PipelineSettings.CombinedTextureResolution);
                    GUILayout.Space(16);
                    GUILayout.Label("GLTF Optimization");

                    PipelineSettings.InstanceMeshes = EditorGUILayout.Toggle("Instanced Meshes", PipelineSettings.InstanceMeshes);

                    PipelineSettings.MeshOptCompression = EditorGUILayout.Toggle("MeshOpt Compression", PipelineSettings.MeshOptCompression);

                    PipelineSettings.KTX2Compression = EditorGUILayout.Toggle("KTX2 Compression", PipelineSettings.KTX2Compression);

                    PipelineSettings.CombineMaterials = EditorGUILayout.Toggle("Combine Materials (breaks lightmaps)", PipelineSettings.CombineMaterials);

                    PipelineSettings.CombineNodes = EditorGUILayout.Toggle("Combine Nodes (breaks xrengine components)", PipelineSettings.CombineNodes);
                    GUILayout.Space(16);
                    PipelineSettings.ExportFormat = (ExportFormat)EditorGUILayout.EnumPopup("Exported File Format", PipelineSettings.ExportFormat);
                    GUILayout.Space(8);
                    PipelineSettings.BasicGLTFConvert = EditorGUILayout.Toggle("Use Basic GLTF Converter", PipelineSettings.BasicGLTFConvert);
                    GUILayout.Space(16);
                    if (GUILayout.Button("Save Settings as Default"))
                    {
                        PipelineSettings.SaveSettings();
                    }
                    GUILayout.Space(16);

                    showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Tools");

                    if(showAdvancedOptions)
                    {
                        savePersistentSelected = GUILayout.Toggle(savePersistentSelected, "Serialize into Persistent Assets (Are not deleted after export)");
                        if(GUILayout.Button("Serialize Selected Assets"))
                        {
                            SerializeSelectedAssets(savePersistentSelected);
                        }
                        GUILayout.Space(8);
                        if(GUILayout.Button("Serialize All Assets"))
                        {
                            SerializeAllMaterials(savePersistentSelected);
                            CreateUVBakedMeshes(savePersistentSelected);
                        }
                        GUILayout.Space(8);
                        if(GUILayout.Button("Deserialize Selected Assets"))
                        {
                            DeserializeSelectedAssets();
                        }
                        GUILayout.Space(8);
                        if (GUILayout.Button("Deserialize All Assets"))
                        {
                            RestoreAllGLLinks();
                            DeserializeAllMaterials();
                        }
                        GUILayout.Space(8);
                        if (PipelineSettings.meshMode == MeshExportMode.COMBINE)
                        {
                            PipelineSettings.preserveLightmapping = EditorGUILayout.ToggleLeft("Preserve Lightmapping", PipelineSettings.preserveLightmapping);
                        }
                        GUILayout.BeginVertical();
                        if (GUILayout.Button("Do MeshBake"))
                        {
                            SerializeAllMaterials(true);
                            CreateUVBakedMeshes(true);
                            CombineMeshes(true);
                        }
                        GUILayout.Space(8);
                        if (GUILayout.Button("Format LODGroups"))
                        {
                            LODFormatter.FormatLODs();
                        }
                        GUILayout.Space(8);
                       
                        if (GUILayout.Button("Undo MeshBake"))
                        {
                            CleanupMeshCombine();
                            RestoreAllGLLinks();
                            DeserializeAllMaterials();
                        }
                        GUILayout.Space(16);
                        if (GUILayout.Button("Revert Backups"))
                        {
                            var mats = FindObjectsOfType<GameObject>().SelectMany((go) => go.GetComponent<MeshRenderer>() ? go.GetComponent<MeshRenderer>().sharedMaterials : new Material[0])
                                .Distinct().ToArray();
                            foreach (var mat in mats)
                            {
                                string assetPath = AssetDatabase.GetAssetPath(mat);
                                if (!string.IsNullOrEmpty(assetPath))
                                {
                                    string prefix = Regex.Replace(assetPath, @"(?<=.*\/+)([\w\d\._ -]*).mat$", "$1_$1_bak.mat");
                                    //prefix = assetPath.Replace(".mat", "_bak.mat");
                                    prefix = Regex.Replace(prefix, @"(?<=.*)\/+(?=[\w\d_\. -]*$)", "/bak/");
                                    Material backup = AssetDatabase.LoadAssetAtPath<Material>(prefix);
                                    UnityEngine.Debug.Log("--\nfor material at path " + assetPath + "\nfound backup " + prefix + "\n--");
                                    if (backup != null)
                                    {
                                        UnityEngine.Debug.Log("setting shader from " + mat.shader.name + " to " + backup.shader.name);
                                        mat.shader = backup.shader;

                                        mat.CopyPropertiesFromMaterial(backup);
                                    }
                                }
                            }
                        }
                        
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(8);
                    if (PipelineSettings.XREProjectFolder != null)
                    {
                        doDebug = EditorGUILayout.Toggle("Debug Execution", doDebug);

                        if (GUILayout.Button("Export"))
                        {
                            state = State.PRE_EXPORT;
                            Export(false);
                        }
                    }
                    break;


                #endregion

                #region Debugging Stepper
                case State.PRE_EXPORT:
                    if(GUILayout.Button("Continue"))
                    {
                        state++;
                    }
                    break;

                case State.POST_EXPORT:
                    if (GUILayout.Button("Continue"))
                    {
                        state++;
                    }
                    break;
                #endregion
                default:
                    GUILayout.Label("Exporting...");
                    break;
            }
        }

        #region UTILITY FUNCTIONS

        Dictionary<Material, Material> matLinks;
        Dictionary<string, Material> matRegistry;
        private Material BackupMaterial(Material material, Renderer _renderer, bool savePersistent)
        {
            if (matRegistry == null) matRegistry = new Dictionary<string, Material>();
            bool hasLightmap = XREUnity.HasLightmap(_renderer);
            string registryKey = string.Format("{0}_{1}", material.name, hasLightmap ? _renderer.lightmapIndex : -2);
            
            if (matRegistry.ContainsKey(registryKey))
            {
                return matRegistry[registryKey];
            }
                
            
            string origPath = AssetDatabase.GetAssetPath(material);
            if (origPath == null || Regex.IsMatch(origPath, @".*\.glb"))
            {

                UnityEngine.Debug.Log("Creating link Material to glb");
                Material dupe = new Material(material);
                string dupeRoot = savePersistent ? PipelineSettings.PipelinePersistentFolder : PipelineSettings.PipelineAssetsFolder;
                string dupePath = dupeRoot + material.name + "_" + DateTime.Now.Ticks + ".mat";
                string dupeDir = Regex.Match(dupePath, @"(.*[\\\/])[\w\.\d\-]+").Value;
                dupePath = dupePath.Replace(Application.dataPath, "Assets");
                if(!Directory.Exists(dupeDir))
                {
                    Directory.CreateDirectory(dupeDir);
                }
                AssetDatabase.CreateAsset(dupe, dupePath);

                if(matLinks == null)
                {
                    matLinks = new Dictionary<Material, Material>();
                }

                matLinks[dupe] = material;
                UnityEngine.Debug.Log("material " + dupe.name + " linked to glb material " + material.name);
                matRegistry[registryKey] = dupe;
                Material check = material;
                foreach (var renderer in FindObjectsOfType<Renderer>())
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select((sharedMat) =>
                        sharedMat &&
                        sharedMat.name == check.name ? dupe : sharedMat
                    ).ToArray();
                }

                return dupe;
            }
            return material;
        }

        

        private Tuple<Material, string, string>[] BackupTextures(ref Material mat, bool savePersistent)
        {
            //Material mat = _mat;
            /*
            string[] mapTests = new string[]
            {
                "_MainTex",
                "_BumpMap",
                "_EmissionMap",
                "_MetallicGlossMap",
                "_OcclusionMap",
                "_baseColorMap"
            };
            */
            var maps = mat.GetTexturePropertyNames();
            var matTexes = new List<Texture>();
            for(int i = 0; i < maps.Length; i++)
            {
                matTexes.Add(mat.GetTexture(maps[i]));
            }
            var textures = maps
                .Select((map, i) =>
                {
                    Texture tex = matTexes[i];
                    if (tex == null) return null;
                    return new Tuple<string, Texture>(map, tex);
                })
                .Where((x) => x != null && x.Item2.GetType() == typeof(Texture2D))
                .Select((x) => new Tuple<string, Texture2D>(x.Item1, (Texture2D)x.Item2))
                .ToArray();
            var texPaths = new List<Tuple<Material, string, string>>();
            foreach(var texture in textures)
            {
                var tex = texture.Item2;
                string texPath = AssetDatabase.GetAssetPath(tex);
                if (texPath == null || texPath == "" || Regex.IsMatch(texPath, @".*\.glb"))
                {
                    string nuPath;
                    Texture2D nuTex = GenerateAsset(tex, out nuPath, savePersistent);
                    texPaths.Add(new Tuple<Material, string, string>(mat, texture.Item1, nuPath));
                }
            }
            return texPaths.ToArray();
        }

        Dictionary<Texture2D, Texture2D> texLinks;
        Dictionary<Texture2D, Tuple<Texture2D, string>> texRegistry;
        public Texture2D GenerateAsset(Texture2D tex, out string path, bool savePersistent)
        {
            if(texRegistry != null && texRegistry.ContainsKey(tex))
            {
                var registry = texRegistry[tex];
                path = registry.Item2;
                return registry.Item1;
            }
            Texture2D nuTex = new Texture2D(tex.width, tex.height, tex.format, tex.mipmapCount, false);
            nuTex.name = tex.name + "_" + System.DateTime.Now.Ticks;
            Graphics.CopyTexture(tex, nuTex);
            nuTex.Apply();
            string pRoot = savePersistent ? PipelineSettings.PipelinePersistentFolder : PipelineSettings.PipelineAssetsFolder;
            if (!Directory.Exists(pRoot))
            {
                Directory.CreateDirectory(pRoot);
            }
            string nuPath = pRoot.Replace(Application.dataPath, "Assets") + nuTex.name + ".png";
            File.WriteAllBytes(nuPath, nuTex.EncodeToPNG());

            UnityEngine.Debug.Log("Generated texture " + nuTex + " from " + tex);
            if (texLinks == null)
                texLinks = new Dictionary<Texture2D, Texture2D>();
            if (texRegistry == null)
                texRegistry = new Dictionary<Texture2D, Tuple<Texture2D, string>>();
            texLinks[nuTex] = tex;
            texRegistry[tex] = new Tuple<Texture2D, string>(tex, nuPath);
            path = nuPath;
            return nuTex;
        }
        #endregion

        #region LODS
        Dictionary<Transform, string> lodRegistry;
        private void FormatForExportingLODs()
        {
            lodRegistry = new Dictionary<Transform, string>();
            LODGroup[] lodGroups = GameObject.FindObjectsOfType<LODGroup>();
            foreach(var lodGroup in lodGroups)
            {
                Transform tr = lodGroup.transform;
                lodRegistry.Add(tr, tr.name);
                if(!Regex.IsMatch(tr.name, @".*_LODGroup"))
                    tr.name += "_LODGroup";
                var children = Enumerable.Range(0, tr.childCount).Select((i) => tr.GetChild(i)).ToArray();
                foreach(var child in children)
                {
                    child.name = Regex.Match(child.name, @"^.*LOD\d+").Value;
                }
            }
        }

        private void CleanupExportingLODs()
        {
            if(lodRegistry != null)
            {
                foreach(var kv in lodRegistry)
                {
                    kv.Key.name = kv.Value;
                }
                lodRegistry = null;
            }
        }
        #endregion

        #region LIGHTS
        Light[] bakeLights;
        public void StageLights()
        {
            bakeLights = FindObjectsOfType<Light>().Where((light) => light.gameObject.activeInHierarchy && light.lightmapBakeType == LightmapBakeType.Baked).ToArray();

            foreach (var light in bakeLights)
            {
                light.gameObject.SetActive(false);
            }
        }

        public void CleanupLights()
        {
            foreach(var light in bakeLights)
            {
                light.gameObject.SetActive(true);
            }
            bakeLights = null;
        }
        #endregion

        #region LIGHTMAPPING

        List<IgnoreLightmap> ignoreChildren;
        private void StageLightmapping()
        {
            MOZ_lightmap_Factory.ClearRegistry();

            if(ignoreChildren == null)
            {
                ignoreChildren = new List<IgnoreLightmap>();
            }
            var ignorers = FindObjectsOfType<IgnoreLightmap>().Where((ignorer) => ignorer.applyToChildren);

            foreach(var ignorer in ignorers)
            {
                ignoreChildren.AddRange(XREUnity.ChildComponents<MeshRenderer>(ignorer.transform)
                    .Select(renderer => renderer.gameObject.AddComponent<IgnoreLightmap>()));
            }
        }

        private void CleanupLightmapping()
        {
            if(ignoreChildren != null)
            {
                foreach(var child in ignoreChildren)
                {
                    DestroyImmediate(child);
                }
                ignoreChildren = null;
            }

            MOZ_lightmap_Factory.ClearRegistry();
        }
        #endregion

        #region SKYBOX
        private void FormatForExportingSkybox()
        {
            if (PipelineSettings.ExportSkybox)
            {
                var skyMat = RenderSettings.skybox;
                string[] fNames = new string[]
                    {
                    "negx",
                    "posx",
                    "posy",
                    "negy",

                    "posz",
                    "negz"
                    };
                string nuPath = Path.Combine(PipelineSettings.XREProjectFolder, "cubemap");
                if(!Directory.Exists(nuPath))
                {
                    Directory.CreateDirectory(nuPath);
                }
                SkyBox.Mode outMode = SkyBox.Mode.CUBEMAP;
                if (skyMat.shader.name == "Skybox/6 Sided")
                {
                    string[] texNames = new[]
                    {
                        "_FrontTex",
                        "_BackTex",
                        "_UpTex",
                        "_DownTex",
                        "_LeftTex",
                        "_RightTex",
                    };
                    string[] faceTexes = texNames.Select((x, i) =>
                    {
                        string facePath = string.Format("{0}/{1}.jpg", nuPath, fNames[i]);
                        File.WriteAllBytes(facePath, ((Texture2D)skyMat.GetTexture(x)).EncodeToJPG());
                        return x;
                    }).ToArray();
                }
                else if(skyMat.shader.name.Contains("Skybox/Panoramic"))
                {
                    var hdri = skyMat.GetTexture("_MainTex") as Texture2D;
                    outMode = SkyBox.Mode.EQUIRECTANGULAR;
                    string outPath = nuPath + "/rect.jpg";
                    File.WriteAllBytes(outPath, hdri.EncodeToJPG(100));
                }
                else
                {
                    var cubemap = skyMat.GetTexture("_Tex") as Cubemap;
                    string srcPath = AssetDatabase.GetAssetPath(cubemap);
                    string srcName = Regex.Match(srcPath, @"(?<=.*/)\w*(?=\.hdr)").Value;
                    
                    var cubemapDir = new DirectoryInfo(nuPath);
                    if (!cubemapDir.Exists)
                    {
                        cubemapDir.Create();
                    }

                    CubemapFace[] faces = Enumerable.Range(0, 6).Select((i) => (CubemapFace)i).ToArray();
                    
                    Texture2D[] faceTexes = faces.Select((x, i) =>
                    {
                        Texture2D result = new Texture2D(cubemap.width, cubemap.height);// cubemap.format, false);
                        var pix = cubemap.GetPixels(x);
                        System.Array.Reverse(pix);
                        result.SetPixels(pix);
                        result.Apply();

                        string facePath = string.Format("{0}/{1}.jpg", nuPath, fNames[i]);
                        File.WriteAllBytes(facePath, result.EncodeToJPG());
                        return result;
                    }).ToArray();
                }
                

                GameObject skyboxGO = new GameObject("__skybox__");
                skyboxGO.AddComponent<SkyBox>().mode = outMode;
            }
        }
            
        private void CleanupExportingSkybox()
        {
            var skyboxes = FindObjectsOfType<SkyBox>();
            for(int i = 0; i < skyboxes.Length; i++)
            {
                DestroyImmediate(skyboxes[i].gameObject);
            }
        }
        #endregion

        #region ENVMAP
        private void FormatForExportingEnvmap()
        {
            GameObject envmapGO = new GameObject("__envmap__");
            envmapGO.AddComponent<Envmap>();
        }

        private void CleanupExportEnvmap()
        {
            var envmaps = FindObjectsOfType<Envmap>();
            for (int i = 0; i < envmaps.Length; i++)
            {
                DestroyImmediate(envmaps[i].gameObject);
            }
        }
        #endregion

        #region MESH BAKING
        class MatRend
        {
            public Material mat;
            public Renderer rend;
            public MatRend(Material _mat, Renderer _rend)
            {
                mat = _mat;
                rend = _rend;
            }
        }

        private void SerializeSelectedAssets(bool savePersistent = false)
        {
            var renderers = Selection.gameObjects.Select((go) => go.GetComponent<Renderer>()).Where((rend) => rend != null);
            foreach(var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null)
                {
                    GenerateMesh(renderer, mesh, savePersistent);
                }
            }
            SerializeMaterials(renderers, savePersistent);
        }

        private void DeserializeSelectedAssets()
        {
            var renderers = Selection.gameObjects.Select((go) => go.GetComponent<Renderer>()).Where((rend) => rend != null);
            DeserializeMaterials(renderers);
            RestoreGLLinks(renderers.Select((rend) => rend.GetComponent<MeshFilter>()).Where((filt) => filt != null && filt.sharedMesh != null));
        }

        private void SerializeMaterials(IEnumerable<Renderer> renderers, bool savePersistent = false)
        {
            matRegistry = matRegistry != null ? matRegistry : new Dictionary<string, Material>();
            matLinks = matLinks != null ? matLinks : new Dictionary<Material, Material>();
            texLinks = texLinks != null ? texLinks : new Dictionary<Texture2D, Texture2D>();
            var mats = renderers.SelectMany((rend) => rend.sharedMaterials.Select((mat) => new MatRend(mat, rend)))
                .Where((x) => x != null && x.mat != null && x.rend != null).ToArray();

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i].mat;
                var rend = mats[i].rend;
                mats[i].mat = BackupMaterial(mat, rend, savePersistent);
            }
            AssetDatabase.Refresh();
            var remaps = new List<Tuple<Material, string, string>>();
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i].mat;
                remaps.AddRange(BackupTextures(ref mat, savePersistent));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            for (int i = 0; i < remaps.Count; i++)
            {
                var remap = remaps[i];
                var nuMat = remap.Item1;
                var nuTex = AssetDatabase.LoadAssetAtPath<Texture2D>(remap.Item3);
                GLTFUtilities.SetTextureImporterFormat(nuTex, true);
                nuMat.SetTexture(remap.Item2, nuTex);
                UnityEngine.Debug.Log("material " + nuMat + " path of " + AssetDatabase.GetAssetPath(nuMat));
                UnityEngine.Debug.Log("setting material " + nuMat + " texture " + remap.Item2 + " to " + nuTex + ", path of " + AssetDatabase.GetAssetPath(nuTex));

            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("processed materials");
            var updates = mats.GroupBy((mat) => mat.rend).Select((rGroup) =>
            new Dictionary<Renderer, Material[]>
            (
                new KeyValuePair<Renderer, Material[]>[]
                {
                    new KeyValuePair<Renderer, Material[]>
                    (
                        rGroup.Key,
                        rGroup.Select((r) => r.mat).ToArray()
                    )
                }
            )).Aggregate((rGroup1, rGroup2) =>
            {
                if (rGroup2 == null) return rGroup1;
                if (rGroup1 == null) return rGroup2;
                foreach (var key in rGroup2.Keys)
                {
                    rGroup1[key] = rGroup2[key];
                }
                return rGroup2;
            });
            foreach (var update in updates)
            {
                update.Key.sharedMaterials = update.Value;
            }
        }
        private void SerializeAllMaterials(bool savePersistent = false)
        {
            var renderers = FindObjectsOfType<MeshRenderer>()
                .Where((x) => x.gameObject.activeInHierarchy && x.enabled);
                
            SerializeMaterials(renderers, savePersistent);
        }

        private void DeserializeMaterials(IEnumerable<Renderer> renderers)
        {
            HashSet<Material> toRemove = new HashSet<Material>();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select
                (
                    (mat) =>
                    {
                        if (matLinks.ContainsKey(mat))
                        {
                            toRemove.Add(mat);
                            mat = matLinks[mat];
                        }
                        return mat;
                    }
                ).ToArray();
            }
            foreach (var material in toRemove)
            {
                matLinks.Remove(material);
            }
        }

        private void DeserializeAllMaterials()
        {
            var renderers = FindObjectsOfType<MeshRenderer>();
            DeserializeMaterials(renderers);
            matRegistry = null;
            matLinks = null;
            texRegistry = null;
            texLinks = null;
        }

        private void CreateBakedMeshes(bool savePersistent)
        {
            if(PipelineSettings.meshMode == MeshExportMode.DEFAULT ||
               PipelineSettings.meshMode == MeshExportMode.COMBINE)
            {
                CreateUVBakedMeshes(savePersistent);
                if(PipelineSettings.meshMode == MeshExportMode.COMBINE)
                {
                    CombineMeshes();
                }
            }
        }

        struct MeshRegistryKey : IEquatable<MeshRegistryKey>
        {
            public Mesh mesh;
            public string registryID;

            public bool Equals(MeshRegistryKey other)
            {
                return mesh == other.mesh && registryID == other.registryID;
            }

            public MeshRegistryKey(Mesh mesh, Renderer renderer)
            {
                this.mesh = mesh;
                bool hasLightmap = XREUnity.HasLightmap(renderer);
                bool hasTxrOffset = renderer.sharedMaterial != null &&
                    (renderer.sharedMaterial.mainTextureOffset != Vector2.one ||
                     renderer.sharedMaterial.mainTextureScale != Vector2.one);
                int lightIdx = hasLightmap ? renderer.lightmapIndex : -2;
                Vector2 txrOffset = hasTxrOffset ? renderer.sharedMaterial.mainTextureOffset : Vector2.negativeInfinity;
                registryID = String.Format("%d_%f_%f", lightIdx, txrOffset.x, txrOffset.y);
            }
        }

        private Mesh GenerateMesh(Renderer renderer, Mesh mesh, bool savePersistent = false)
        {
            glLinks = glLinks != null ? glLinks : new Dictionary<Mesh, Mesh>();
            glRegistry = glRegistry != null ? glRegistry : new Dictionary<MeshRegistryKey, Mesh>();

            var regKey = new MeshRegistryKey(mesh, renderer);
            if(glRegistry.ContainsKey(regKey))
            {
                return glRegistry[regKey];
            }

            string assetFolder = savePersistent ? PipelineSettings.PipelinePersistentFolder : PipelineSettings.PipelineAssetsFolder;
            if (!Directory.Exists(assetFolder))
            {
                Directory.CreateDirectory(assetFolder);
            }
            string nuMeshPath = assetFolder.Replace(Application.dataPath, "Assets") + renderer.transform.name + "_" + System.DateTime.Now.Ticks + ".asset";
            UnityEngine.Mesh nuMesh = UnityEngine.Object.Instantiate(mesh);

            AssetDatabase.CreateAsset(nuMesh, nuMeshPath);
            glRegistry[regKey] = nuMesh;
            return nuMesh;
        }

        Dictionary<UnityEngine.Mesh, UnityEngine.Mesh> glLinks;
        Dictionary<MeshRegistryKey, UnityEngine.Mesh> glRegistry;
        private void CreateUVBakedMeshes(bool savePersistent = false)
        {
            var renderers = FindObjectsOfType<Renderer>().Where((renderer) => renderer.gameObject.activeInHierarchy && renderer.enabled);
            foreach(var renderer in renderers)
            {
                bool isSkinned = renderer.GetType() == typeof(SkinnedMeshRenderer);
                Mesh mesh = null;
                if (isSkinned)
                {
                    mesh = ((SkinnedMeshRenderer)renderer).sharedMesh;
                }
                else
                {
                    var filt = renderer.GetComponent<MeshFilter>();
                    if (filt != null)
                        mesh = filt.sharedMesh;
                }
                if (mesh == null) continue;
                
                bool hasLightmap = XREUnity.HasLightmap(renderer);
                bool hasTxrOffset = renderer.sharedMaterial != null && 
                    (renderer.sharedMaterial.mainTextureOffset != Vector2.one ||
                     renderer.sharedMaterial.mainTextureScale != Vector2.one);
                
                if ((hasLightmap && PipelineSettings.lightmapMode == LightmapMode.BAKE_SEPARATE) ||
                     hasTxrOffset ||
                     Regex.IsMatch(AssetDatabase.GetAssetPath(mesh), @".*\.glb"))
                {

                    var nuMesh = GenerateMesh(renderer, mesh, savePersistent);

                    if (hasLightmap)
                    {
                        
                        var off = renderer.lightmapScaleOffset;
                        var nuUv2s = nuMesh.uv2.Select((uv2) => uv2 * new Vector2(off.x, off.y) + new Vector2(off.z, off.w)).ToArray();
                        nuMesh.uv2 = nuUv2s;
                        nuMesh.UploadMeshData(false);
                    }

                    if (hasTxrOffset)
                    {
                        var mat = renderer.sharedMaterial;
                        var off = mat.mainTextureOffset;
                        var scale = mat.mainTextureScale;
                        var nuUvs = nuMesh.uv.Select((uv) => uv * new Vector2(scale.x, scale.y) + new Vector2(off.x, off.y)).ToArray();
                        nuMesh.uv = nuUvs;
                        nuMesh.UploadMeshData(false);
                    }

                    if (!isSkinned)
                        renderer.GetComponent<MeshFilter>().sharedMesh = nuMesh;
                    else
                    {
                        ((SkinnedMeshRenderer)renderer).sharedMesh = nuMesh;
                    }
                    glLinks[nuMesh] = mesh;
                    mesh = nuMesh;
                }
            }
            AssetDatabase.Refresh();
        }

        MeshBakeResult[] bakeResults;
        private void CombineMeshes(bool savePersistent = false)
        {
            var stagers = FindObjectsOfType<MeshBake>();
#if USING_MESHBAKER
            bakeResults = stagers.Select((baker) => baker.Bake(savePersistent)).Where((x) => x != null).ToArray();
            foreach(var result in bakeResults)
            {
                foreach(var original in result.originals)
                {
                    original.GetComponent<MeshRenderer>().enabled = false;
                }
            }
            AssetDatabase.Refresh();
#endif
        }

        private void CleanupMeshCombine()
        {
            if(bakeResults != null)
            {
                foreach (var result in bakeResults)
                {
                    foreach(var original in result.originals)
                    {
                        if(original && original.GetComponent<MeshRenderer>())
                            original.GetComponent<MeshRenderer>().enabled = true;
                    }
                    foreach(var combined in result.combined)
                    {
                        if(combined != null)
                            DestroyImmediate(combined.gameObject);
                    }
                }
            }
            //MeshStager.ResetAll();
        }

        private void RestoreGLLinks(IEnumerable<MeshFilter> filts)
        {
            List<Mesh> toRemove = new List<Mesh>();
            foreach (var filt in filts)
            {
                if (filt &&
                    filt.sharedMesh != null &&
                    glLinks.ContainsKey(filt.sharedMesh))
                {
                    toRemove.Add(filt.sharedMesh);
                    var originalMesh = glLinks[filt.sharedMesh];
                    filt.sharedMesh = originalMesh;
                    glRegistry.Remove(new MeshRegistryKey(originalMesh, filt.GetComponent<MeshRenderer>()));
                }
            }
            foreach(var doneMesh in toRemove)
            {
                glLinks.Remove(doneMesh);
            }
        }

        private void RestoreAllGLLinks()
        {
            if (glLinks != null)
            {
                MeshFilter[] filts = GameObject.FindObjectsOfType<MeshFilter>();
                RestoreGLLinks(filts);
            }
            glLinks = null;
            glRegistry = null;
        }
#endregion

        #region MESH INSTANCING
        InstanceMeshNode[] iNodes;
        public void FormatMeshInstancing()
        {
            iNodes = InstanceMeshNode.GenerateMeshNodes();
            if(iNodes != null)
            {
                foreach(InstanceMeshNode node in iNodes)
                {
                    foreach(Transform xform in node.xforms)
                    {
                        xform.gameObject.SetActive(false);
                        //xform.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
        }

        public void CleanupMeshInstancing()
        {
            InstanceMeshNode.CleanupGeneratedMeshNodes();
            if (iNodes != null)
            {
                foreach(InstanceMeshNode node in iNodes)
                {
                    foreach(Transform xform in node.xforms)
                    {
                        //xform.GetComponent<MeshRenderer>().enabled = true;
                        xform.gameObject.SetActive(true);
                    }
                    DestroyImmediate(node.gameObject);
                }
            }
            iNodes = null;
        }
#endregion

        #region COLLIDERS
        /// <summary>
        /// Formats the scene to correctly export colliders to match XREngine colliders spec
        /// </summary>
        GameObject cRoot;
        private void FormatForExportingColliders()
        {
            cRoot = new GameObject("Colliders", typeof(ColliderParent));
            //Dictionary<Collider, Transform> parents = new Dictionary<Collider, Transform>();
            Material defaultMat = AssetDatabase.LoadMainAssetAtPath(defaultMatPath) as Material;
            Collider[] colliders = GameObject.FindObjectsOfType<Collider>().Where((col) => col.gameObject.activeInHierarchy).ToArray();
            foreach(var collider in colliders)
            {
                Transform xform = collider.transform;
                Vector3 position = xform.position;
                Quaternion rotation = xform.rotation;
                Vector3 scale = xform.lossyScale;
                //parents[collider] = xform;
                if (collider.GetType() == typeof(BoxCollider))
                {
                    var box = (BoxCollider)collider;
                    GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    clone.name = xform.gameObject.name + "__COLLIDER__";
                    clone.transform.position = position;
                    clone.transform.rotation = rotation;
                    clone.transform.localScale = scale;



                    clone.transform.position += clone.transform.localToWorldMatrix.MultiplyVector(box.center);
                    Vector3 nuScale = clone.transform.localScale;
                    nuScale.x *= box.size.x;
                    nuScale.y *= box.size.y;
                    nuScale.z *= box.size.z;
                    nuScale *= 0.5f;
                    clone.transform.localScale = nuScale;
                    MeshRenderer rend = clone.GetComponent<MeshRenderer>();
                    //rend.lightmapIndex = -1;
                    rend.material = defaultMat;
                    clone.transform.SetParent(cRoot.transform, true);
                }
                else
                {
                    GameObject clone = Instantiate(xform.gameObject, cRoot.transform, true);
                    clone.transform.position = position;
                    clone.transform.rotation = rotation;
                    clone.name += "__COLLIDER__";
                    MeshRenderer rend = clone.GetComponent<MeshRenderer>();
                    rend.enabled = true;
                    rend.lightmapIndex = -1;
                }
                
                
                
                
            }
        }
        private void CleanUpExportingColliders()
        {
            if(cRoot)
            {
                DestroyImmediate(cRoot);
            }
        }

#endregion

        private void Export(bool savePersistent)
        {
            ExportSequence(savePersistent);
        }
        private void ExportSequence(bool savePersistent)
        {
            DirectoryInfo directory = new DirectoryInfo(PipelineSettings.ConversionFolder);
            if(!directory.Exists)
            {
                Directory.CreateDirectory(PipelineSettings.ConversionFolder);
            }
            string exportFolder = Path.Combine(PipelineSettings.XREProjectFolder, "assets");
            DirectoryInfo outDir = new DirectoryInfo(exportFolder);
            if(!outDir.Exists)
            {
                Directory.CreateDirectory(exportFolder);
            }
                
            var files = directory.GetFiles();
            var subDirectories = directory.GetDirectories();

            //delete files in pipeline folder to make way for new export
            foreach(var file in files)
            {
                file.Delete();
            }

            foreach(var subDir in subDirectories)
            {
                subDir.Delete(true);
            }

            //set exporter path
            ExporterSettings.Export.name = PipelineSettings.GLTFName;
            ExporterSettings.Export.folder = PipelineSettings.ConversionFolder;
            //set other exporter parameters
            ExporterSettings.NormalTexture.maxSize = PipelineSettings.CombinedTextureResolution;

            //before we start staging the scene for export, invoke the OnExport event to have all
            //active XRE nodes export files to the linked project

            RPComponent.InvokeExport();

            //TODO: move most of these export helper functions into the OnExport handlers of the
            //      xrengine classes

            StageLights();
            StageLightmapping();
            FormatForExportingLODs();

            if(PipelineSettings.ExportColliders)
            {
                FormatForExportingColliders();
            }
            
            if (PipelineSettings.InstanceMeshes && PipelineSettings.BasicGLTFConvert)
            {
                FormatMeshInstancing();
            }
            
            SerializeAllMaterials();

            CreateBakedMeshes(savePersistent);
            
            if (PipelineSettings.ExportSkybox)
            {
                FormatForExportingSkybox();
            }

            if(PipelineSettings.ExportEnvmap)
            {
                FormatForExportingEnvmap();
            }

            

            //convert materials to SeinPBR
            StandardToSeinPBR.AllToSeinPBR();

            try
            {
                exporter.Export();
            } catch (System.NullReferenceException e)
            {
                UnityEngine.Debug.LogError("export error:" + e);
            }

            state = State.POST_EXPORT;

            if (PipelineSettings.ExportColliders)
            {
               CleanUpExportingColliders();
            }


            //restore materials
            StandardToSeinPBR.RestoreMaterials();
            if (PipelineSettings.meshMode == MeshExportMode.COMBINE)
            {
                CleanupMeshCombine();
            }
            RestoreAllGLLinks();
            DeserializeAllMaterials();


            CleanupExportingLODs();

            if(PipelineSettings.ExportSkybox)
            {
                CleanupExportingSkybox();
            }

            if(PipelineSettings.ExportEnvmap)
            {
                CleanupExportEnvmap();
            }
            
            if(PipelineSettings.InstanceMeshes && PipelineSettings.BasicGLTFConvert)
            {
                CleanupMeshInstancing();
            }
            
            CleanupLightmapping();
            CleanupLights();

            //clear generated pipeline assets
            PipelineSettings.ClearPipelineJunk();

            //now execute the GLTF conversion script in the Pipeline folder

            var cmd = new ProcessStartInfo();

            if (SystemInfo.operatingSystem.ToLower().Contains("mac"))
            {
                //for mac
                cmd.FileName = "/bin/bash";
                cmd.UseShellExecute = false;
                cmd.RedirectStandardError = true;
                cmd.RedirectStandardInput = true;
                cmd.RedirectStandardOutput = true;
                cmd.WindowStyle = ProcessWindowStyle.Hidden;

            }
            else
            {
                //windows
                cmd.UseShellExecute = false;
                //cmd.Verb = "runas";
                cmd.RedirectStandardInput = true;
                cmd.RedirectStandardOutput = true;
                cmd.RedirectStandardError = true;
                cmd.WindowStyle = ProcessWindowStyle.Hidden;

                cmd.FileName = "CMD.exe";
            }

            

            Process proc = new Process();
            proc.StartInfo = cmd;
            proc.Start();
            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine(string.Format("cd {0}", PipelineSettings.PipelineFolder));
            proc.StandardInput.Flush();
            string fileName = PipelineSettings.GLTFName;

            if(PipelineSettings.BasicGLTFConvert)
            {
                /* old pipeline */
                if (SystemInfo.operatingSystem.ToLower().Contains("mac"))
                {
                    //mac
                    proc.StandardInput.WriteLine(string.Format("\"/usr/local/bin/node\" gltf_converter.js {0} \"{1}\"", fileName, ExportPath));
                }
                else
                {
                    //windows
                    proc.StandardInput.WriteLine(string.Format("\"C:/Program Files/nodejs/node.exe\" gltf_converter.js {0} \"{1}\"", fileName, ExportPath));
                }
            }
            else
            {
                if (SystemInfo.operatingSystem.ToLower().Contains("mac"))
                {
                    proc.StandardInput.WriteLine("export TOKTX_PATH=\"toktx\"");
                    proc.StandardInput.WriteLine("export BASISU_PATH=\"basisu\"");
                }
                else
                {
                    proc.StandardInput.WriteLine("set TOKTX_PATH=\"toktx.exe\"");
                    proc.StandardInput.WriteLine("set BASISU_PATH=\"basisu.exe\"");
                }

                proc.StandardInput.Flush();
                proc.StandardInput.WriteLine(string.Format("gltfpack.exe -i ../Outputs/GLTF/{0}.gltf -o \"{1}{0}.{7}\"{2}{3}{4}{5}{6} -noq", fileName, ExportPath,
                    PipelineSettings.MeshOptCompression ? " -cc" : "",
                    !PipelineSettings.CombineMaterials ? " -km" : "",
                    !PipelineSettings.CombineNodes ? " -ke -kn" : "",
                    PipelineSettings.InstanceMeshes ? " -mi" : "",
                    PipelineSettings.KTX2Compression ? " -tc" : "",
                    PipelineSettings.ExportFormat == ExportFormat.GLTF ? "gltf" : "glb"));
            }
            
            proc.StandardInput.Flush();
            proc.StandardInput.Close();
            //proc.WaitForExit();
            UnityEngine.Debug.Log(proc.StandardOutput.ReadToEnd());
            UnityEngine.Debug.Log(proc.StandardError.ReadToEnd());
            //OpenInFileBrowser.Open(exportFolder);*/
            state = State.INITIAL;
        }
    }

}
