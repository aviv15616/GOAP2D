# GOAP2D – Goal-Oriented Action Planning Simulation (Unity 2D)

## Overview
GOAP2D is a Unity 2D project that implements a Goal-Oriented Action Planning (GOAP) system for NPCs in a small survival-style world.
NPCs dynamically plan and execute actions based on their needs, world state, and available resources.

## Features
- Multiple NPC agents with different primary needs (Sleep, Hunger, Warmth)
- GOAP planner with world-state snapshots, action costs, and dynamic replanning
- Resource gathering (wood)
- Station usage (Bed, Pot, Fire)
- Station building with validation
- Grid-based pathfinding used for movement and cost estimation
- Automated strict GOAP scenario tester
- Saboteur NPC that interferes with other agents indirectly

## Project Structure
Assets/
Scripts/
GOAP/
Core/
Actions/
Systems/
Navigation/
Utilities/

## How It Works
1. Agents generate a world-state snapshot
2. Planner searches for the lowest-cost action sequence
3. Actions are executed sequentially
4. World changes trigger replanning

## NPC Roles
- SleepGuy – prioritizes sleep
- EaterGuy – prioritizes hunger
- WarmthGuy – prioritizes warmth

All NPCs can gather resources, build stations, and use any station.

## Saboteur
In addition to standard GOAP-driven NPCs, the simulation includes a **Saboteur** agent.

The saboteur is implemented as a dedicated behavior controller rather than a full GOAP goal or action. It continuously observes other NPCs and detects when an agent is actively moving toward a station for usage. Using the same movement, travel-time estimation, and station-selection APIs exposed by the GOAP system, the saboteur evaluates whether it can reach the target station before the intended user.

If it determines that it can arrive first by a sufficient margin, the saboteur moves to the station and destroys it, forcing the affected NPC to replan (e.g., gather resources and rebuild the station).

Although the saboteur is not driven by its own GOAP plan, it heavily reuses the GOAP agent infrastructure for navigation, timing estimation, and world interaction. Conceptually, it functions as a **hybrid agent** — not fully GOAP-driven, but tightly integrated with the GOAP framework.

## Requirements
- Unity 2021 LTS or newer
- 2D project

## How to Run
1. Open the project in Unity
2. Load the main scene
3. Press Play
4. Add Stations (Bed/Pot/Fire) by pressing (1/2/3) and left click.
5. Remove Stations with right click.
6. Observe planning and execution.

## Authors
- Aviv Neeman
- Gal Maymon

## ITCH.IO
[Play on Itch.io](https://gamedevteamx.itch.io/goap2davivnv)
