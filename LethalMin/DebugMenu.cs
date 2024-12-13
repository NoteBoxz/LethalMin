using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LethalMin
{
    public class DebugMenu : MonoBehaviour
    {
        private Dictionary<string, List<(GameObject gameObject, List<(string name, FieldInfo field, PropertyInfo property, object target)> items)>> debugItems = new Dictionary<string, List<(GameObject, List<(string, FieldInfo, PropertyInfo, object)>)>>();
        private Vector2 mainScrollPosition;
        private Dictionary<string, Vector2> classScrollPositions = new Dictionary<string, Vector2>();
        private bool showDebugMenu = false;
        private string selectedClassName = null;
        private float classContentHeight = 0f;
        private const float MenuWidth = 400f;
        private const float MenuHeight = 600f;
        private const float ClassViewHeight = 400f;

        private void Start()
        {
            CollectDebuggableItems();
        }

        private void Update()
        {
            Vector3 pos = Vector3.zero;
            if (StartOfRound.Instance.localPlayerController != null)
            {
                pos = StartOfRound.Instance.localPlayerController.transform.position;
            }
            foreach (var className in debugItems.Keys.ToList())
            {
                if (debugItems[className].Count == 0)
                {
                    debugItems.Remove(className);
                    continue;
                }
                debugItems[className] = debugItems[className]
                    .Where(item => item.gameObject != null)
                    .OrderBy(item => Vector3.Distance(item.gameObject.transform.position, pos))
                    .ToList();
            }
            if (showDebugMenu)
            {
                //CollectDebuggableItems();
            }
        }

        private void CollectDebuggableItems()
        {
            debugItems.Clear();
            IDebuggable[] debuggables = FindObjectsOfType<MonoBehaviour>().OfType<IDebuggable>().ToArray();

            //LethalMin.Logger.LogInfo($"Found {debuggables.Length} IDebuggable objects");

            foreach (var debuggable in debuggables)
            {
                if (debuggable == null) continue;

                MonoBehaviour monoBehaviour = debuggable as MonoBehaviour;
                if (monoBehaviour == null) continue;

                string className = monoBehaviour.GetType().Name;
                if (!debugItems.ContainsKey(className))
                {
                    debugItems[className] = new List<(GameObject, List<(string, FieldInfo, PropertyInfo, object)>)>();
                }

                var instanceItems = new List<(string, FieldInfo, PropertyInfo, object)>();

                instanceItems.Add(("Position", null, monoBehaviour.transform.GetType().GetProperty("position"), monoBehaviour.transform));

                FieldInfo[] fields = monoBehaviour.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo[] properties = monoBehaviour.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (Attribute.GetCustomAttribute(field, typeof(IDebuggable.DebugAttribute)) != null)
                    {
                        instanceItems.Add((field.Name, field, null, monoBehaviour));
                    }
                }

                foreach (var property in properties)
                {
                    if (Attribute.GetCustomAttribute(property, typeof(IDebuggable.DebugAttribute)) != null)
                    {
                        instanceItems.Add((property.Name, null, property, monoBehaviour));
                    }
                }

                debugItems[className].Add((monoBehaviour.gameObject, instanceItems));
            }

            foreach (var className in debugItems.Keys)
            {
                break;
                //LethalMin.Logger.LogInfo($"Class: {className}, Instances: {debugItems[className].Count}");
            }
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 100, 30), "Debug Menu"))
            {
                showDebugMenu = !showDebugMenu;
                if (showDebugMenu)
                {
                    CollectDebuggableItems();
                }
            }

            if (showDebugMenu)
            {
                GUI.Box(new Rect(10, 50, MenuWidth, MenuHeight), "Debug Menu");

                mainScrollPosition = GUI.BeginScrollView(new Rect(10, 80, MenuWidth, MenuHeight - 30), mainScrollPosition, new Rect(0, 0, MenuWidth - 30, classContentHeight));

                float yOffset = 0;
                classContentHeight = 0;

                foreach (var className in debugItems.Keys)
                {
                    if (GUI.Button(new Rect(10, yOffset, MenuWidth - 40, 30), $"{className} ({debugItems[className].Count})"))
                    {
                        selectedClassName = (selectedClassName == className)  null : className;
                    }
                    yOffset += 35;
                    classContentHeight += 35;

                    if (selectedClassName == className)
                    {
                        if (!classScrollPositions.ContainsKey(className))
                        {
                            classScrollPositions[className] = Vector2.zero;
                        }

                        float classContentHeight = CalculateClassContentHeight(className);
                        Rect classViewPort = new Rect(20, yOffset, MenuWidth - 60, ClassViewHeight);
                        Rect classContentRect = new Rect(0, 0, MenuWidth - 80, classContentHeight);

                        classScrollPositions[className] = GUI.BeginScrollView(classViewPort, classScrollPositions[className], classContentRect);

                        float classYOffset = 0;
                        foreach (var instance in debugItems[className])
                        {
                            if (instance.gameObject == null) continue;

                            GUI.Label(new Rect(0, classYOffset, MenuWidth - 80, 25), instance.gameObject.name, GUI.skin.button);
                            classYOffset += 30;

                            foreach (var item in instance.items)
                            {
                                string value = "N/A";
                                object rawValue = null;
                                try
                                {
                                    rawValue = item.field != null
                                         item.field.GetValue(item.target)
                                        : item.property.GetValue(item.target);
                                    value = rawValue.ToString()  "null";
                                }
                                catch (Exception e)
                                {
                                    LethalMin.Logger.LogError($"Error getting value for {item.name}: {e.Message}");
                                }

                                GUI.Label(new Rect(10, classYOffset, MenuWidth - 90, 20), $"{item.name}: {value}");
                                classYOffset += 25;

                                if (rawValue != null && (rawValue is System.Collections.IEnumerable) && !(rawValue is string))
                                {
                                    RenderListOrArray(rawValue, ref classYOffset, MenuWidth - 90);
                                }
                            }

                            classYOffset += 10;
                        }

                        GUI.EndScrollView();

                        yOffset += ClassViewHeight + 10;
                        classContentHeight += ClassViewHeight + 10;
                    }
                }

                GUI.EndScrollView();
            }
        }

        private float CalculateClassContentHeight(string className)
        {
            float height = 0;
            foreach (var instance in debugItems[className])
            {
                if (instance.gameObject == null) continue;
                height += 30 + (instance.items.Count * 25) + 10;
            }
            return height;
        }
        private void RenderListOrArray(object value, ref float classYOffset, float width)
        {
            if (value == null) return;

            var enumerable = value as System.Collections.IEnumerable;
            if (enumerable == null) return;

            int index = 0;
            foreach (var item in enumerable)
            {
                GUI.Label(new Rect(20, classYOffset, width - 100, 20), $"[{index}]: {item.ToString()  "null"}");
                classYOffset += 25;
                index++;

                // Limit the number of items displayed to prevent overwhelming the UI
                if (index >= 10)
                {
                    GUI.Label(new Rect(20, classYOffset, width - 100, 20), "...");
                    classYOffset += 25;
                    break;
                }
            }
        }
    }
}