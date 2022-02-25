using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using SeinJS;

namespace XREngine.XREngineProject
{
    public class MOZ_lightmap : Extension
    {
        public int index;
        public float intensity;

        public JProperty Serialize()
        {
            return new JProperty
            (
                "MOZ_lightmap", new JObject
                (
                    new JProperty("index", index),
                    new JProperty("intensity", intensity)
                )
            );
        }
    }

}


