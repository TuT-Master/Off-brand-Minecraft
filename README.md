# Off-brand MineCraft (Minicraft)
![Unity](https://img.shields.io/badge/Unity-6-black)
![C#](https://img.shields.io/badge/C%23-.NET-blue)
![Status](https://img.shields.io/badge/Status-In%20Development-orange)

A little hobby project aimed at challenging my Unity and C# skills. Though it probably isn't going to be visual challenge it will be coding challenge for sure. I will try my best to use internet only for music while coding not for any type of help so I can truly challenge myself.

## Software

- Unity version 6000.5.8f1
- Visual Studio 2026

## Controls

- WSAD - movement
- Spacebar - jump
- Mouse - looking around
- LMB - destroying block
- RMB - placing selected block
- 1, 2, 3, 4 - block selection

## Scenes

- MainMenu - very very very basic main menu with buttons to start the game or to quit the application
- TestWorld - scene where I work the most. Almost all of my work is being done here.
- World - blank scene. I created it with idea of preparing stuff in TestWorld then moving it here but I think I will just rename the TestWorld when I'm done..

## Current Features

- First-person controller
- Mouse camera
- 4 block prefabs (grass, dirt, stone and snow)
- Simple UI with crosshair and toolbar
- Main menu with loading to Test Scene
- Block placement
- Block destruction
- World generation (custom random-based algorithm)
- Snow blocks on hills

## Planned Features

- Spawning player in center of generated world
- Dynamically re-render blocks when their surroundings change (update visibility when you destroy block or place new)
- Chunk saving and loading
- Block health points (destroying will take some time)

## Technical Challenges

### Generating chunks with random heights
Since I don't know how to use perlin noise for world generation I decided to create my own random-based algorithm for generating maps. The generation has two steps

First I create array of integers - I call it "heightmap" - and generate heights for each chunk. This is done by random height for first generated chunk then limited randomized step up or down in height. With this I then generate whole map (for example 9x9 chunks)

Second I smooth out terrain in chunk-to-chunk scope. Each chunk "looks" around itself and calculates slope based of height difference between its own height and neighbour chunk's height. This create three-dimensional array of virtual blocks in each chunk (in my set up it's 16x16x128 virtual blocks). Then for the each virtual block the algorithm decides whether it will be a stone or dirt or grass or snow or air based of virtual block's height and by how deep beneath the surface it is.

### Finding a cheap way to render all the blocks
Rendering all 16x16x128x9x9 (blocks in chunk * amount of chunk in my set up = 2 654 208 blocks) wouldn't be the brightest idea. I found a way to cheaply render at least this relatively small piece of terrain I'm working with right now. The main idea is _If the block is touching air it should be visible_. So far this works pretty well, when I have bigger world and do some digging I may will need to find a better way for it.

### Saving and loading chunks as player travelers through the world

## How It Looks

![Falling into the void...](Docs/Game_2.png)

![Sky block off-brand edition](Docs/Game_1.png)

![Behind the scene](Docs/Unity_1.png)

![Main Menu](Docs/MainMenu_1.png)
