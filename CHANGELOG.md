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

### Notable Changes

* New dependencies: ClickThroughBlocker, ToolbarController
* Remove patch that disables scatterer's temporal antialiasing
* Better default profile settings (mostly removing the neutral tonemapper which tended to result in a desaturated image)
* Added clickthroughblocker support (this is now a dependency)
* Fixed icons appearing blurry when game is not at full texture resolution
* Added a "close window" button to the config window so that you can click it when in photo mode.  To use TUFX in photo mode:
    * Open the TUFX window
    * Press escape
    * Press F2
    * You can now move the camera around and change TUFX settings
    * When you have the settings you like, close the TUFX window and press F1 to take a screenshot
* Disable stock antialiasing when HDR and bloom are enabled to prevent strobing artifacts
* Configuration window now loads and saves directly to cfg files
* Redesigned profile editor UI
* Postprocessing effects now only apply to the final camera so that they don't get doubled up when in IVA or other situations.
* Added a secondary camera antialiasing setting.  The old setting will be applied only to the main camera (local space in flight).  The secondary antialiasing method will be applied to other cameras (galaxycam, scaled space, internal camera).  This prevents smearing when using temporal antialiasing because the cameras do not share motion information.
