import tkinter as tk
from tkinter import messagebox
from tkinter.ttk import Combobox
import re
import os

LethalMincsPath = os.getcwd() + r"\LethalMin\LethalMin.cs"

def LowerCaseBoolean(val: bool):
    if val == True:
        return "true"
    else:
        return "false"

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



ConfigItemsToAdd = []

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
        ConfigLCBindingsToAdd.append(f'LethalConfigManager.AddConfigItem(new {KnownLCTypes[item.type]}({item.InternalName}Config,{LowerCaseBoolean(item.NeedsRestart)}));')
    else:
        print(f"Unknown type {item.type} for {item.name}. Resorting to enums")
        ConfigLCBindingsToAdd.append(f'LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<{item.type}>({item.InternalName}Config,{LowerCaseBoolean(item.NeedsRestart)}));')
        pass
    pass

def WriteToLethalMin(Entry: str, ContentB: str):
    with open(LethalMincsPath, 'r') as file:
        content = file.read()

    new_content = re.sub(Entry, r"\1" + "\n" + ContentB, content, count=1)

    with open(LethalMincsPath, 'w') as file:
        file.write(new_content)

def InjectCodeToLethalMin():
    print("Generating...")
    
    # Clear previous entries
    ConfigUseVarsToAdd.clear()
    ConfigVarsToAdd.clear()
    ConfigBindingsToAdd.clear()
    ConfigSettingsToAdd.clear()
    ConfigEventsToAdd.clear()
    ConfigLCBindingsToAdd.clear()

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


def update_config_list():
    """Update the Listbox with the current configs."""
    config_list.delete(0, tk.END)  # Clear the listbox
    for idx, config in enumerate(ConfigItemsToAdd):
        config_list.insert(idx, f"{config.name} ({config.type})")

def add_config_item():
    def submit_config():
        type_ = type_var.get()
        internal_name = internal_name_entry.get()
        default_val = default_val_entry.get()
        name = name_entry.get()
        section = section_entry.get()
        description = description_entry.get("1.0", tk.END).strip()  # Get all text from the Text widget
        needs_restart = needs_restart_var.get()

        if not all([type_, internal_name, default_val, name, section, description]):
            messagebox.showerror("Error", "All fields are required!")
            return

        new_config = ConfigItem(
            type=type_,
            InternalName=internal_name,
            defultVal=default_val,
            name=name,
            section=section,
            description=description,
            NeedsRestart=needs_restart
        )
        ConfigItemsToAdd.append(new_config)
        update_config_list()
        messagebox.showinfo("Success", f"Config '{name}' added!")
    
    config_window = tk.Toplevel(root)
    config_window.title("Add Config Item")
    config_window.geometry("450x350")  # Increased window size

    tk.Label(config_window, text="Type:").grid(row=0, column=0, padx=5, pady=5, sticky="e")
    type_var = tk.StringVar()
    type_combo = Combobox(config_window, textvariable=type_var, values=list(KnownLCTypes.keys()), width=30)
    type_combo.grid(row=0, column=1, padx=5, pady=5, sticky="w")

    tk.Label(config_window, text="Internal Name:").grid(row=1, column=0, padx=5, pady=5, sticky="e")
    internal_name_entry = tk.Entry(config_window, width=32)
    internal_name_entry.grid(row=1, column=1, padx=5, pady=5, sticky="w")

    tk.Label(config_window, text="Default Value:").grid(row=2, column=0, padx=5, pady=5, sticky="e")
    default_val_entry = tk.Entry(config_window, width=32)
    default_val_entry.grid(row=2, column=1, padx=5, pady=5, sticky="w")

    tk.Label(config_window, text="Display Name:").grid(row=3, column=0, padx=5, pady=5, sticky="e")
    name_entry = tk.Entry(config_window, width=40)  # Increased width
    name_entry.grid(row=3, column=1, padx=5, pady=5, sticky="w")

    tk.Label(config_window, text="Section:").grid(row=4, column=0, padx=5, pady=5, sticky="e")
    section_entry = tk.Entry(config_window, width=32)
    section_entry.grid(row=4, column=1, padx=5, pady=5, sticky="w")

    tk.Label(config_window, text="Description:").grid(row=5, column=0, padx=5, pady=5, sticky="ne")
    description_entry = tk.Text(config_window, width=40, height=5)  # Changed to Text widget for multiline input
    description_entry.grid(row=5, column=1, padx=5, pady=5, sticky="w")

    tk.Label(config_window, text="Needs Restart:").grid(row=6, column=0, padx=5, pady=5, sticky="e")
    needs_restart_var = tk.BooleanVar()
    needs_restart_check = tk.Checkbutton(config_window, variable=needs_restart_var)
    needs_restart_check.grid(row=6, column=1, padx=5, pady=5, sticky="w")

    tk.Button(config_window, text="Add Config", command=submit_config).grid(row=7, column=0, columnspan=2, pady=10)

 

# Main Application Window
root = tk.Tk()
root.title("Config Manager")

# Add GUI elements
frame = tk.Frame(root)
frame.pack(pady=10)

add_button = tk.Button(frame, text="Add Config Item", command=add_config_item)
add_button.grid(row=0, column=0, padx=5)

generate_button = tk.Button(frame, text="Generate Config Code", command=lambda: InjectCodeToLethalMin())
generate_button.grid(row=0, column=1, padx=5)

# Listbox for displaying configs
config_list = tk.Listbox(root, width=50, height=10)
config_list.pack(pady=10)

root.mainloop()
