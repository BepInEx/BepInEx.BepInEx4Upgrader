# BepInEx 4 Upgrader patcher

This preloader patcher allows to use BepInEx 4 plugins in BepInEx 5.  
The patcher works by automatically patching BepInEx 4 plugins before they are loaded by BepInEx.

## How to use

1. **BACK UP YOUR BEPINEX 4 INSTALLATION**
2. Remove BepInEx 4 and install BepInEx 5
3. Put the downloaded plugin DLL into `BepInEx/patchers`
4. Create a folder `BepInEx/plugins/bepinex4_plugins` and move ALL your BepInEx 4 plugins in there
5. Run the game normally

## What about non-plugins files (configurations, translations)?

Placmenet of additional files depends on how BepInEx 4 plugin was programmed.  

*As a rule of thumb, non-plugin files go into `BepInEx/plugins/bepinex4_plugins`.*  
That is, that folder works as BepInEx 4 folder (but without the `core`).

**In some case that doesn't work:** for that, you can try putting the files into `BepInEx` folder.
