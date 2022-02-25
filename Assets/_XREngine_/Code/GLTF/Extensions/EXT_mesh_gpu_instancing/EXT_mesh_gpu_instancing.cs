using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using SeinJS;

namespace XREngine.XREngineProject
{
    public class EXT_mesh_gpu_instancing : Extension
    {
        public AccessorId position, rotation, scale;
        public JProperty Serialize()
        {
            return new JProperty
            (
                "EXT_mesh_gpu_instancing", new JObject
                (
                    new JProperty("attributes", new JObject(
                        new JProperty("TRANSLATION", position.Id),
                        new JProperty("ROTATION", rotation.Id),
                        new JProperty("SCALE", scale.Id)
                        )
                    )
                )
            );
        }
    }

}


