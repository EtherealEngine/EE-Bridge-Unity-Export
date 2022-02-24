/**
 * @File   : EditorExportor.cs
 * @Author : dtysky (dtysky@outlook.com)
 * @Link   : dtysky.moe
 * @Date   : 2019/09/09 0:00:00PM
 */
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GLTF;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using XREngine;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using XREngine.XRProject;

namespace SeinJS
{
    public class EditorExporter
    {
        TaskManager _taskManager;
        bool _isDone;
        bool _userStopped;
        //Dictionary<UnityEngine.Mesh, UnityEngine.Mesh> glLinks;
        //public delegate void ProgressCallback(string step, string details, int current, int total);

        public EditorExporter()
        {
            _taskManager = new TaskManager();
        }

        public void Export(List<ExporterEntry> entries)
        {
			ExporterEntry.Init();
            ExtensionManager.BeforeExport();
            _isDone = false;
            _userStopped = false;

            foreach (var entry in entries)
            {
                ExportOne(entry);
            }

            FinishExport();
        }

        private void ExportOne(ExporterEntry entry)
        {
            //glLinks = new Dictionary<UnityEngine.Mesh, UnityEngine.Mesh>();

            var root = entry.root;
            root.Asset = new Asset();
            root.Asset.Generator = Config.GeneratorName;
            root.Asset.Version = "2.0";
            root.Scenes = new List<Scene>();
            root.Scenes.Add(new Scene());
            root.Scene = new SceneId{ Id = 0, Root = root };



            // gamma and hdr setting
            entry.root.Extensions = new Dictionary<string, Extension>();
            //ExtensionManager.Serialize(ExtensionManager.GetExtensionName(typeof(Sein_rendererExtensionFactory)), entry, entry.root.Extensions);

            

            // if ambientMode is not Flat, use sein_imageBaseLighting extension
            if (ExporterSettings.Lighting.ambient && RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
            {
                //ExtensionManager.Serialize(ExtensionManager.GetExtensionName(typeof(Sein_ambientLightExtensionFactory)), entry, entry.root.Extensions);
            }


            foreach (Transform tr in entry.transforms)
            {
                SkinnedMeshRenderer skin = tr.GetComponent<SkinnedMeshRenderer>();
                if (skin)
                {
                    foreach (Transform bone in skin.bones)
                    {
                        entry.bones.Add(bone);
                    }
                }
            }

            foreach (var tr in entry.transforms)
            {
                if (!tr.gameObject.activeInHierarchy)
                {
                    continue;
                }



                ExportNode(tr, entry);
            }

            // process children
            ProcessChildren(entry);

            // process skinning and bones
            foreach (var tr in entry.transforms)
            {
                if (!tr.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ExportSkin(tr, entry);
            }

            // process animations
            foreach (var tr in entry.transforms)
            {
                if (!tr.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ExportAnimations(tr, entry);
            }

            // process extensions
            foreach (var tr in entry.transforms)
            {
                if (!tr.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ExportCamera(tr, entry);
                ExportExtensions(tr, entry);
            }

            ExportDone(entry);
        }

        private void ExportNode(Transform tr, ExporterEntry entry)
        {
            if (!tr.gameObject.activeInHierarchy) return;
            var id = entry.SaveNode(tr, PipelineSettings.ExportColliders);

            var seinNode = tr.GetComponent<SeinNode>();
            if (seinNode)
            {
                if (seinNode.selfType == ESeinNodeType.Actor)
                {
                    entry.transformsInSameActor.Add(tr, new List<Transform>());
                }
                else
                {
                    var parent = tr.parent;
                    while (parent)
                    {
                        var parentSeinNode = parent.GetComponent<SeinNode>();
                        if (parentSeinNode == null || parentSeinNode.selfType == ESeinNodeType.Component)
                        {
                            parent = parent.parent;
                            continue;
                        }

                        entry.transformsInSameActor[parent].Add(tr);
                        break;
                    }
                }
            }
            /*if (
                PipelineSettings.ExportColliders &&
                tr.GetComponent<Collider>() == null
               )
            {
                return;
            }*/
            ExportMesh(tr, entry);
        }

        private void ExportMesh(Transform tr, ExporterEntry entry)
        {
            
            var renderer = GetRenderer(tr);
            
            var mesh = GetMesh(tr);

            if (!mesh)
            {
                return;
            }
            if (mesh.vertexCount == 0)
                return;

            if (tr.GetComponent<SeinSprite>() != null)
            {
                return;
            }
            /*
            bool hasLightmap = renderer.lightmapIndex >= 0;

            if ((hasLightmap && PipelineSettings.lightmapMode == LightmapMode.BAKE_SEPARATE) ||
                Regex.IsMatch(AssetDatabase.GetAssetPath(mesh), @".*\.glb"))
            {
                MeshFilter filt = tr.GetComponent<MeshFilter>();

                string nuMeshPath = PipelineSettings.PipelineAssetsFolder.Replace(Application.dataPath, "Assets") + renderer.transform.name + "_" + System.DateTime.Now.Ticks + ".asset";

                UnityEngine.Mesh nuMesh = Object.Instantiate(mesh);
                
                AssetDatabase.CreateAsset(nuMesh, nuMeshPath);
                AssetDatabase.Refresh();
                if(hasLightmap)
                {
                    var off = renderer.lightmapScaleOffset;
                    var nuUv2s = nuMesh.uv2.Select((uv2) => uv2 * new Vector2(off.x, off.y) + new Vector2(off.z, off.w)).ToArray();
                    nuMesh.uv2 = nuUv2s;
                    nuMesh.UploadMeshData(false);
                }
                if(filt != null)
                    filt.sharedMesh = nuMesh;
                else
                {
                    SkinnedMeshRenderer mrend = tr.GetComponent<SkinnedMeshRenderer>();
                    if(mrend != null)
                    {
                        mrend.sharedMesh = nuMesh;
                    }
                }
                glLinks[nuMesh] = mesh;
                mesh = nuMesh;
            }
            */

            var result = entry.SaveMesh(mesh, renderer);
            var id = result.key;
            var needProcessMatrials = result.value;
            var node = entry.tr2node[tr];
            node.Mesh = id;

            bool isInstanced = renderer.GetComponent<InstanceMeshNode>() != null;
            if(isInstanced)
            {
                if(node.Extensions == null)
                {
                    node.Extensions = new Dictionary<string, Extension>();
                }
                ExtensionManager.Serialize(ExtensionManager.GetExtensionName(typeof(EXT_mesh_gpu_instancing_Factory)), entry, node.Extensions, component:renderer);
            }

            if (needProcessMatrials)
            {
                var materialComponents = tr.GetComponents<SeinCustomMaterial>();
                int i = 0;
                var materials = renderer.sharedMaterials;
                foreach (var primitive in id.Value.Primitives)
                {
                    if (i >= materials.Length)
                    {
                        Debug.LogError("No material in primitive" + " " + i + " in mesh " + mesh.name + " !");
                        break;
                    }

                    if (materialComponents.Length == 1 && materials.Length == 1)
                    {
                        ExportComponentMaterial(materialComponents[0], primitive, entry);
                        continue;
                    }

                    bool hasComponent = false;
                    foreach (var materialComponent in materialComponents)
                    {
                        if (materialComponent.unityMaterialName == materials[i].name)
                        {
                            ExportComponentMaterial(materialComponent, primitive, entry);
                            hasComponent = true;
                            break;
                        }
                    }

                    if (!hasComponent)
                    {
                        //if (renderer.GetType() == typeof(SkinnedMeshRenderer))
                        //    renderer = null;
                        ExportNormalMaterial(materials[i], primitive, entry, renderer);
                    }

                    i += 1;
                }
            }
        }


        private void ExportNormalMaterial(UnityEngine.Material material, MeshPrimitive primitive, ExporterEntry entry, Renderer renderer)
        {
            primitive.Material = entry.SaveNormalMaterial(material, renderer);
        }

        private void ExportComponentMaterial(SeinCustomMaterial material, MeshPrimitive primitive, ExporterEntry entry)
        {
            primitive.Material = entry.SaveComponentMaterial(material);
        }

        private void ProcessChildren(ExporterEntry entry)
        {
            foreach (var tr in entry.transforms)
            {
                if (!tr.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var id = entry.tr2nodeId[tr];
                if (!tr.parent || !entry.tr2node.ContainsKey(tr.parent))
                {
                    var scene = entry.root.Scene.Value;
                    if (scene.Nodes == null)
                    {
                        scene.Nodes = new List<NodeId>();
                    }

                    scene.Nodes.Add(id);
                }
                else
                {
                    var parent = entry.tr2node[tr.parent];
                    if (parent.Children == null)
                    {
                        parent.Children = new List<NodeId>();
                    }

                    parent.Children.Add(id);
                }
            }
            
            foreach (var trs in entry.transformsInSameActor.Values)
			{
                var names = new Dictionary<string, int>();

                foreach (var tr in trs)
                {
                    var node = entry.tr2node[tr];
                    if (names.ContainsKey(node.Name)) {
                        names[node.Name] += 1;
                        node.Name += names[node.Name];
                    }
                    else
                    {
                        names.Add(node.Name, 0);
                    }
                }
			}
        }

        private void ExportSkin(Transform tr, ExporterEntry entry)
        {
            var skinMesh = tr.GetComponent<SkinnedMeshRenderer>();

            if (skinMesh != null && skinMesh.enabled && CheckSkinValidity(skinMesh, entry) && skinMesh.rootBone != null)
            {
                var node = entry.tr2node[tr];
                node.Skin = entry.SaveSkin(tr);
            }
        }

        private void ExportAnimations(Transform tr, ExporterEntry entry)
        {
            if (tr.GetComponent<UnityEngine.Animation>())
            {
                //Debug.LogError("Only support animator now !");
                return;
            }

            var anim = tr.GetComponent<Animator>();
            if (anim)
            {
                entry.SaveAnimations(tr);
            }
        }

        private void ExportCamera(Transform tr, ExporterEntry entry)
        {
            var node = entry.tr2node[tr];
            var camera = tr.GetComponent<UnityEngine.Camera>();

            if (camera != null)
            {
                node.Camera = entry.SaveCamera(camera);
            }
        }

        private void ExportExtensions(Transform tr, ExporterEntry entry)
        {
            var node = entry.tr2node[tr];
            foreach (var component in tr.GetComponents<Component>())
            {
                if (component && ExtensionManager.Component2Extensions.ContainsKey(component.GetType()))
                {
                    if (node.Extensions == null)
                    {
                        node.Extensions = new Dictionary<string, Extension>();
                    }

                    ExtensionManager.Serialize(component, entry, node.Extensions);
                }
            }
        }

        private void ExportDone(ExporterEntry entry)
        {
            entry.Finish();
        }

        private Renderer GetRenderer(Transform tr)
        {
            Renderer mr = tr.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                mr = tr.GetComponent<SkinnedMeshRenderer>();
            }
            return mr;
        }

        private UnityEngine.Mesh GetMesh(Transform tr)
        {
            var mr = GetRenderer(tr);
            UnityEngine.Mesh m = null;
            if (mr != null && mr.enabled)
            {
                var t = mr.GetType();
                if (t == typeof(MeshRenderer))
                {
                    MeshFilter mf = tr.GetComponent<MeshFilter>();
                    if (!mf)
                    {
                        Debug.Log("The gameObject " + tr.name + " will be exported as Transform (object has no MeshFilter component attached)");
                        return null;
                    }
                    m = mf.sharedMesh;
                }
                else if (t == typeof(SkinnedMeshRenderer))
                {
                    SkinnedMeshRenderer smr = mr as SkinnedMeshRenderer;
                    m = smr.sharedMesh;
                }
            }
            return m;
        }

        private bool CheckSkinValidity(SkinnedMeshRenderer skin, ExporterEntry entry)
        {
            foreach (Transform tr in skin.bones)
            {
                if (!entry.bones.Contains(tr))
                {
                    Utils.ThrowExcption("Error while exporting skin for " + skin.name + " (skipping skinning export).\nClick for more details:\n \nThe following bones are used but are not selected" + tr.name + "\n");
                    return false;
                }
            }

            return true;
        }

        public void Update()
        {
            if (!_isDone)
            {
                if (_userStopped)
                {
                    _userStopped = false;
                    Clear();
                    _isDone = true;
                }
                else
                {
                    if (_taskManager != null && _taskManager.play())
                    {
                        // Do stuff
                    }
                    else
                    {
                        _isDone = true;
                        FinishExport();
                    }
                }
            }
        }

        

        private void Clear()
        {
            //RestoreGLLinks();
            //PipelineSettings.ClearPipelineJunk();
            ExporterUtils.FinishExport();
            ExtensionManager.FinishExport();
            Resources.UnloadUnusedAssets();
        }

        private void FinishExport()
        {
            Clear();
        }
    }
}
