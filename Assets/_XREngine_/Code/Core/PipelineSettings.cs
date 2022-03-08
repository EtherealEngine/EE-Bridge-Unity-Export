using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using System.Linq;



namespace XREngine
{
    public enum LightmapMode
    {
        IGNORE,
        BAKE_COMBINED,
        BAKE_SEPARATE
    }

    public enum MeshExportMode
    {
        DEFAULT,
        COMBINE,
        NO_MESHES
    }

    [InitializeOnLoad]
    public static class PipelineSettings
    {
        [System.Serializable]
        struct Data
        {
            public string GLTFName;
            public string XREProjectFolder;
            public bool ExportLights;
            public bool ExportColliders;
            public bool ExportSkybox;
            public bool ExportEnvmap;

            public bool InstanceMeshes;
            public bool MeshOptCompression;
            public bool KTX2Compression;
            public bool CombineMaterials;
            public bool CombineNodes;
            
            public MeshExportMode meshMode;
            public bool preserveLightmapping;
            public LightmapMode lightmapMode;
            public int CombinedTextureResolution;
            public void Apply()
            {
                PipelineSettings.GLTFName = GLTFName;
                PipelineSettings.XREProjectFolder = this.XREProjectFolder;

                PipelineSettings.ExportLights = this.ExportLights;
                PipelineSettings.ExportColliders = this.ExportColliders;
                PipelineSettings.ExportSkybox = this.ExportSkybox;
                PipelineSettings.ExportEnvmap = ExportEnvmap;

                PipelineSettings.InstanceMeshes = this.InstanceMeshes;
                PipelineSettings.MeshOptCompression = this.MeshOptCompression;
                PipelineSettings.KTX2Compression = this.KTX2Compression;
                PipelineSettings.CombineMaterials = this.CombineMaterials;
                PipelineSettings.CombineNodes = this.CombineNodes;

                PipelineSettings.lightmapMode = this.lightmapMode;
                PipelineSettings.preserveLightmapping = this.preserveLightmapping;
                PipelineSettings.meshMode = meshMode;

                PipelineSettings.CombinedTextureResolution = this.CombinedTextureResolution;
            }
            public void Set()
            {
                GLTFName = PipelineSettings.GLTFName;
                XREProjectFolder = PipelineSettings.XREProjectFolder;
               
                ExportLights = PipelineSettings.ExportLights;
                ExportColliders = PipelineSettings.ExportColliders;
                ExportSkybox = PipelineSettings.ExportSkybox;
                ExportEnvmap = PipelineSettings.ExportEnvmap;
                
                InstanceMeshes = PipelineSettings.InstanceMeshes;
                MeshOptCompression = PipelineSettings.MeshOptCompression;
                KTX2Compression = PipelineSettings.KTX2Compression;
                CombineMaterials = PipelineSettings.CombineMaterials;
                CombineNodes = PipelineSettings.CombineNodes;

                lightmapMode = PipelineSettings.lightmapMode;
                meshMode = PipelineSettings.meshMode;

                preserveLightmapping = PipelineSettings.preserveLightmapping;
                CombinedTextureResolution = PipelineSettings.CombinedTextureResolution;
            }
        }

        public static readonly string ConversionFolder = Application.dataPath + "/../Outputs/GLTF/";
        public static readonly string PipelineFolder = Application.dataPath + "/../Pipeline/";
        public static readonly string configFile = PipelineFolder + "settings.conf";
        public static readonly string PipelineAssetsFolder = Application.dataPath + "/_XREngine_/PipelineAssets/";
        public static readonly string PipelinePersistentFolder = Application.dataPath + "/_XREngine_/PersistentAssets/";
        public static string GLTFName;
        public static string XREProjectFolder;// = Application.dataPath + "/../Outputs/GLB/";

        

        public static string XREProjectName => 
            Regex.Match(XREProjectFolder, @"(?<=[\\\/])[\w-_]+(?=[\\\/]*$)").Value;

        public static string XRELocalPath => "https://localhost:8642/" + 
            Regex.Match(XREProjectFolder, @"[\w-]+[\\/]+[\w-]+[\\/]*$").Value;

        public static string XREScriptsFolder => XREProjectFolder + "/assets/scripts/";
        public static bool ExportLights;
        public static bool ExportColliders;
        public static bool ExportSkybox;
        public static bool ExportEnvmap;

        public static bool InstanceMeshes;
        public static bool MeshOptCompression;
        public static bool KTX2Compression;
        public static bool CombineMaterials;
        public static bool CombineNodes;

        public static MeshExportMode meshMode;

        public static bool preserveLightmapping;

        public static LightmapMode lightmapMode;

        public static int CombinedTextureResolution;// = 8192;
        
        static PipelineSettings()
        {
            ReadSettingsFromConfig();
        }

        public static void ReadSettingsFromConfig()
        {
            var config = new FileInfo(configFile);
            if (!config.Exists)
            {
                return;
            }
            var data = JsonConvert.DeserializeObject<Data>
            (
                File.ReadAllText(configFile)
            );
            data.Apply();
        }

        public static void SaveSettings()
        {
            var data = new Data();
            data.Set();
            File.WriteAllText(configFile, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public static void ClearPipelineJunk()
        {
            Regex filter = new Regex(@".*\.(jpg|png|tga|asset|mat)");
            var pipelineFiles = Directory.GetFiles(PipelineFolder);
            foreach(var path in pipelineFiles)
            {
                if(Directory.Exists(path))
                {
                    Directory.Delete(path);
                }
                else
                if(filter.IsMatch(path))
                {
                    File.Delete(path);
                }
            }
            
            AssetDatabase.DeleteAsset(PipelineAssetsFolder.Replace(Application.dataPath, "Assets"));
        }
    }

    
}

