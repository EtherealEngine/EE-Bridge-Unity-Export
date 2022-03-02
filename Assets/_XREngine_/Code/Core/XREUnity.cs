using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XREngine
{
    public class XREUnity 
    {
        public static IEnumerable<T> ChildComponents<T>(Transform root, bool includeInactive = false) where T:Component
        {
            List<T> result = new List<T>();
            Queue<Transform> frontier = new Queue<Transform>();
            frontier.Enqueue(root);

            while (frontier.Count > 0)
            {
                Transform tr = frontier.Dequeue();
                var comp = tr.GetComponent<T>();
                if(comp != null &&
                   (includeInactive || tr.gameObject.activeInHierarchy))
                {
                    result.Add(comp);
                }
                var children = Enumerable.Range(0, tr.childCount).Select(i => tr.GetChild(i));
                foreach(var child in children)
                {
                    frontier.Enqueue(child);
                }
            }

            return result;
        }
    }

}
