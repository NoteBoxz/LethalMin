using System.Linq;
using UnityEngine;
// this does not work for some reason
namespace LethalMin
{
public static class ScriptableObjectHelper
{
    // Method to find all instances of a specific ScriptableObject type
    public static T[] FindAllInstances<T>() where T : ScriptableObject
    {
        return Resources.FindObjectsOfTypeAll<T>();
    }

    // Method to find a specific instance by name
    public static T FindInstance<T>(string objectName) where T : ScriptableObject
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(obj => obj.name == objectName);
    }
}
}