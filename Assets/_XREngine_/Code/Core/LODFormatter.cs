using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
namespace XREngine
{
    public class LODFormatter
    {
        public static void FormatLODs()
        {
            var lodGroups = GameObject.FindObjectsOfType<Transform>().Where((tr) => Regex.IsMatch(tr.name, ".*_LODGroup"));
            foreach (var group in lodGroups)
            {
                LODGroup thisLOD = group.gameObject.GetComponent<LODGroup>();
                if(thisLOD == null)
                    thisLOD = group.gameObject.AddComponent<LODGroup>();

                var children = group.GetComponentsInChildren<Transform>().Where((tr) => tr != group && Regex.IsMatch(tr.name, @"(?<=_LOD)\d+"));
                int nChildren = children.Count();
                thisLOD.SetLODs(children.Select((tr, i) => new LOD((nChildren - i - 1) * 1f / nChildren, tr.GetComponentsInChildren<MeshRenderer>())).ToArray());
            }
        }
    }

}
