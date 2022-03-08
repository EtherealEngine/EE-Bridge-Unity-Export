using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SeinJS;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using XREngine.Utilities;

namespace XREngine.XREngineProject
{
    public class MOZ_lightmap_Factory : SeinExtensionFactory
    {

        public override string GetExtensionName()
        {
            return "MOZ_lightmap";
        }

        public override List<System.Type> GetBindedComponents()
        {
            return new List<Type> { };
        }

        public override List<EExtensionType> GetExtensionTypes()
        {
            return new List<EExtensionType> { EExtensionType.Material };
        }

        static Dictionary<int, Texture2D> _lightmapRegistry;
        static Dictionary<int, Texture2D> LightmapRegistry
        {
            get
            {
                if(_lightmapRegistry == null)
                    _lightmapRegistry = new Dictionary<int, Texture2D>();
                return _lightmapRegistry;
            }
        }

        public static void ClearRegistry()
        {
            LightmapRegistry.Clear();
        }

        public Texture2D GenerateLightmapAsset(int lmIdx)
        {
            if(LightmapRegistry.ContainsKey(lmIdx))
            {
                return LightmapRegistry[lmIdx];
            }
            
            var lightmapData = LightmapSettings.lightmaps[lmIdx];
            var txrData = lightmapData.lightmapColor;
            GLTFUtilities.SetTextureImporterFormat(txrData, true);
            string path = PipelineSettings.PipelineAssetsFolder + "lightmap_txr_" + lmIdx + ".jpg";
            path = path.Replace(Application.dataPath, "Assets");
            Texture2D txr = new Texture2D(txrData.width, txrData.height);
            Graphics.CopyTexture(txrData, txr);
            txr.Apply();
            File.WriteAllBytes(path, txr.EncodeToJPG());
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
            txr = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            GLTFUtilities.SetTextureImporterFormat(txr, true);
            AssetDatabase.Refresh();
            txr = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            LightmapRegistry[lmIdx] = txr;
            return txr;
        }


        public override void Serialize(ExporterEntry entry, Dictionary<string, Extension> extensions, UnityEngine.Object component = null, object options = null)
        {
            var rend = component as MeshRenderer;
            if (PipelineSettings.lightmapMode != LightmapMode.BAKE_SEPARATE)
            {
                return;
            }
            if(!rend.gameObject.isStatic || rend.lightmapIndex < 0 || rend.gameObject.GetComponent<IgnoreLightmap>() != null)
            {
                return;
            }
            var extension = new MOZ_lightmap();
            var lightmap = GenerateLightmapAsset(rend.lightmapIndex);

            var lmID = entry.SaveTexture(lightmap, maxSize: PipelineSettings.CombinedTextureResolution);
            extension.index = lmID.Id;
            extension.intensity = 1;

            AddExtension(extensions, extension);
        }
        public override Extension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            throw new System.NotImplementedException();
        }
    }

}
