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

## License
Educational / personal use

## ITCH.IO

[Play on Itch.io](https://gamedevteamx.itch.io/goap2davivnv)
