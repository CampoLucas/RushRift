# RushRift

## Table of Contents
- [Setup Instructions](#setup-instructions)
   - [Requirements](#requirements)
   - [How to Run](#how-to-run)
- [Development Guidelines](#development-guidelines)
   - [Folder Structure](#folder-structure)
   - [Coding Style](#coding-style)
- [Git Conventions](#git-conventions)
   - [Branch Naming](#branch-naming)
   - [Mergue Guidelines](#mergue-guidelines)
- [Documentation](#documentation)
   - [Scene Requirements for the Player to Work](#scene-requirements-for-the-player-to-work)

## Setup Instructions
### Requirements
- **Unity Version**: 2022.3.46f1
- **Platform**: Windows
- **Packages:** Unity's Input System | Cinemachine

### How to Run
1. Clone the repository
   ```bash
   git clone https://github.com/(your github username)/RushRift.git

2. Install Unity [2022.3.46f1](https://unity.com/releases/editor/whats-new/2022.3.46#installs)
3. Open the project in Unity
4. Open the scene (Path of the main menu scene)

## Development Guidelines

### Folder Structure
```bash
Assets/
├── _Main/
│   ├── Art/
│   │   ├── Animations/
│   │   ├── Fonts/
│   │   ├── Materials/
│   │   ├── Models/
│   │   ├── Shaders/
│   │   └── Textures/ (also sprites, icons, etc.)
│   ├── Data/ (Scriptable Objects)
│   ├── Prefabs/
│   ├── Scenes/
│   └── Scripts/
│       ├── _Managers/
│       ├── Entities/
│       ├── General/
│       ├── Tools/
│       └── (etc.)
└── (Any third-party assets)
```
### Coding Style
* ```PascalCase``` for classes, method names, protected variables and properties
* ```camelCase``` for public and serialized private variables
* ```_camelCase``` for private variables
* ```UPPER_SNAKE_CASE``` for constants
* For ```Interfaces``` add an uppercase **"I"** at the name (e.g. ```IMovement```) 

#### Script Organization Order
1. public fields<br/>
2. protected fields<br/>
3. private fields<br/>
4. Unity methods (Awake, Start, Update, etc.)<br/>
5. public methods<br/>
6. private methods<br/>

## Git Conventions
### Branch Naming
Type | Format | Example
--- | --- | ---
Feature | ```feature/feature-name``` | ```feature/player-dash```
Bugfix | ```bugfix/short-description``` | ```bugfix/enemies-are-not-moving```
Refactor | ```refactor/description``` | ```refactor/pathfinding-optimization```
Art | ```art/short-description``` | ```art/enemy-model-and-textures```

To keep the workflow clean and easy to collaborate, we follow a structurated branch naming convections. Here is why:<br/>
1. **We work as a team:** if everyone names branches however they want, it becomes chaotic and **difficult to mergue** without conflicts or confusion.
2. **Each Task should have a short and focused branch:** small, single-purpose branches are easier to track, test and merge.
3. **We are professionals:** consistent structure and good practices reflect the quality of our work and help us scale the project properly.

### Mergue Guidelines
#### ✅ Before Mergung Into ```develop```:
1. **Test your changes**
   Make sure that everithing you've worked on is functioning as expected. No broken features or console errors.
2. **Merge the latest ```develop``` into your branch**
   This makes certain that your branch is up to date and helps you resolve any merge conflicts **before** pushing to ```develop```.
3. **Resolve any conflicts**
   Don't push unresolved branches. Make sure the code you're merging plays nice with the rest of the team's work.

#### 🧹 After Merging:
* **Delete your branch** (locally and remotely)
   * This keeps keeps the repository clean and avoids confusion
* Ask if you are not sure whether to delete a branch, but, in most cases, once it's merged, it's safe to remove. 

#### 🚫 Never:
* Merge directly into ```develop``` without testing
* Leave conflicts unresolved
* Push untested or broken changes just to “get it in”

## Documentation
### Scene Requirements for the Player to Work
* A **GameObject** with the ```InputManager``` component in the scene.
* All **floors** must be on the ```Ground``` layer.
* All **walls** must be on the ```Wall``` layer.
* All **obstacles** must be on the ```Obstacle``` layer.


