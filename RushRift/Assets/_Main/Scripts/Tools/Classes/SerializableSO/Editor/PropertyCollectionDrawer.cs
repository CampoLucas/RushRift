using System;
using System.Collections.Generic;
using Game.Tools;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    [CustomPropertyDrawer(typeof(SerializableSOCollection<>))]
    public class PropertyCollectionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var collectionProperty = property.FindPropertyRelative("collection");
            if (collectionProperty == null)
                return EditorGUIUtility.singleLineHeight + 4;

            // Always update before measuring so nested changes (predicates added/removed) are visible
            property.serializedObject.Update();

            // Create a temporary list just to get its layout height
            var list = new ReorderableList(property.serializedObject, collectionProperty, true, false, true, true)
            {
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = collectionProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none, true);
                },
                elementHeightCallback = i =>
                {
                    var element = collectionProperty.GetArrayElementAtIndex(i);
                    return EditorGUI.GetPropertyHeight(element, true) + 2;
                }
            };
            
            var headerHeight = EditorGUIUtility.singleLineHeight + 2;
            var listHeight = list.GetHeight();

            // Add a small buffer to avoid cutoff at bottom
            return headerHeight + listHeight + 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!TryGetReferences(property, out var parent, out var propertyCollection, out var selfReference, out var errorMessage))
            {
                EditorGUI.HelpBox(position, errorMessage, MessageType.Error);
                return;
            }

            var list = GetList(property);
            if (list == null)
            {
                return;
            }
            
            // draw header manually
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, label);

            // shift down for list body
            var listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2,
                position.width, position.height - EditorGUIUtility.singleLineHeight - 2);

            if (property.serializedObject != null && property.serializedObject.targetObject != null)
            {
                try
                {
                    list.DoList(listRect);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ReorderableList failed for {property.propertyPath}: {e.Message}");
                }
            }
            
            
        }

        private ReorderableList GetList(SerializedProperty property)
        {
            if (property == null) return null;
            
            var collectionProperty = property.FindPropertyRelative("collection");
            var list = new ReorderableList(property.serializedObject, collectionProperty, true, false, true, true)
                {
                    drawElementCallback = (rect, index, active, focused) =>
                        DrawElementCallback(rect, index, active, focused, collectionProperty),
                    elementHeightCallback = i => ElementHeightCallback(i, collectionProperty),
                    onAddCallback = _ => OnAddCallback(property),
                    onRemoveCallback = l => OnRemoveCallback(l, property)
                };

            return list;
        }
        

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            
            if (!TryGetReferences(property, out var parent, out var propertyCollection, out var selfReference, out var errorMessage))
            {
                root.Add(new HelpBox(errorMessage, HelpBoxMessageType.Error));
                return root;
            }
            
            AddContainer(ref root, out var containerElement);
            AddButtons(ref containerElement, parent, propertyCollection, ref property);

            var list = GetList(property);
            containerElement.Add(new IMGUIContainer(() => CustomGUI(property.serializedObject, ref list)));
            
            return root;
        }
        
        private bool TryGetReferences(SerializedProperty property, out ScriptableObject parent, out ISerializableSOCollection serializableSoCollection, out object selfReference, out string errorMessage)
        {
            parent = null;
            serializableSoCollection = null;
            errorMessage = "";
            
            var parentReference = property.serializedObject.targetObject;
            
            selfReference = property.GetUnderlyingValue();
            if (parentReference is not ScriptableObject castedParent)
            {
                errorMessage = "The PropertyCollection class only works on scriptable objects.";
                return false;
            }

            if (selfReference is not ISerializableSOCollection castedCollection)
            {
                errorMessage = "The PropertyCollection doesn't have the interface IPropertyCollection.";
                return false;
            }

            parent = castedParent;
            serializableSoCollection = castedCollection;

            return true;
        }

        private void AddContainer(ref VisualElement parent, out VisualElement container)
        {
            container = new VisualElement();
            
            // Set it's style
            container.style.marginTop = 10;
            container.style.marginBottom = 10;
            container.style.paddingTop = 2;
            container.style.paddingBottom = 2;
            container.style.paddingRight = 2;
            container.style.paddingLeft = 2;
            
            // Set it's border
            var borderWidth = 1;
            var borderColor = Color.white;
            var borderRadius = 5f;
            
            container.style.borderRightWidth = borderWidth;
            container.style.borderBottomWidth = borderWidth;
            container.style.borderLeftWidth = borderWidth;
            container.style.borderTopWidth = borderWidth;
            container.style.borderBottomColor = borderColor;
            container.style.borderLeftColor = borderColor;
            container.style.borderRightColor = borderColor;
            container.style.borderTopColor = borderColor;
            container.style.borderBottomLeftRadius = borderRadius;
            container.style.borderBottomRightRadius = borderRadius;
            container.style.borderTopLeftRadius = borderRadius;
            container.style.borderTopRightRadius = borderRadius;
            
            parent.Add(container);
        }
        
        private void AddButtons(ref VisualElement parentElement, ScriptableObject parentRef, ISerializableSOCollection pCollection, ref SerializedProperty property)
        {
            var root = new VisualElement();
            root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            root.style.alignItems = new StyleEnum<Align>(Align.Center);
            
            var labelContainer = new VisualElement();
            labelContainer.style.flexGrow = 1;
            labelContainer.Add(new Label(property.name));
            root.Add(labelContainer);
            
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            root.Add(buttonContainer);
            
            parentElement.Add(root);
        }

        private void CustomGUI(SerializedObject serializedObject, ref ReorderableList list)
        {
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused, SerializedProperty property)
        {
            var element = property.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, GUIContent.none);
            
            // var objectReference = element.objectReferenceValue;
            // var name = objectReference.name;
            //
            // element.isExpanded = EditorGUI.Foldout(rect, element.isExpanded, name);
            //
            // if (element.isExpanded)
            // {
            //     
            // }
        }

        private float ElementHeightCallback(int index, SerializedProperty property)
        {
            var el = property.GetArrayElementAtIndex(index);
            if (el == null)
            {
                Debug.LogError("ERROR: el in PropertyCollectionDrawer is null");
                return 0;
            }

            return EditorGUI.GetPropertyHeight(el, true) + 2;
        }
        
        
        private void OnRemoveCallback(ReorderableList list, SerializedProperty property)
        {
            if (!TryGetReferences(property, out var parent, out var pCollection, out var selfReference,
                    out var errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }
            
            var idx = list.selectedIndices;
            
            if (idx == null || idx.Count == 0)
            {
                if (pCollection.Count > 0)
                {
                    var index = pCollection.Count - 1;

                    var removedObject = pCollection.GetAtIndex(index);
                    if (removedObject == null) return;
                    AssetDatabase.RemoveObjectFromAsset(removedObject);
                    AssetDatabase.SaveAssets();
                    
                    pCollection.RemoveAt(index);
                    
                }
                return;    
            }
            
            for (var i = 0; i < idx.Count; i++)
            {
                var index = idx[i];
                
                var removedObject = pCollection.GetAtIndex(index);
                if (removedObject == null) continue;
                AssetDatabase.RemoveObjectFromAsset(removedObject);
                AssetDatabase.SaveAssets();
                    
                pCollection.RemoveAt(index);
            }
        }

        private void OnAddCallback(SerializedProperty property)
        {
            if (!TryGetReferences(property, out var parent, out var pCollection, out var selfReference,
                    out var errorMessage))
            {
                Debug.LogError(errorMessage);
                return;
            }
            
            pCollection.OpenSearchWindow((a) => OnSelectedItem(property, a, parent));
        }

        private void DrawHeaderCallback(Rect rect)
        {
            
        }
        
        private void OnSelectedItem(in SerializedProperty property, in Object objectAdded, in ScriptableObject parentRef)
        {
            AssetDatabase.AddObjectToAsset(objectAdded, parentRef);
            AssetDatabase.SaveAssets();
            property.serializedObject.Update();
        }
    }
}