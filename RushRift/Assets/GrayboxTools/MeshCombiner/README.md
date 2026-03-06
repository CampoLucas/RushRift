# Overview
This tool combines multiple `MeshFilters` into a single mesh and has different settings to handle materials and repositioning to a custom pivot.

# Basic Workflow
1. Assign the target `MeshFilter` and `MeshRenderer`.
2. Select the meshes you want to combine.
3. Configure the pivot (optional).
4. Choose how submeshes are handled.
5. Configure the output asset name and folder.
6. Click Combine.
7. Click Save to create the mesh .asset file.

> [!Warning]
If you combine but not save, the mesh will be lost after the scene or prefab is changed.

# Explanations
### Meshes To Combine
The list of `MeshFilters` that will be merged.

| Options                  | Description                                                                                                                                                                                                                                            |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **From Selection**       | Selects all the meshes that are selected, this button has a dropdown that can be configured to:<br>	• Exclude the target `MeshFilter`.<br>	• To look for the children `MeshFilter` from the selected `GameObject`.<br>	• To exclude inactive children. |
| **Clear (trash icon)**   | Removes the assigned `MeshFilters` from the list.                                                                                                                                                                                                      |
| **Disable Meshes After** | Disables all the `MeshFilters` `GameObjects`.                                                                                                                                                                                                          |

### Mesh Target
This is the destination `GameObject` that will receive the combined mesh. It needs the reference of the `MeshFilter` and `MeshRenderer` components to assign the combined mesh and resulting materials.

| Options                | Description                                                                                                                                                                                                |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **From Selection**     | Assigns `MeshFilter` and `MeshRenderer` from the active `GameObject`.                                                                                                                                      |
| **Add And Select**     | It adds the `MeshFilter` and `MeshRenderer` components to a `GameObject` and selects it. **If it doesn't have those component it props a pop up, so that you don't accidentally add it to any component.** |
| **Clear (trash icon)** | Removes the assigned target references.                                                                                                                                                                    |
> [!NOTE]
> To combine the meshes is necessary to assign the target `MeshFilter` and `MeshRenderer`, and the `MeshFilters` to combine.

### Pivot Options
Controls where the new pivot is and how it is applied.

| Option                      | Description                                                                                                                                                                                                                                                |
| --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Selection**               | Assigns the current selected `Transform` as pivot (disabled if **Same as target** is enabled).                                                                                                                                                             |
| **Local / Global toggle**   | • In local, the offset and rotation are applied in local space.<br>• In global, the offset and rotation are applied in world space.                                                                                                                        |
| **Focus (camera icon)**     | Moves `ScrenView` to look at the pivot.                                                                                                                                                                                                                    |
| **Show Pivot (gizmo icon)** | Toggles pivot gizmo visibility in `SceneView`. It has options to change the gizmo's scale.                                                                                                                                                                 |
| **New Mesh Pivot**          | `Transform` reference used as pivot.                                                                                                                                                                                                                       |
| **Same As Target**          | Uses the target's `Transform` as pivot reference.                                                                                                                                                                                                          |
| **Offset**                  | `Vector3` offset added to pivot position.                                                                                                                                                                                                                  |
| **Rotation**                | Euler rotation offset added to pivot rotation.                                                                                                                                                                                                             |
| **Apply Pivot**             | It is how it handles the pivot when combining meshes. It has two options<br>	• **Move To Pivot**, moves the target transform to the computed pivot while preserving children world positions.<br>	• **Keep In Place**, does not move the target transform. |
| **Clear (trash icon)**      | Resets the pivot options.                                                                                                                                                                                                                                  |
>[!WARNING]
>Using the **Move To Pivot** option changes the position and rotation of the selected target.

### Submesh Options
Defines how submeshes and materials are handled.

| Options                 | Description                                                                       |
| ----------------------- | --------------------------------------------------------------------------------- |
| **Merge All Submeshes** | All geometry is merged into one submesh and the first valid material is assigned. |
| **Keep All Submeshes**  | Keeps submeshes per original material slot.                                       |
| **Merge By Material**   | Groups geometry by material reference. Each unique material becomes one submesh.  |

### Output
How the file is saved

| Options          | Description                                                                                    |
| ---------------- | ---------------------------------------------------------------------------------------------- |
| **Asset Name**       | Name of the mesh asset. Has a button for setting the name as the target's `GameObject`'s name. |
| **Use Prefix**   | When enabled, adds a prefix to the asset name.                                                 |
| **Asset Prefix** | Text added before Asset Name if prefix is enabled.                                             |
| **Folder**       | Displays the current folder path.                                                              |
