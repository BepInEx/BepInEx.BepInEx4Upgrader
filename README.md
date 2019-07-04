# BepInEx 4 Upgrader patcher

This preloader patcher allows to use BepInEx 4 plugins in BepInEx 5.  
The patcher works by automatically patching BepInEx 4 plugins before they are loaded by BepInEx.

## How to use

1. **BACK UP YOUR BEPINEX 4 INSTALLATION**
2. Remove BepInEx 4 and install BepInEx 5
3. Put the downloaded plugin DLL into `BepInEx/patchers`
4. Install your BepInEx 4 plugins just like you would in BepInEx 5 (DLLs into `BepInEx` folder, etc).
5. Run the game normally

## Configuration

In case you don't like the original behaviour of BepInEx 4 (plugin DLLs are in BepInEx folder), you can edit `BepInEx/config/bepinex4loader.cfg` and set a custom folder for BepInEx 4 plugins.

**NOTE:** Some plugins might have been hardcoded to assume they are always in BepInEx root folder. As such, moving DLLs to a different folder might cause them to not be loaded.
