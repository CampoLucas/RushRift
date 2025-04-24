# RushRift

## Setup Instructions

### Requirements
- **Unity Version**: 2022.3.46f1
- **Platform**: Windows

### How to Run
1. Clone the repository
   ```bash
   git clone https://github.com/CampoLucas/RushRift.git

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
│       ├── _Main/
│       ├── Entities/
│       ├── General/
│       ├── Tools/
│       └── (etc.)
└── (Any third-party assets)
```
### Coding Style
• ```PascalCase``` for classes, method names, protected variables and properties<br/>
• ```camelCase``` for public and serialized private variables<br/>
• ```_camelCase``` for private variables<br/>

#### Script Organization Order
1. public fields<br/>
2. protected fields<br/>
3. private fields<br/>
4. Unity methods (Awake, Start, Update, etc.)<br/>
5. public methods<br/>
6. private methods<br/>

## Commiting Convetions
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
