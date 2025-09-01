# Front Page
### Welcome
This documentation provides an organized reference for the main systems in the game. It includes architecture overviews, class references and guides regarding on the use of each system.

Use this page as your starting point to navigate quickly to the most important parts of the codebase.

### Core Systems Overview
#### Entity System
- [Overview of Entity MVC + Components](</Documentation/2_Core Systems/Entity & Component System/Overview.md>)
- [Entity Controller](</Documentation/2_Core Systems/Entity & Component System/Controller/EntityController.md>)
- [Entity Model](</Documentation/2_Core Systems/Entity & Component System/Model/EntityModel.md>)
- [Entity View](</Documentation/2_Core Systems/Entity & Component System/View/EntityView.md>)
- Entity Components

#### Managers
- [Audio Manager](../4_Managers/Audio%20Manager/AudioManager.md)
- [Input Manager](../4_Managers/Input%20Manager/Overview.md)
- [Level Manager](../4_Managers/Level%20Manager/Overview.md)
- [UI Manager](../4_Managers/UI%20Manager/Overview.md)

#### Design Patterns
- [Observers](../5_Design%20Patterns/Observers/Overview.md)
- [Pool](../5_Design%20Patterns/Pool/IPool.md)
- Factory
- Subject
- Builder

#### Tools
- Behaviour Tree

### Scene Requirements for the Player to Work
- A **GameObject** with the ```InputManager``` component in the scene.
- All **floors** must be on the ```Ground``` layer.
- All **walls** must be on the ```Wall``` layer.
- All **obstacles** must be on the ```Obstacle``` layer.
