import re

LethalMincsPath = r"C:\Users\ervin\OneDrive\Documents\NotezMain\Scripting Projects\LethalMin AT2\LethalMin\LethalMin\LethalMin.cs"

class ConfigItem:
    def __init__(self, type:str, InternalName:str, defultVal:str, name: str, section:str, description: str, NeedsRestart : bool):
        self.type = type
        self.name = name
        self.InternalName = InternalName
        self.description = description
        self.defultVal = defultVal
        self.section = section
        self.NeedsRestart = NeedsRestart

KnownLCTypes = {
    "bool" : "BoolCheckBoxConfigItem",
    "int" : "IntInputFieldConfigItem",
    "float" : "FloatInputFieldConfigItem",
    "string" : "TextInputFieldConfigItem"}

ConfigItemsToAdd = [ConfigItem("bool", "UseFpsBoost", "true", "Use FPS Boost", "LethalMin", "Enable or disable FPS Boost","false"),]

ConfigUseString = r"(Generated Useable Varibles GoES HERE\n)"

ConfigUseVarsToAdd = []

ConfigVarString = r"(Generated Config Varibles GoES HERE\n)"

ConfigVarsToAdd = []

ConfigBindingString = r"(Generated ConfigBindings goes here\n)"

ConfigBindingsToAdd = []

ConfigSettingString = r"(Generated Settings Valuse Goes Here\n)"

ConfigSettingsToAdd = []

ConfigEventString = r"(Generated Settings Events Goes here\n)"

ConfigEventsToAdd = []

ConfigLCBindingString = r"(Generated LC bindings goes here\n)"

ConfigLCBindingsToAdd = []

def ConstructConfigItemCode(item: ConfigItem):
    ConfigUseVarsToAdd.append(f"public static {item.type} {item.InternalName};")
    ConfigVarsToAdd.append(f"public static ConfigEntry<{item.type}> {item.InternalName}Config;")
    ConfigBindingsToAdd.append(f'{item.InternalName}Config = Config.Bind("{item.section}", "{item.name}", {item.defultVal},"{item.description}");')
    ConfigSettingsToAdd.append(f'{item.InternalName} = {item.InternalName}Config.Value;')
    ConfigEventsToAdd.append(f'{item.InternalName}Config.SettingChanged += (_, _) => {item.InternalName} = {item.InternalName}Config.Value;')
    #Check if KnownLCTypes contains the item type
    if item.type in KnownLCTypes:
        ConfigLCBindingsToAdd.append(f'LethalConfigManager.AddConfigItem(new {KnownLCTypes[item.type]}({item.InternalName}Config,{item.NeedsRestart}));')
    else:
        print(f"Unknown type {item.type} for {item.name}. Resorting to enums")
        ConfigLCBindingsToAdd.append(f'LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<{item.type}>({item.InternalName}Config,{item.NeedsRestart}));')
        pass
    pass

def WriteToLethalMin(Entry: str, ContentB: str):
    new_content = ""

    with open(LethalMincsPath, 'r') as file:
        content = file.read()

    for item in ConfigUseVarsToAdd:
        new_content += re.sub(Entry, r"\1" +"\n" + ContentB, content, count=1)

    with open(LethalMincsPath, 'w') as file:
        file.write(new_content)


for item in ConfigItemsToAdd:
    ConstructConfigItemCode(item)
    print(f"constructed code for {item.name}")
    print(f"UseCode: {ConfigUseVarsToAdd[0]}")
    print(f"VarCode: {ConfigVarsToAdd[0]}")
    print(f"BindCode: {ConfigBindingsToAdd[0]}")
    print(f"SettingCode: {ConfigSettingsToAdd[0]}")
    print(f"EventCode: {ConfigEventsToAdd[0]}")
    print(f"LCBindCode: {ConfigLCBindingsToAdd[0]}")

for item in ConfigUseVarsToAdd:
    WriteToLethalMin(ConfigUseString, item)

for item in ConfigVarsToAdd:
    WriteToLethalMin(ConfigVarString, item)

for item in ConfigBindingsToAdd:
    WriteToLethalMin(ConfigBindingString, item)

for item in ConfigSettingsToAdd:
    WriteToLethalMin(ConfigSettingString, item)

for item in ConfigEventsToAdd:
    WriteToLethalMin(ConfigEventString, item)

for item in ConfigLCBindingsToAdd:
    WriteToLethalMin(ConfigLCBindingString, item)

print("Code has been added to LethalMin.cs successfully.")