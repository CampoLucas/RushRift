# Sound
**Type:** `class` | Implements: `IDisposable` | **Implemented in:** `Game`
### Description
A class containing the configuration of a sound, including clips, volume, pitch, looping and mixer settings.
### Public Properties
| Property      | Description                                                                     |
| ------------- | ------------------------------------------------------------------------------- |
| `Pitch`       | Returns either a fixed pitch or a randomized pitch based on the pitch range.    |
| `Volume`      | Returns either a fixed volume or a randomized volume based on the volume range. |
| `Name`        | Name used to reference this sound.                                              |
| `PlayOnAwake` | If the sound is played when the `AudioManager` is Initialized.                  |
### Serialized Variables
| Variable           | Description                                                           |
| ------------------ | --------------------------------------------------------------------- |
| `name`             | The name of the sound, used to play the sound with the `AudioManager` |
| `loop`             | If the audio loops.                                                   |
| `playOnAwake`      | If the sound is played when the `AudioManager` is Initialized.        |
| `clips`            | Array of clips to play randomly.                                      |
| `mixer`            | The mixer group the audio clip belongs to.                            |
| `randomVolume`     | If the clip should have a random volume.                              |
| `volume`           | The clip volume in the case the `randomVolume` is false.              |
| `volumeRange`      | The range of the volume in case `randomVolume` is true.               |
| `pitch`            | If the clip should have a random pitch.                               |
| `randomPitch`      | The clip pitch in the case the `randomPitch` is false.                |
| `pitchRange`       | The range of the pitch in case `randomPitch` is true.                 |
| `timeBetweenPlays` |                                                                       |
### Public Methods
| Method       | Description                                                            |
| ------------ | ---------------------------------------------------------------------- |
| `Initialize` | Applies this sound's settings to the provided `AudioSource`.           |
| `Play`       | Initializes and plays the sound clip using the provided `AudioSource`. |
[← Previous Page](AudioManager.md) | [Next Page →](AudioSourcePool.md)
