using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SeinJS;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using System;

namespace XREngine.XRProject
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

        public override void Serialize(ExporterEntry entry, Dictionary<string, Extension> extensions, UnityEngine.Object component = null, object options = null)
        {
            var rend = component as MeshRenderer;
            if (PipelineSettings.lightmapMode != LightmapMode.BAKE_SEPARATE)
            {
                return;
            }
            if(!rend.gameObject.isStatic || rend.lightmapIndex < 0)
            {
                return;
            }
            var extension = new MOZ_lightmap();
            var lightmap = LightmapSettings.lightmaps[rend.lightmapIndex];
            var lmID = entry.SaveTexture(lightmap.lightmapColor, maxSize: PipelineSettings.CombinedTextureResolution);
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
