using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SeinJS;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using System;
using static SeinJS.ExporterEntry;
using System.Linq;

namespace XREngine.XRProject
{
    public class EXT_mesh_gpu_instancing_Factory : SeinExtensionFactory
    {

        public override string GetExtensionName()
        {
            return "EXT_mesh_gpu_instancing";
        }

        public override List<System.Type> GetBindedComponents()
        {
            return new List<Type> { };
        }

        public override List<EExtensionType> GetExtensionTypes()
        {
            return new List<EExtensionType> { EExtensionType.Node };
        }

        public override void Serialize(ExporterEntry entry, Dictionary<string, Extension> extensions, UnityEngine.Object component = null, object options = null)
        {
            var rend = component as MeshRenderer;
            if (rend.GetComponent<InstanceMeshNode>() == null) return;

            var iNode = rend.GetComponent<InstanceMeshNode>();

            var extension = new EXT_mesh_gpu_instancing();
            //check if node is an InstanceMeshNode

            var iMeshNode = iNode;

            System.Func<string, int, EntryBufferView> BufView = (suffix, _stride) =>
            {
                return entry.CreateByteBufferView(iMeshNode.mesh.name + "-" + suffix, _stride * iMeshNode.xforms.Count(), _stride);
            };
            //create buffer for position, rotation, scale
            var positionBuf = entry.PackAttrToBuffer(BufView("POSITION", 4 * 3), iMeshNode.xforms.Select((xform) =>
            {
                var pos = xform.position;
                return new Vector3(pos.x, pos.y, -pos.z);
            }).ToArray(), 0);

            var rotationBuf = entry.PackAttrToBuffer(BufView("ROTATION", 4 * 4), iMeshNode.xforms.Select((xform) =>
            {
                var r = xform.rotation;
                return new Vector4(r.x, r.y, r.z, r.w);
            }).ToArray(), 0);

            var scaleBuf = entry.PackAttrToBuffer(BufView("SCALE", 4 * 3), iMeshNode.xforms.Select((xform) => xform.lossyScale).ToArray(), 0);

            extension.position = positionBuf;
            extension.rotation = rotationBuf;
            extension.scale    = scaleBuf;

            AddExtension(extensions, extension);
        }
        public override Extension Deserialize(GLTFRoot root, JProperty extensionToken)
        {
            throw new System.NotImplementedException();
        }
    }

}
