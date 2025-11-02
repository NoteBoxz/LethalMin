using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LethalMin.Utils
{
    public class GrowthObjectCache : MonoBehaviour
    {
        public List<int> Keys = new List<int>();
        public List<GameObject> AllObjects = new List<GameObject>();
        public List<int> ObjectCounts = new List<int>();

        public void ConvertDictionaryToCache(Dictionary<int, List<GameObject>> dictionary)
        {
            Keys = new List<int>(dictionary.Keys);
            AllObjects = new List<GameObject>();
            ObjectCounts = new List<int>();

            foreach (var kvp in dictionary)
            {
                AllObjects.AddRange(kvp.Value);
                ObjectCounts.Add(kvp.Value.Count);
            }
        }

        public Dictionary<int, List<GameObject>> ConvertCacheToDictionary()
        {
            Dictionary<int, List<GameObject>> dictionary = new Dictionary<int, List<GameObject>>();
            int startIndex = 0;

            for (int i = 0; i < Keys.Count; i++)
            {
                int count = ObjectCounts[i];
                dictionary[Keys[i]] = AllObjects.GetRange(startIndex, count);
                startIndex += count;
            }

            return dictionary;
        }
    }
}