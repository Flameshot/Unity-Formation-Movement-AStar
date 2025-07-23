# Unity Formation Movement A*
<img src="https://i.imgur.com/z9PuDt8.png" width="273.5" height="426"> <img src="https://i.imgur.com/9q6IXCW.png" width="273.5" height="426"> <img src="https://i.imgur.com/oRLp5Oo.png" width="273.5" height="426">
<img src="https://i.imgur.com/yibjVl6.png" width="273.5" height="426"> <img src="https://i.imgur.com/flm5bkJ.png" width="273.5" height="426"> <img src="https://i.imgur.com/kxgHsoc.png" width="273.5" height="426">
<img src="https://i.imgur.com/ZDVHBs9.png" width="273.5" height="426"> <img src="https://i.imgur.com/QDMKVA7.png" width="273.5" height="426"> <img src="https://i.imgur.com/V8BvpHb.png" width="273.5" height="426">

## Description
A formation system for Unity that positions and moves units in dynamic layouts (ex. Wedge, Circle etc) formations. Suitable for RTS, TD and other strategy game genres.</br>
Supports **historical** and **live** positioning modes. 
[A* Pathfinding Project Free](https://arongranberg.com/astar/docs/index.html) version was used to generate graph and move units.
The system can be also implemented in your own project without the A* package. Read more on **Notes for non A*** **Users** section.

## Features
<details><summary>Dynamic Formation Changes</summary></br><img src="https://media3.giphy.com/media/v1.Y2lkPTc5MGI3NjExY21zYWN0Y3VpOWh4Y3dtemJ6eHJoNnQ4ZXUyYXRob2V3bmUzdGYxZCZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/mGVfcFFHRBfNaUQEo0/giphy.gif" width="800" height="450"></details>
<details><summary>Visual Leader (cosmetic)</summary></br><img src="https://media0.giphy.com/media/v1.Y2lkPTc5MGI3NjExNTJ0dW5kM3RsZjY5OHp2aGZ4djZmZTV0bXJtZDhlNHZwdHluMmttaCZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/ItYpCf3WYm4fogkCJ4/giphy.gif" width="800" height="450"></details>
<details><summary>Adaptive positioning fallback</summary></br><img src="https://media0.giphy.com/media/v1.Y2lkPTc5MGI3NjExaWV1eTh1NGZ1eGs5Mmd6YXVkeGVieW0wNmNnNnR2Z2YxbzZoMWk0MCZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/8WjpcEHUtURA6kVxnj/giphy.gif" width="800" height="450"></details>
<details><summary>Simulation Types Support</summary>
</br>
   
* Historical
* Live
  
</details>

<details><summary>ScriptableObjects Formation Layouts</summary>
</br>
   
* Arc
* Circle
* Triangle
* Wedge
* Matrix
* Horizontal Line
* Shield Wall
* Vertical Line
  
</details>

Note: *Click on above items to expand*.

## Quick Start Guide
0. Use Unity 6000.0 or later, although 2022.3 LTS or lower should work.
1. Download and import [A* Pathfinding Project](https://arongranberg.com/astar/download) package.
2. Download and import [Formation Movement](https://github.com/Flameshot/Unity-Formation-Movement-AStar/raw/refs/heads/main/Assets/FormationMovementPackage.unitypackage) package.
3. In a new scene, drag and drop in **Hierarchy** from **Prefabs** folder **GridPath**, **Ground**, **Leader** and **Destination** prefabs.
4. Configure **FormationLeader** GameObject
    * In the **Formation** component, assign desired **FormationTypeSO** (e.g WedgeFormation) in **FormationToSpawn** field. All formations can be found in **ScriptableObjects** folder.
    * In the **AIDestinationSetter** component, assign **Destination** GameObject in **Target** field.
5. Press play and see your formation in action!
</br></br>Note: *After Step 1 and 2, you could open FormationExample scene for a ready-to-use setup and check FormationExample GameObject for some examples*.

## Notes for A* Pro Users
This implementation supports all A* Pro package features, like [Local Avoidance](https://arongranberg.com/astar/docs/localavoidance.html) and [Movement Scripts](https://arongranberg.com/astar/docs/movementscripts.html). 
Generate your desired Graph type and replace the **AIPath** component from both **FormationLeader** and **Followers** prefabs with desired movement script (ex. RichAI, FollowerEntity etc.) component.

## Notes for non A* Users
To use this system without A* manual integration is required. This requires that a movement system for units is already implemented.
1. For formation types use **FormationTypeSO** class along with all the inherited classes and replace **FormationFollower** reference with your movement script.
2. **Formation** script controls unit spawning, placement, switching and related functionality.
3. **FormationLeader** generates grid position (FormationGridPoint) for formation.
4. **FormationGridPoint** holds and updates each unit grid position.
5. Read method definitions for a better understanding.

## Technical Overview
The system creates formations using virtual **Grid Points** positioned relative to a hidden **Formation Leader**. **Followers** move toward their assigned **Grid Point** to maintain formation.
* **Formation**: Holds data related to formation as **Grid Points**, **FormationFollower** and handles formation changes.
* **Formation Leader**: The brain unit, always placed ahead of formation, that navigates and records movement history. It is invisible. 
* **Visual Leader**: Any unit that appears to "lead" the formation (optional, purely cosmetic).
* **Followers**: Units that track their assigned **Grid Point** position to maintain formation.
* **Grid Points**: Virtual targets defining where each **Follower** should move. Constantly updated by the **Formation Leader**.
* **FormationTypeSO**: ScriptableObject system used to define different formation layouts in a modular way.

**Positioning Modes**: **Historical** and **Live**
* **Historical** - **Grid Points** use sampled historical positions of the **Formation Leader**. This results in smooth and natural **Follower** movement.
* **Live** - **Followers** track real-time offsets relative to the **Formation Leader**. Suitable for simple, tight formations (e.g. Wedge, Circle, Arc).
* Switching betwen modes can be made from formation scriptable object (e.g. **WedgeFormation**) found in ScriptableObjects folder.

## Known issues
* For **Historical** simulation type, **Adaptive Positioning Fallback** doesn't always return the best path direction for some formation layouts.

## Contribution
This is far from perfect and issues will be encountered. Feel free to:
* Open issues for bug reports or suggestions.
* Submit Pull Requests with improvements or fixes.
* All contributions are welcome, thank you!