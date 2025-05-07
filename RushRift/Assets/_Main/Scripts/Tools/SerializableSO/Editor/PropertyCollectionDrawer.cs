using System;
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
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!TryGetReferences(property, out var parent, out var propertyCollection, out var selfReference, out var errorMessage))
            {
                EditorGUI.HelpBox(position, errorMessage, MessageType.Error);
                return;
            }
            
            var collectionProperty = property.FindPropertyRelative("collection");
            var list = new ReorderableList(property.serializedObject, collectionProperty, true, false, true, true)
            {
                // drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = (a, b, c, d) => DrawElementCallback(a, b, c, d, collectionProperty),
                // //drawFooterCallback = DrawFooterCallback,
                onAddCallback = (a) => OnAddCallback(a, propertyCollection, parent),
                onRemoveCallback = (a) => OnRemoveCallback(a, propertyCollection, parent),
            };
            CustomGUI(property.serializedObject, ref list);
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

            var collectionProperty = property.FindPropertyRelative("collection");
            var list = new ReorderableList(property.serializedObject, collectionProperty, true, false, true, true)
            {
                // drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = (a, b, c, d) => DrawElementCallback(a, b, c, d, collectionProperty),
                // //drawFooterCallback = DrawFooterCallback,
                onAddCallback = (a) => OnAddCallback(a, propertyCollection, parent),
                onRemoveCallback = (a) => OnRemoveCallback(a, propertyCollection, parent),
            };
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

            EditorGUI.PropertyField(rect, element);
            
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
        
        
        private void OnRemoveCallback(ReorderableList list, ISerializableSOCollection collection, ScriptableObject parentRef)
        {
            var indices = list.selectedIndices;
            
            if (indices == null || indices.Count == 0)
            {
                if (collection.Count > 0)
                {
                    var index = collection.Count - 1;

                    var removedObject = collection.GetAtIndex(index);
                    if (removedObject == null) return;
                    AssetDatabase.RemoveObjectFromAsset(removedObject);
                    AssetDatabase.SaveAssets();
                    
                    collection.RemoveAt(index);
                    
                }
                return;    
            }
            
            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];
                
                var removedObject = collection.GetAtIndex(index);
                if (removedObject == null) continue;
                AssetDatabase.RemoveObjectFromAsset(removedObject);
                AssetDatabase.SaveAssets();
                    
                collection.RemoveAt(index);
            }
        }

        private void OnAddCallback(ReorderableList list, ISerializableSOCollection collection, ScriptableObject parentRef)
        {
            collection.OpenSearchWindow((a) => OnSelectedItem(a, parentRef));
        }

        private void DrawHeaderCallback(Rect rect)
        {
            
        }
        
        private void OnSelectedItem(Object objectAdded, ScriptableObject parentRef)
        {
            AssetDatabase.AddObjectToAsset(objectAdded, parentRef);
            AssetDatabase.SaveAssets();
        }
    }
}