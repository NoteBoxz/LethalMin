# LethalMin

                       !!This was programmed with AI Asistance!!

A lethal company mod that adds function pikmin to the game! 
LetalLib is requried to install this mod.
Because of the previously mentioned AI assitance and the fact that I've kinda given up on keeping clean and readable code, the code will be super messy.
I apologize in advance for the poorly documented, unclean code.

## Building the Mod (Unfinished)

### Required Dependencies

The following dependencies are required:

- [LethalConfig.dll](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/)
- [LethalLib.dll](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/)
- [LethalMon.dll](https://thunderstore.io/c/lethal-company/p/Feiryn/LethalMon/)
- [MaxWasUnavailable.LethalModDataLib.dll](https://thunderstore.io/c/lethal-company/p/MaxWasUnavailable/LethalModDataLib/)

### Patching
Use [EvaisaDev's UnityNetcodePatcher](https://github.com/EvaisaDev/UnityNetcodePatcher) to patch the mod.

## Making Custom Pikmin Types

**Important: You need to have at least some basic knowledge of Unity.**

### Unity Set-up
1. Download the Lethal Company Unity Project Template from [here](https://github.com/XuuXiaolan/LethalCompanyUnityTemplate/tree/main) or [here](https://github.com/EvaisaDev/LethalCompanyUnityTemplate).
2. To fix the reference errors, follow the DLL instructions from [here](https://lethal.wiki/dev/apis/lethallib/custom-enemies/unity-project#setting-up-our-unity-project).
3. Download the LethalMin.dll file and import it into Unity.
4. IMPORTANT: With the LethalMin.dll selected in Unity, uncheck "Validate References" in the inspector to remove the reference errors.
5. Download and Import the ExamplePikminType.unitypackage into the Unity project.
6. In unity create a folder, Put everything that will be used for your Pikmin type into one folder.
7. Right click in the folder in unity.
8. Click: Create -> LethalMin -> PikminType, and Create -> LethalMin -> Pikmin Sound pack. To create new PikminType and SoundPack scriptable objects.
9. With the folder selected, go to AssetBundle and create a new one. Name it something like "MyPikminType.lethalmin".


### Type Set-up
1. Go into Blender or your preferred 3D modeling application and create and animate your Pikmin's 3D model.
2. Import the model and set it up in Unity.
3. Open the ExamplePikminMesh prefab and drag your model into it.
4. Resize and position it based on the reference in the prefab. When you're done, delete the size reference.
5. Copy the parameters from the ExampleAnimator and put them into the animator on the mesh. Or just use the ExampleAnimator for your Pikmin's animtor.

### References
Because this mod relies on Transform.Find so much, you need to input the paths to certain objects in the PikminType.
To do this more easily:
1. Download GetPath.cs.
2. Create a folder called 'Editor' in your Unity project.
3. Import GetPath.cs into the Editor folder.
4. You should now see a new menu item in the Tools section at the top of the Unity window.
5. Click on 'Get GameObject Paths'. With the prefab open, select the top object in the hierarchy and then click "Copy Paths To Clipboard".
6. You now should have every child path in your Pikmin Type.
7. Use a program like Notepad to sort out which object path goes where.
8. Every item in the PikminType scriptable object is explained by tooltips in the Unity editor.

### Requirements
Not everything in the PikminType is required. Here is a list of things that do not need to be filled out in the Pikmin type:
- Pikmin Icon
- Pikmin Glow
- Pikmin Glow Path
- Pikmin Scripts
- Sprout Mesh Prefab
- Sound Pack
- Pikmin Material (It's currently unused)
- Scientific Name
- Bestiary Segment

LethalMin uses it's own Spawning System that does not affect a moon's power level.
If SpawnsIndoors and Spawns outdoors is off then the Pikmin won't spawn in very often.
Pikmin can still spawn outdoors from lethal company's spawn system.
When this happens a pikmin chose a random type when spawning, this can be disabled for your type by unchecking "SpawnsNaturally".

### Building
1. After setting up your Pikmin type, go to the Unity Package Manager and install this with the git URL: "https://github.com/Unity-Technologies/AssetBundles-Browser.git"
2. Go to the AssetBundles window and look at the Configure tab to make sure everything is okay.
3. Then go to the Build tab and build the AssetBundle.
4. When built, take your asset bundle file and put it in the BepInEx plugins folder. Make sure it has the .lethalmin file extension.
5. Then run the game, and depending on your spawn variables, your Pikmin type should start spawning in the game!

## Making Custom Onions

**Important: You need to have at least some basic knowledge of Unity.**

Custom onions and custom pikmin types do not necessarily need to be in different .lethalmin bundles.

### Unity Set-up
1. Download the Lethal Company Unity Project Template from [here](https://github.com/XuuXiaolan/LethalCompanyUnityTemplate/tree/main) or [here](https://github.com/EvaisaDev/LethalCompanyUnityTemplate).
2. To fix the reference errors, follow the DLL instructions from [here](https://lethal.wiki/dev/apis/lethallib/custom-enemies/unity-project#setting-up-our-unity-project).
3. Download the LethalMin.dll file and import it into Unity.
4. IMPORTANT: With the LethalMin.dll selected in Unity, uncheck "Validate References" in the inspector to remove the reference errors.
5. In Unity create a new folder.
6. With the folder selected, go to AssetBundle and create a new one. Name it something like "MyOnion.lethalmin".
7. Open the folder and right click in it. Click {Create -> Lethalmin -> OnionType}

### Basic Onion Set-up
The Onion's Texture/Material do not need to be filled out.

1. Set the Onion's Color
2. Input the TypeName
3. Important, add the pikmin types you want to be held in the onion to TypeCanHold.
4. Check "SpawnInAsItem" Unless you want to program in a special way for your onion to be obtained.
5. Go to the PikminTypes you set in TypesCanHold and set their target onion to your custom onion.

### Onion Fuse Rules
1. Open the Onion's folder and right click in it. Click {Create -> Lethalmin -> OnionFuseRules}
2. Input the custom onions you want to fuse in to the CompatibleOnions array. 

### Building
1. After setting up your Onion type, go to the Unity Package Manager and install this with the git URL: "https://github.com/Unity-Technologies/AssetBundles-Browser.git"
2. Go to the AssetBundles window and look at the Configure tab to make sure everything is okay.
3. Then go to the Build tab and build the AssetBundle.
4. When built, take your asset bundle file and put it in the BepInEx plugins folder. Make sure it has the .lethalmin file extension.
5. Then run the game, and depending on your spawn variables, your Onion should start spawning in the game!
6. If you have multiple onions with their FuseRules set up, then when the ship leaves, the onions should fuse when they land again.
