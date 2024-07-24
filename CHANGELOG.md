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

### New Dependencies

* ClickThroughBlocker
* ToolbarController

### Notable Changes

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