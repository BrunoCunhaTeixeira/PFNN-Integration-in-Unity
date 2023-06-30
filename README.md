# PFNN-Integration-in-Unity
As part of my master's thesis, I integrated the Phase-Functioned Neural Network (PFNN) for Character Control by Holden et al. into Unity. This implementation is based on the one by Sebastian Starke and extends it by the calculation of the joint angles, the integration of a mesh and new controll options.

I would like to thank Daniel Holden for his interesting work and the publication of his paper and articles. Also thanks to Sebastian Starke, whose project I used as a basis for my thesis.


You can find the original PFNN Paper by Holden here: 

https://theorangeduck.com/page/phase-functioned-neural-networks-character-control

You can find the implementation of the PFNN in Unity by Starke here:

https://github.com/sebastianstarke/AI4Animation

## Demo
<img src="https://github.com/Nachbarino/PFNN-Integration-in-Unity/blob/main/img/PAndS.png" alt="drawing" width="400"/>

Imgur-Video 1: https://imgur.com/a/HpZIF8j

Imgur-Video 2: https://imgur.com/a/lOVyobC

## Installation

1. Navigate to the Scenes folder and open a scene.

2. Navigate to the inspector of the "PFNN" Script which is attached to the character Gameobject.

3. Click on the Button "Store Parameters" and wait 10 - 20 sec.

4. Now you can start the scene!

# Controlls
As already mentioned, there are two other control options available besides the keyboard. The first includes control via a gamepad and the second via waypoints. Only one control option can be active at a time, which can be selected in the "Bio Animation_Original" script in the Unity editor.
## Gamepad
| Action  | Controll |
| ------------- | ------------- |
| Walk  | Left Stick |
| Run  | Left Stick + RT  |
| Crouch | Left Stick + A  |
| Turn left  | LB  |
| Turn right  | RB |

## Waypoints
Using waypoint control, the character runs through the waypoints in the given order. At each waypoint you can set the speed and gait of the character as well as the break time. To use the waypoints, follow these instructions:

1. Enable waypoint control by checking "Waypoints active?" on the "Bio Animation_Original" script in the Unity editor.

2. Make sure that a "WaypointManager" prefab is active in the Unity scene. Drag the active character into the WaypointManager. 

3. New waypoints can be created by cloning (CTRL+D) the "Waypoint" child elements of the "WaypointManager" prefab and repositioning them in the Unity scene.
   
# Copyright Information
This project is only for research or education purposes, and not freely available for commercial use or redistribution. The motion capture data is available only under the terms of the [Attribution-NonCommercial 4.0 International](https://creativecommons.org/licenses/by-nc/4.0/legalcode) (CC BY-NC 4.0) license.

Since the project is based on other published projects, the license is oriented to them. Please note that the human models used in the project may use different licensing.

Model "Steve": https://sketchfab.com/3d-models/rigged-human-character-free-296f9f80c4ac431aa3d354f7ef955605
