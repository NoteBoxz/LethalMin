using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using UnityEngine;

namespace LethalMin
{
    /// <summary>
    /// ConfigItem class for managing configuration entries.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <remarks>
    /// Usable args and their formatting:
    /// 
    /// 1. For int and float types:
    ///    - "slider(min,max)" : Creates a slider with the specified min and max values.
    ///      Example: "slider(0,100)" for a slider from 0 to 100.
    /// 
    /// 2. For float type only:
    ///    - "floatstepslider(min,max,step)" : Creates a step slider with the specified min, max, and step values.
    ///      Example: "floatstepslider(0,10,0.5)" for a slider from 0 to 10 with 0.5 increments.
    /// 
    /// 3. For string type:
    ///    - "color" : Creates a color input field (case-insensitive).
    ///      Example: "color" or "COLOR" for a hex color input field.
    /// 
    /// 4. For enum types:
    ///    - "boolenum" : Treats the enum as a boolean checkbox (case-insensitive).
    ///      Example: "boolenum" or "BOOLENUM" for a boolean checkbox instead of a dropdown.
    /// 
    /// 5. For List and Array types:
    ///    - Lists and arrays are automatically converted to comma-separated strings without spaces or brackets.
    ///    - Example: A List<string> with values ["item1", "item2", "item3"] is stored as "item1,item2,item3"
    ///    - Supported element types: Enum, Color, bool, int, float, and string.
    ///    - Note: ItemArgs for LC binding won't apply to list types as they're converted to strings.
    /// 
    /// Note: If no args are provided or if they don't match the patterns above, 
    /// default input fields or dropdowns will be used based on the type.
    /// </remarks>
    public class ConfigItem<T>
    {
        public ConfigEntry<T> Entry = null!;
        public string Section = "";
        public string Name = "";
        public string Description = "";
        public T DefaultValue { get; private set; } = default!;
        public T InternalValue = default!;
        public string ItemArgs = string.Empty;
        public bool NeedsRestart = false;
        public ConfigItemAuthority Authority;
        public event Action<T> OnValueChanged = null!;

        // Flag to indicate if this is a collection type (List or Array)
        private bool _isCollection = false;
        // The element type of the collection
        private Type _elementType = null!;
        // For string representation of collection types
        public ConfigEntry<string> StringEntry = null!;
        private bool _isPesudoStringType => _isCollection || typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3);
        //public bool IsLockedToHost => PikminManager.instance != null && !PikminManager.instance.IsServer && ConfigItemAuthorityManager.GetAuthoritiesOfSection(Section).Contains(Authority);
        public object? baseConfigItem = null;
        public static Dictionary<Type, string> InstructionsMap = new Dictionary<Type, string>
        {
            {typeof(Vector2),  LethalMin.UseCommasInVectorConfigs.Value
            ? $"\n(Vector2), sperated by commas, no spaces (x,y)" : $" (Vector2), sperated by spaces, no commas (x y)"},
            {typeof(Vector3),  LethalMin.UseCommasInVectorConfigs.Value
            ? $"\n(Vector3), sperated by commas, no spaces (x,y,z)" : $" (Vector3), sperated by spaces, no commas (x y z)"},
            {typeof(Color), $"\n(Color), in hex format (RRGGBBAA)"},
        };
        public ConfigFile? OverrideConfigFile = null;

        public ConfigItem(string section, string key, T defaultValue, string description, bool needsRestart, ConfigItemAuthority authority, ConfigFile ovrcfg, string args = "")
        {
            OverrideConfigFile = ovrcfg;
            Initialize(section, key, defaultValue, description, needsRestart, authority, args);
        }
        public ConfigItem(string section, string key, T defaultValue, string description, bool needsRestart, ConfigItemAuthority authority, string args = "")
        {
            Initialize(section, key, defaultValue, description, needsRestart, authority, args);
            OverrideConfigFile = null;
        }
        private void Initialize(string section, string key, T defaultValue, string description, bool needsRestart, ConfigItemAuthority authority, string args)
        {
            Section = section;
            Name = key;
            DefaultValue = defaultValue;
            Description = description;
            ItemArgs = args;
            NeedsRestart = needsRestart;
            Authority = authority;

            Type type = typeof(T);
            string instructions = InstructionsMap.ContainsKey(type) ? InstructionsMap[type] : "";
            _isCollection = IsCollectionType(type, out _elementType);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                instructions = $"\n(List), separated by commas, no spaces in between (item1,item2,item3)";
            }
            if (type.IsArray)
            {
                instructions = $"\n(Array), separated by commas, no spaces in between (item1,item2,item3)";
            }

            Description += instructions;

            if (_isCollection && _elementType.IsEnum)
            {
                var enumValues = Enum.GetNames(_elementType);
                Description += $"\nPossible values: {string.Join(", ", enumValues)}";
            }

            BindEntry();
            try
            {
                if (LethalMin.IsDependencyLoaded("ainavt.lc.lethalconfig") && OverrideConfigFile == null)
                    BindLCEntry();
            }
            catch (Exception e)
            {
                LethalMin.Logger.LogError($"Error binding LC config item {Name}: {e}");
            }

            if (_isPesudoStringType)
            {
                // For collections, use the string entry's value
                InternalValue = ConvertFromString(StringEntry.Value);
                StringEntry.SettingChanged += OnPesudoStringSettingChanged;
            }
            else
            {
                InternalValue = Entry.Value;
                Entry.SettingChanged += OnSettingChanged;
            }

            //LethalMin.ConfigItems.Add(this);
        }

        private bool IsCollectionType(Type type, out Type elementType)
        {
            elementType = null!;

            // Check if type is a List
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = type.GetGenericArguments()[0];
                return IsSupportedElementType(elementType);
            }

            // Check if type is an Array
            if (type.IsArray)
            {
                elementType = type.GetElementType()!;
                return IsSupportedElementType(elementType);
            }

            return false;
        }

        public bool IsSupportedElementType(Type type)
        {
            return type == typeof(string) ||
                   type == typeof(int) ||
                   type == typeof(float) ||
                   type == typeof(bool) ||
                   type == typeof(Color) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type.IsEnum;
        }

        public void BindEntry()
        {
            ConfigFile configFile = OverrideConfigFile ?? LethalMin.Instance.Config;
            if (_isCollection)
            {
                // For collections, we store as string and convert internally
                string stringValue = ConvertToString(DefaultValue);
                StringEntry = configFile.Bind(Section, Name, stringValue, Description);
                // Create a dummy entry to satisfy the generic type requirement
                Entry = default!;
            }
            else if (typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3))
            {
                // For Vector types, we store as string and convert internally
                string stringValue = ConvertToString(DefaultValue);
                StringEntry = configFile.Bind(Section, Name, stringValue, Description);
                // Create a dummy entry to satisfy the generic type requirement
                Entry = default!;
            }
            else
            {
                // Regular binding for non-collection types
                Entry = configFile.Bind(Section, Name, DefaultValue, $"({Authority.ToString()}) " + Description);
            }
        }

        public string ConvertToString(T value)
        {
            if (value == null) return string.Empty;

            // Handle list or array types
            if (_isCollection)
            {
                IEnumerable? collection = value as IEnumerable;
                if (collection == null) return string.Empty;

                List<string> stringItems = new List<string>();
                foreach (var item in collection)
                {
                    stringItems.Add(ConvertItemToString(item));
                }

                return string.Join(",", stringItems);
            }

            // Handle Vector2 and Vector3 specially when not in collections
            if (value is Vector2 vec2)
            {
                return LethalMin.UseCommasInVectorConfigs.Value ? $"{vec2.x},{vec2.y}" : $"{vec2.x} {vec2.y}";
            }
            else if (value is Vector3 vec3)
            {
                return LethalMin.UseCommasInVectorConfigs.Value ? $"{vec3.x},{vec3.y},{vec3.z}" : $"{vec3.x} {vec3.y} {vec3.z}";
            }

            return value.ToString();
        }

        private string ConvertItemToString(object item)
        {
            if (item == null) return string.Empty;

            // Handle Color specially
            if (item is Color color)
            {
                return ColorUtility.ToHtmlStringRGBA(color);
            }
            // Handle Vector2 specially
            else if (item is Vector2 vec2)
            {
                return LethalMin.UseCommasInVectorConfigs.Value ? $"{vec2.x},{vec2.y}" : $"{vec2.x} {vec2.y}";
            }
            // Handle Vector3 specially
            else if (item is Vector3 vec3)
            {
                return LethalMin.UseCommasInVectorConfigs.Value ? $"{vec3.x},{vec3.y},{vec3.z}" : $"{vec3.x} {vec3.y} {vec3.z}";
            }

            return item.ToString();
        }

        private object ConvertStringToItem(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null!;

            // Convert based on the target type
            if (targetType == typeof(string))
            {
                return value;
            }
            else if (targetType == typeof(int))
            {
                if (int.TryParse(value, out int result))
                    return result;
                return 0;
            }
            else if (targetType == typeof(float))
            {
                if (float.TryParse(value, out float result))
                    return result;
                return 0f;
            }
            else if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out bool result))
                    return result;
                return false;
            }
            else if (targetType == typeof(Color))
            {
                if (ColorUtility.TryParseHtmlString("#" + value, out Color color))
                    return color;
                return Color.white;
            }
            else if (targetType == typeof(Vector2))
            {
                string[] components = value.Split(LethalMin.UseCommasInVectorConfigs.Value ? ',' : ' ');
                if (components.Length >= 2 &&
                    float.TryParse(components[0], out float x) &&
                    float.TryParse(components[1], out float y))
                {
                    return new Vector2(x, y);
                }
                return Vector2.zero;
            }
            else if (targetType == typeof(Vector3))
            {
                string[] components = value.Split(LethalMin.UseCommasInVectorConfigs.Value ? ',' : ' ');
                if (components.Length >= 3 &&
                    float.TryParse(components[0], out float x) &&
                    float.TryParse(components[1], out float y) &&
                    float.TryParse(components[2], out float z))
                {
                    return new Vector3(x, y, z);
                }
                return Vector3.zero;
            }
            else if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, value);
                }
                catch
                {
                    return Enum.GetValues(targetType).GetValue(0)!;
                }
            }

            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null!;
        }

        private T ConvertFromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Return empty collection for empty string
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    // Create empty list of appropriate type
                    return (T)Activator.CreateInstance(typeof(T))!; // Line 242 fix
                }
                else if (typeof(T).IsArray)
                {
                    // Create empty array of appropriate type
                    return (T)Activator.CreateInstance(typeof(T), new object[] { 0 })!; // Line 247 fix
                }

                return default!; // Handle nullable types correctly
            }

            if (typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3))
            {
                return (T)ConvertStringToItem(value, typeof(T));
            }

            if (!_isCollection) return default!;

            string[] items = value.Split(',');

            // Handle List<T>
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
            {
                // Create a list of the appropriate type
                var list = Activator.CreateInstance(typeof(T));
                var addMethod = typeof(T).GetMethod("Add");

                if (list != null && addMethod != null)
                {
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var convertedItem = ConvertStringToItem(item, _elementType);
                            addMethod.Invoke(list, new[] { convertedItem });
                        }
                    }

                    return (T)list;
                }
            }
            // Handle arrays
            else if (typeof(T).IsArray)
            {
                // Create an array of the appropriate type and size
                Array array = Array.CreateInstance(_elementType, items.Length);

                for (int i = 0; i < items.Length; i++)
                {
                    if (!string.IsNullOrEmpty(items[i]))
                    {
                        array.SetValue(ConvertStringToItem(items[i], _elementType), i);
                    }
                }

                return (T)(object)array;
            }

            return default!; // Line 290 fix
        }


        public void BindLCEntry()
        {
            BaseConfigItem? _baseConfigItem = baseConfigItem as BaseConfigItem;
            // For collection types, we always bind as a text input field
            if (_isPesudoStringType)
            {
                _baseConfigItem = new TextInputFieldConfigItem(StringEntry, NeedsRestart);
                LethalConfigManager.AddConfigItem(_baseConfigItem);
                baseConfigItem = _baseConfigItem;
                return;
            }

            // Regular binding for non-collection types
            if (typeof(T) == typeof(int) || typeof(T) == typeof(float))
            {
                if (ItemArgs.Contains("slider("))
                {
                    var args = ItemArgs.Split('(', ')')[1].Split(',');
                    float min = float.Parse(args[0]);
                    float max = float.Parse(args[1]);
                    if (typeof(T) == typeof(int))
                    {
                        _baseConfigItem = new IntSliderConfigItem(Entry as ConfigEntry<int>, new IntSliderOptions
                        {
                            Min = (int)min,
                            Max = (int)max,
                            RequiresRestart = NeedsRestart
                        });
                        LethalConfigManager.AddConfigItem(_baseConfigItem);
                    }
                    else
                    {
                        _baseConfigItem = new FloatSliderConfigItem(Entry as ConfigEntry<float>, new FloatSliderOptions
                        {
                            Min = min,
                            Max = max,
                            RequiresRestart = NeedsRestart
                        });
                        LethalConfigManager.AddConfigItem(_baseConfigItem);
                    }
                }
                else if (ItemArgs.Contains("floatstepslider(") && typeof(T) == typeof(float))
                {
                    var args = ItemArgs.Split('(', ')')[1].Split(',');
                    float min = float.Parse(args[0]);
                    float max = float.Parse(args[1]);
                    float step = float.Parse(args[2]);
                    _baseConfigItem = new FloatStepSliderConfigItem(Entry as ConfigEntry<float>, new FloatStepSliderOptions
                    {
                        Min = min,
                        Max = max,
                        Step = step,
                        RequiresRestart = NeedsRestart
                    });
                    LethalConfigManager.AddConfigItem(_baseConfigItem);
                }
                else
                {
                    if (typeof(T) == typeof(int))
                    {
                        _baseConfigItem = new IntInputFieldConfigItem(Entry as ConfigEntry<int>, NeedsRestart);
                        LethalConfigManager.AddConfigItem(_baseConfigItem);
                    }
                    else
                    {
                        _baseConfigItem = new FloatInputFieldConfigItem(Entry as ConfigEntry<float>, NeedsRestart);
                        LethalConfigManager.AddConfigItem(_baseConfigItem);
                    }
                }
            }
            else if (typeof(T) == typeof(string))
            {
                if (ItemArgs.ToLower().Contains("color"))
                {
                    _baseConfigItem = new HexColorInputFieldConfigItem(Entry as ConfigEntry<string>, NeedsRestart);
                    LethalConfigManager.AddConfigItem(_baseConfigItem);
                }
                else
                {
                    _baseConfigItem = new TextInputFieldConfigItem(Entry as ConfigEntry<string>, NeedsRestart);
                    LethalConfigManager.AddConfigItem(_baseConfigItem);
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                _baseConfigItem = new BoolCheckBoxConfigItem(Entry as ConfigEntry<bool>, NeedsRestart);
                LethalConfigManager.AddConfigItem(_baseConfigItem);
            }
            else if (typeof(T).IsEnum)
            {
                if (ItemArgs.ToLower().Contains("boolenum"))
                {
                    _baseConfigItem = new BoolCheckBoxConfigItem(Entry as ConfigEntry<bool>, NeedsRestart);
                    LethalConfigManager.AddConfigItem(_baseConfigItem);
                }
                else
                {
                    var configItemType = typeof(EnumDropDownConfigItem<>).MakeGenericType(typeof(T));
                    var configItem = Activator.CreateInstance(configItemType, Entry, NeedsRestart);

                    // Cast the configItem to BaseConfigItem
                    _baseConfigItem = configItem as BaseConfigItem;

                    if (_baseConfigItem != null)
                    {
                        LethalConfigManager.AddConfigItem(_baseConfigItem);
                    }
                    else
                    {
                        LethalMin.Logger.LogWarning($"Failed to create config item for enum type {typeof(T)}.");
                    }
                }
            }
            else
            {
                LethalMin.Logger.LogWarning($"{Name}: Unsupported type {typeof(T)} for LethalConfig binding.");
            }

            if (_baseConfigItem != null)
            {
                // _baseConfigItem.Options.CanModifyCallback = () =>
                // {
                //     if (IsLockedToHost)
                //     {
                //         return CanModifyResult.False("This config has been locked by the host.");
                //     }
                //     return CanModifyResult.True();
                // };

                baseConfigItem = _baseConfigItem;
            }
            else
            {
                LethalMin.Logger.LogWarning($"Failed to bind config item {Name} of type {typeof(T)} to LethalConfig. No baseConfigItem created.");
            }
        }

        private void OnSettingChanged(object sender, EventArgs e)
        {
            InternalValue = Entry.Value;
            OnValueChanged?.Invoke(InternalValue);

            // if (PikminManager.instance.IsServer && ConfigItemAuthorityManager.GetAuthoritiesOfSection(Section).Contains(Authority))
            // {
            //     ConfigItemAuthorityManager.Instance.SendSpecificConfigServerRpc(Section, Name);
            // }
        }

        private void OnPesudoStringSettingChanged(object sender, EventArgs e)
        {
            InternalValue = ConvertFromString(StringEntry.Value);
            OnValueChanged?.Invoke(InternalValue);

            // if (PikminManager.instance.IsServer && ConfigItemAuthorityManager.GetAuthoritiesOfSection(Section).Contains(Authority))
            // {
            //     ConfigItemAuthorityManager.Instance.SendSpecificConfigServerRpc(Section, Name);
            // }
        }



        public static implicit operator bool(ConfigItem<T> item)
        {
            if (typeof(T) != typeof(bool))
            {
                LethalMin.Logger.LogFatal($"Config {item?.Name} is not a bool, cannot implictly convert.");
            }
            if (typeof(T) == typeof(bool) && item != null && item.InternalValue != null)
            {
                return (bool)(object)item.InternalValue!;
            }
            LethalMin.Logger.LogFatal($"Unable to properly implictly convert config {item?.Name} to bool. Type: {typeof(T)}");
            return false;
        }

        public static implicit operator float(ConfigItem<T> item)
        {
            if (typeof(T) != typeof(float))
            {
                LethalMin.Logger.LogFatal($"Config {item?.Name} is not a float, cannot implictly convert.");
            }
            if (typeof(T) == typeof(float) && item != null && item.InternalValue != null)
            {
                return (float)(object)item.InternalValue!;
            }
            LethalMin.Logger.LogFatal($"Unable to properly implictly convert config {item?.Name} to float. Type: {typeof(T)}");
            return 0f;
        }

        public static implicit operator int(ConfigItem<T> item)
        {
            if (typeof(T) != typeof(int))
            {
                LethalMin.Logger.LogFatal($"Config {item?.Name} is not a int, cannot implictly convert.");
            }
            if (typeof(T) == typeof(int) && item != null && item.InternalValue != null)
            {
                return (int)(object)item.InternalValue!;
            }
            LethalMin.Logger.LogFatal($"Unable to properly implictly convert config {item?.Name} to int. Type: {typeof(T)}");
            return 0;
        }

        public static implicit operator string(ConfigItem<T> item)
        {
            if (typeof(T) != typeof(string))
            {
                LethalMin.Logger.LogFatal($"Config {item?.Name} is not a string, cannot implictly convert.");
            }
            if (typeof(T) == typeof(string) && item != null && item.InternalValue != null)
            {
                return (string)(object)item.InternalValue!;
            }
            LethalMin.Logger.LogFatal($"Unable to properly implictly convert config {item?.Name} to string. Type: {typeof(T)}");
            return string.Empty;
        }
    }
}