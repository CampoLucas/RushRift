# AudioSourcePool
**Type:** `class` | Implements: [`IPool`](../../5_Design%20Patterns/Pool/IPool.md), `IDisposable`  | **Implemented in:** `Game.Entities`

### Description
A pool for `AudioSource` Components, with support for runtime creation and editor-assigned sources.

### Public Methods
| Method    | Description                                                                                   |
| --------- | --------------------------------------------------------------------------------------------- |
| `Get`     | Retrieves an available `AudioSource` from the pool, or creates one if none are available.     |
| `Recycle` | Returns an `AudioSource` back to the pool, stopping playback and clearing the clip.           |
| `Remove`  | Completely removes an AudioSource from the pool and destroys it if it was created at runtime. |
| `Dispose` | Disposes of all runtime-created AudioSources and clears the pool.                             |

[← Previous Page](Sound.md)