# Changelog

| modName | TUFX                                 |
| ------- | ------------------------------------ |
| license | GPL-3                                |
| author  | shadowmage45, JonnyOThan             |

### Tools used for maintaining this file:

* https://pypi.org/project/yaclog-ksp/
* https://yaclog.readthedocs.io
* https://keepachangelog.com

### Known Issues

* Restock launch clamps do not render properly in the editor
* ***IMPORTANT***:  If you experience blurring or smearing, try turning off temporal antialiasing in scatterer's settings.  If they persist, also disable temporal antialiasing in your TUFX profile as well as motion blur.  Once you've eliminated the issue, turn settings back on one by one until you find something you like.

## Unreleased

- Fixed a lot of issues in IVA
- Can now type decimals into TAA fields

### Changes

* Actually hooked up Default-Internal profile as the default for IVA mode
* Switched Default-Internal to use SVO for Ambient Occlusion
* Merged changes from @LGhassen to fix artifacts with MSVO Ambient Occlusion with Deferred

## 1.1.0 - 2024-07-24

### New Dependencies

* ClickThroughBlocker
* ToolbarController

### Changes

* Remove patch that disables scatterer's temporal antialiasing
* Better default profile settings (mostly removing the neutral tonemapper which tended to result in a desaturated image)
* Added toolbarcontrol integration (you can use this to hide the toolbar button)
* Added clickthroughblocker support and other code to prevent mouse interactions from affecting other things when interacting with the config window
* Fixed icons appearing blurry when game is not at full texture resolution
* TUFX window is now hidden when you press F2 (this is useful in photo mode - press escape then F2)
* Disable stock antialiasing when HDR and bloom are enabled to prevent strobing artifacts
* Configuration window now loads and saves directly to cfg files
* Redesigned profile editor UI
* Postprocessing effects now only apply to the final camera so that they don't get doubled up when in IVA or other situations.
* Added a secondary camera antialiasing setting.  The old setting will be applied only to the main camera (local space in flight).  The secondary antialiasing method will be applied to other cameras (scaled space, internal camera, editor cameras).  This prevents smearing when using temporal antialiasing because the cameras do not share motion information.
* Fixed a bug where the bloom diffusion parameter would not get loaded from the profile
* Fixed a NullReferenceException when saving a texture param with nothing selected
* Added properties to customize antialiasing settings
* Added support for changing the mainmenu profile in-game
* Added a separate default profile for IVA mode
* Properly set HDR on the GalaxyCamera
* Add support for [KerbalChangelog](https://forum.kerbalspaceprogram.com/topic/200702-19%E2%80%93112-kerbal-changelog-v142-adopted/)


## 1.0.7.1 - 2023-08-13

### Changes

* Fixes atmospheres flickering when at high altitude
* Fix depth of field applying to scaled space camera
* Fix flashing kerbals in the VAB/SPH


## 1.0.7.0 - 2024-08-10

### Changes

* Fixed a bug when zooming while in IVA
* The editor scene is now supported
* Default profiles can be customized with MM patches (thanks @al2me6 )


## 1.0.6.0 - 2023-05-01

### New Dependencies

* Shabby

### Changes

* Enabling HDR no longer turns some parts transparent
* Fixed ambient occlusion stripe issue when the camera is close to a surface
* The default-flight profile now includes ambient occlusion
* Now includes a MM patch that disables scatterer's temporal antialiasing because it interferes with HDR

### New Known Issues

* The restock launch clamp tower section is invisible in the VAB*


## 1.0.5.0 - 2022-08-30

### Changes

* Added a configuration option to hide the toolbar button (see TUFX/TexturesUnlimitedFX.cfg)
* Updated default profiles:
  * All default profiles now use HDR, and the HDR-specific profiles have been removed
  * Disabled AO on most profiles since it causes banding
  * Enabled TAA on most profiles
  * Increased bloom intensity slightly
* Map mode now defaults to using the tracking station profile instead of flight
* Updated unity postprocessing stack to 3.0.3 to support [VR](https://github.com/JonnyOThan/Kerbal-VR/wiki)
* Fall back to default profiles if the old selected one is missing


## 1.0.4.0 - 2022-07-26

### Changes

* added fov-dependent depth of field (thanks @jrodrigv)
* hooked up chromatic aberration and color grading LUTs (thanks @jrodrigv)
* fixed a bug where the map mode profile was not being applied to the camera properly (thanks @Vandest1)


## 1.0.3.0 - 2022-06-09

### Changes

* Fix AA setting getting lost on scene switches (thanks @diamond-optic )
* Fix Gamma setting not loading from profile cfg (thanks @hairintd )
* Fix texture names (dirt, etc) not loading from profile cfg
* Fix TUFX button not closing GUI window
* Add support for IVA mode
* Add AO Mode selection
* Lots of in-progress work from shadowmage45 (unfortunately I don't really know what state this stuff is in)
* Update bundled ModuleManager version to 4.2.1


## 1.0.2.3 - 2020-03-08

### Changes

* Add support for AA mode selection in UI and profiles.  This is Post Processing based AA, and can be used in addition to or even when hardware MSAA is not available.
* Update default profiles to be less aggressive, reduce AO, disable chromatic aberration, disable HDR, disable color grading (only works well in HDR).
* Add the old versions of the defaults as new Default-HDR-XXX profiles; will continue to tweak these in the future.


## 1.0.1.2 - 2020-03-07

### Changes

* Fix release version compilation flags to fix the Unity #define values


## 1.0.0.1 - 2020-03-07

### Changes

* Initial Release
* KSP 1.9.x only
* DX11 only (OpenGL untested)
* See issues list for known issues.
