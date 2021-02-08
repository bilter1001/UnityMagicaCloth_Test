//------------------------------------------------------------------------------
// Magica Cloth
// Copyright (c) Magica Soft, 2020
// https://magicasoft.jp
//------------------------------------------------------------------------------

### About
Magica Cloth is a high-speed cloth simulation operated by Unity Job System + Burst compiler.


### Support Unity versions
Unity2018.4.0(LTS) or higher


### Feature

* Fast cloth simulation with Unity Job System + Burst compiler
* Works on all platforms except WebGL
* Implement BoneCloth driven by Bone (Transform) and MeshCloth driven by mesh
* MeshCloth can also work with skinning mesh
* Easy setup with an intuitive interface
* Time operation such as slow is possible
* With full source code


### Documentation
Since it is an online manual, please refer to the following URL for details.
https://magicasoft.jp/en/magica-cloth-install-2/

First, import the package according to the [Installation Guide].
Then read the [System Overview] and proceed with the [Setup Guide] for a better understanding.


### Release Notes
[v1.8.0]
Note: The data format has changed. Since the new function cannot be used with the past data, it is necessary to press the [Create] button again to recreate the data.
Added: Added BoneCloth mesh connection method. This will automatically create a mesh from the bone connections. The movement of the skirt by the bone is improved.
Added: It is now possible to turn on/off the entire cloth simulation by operating the enable of MagicaPhysicsManager.
Improvement: Adjusted some preset parameters.
Improvement: Improved collision detection.
Improvement: The operation of Penetration has been improved.
Fix: Fixed the problem at the time of negative scale (flip) of Penetration.

[v1.7.6]
Added: Added support for character minus scale (flip). However, only one xyz axis can be reversed.
Added: Added negative scale sample scene.
Improvement: The [Once per Frame] update option is now left forever.
Improvement: Changed the minimum value of particle radius from 0.01 to 0.001.
Fix: Fixed the problem that the normal/tangent recalculation of MeshSpring was not done correctly. Affects lighting.
Fix: Fixed the problem that an error occurs when the execution is stopped in the editor.

[v1.7.5]
Added: Added maximum speed setting to [World Influence]. The simulation is stable when moving at high speed.
Added: Added a parameter [Stabilization Time After Reset] that improves the problem of particles bouncing on reset.
Improvement: Reduced particle vibration due to collision detection.
Improvement: The accuracy of collision detection has been slightly improved.
Fix: Fixed issue where global colliders would bounce away particles when moved/rotated at high speed.
Fix: Fixed the problem that a bone grows when moving at high speed.
Fix: It was fixed because there was a mistake in the interpolation processing of fixed particles.
Fix: Fixed that the bar did not turn yellow even if some parameters of spring setting were changed.
Fix: Fixed the issue that an error occurs in RenderDeformer when there is no tangent data in the mesh.
Fix: Fixed the problem that it doesn't work when used together with Cubism SDK (Live2D). (However, Unity 2019.3 and above)

[v1.7.4]
Added: Added [Radius][Drag][Gravity][Mass] parameter API.
Improvement: An error is displayed when the number of vertices of the mesh exceeds 65535. This is an existing limitation.
Fix: Fixed the problem that an error occurs in Unity2019.1-2019.2.13 due to scaling processing.

[v1.7.3]
Added: You can now add colliders to the cloth component at runtime.
Added: Added option to take over Avatar collider when connecting Avatar Parts to Avatar.
Added: You can now set the offset position on the Collider component.
Added: Added OnAttachParts and OnDetachParts events to Magica Avatar.
Improvement: Animation control of BlendWeight is now possible.
Improvement: [Adjust Rotation] of Spring component is always enabled and changed to the method to set the behavior according to the mode.
Improvement: Fixed the problem that gizmo of MeshSpring and Collider was displayed even in unnecessary situations.
Improvement: The control of PlayerLoop is exposed as an external function.
Fix: Fixed an issue where mesh vertices would occasionally collapse when swapping multiple cloth components.
Fix: Fixed the problem that a GUI error occurs when loading a preset in Unity 2019.4.
Fix: Fixed the problem that an error occurs when a mesh with vertex weights is put into MeshRenderer and MeshCloth is set.

[v1.7.2]
Added: Added support for scaling at runtime.
Improvement: The color of the parameter bar of the inspector will change to yellow when the data needs to be reconstructed due to the parameter change.
Fix: Fixed an issue that caused an error due to the initialization order of Cloth components.
Fix: Fixed an issue where the Influence Target would not switch when attaching AvatarParts.
Fix: Fixed an issue that sometimes caused an error when deleting a Prefab in Project.

[v1.7.1]
Added: Added access function to [World Influence] [Collider Collision] [Penetration] parameter to API.
Improvement: When data has not been created yet, instead of an error in the information, it now shows the state that there is no data.
FIX: Fixed the problem that an error occurs when the collection package 0.9.0 or more is included.
FIX: Fixed the error that occurs when the [ReadOnly] attribute is custom defined.

[v1.7.0]
Note: The data format has changed and the old data no longer starts. You need to press the [Create] button again to recreate the data.
Added: Added Surface Penetration function.
Added: Added Collider Penetration function.
Added: It can be used together with Entity Component System.In Unity2018.4 / Unity2019.2, you need to set the MAGICACLOTH_ECS define in your project.
Added: It is now possible to display vertex / particle axes (XYZ) from the cloth monitor.
Improvement: The collision detection has been improved.
Improvement: The project setting of Unsafe Code is no longer required.
Improvement: Depth display of cloth monitor now shows the current value.
Improvement: Fixed the problem that the simulation becomes unstable when the weight of the movable bone is already included in the mesh.
Fix: Fixed the spelling mistake of [Create] button of MeshCloth.
Fix: Fixed the problem which falls into an infinite loop at the time of data creation of Virtual Deformer.

[v1.6.1]
Improvement: Improved the friction processing algorithm. The problem of particles vibrating has been reduced.
Fix: Fixed a problem that vertex painting could not be performed properly when there are two or more inspector windows in the editor.
Fix: Fixed an issue that caused an error at the end of execution in the editor.

[v1.6.0]
Improvement: Improved the behavior of ClampPosition / ClampRotation. Collision detection has priority over movement restriction.
Improvement: Improved collision determination processing.
Improvement: Improved the rotation line generation algorithm.
Improvement: Collider Gizmo is basically hidden when not selected.
Improvement: Added a reset simulation button to the running inspector.
Improvement: Improved the friction processing algorithm.
Improvement: Enabled to specify the maximum number of connections in [Near Point] of Restore Distance.
Improvement: Changed the Virtual Deformer weight calculation method to the average weight value of the referenced skinning mesh vertices.
This greatly reduces the problem of unintended vertex deformation during animation.
Improvement: Fixed vertices in VirtualDeformer were set to be completely excluded from the calculation in some situations.
This greatly reduces the problem of unintended vertex deformation during animation.
Improvement: Renamed Adjust Line to Rotation Interpolation.
Improvement: Added FixedNonRotation flag to Rotation Interpolation.
If this flag is set to ON, fixed particles will not rotate at all.
Fix: Fixed an issue where Global Collider was not working properly.

[v1.5.1]
Added: Added API for accessing [Distance Disable] parameter.
Added: Added API for accessing [External Force] parameter.
Improvement: Connection control between components has been strengthened.
Fix: Fixed an issue where an error would occur if [Distance Disable] was turned On / Off during execution.
Fix: Fixed an issue that caused an error when creating a cloth component with LateUpdate during delayed execution.
Fix: Changed the delayed execution to be executed at PostLateUpdate instead of at the end of LateUpdate.
Fix: Fixed an issue where inactive render deformers were being calculated.
Fix: Fixed an issue where inactive render deformers were causing a memory leak.
Fix: Fixed [RenderMeshVertexUsed] [VirtualMeshVertexUsed] values on cloth monitor to be correct.

[v1.5.0]
Added: Added delayed execution mode.
Improvement: Improved performance.
Improvement: Displaying write time to mesh in profiler.
Improvement: Render deformer normal / tangent recalculation can now select normal only or normal + tangent.
Improvement: Scene view can be rotated by Alt + mouse drag while vertex painting.
Fix: Fixed incorrect scale calculation when writing to bones.
Fix: Fixed an issue where references to parent bones could be lost when referring to one bone multiple times.
Fix: Fixed an issue where teleport might not work properly.
Fix: Fixed an issue when selecting the wind component when the cloth monitor was hidden.
Fix: Fixed an issue where data was not written correctly when editing in prefab mode.
Fix: Modified to redraw the scene view when editing MeshSpring axes in the inspector.
Note: The update mode [Once Per Frame] will be deprecated in the future.

[v1.4.2]
Improvement: When creating a collider, it has been changed to adjust the collider scale from the parent scale.
Fix: Fixed a problem where mesh was broken when SkinnedMeshRenderer and MeshRenderer were mixed using Unity2018.
Fix: Fixed Capsule Collider gizmo not displaying correctly.
Fix: Fixed an issue where cloth simulation was not running on frames with cloth components attached.

[v1.4.0]
Added: Added dress-up system (Avatar, AvatarParts).
Added: Teleport is turned off by default.
Improvement: Reduced vibration caused by movement.
Improvement: When creating a cloth component object, it is set to inherit the parent name.
Fix: Fixed issue where MeshOptimizeMissmatch error would occur when loading from asset bundle.
Fix: Fixed an issue where the scene view was not redrawn when painting vertices.
Fix: Fixed an issue where writing transforms was not correct when adding / removing cloth components repeatedly.
Fix: Fixed collider to correctly reflect transform scale.
Fix: Fixed an issue where an error would occur if the main camera did not exist.
Fix: Fixed an issue where data was not created when attaching a RenderDeformer with multiple renderers selected.

[v1.3.0]
Added: Added wind function (Wind).
Added: Added wind sample scene (WindSample).
Improvement: Changed cloth team preprocessing from C # to JobSystem.

[v1.2.0]
Added: Added blending function with original posture (Blend Weight).
Added: Added the function to disable simulation by distance (Distance Disable).
Added: Added a sample scene for distance disable function (DistanceDisableSample).
Improvement: Added scrollbar to cloth monitor.
Improvement: Data can be created even if the mesh has no UV value.
Improvement: Enhanced error handling.
Fix: Fixed slow playback bug. Time.timeScale works correctly.
Fix: Fixed an issue where an error occurred when duplicating a prefab with [Ctrl+D].
Fix: Fixed an issue where trying to create data without vertex painting would result in an error.

[v1.1.0]
Added: Added support for Unity2018.4 (LTS).
Improvement: Error details are now displayed along with error codes.
Improvement: Vertex paint now records by vertex hash instead of vertex index.
Fix: If two or more MagicaPhysicsManagers are found, delete those found later.

[v1.0.3]
Fix: Fixed the problem that reference to data is lost while editing in Unity2019.3.0.

[v1.0.2]
Fix: Fixed an issue where an error occurred when running in the Mac editor environment.

[v1.0.1]
Fix: Fixed an error when writing a prefab in Unity2019.3.

[v1.0.0]
Note: first release.




