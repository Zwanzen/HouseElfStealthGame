using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class PathToolUIBuilder
{
    // Colors for styling
    private readonly Color _insertBeforeColor = new(0.35f, 0.55f, 0.9f);
    private readonly Color _insertAfterColor = new(0.1f, 0.5f, 0.3f);
    private readonly Color _headerBgColor = new(0.22f, 0.22f, 0.25f);
    private readonly Color _sectionBgColor = new(0.18f, 0.18f, 0.21f);
    private readonly Color _sectionCColor = new(0.14f, 0.14f, 0.16f);
    private readonly Color _buttonHoverColor = new(0.25f, 0.25f, 0.28f);
    private readonly Color _primaryActionColor = new(0.35f, 0.55f, 0.9f);
    private readonly Color _secondaryActionColor = new(0.4f, 0.4f, 0.45f);
    private readonly Color _dangerColor = new(0.85f, 0.25f, 0.25f);

    // Events
    public event Action<NpcPath> OnPathSelected;
    public event Action OnAddWaypoint;
    public event Action<int> OnRemoveWaypoint;
    public event Action OnFlipPath;
    public event Action<int> OnSelectWaypoint;
    public event Action OnSaveChanges;
    public event Action<bool> OnAddWaypointRelative;
    public event Action OnGoBack;
    public event Action<NpcPath> OnDeletePath;

    // Build the initial UI for path selection or creation
    public void BuildInitialUI(VisualElement root)
    {
        root.Clear();
        ApplyBaseStyles(root);

        // FULL CONTAINER
        var allContainer = new Box();
        var allColor = _headerBgColor * 0.5f;
        allColor.a = 1f;
        allContainer.style.backgroundColor = allColor;
        
        // Create header
        var header = CreateHeader("Path Creation Tool", "Create or select a path to edit");

        // START OF SELECT PATH SECTION
        var selectSection = new VisualElement();

        var npcPathField = new ObjectField();
        npcPathField.objectType = typeof(NpcPath);
        npcPathField.label = "Path Asset";
        npcPathField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == null) return;

            // Get the asset path
            var assetPath = AssetDatabase.GetAssetPath(evt.newValue);

            // Unload the asset and force reload it completely
            Resources.UnloadAsset(evt.newValue);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // Load a fresh copy of the asset
            var selectedPath = AssetDatabase.LoadAssetAtPath<NpcPath>(assetPath);
            OnPathSelected?.Invoke(selectedPath);
        });
        selectSection.Add(npcPathField);

// Add Recent Paths list
        var recentPathsLabel = new Label("Recent Paths:");
        recentPathsLabel.style.marginTop = 10;
        recentPathsLabel.style.marginBottom = 5;
        selectSection.Add(recentPathsLabel);

        var recentPathsContainer = new ScrollView();
        recentPathsContainer.style.height = 150;
        recentPathsContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.17f);
        recentPathsContainer.style.borderTopLeftRadius = 3;
        recentPathsContainer.style.borderTopRightRadius = 3;
        recentPathsContainer.style.borderBottomLeftRadius = 3;
        recentPathsContainer.style.borderBottomRightRadius = 3;

// Find all path assets and sort by most recently modified
        var pathsFolder = "Assets/Scripts/PathSystem/Paths";
        if (Directory.Exists(pathsFolder))
        {
            var guids = AssetDatabase.FindAssets("t:NpcPath", new[] { pathsFolder });

            // Create list sorted by last modified time
            var pathItems = new List<(string path, DateTime time)>();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var lastModified = File.GetLastWriteTime(assetPath);
                pathItems.Add((assetPath, lastModified));
            }

            // Sort by most recent first
            pathItems.Sort((a, b) => b.time.CompareTo(a.time));

            foreach (var item in pathItems)
            {
                var path = AssetDatabase.LoadAssetAtPath<NpcPath>(item.path);
                var fileName = Path.GetFileNameWithoutExtension(item.path);

                // Create container for path item and delete button
                var pathItemRow = new VisualElement();
                pathItemRow.style.flexDirection = FlexDirection.Row;
                pathItemRow.style.marginBottom = 2;

                // Path select button
                var pathButton = new Button(() => { OnPathSelected?.Invoke(path); });
                pathButton.text = fileName;
                pathButton.style.flexGrow = 1;
                pathButton.style.alignSelf = Align.Stretch;
                pathButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                pathItemRow.Add(pathButton);

                // Delete button
                var deleteButton = new Button(() =>
                {
                    if (EditorUtility.DisplayDialog("Delete Path",
                            $"Are you sure you want to delete '{fileName}'? This cannot be undone.",
                            "Delete", "Cancel"))
                    {
                        var assetPath = AssetDatabase.GetAssetPath(path);
                        AssetDatabase.DeleteAsset(assetPath);
                        pathItemRow.RemoveFromHierarchy(); // Remove from UI
                        AssetDatabase.Refresh();
                    }
                });
                deleteButton.text = "×";
                deleteButton.tooltip = "Delete Path";
                deleteButton.style.width = 24;
                deleteButton.style.backgroundColor = _dangerColor;
                deleteButton.style.marginLeft = 2;

                pathItemRow.Add(deleteButton);
                recentPathsContainer.Add(pathItemRow);
            }
        }
        else
        {
            var noPathsLabel = new Label("No paths found");
            noPathsLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            noPathsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noPathsLabel.style.paddingTop = 10;
            recentPathsContainer.Add(noPathsLabel);
        }

        selectSection.Add(recentPathsContainer);
        selectSection = CreateSection(selectSection, "Select Existing Path");
        // END OF SELECT PATH SECTION
        
        // START OF CREATE PATH SECTION
        var createSection = new VisualElement();


        var nameField = new TextField("Name");
        nameField.value = "New Path";
        nameField.style.marginBottom = 10;
        createSection.Add(nameField);

        // Add save location display
        var assetSavePath = "Assets/Scripts/PathSystem/Paths";
        var pathInfoLabel = new Label($"Save location: {assetSavePath}");
        pathInfoLabel.style.fontSize = 10;
        pathInfoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        pathInfoLabel.style.marginBottom = 10;
        createSection.Add(pathInfoLabel);

        var createPathButton = new Button(() =>
        {
            var path = assetSavePath; // Use the variable for consistency
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (string.IsNullOrEmpty(nameField.value))
                nameField.value = "New Path";

            var npcPath = ScriptableObject.CreateInstance<NpcPath>();
            npcPath.Waypoints = new Waypoint[0];

            var pathToSave = AssetDatabase.GenerateUniqueAssetPath(path + "/" + nameField.value + ".asset");
            AssetDatabase.CreateAsset(npcPath, pathToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            OnPathSelected?.Invoke(npcPath);
        });
        createPathButton.text = "Create Path";
        ApplyButtonStyle(createPathButton, _primaryActionColor, true);
        createSection.Add(createPathButton);
        
        createSection = CreateSection(createSection, "Create New Path");
        // END OF CREATE PATH SECTION



        // ALL CONTAINER
        allContainer.Add(header);
        allContainer.Add(selectSection);
        allContainer.Add(createSection);
        root.Add(allContainer);
        
        // Footer with info
        var footer = new VisualElement();
        footer.style.marginTop = 4;
        footer.style.borderTopWidth = 1;
        footer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
        footer.style.paddingTop = 8;

        var hint = new Label("Tip: You can select paths directly from the Project window");
        hint.style.fontSize = 10;
        hint.style.color = new Color(0.7f, 0.7f, 0.7f);
        footer.Add(hint);
        root.Add(footer);
    }

    // Build the UI for the selected path
    public void BuildPathDetailsUI(VisualElement root, NpcPath selectedPath, int selectedWaypointIndex)
    {
        root.Clear();
        ApplyBaseStyles(root);

        // Ensure the selectedPath is valid
        if (selectedPath == null)
            return;

        // FULL CONTAINER
        var allContainer = new Box();
        var allColor = _headerBgColor * 0.5f;
        allColor.a = 1f;
        allContainer.style.backgroundColor = allColor;
        
        // Create header
        var header = CreateHeader($"Editing: {selectedPath.name}", "Configure waypoints and path properties");

        // START OF TOP ACTIONS
        var topActions = new VisualElement();
        topActions.style.flexDirection = FlexDirection.Row;
        topActions.style.justifyContent = Justify.Center; // Center horizontally
        topActions.style.alignItems = Align.Center; // Center vertically

        // Back button
        var backButton = new Button(() =>
        {
            OnGoBack?.Invoke();
            BuildInitialUI(root);
        });
        backButton.text = "← Back";
        ApplyButtonStyle(backButton, _secondaryActionColor);
        backButton.style.width = 160; // Make wider
        backButton.style.height = 36; // Make taller
        backButton.style.marginRight = 20; // Add space between buttons
        topActions.Add(backButton);

        // Save button
        var saveButton = new Button();
        saveButton.text = "💾 Save Changes";
        ApplyButtonStyle(saveButton, _primaryActionColor, true);
        saveButton.style.width = 160; // Make wider
        saveButton.style.height = 36; // Make taller
        topActions.Add(saveButton);

        // Add the click handler for save button
        saveButton.clicked += () =>
        {
            // Existing save button logic
            OnSaveChanges?.Invoke();
    
            var originalText = saveButton.text;
            var originalColor = _primaryActionColor;
    
            saveButton.text = "✓ Saved!";
            ApplyButtonStyle(saveButton, new Color(0.2f, 0.7f, 0.3f), true);
    
            var resetTime = EditorApplication.timeSinceStartup + 2.0;
    
            EditorApplication.CallbackFunction resetCallback = null;
            resetCallback = () =>
            {
                if (EditorApplication.timeSinceStartup >= resetTime)
                {
                    if (saveButton != null)
                    {
                        saveButton.text = originalText;
                        ApplyButtonStyle(saveButton, originalColor, true);
                    }
                    EditorApplication.update -= resetCallback;
                }
            };
    
            EditorApplication.update += resetCallback;
        };
        topActions = CreateSection(topActions);
        // END OF TOP ACTIONS

        
        // Ensure the waypoints array is initialized
        if (selectedPath.Waypoints == null)
        {
            selectedPath.Waypoints = new Waypoint[0];
            EditorUtility.SetDirty(selectedPath);
        }

        // START OF PATH PROPERTIES SECTION
        var pathProperties = new VisualElement();

        // Add Path Name field
        var pathNameField = new TextField("Path Name");
        pathNameField.isDelayed = true; // This makes it only trigger on Enter or focus loss
        pathNameField.value = selectedPath.name;
        pathNameField.RegisterValueChangedCallback(evt =>
        {
            if (string.IsNullOrWhiteSpace(evt.newValue)) return;
            if (evt.newValue == selectedPath.name) return;

            // Get current asset path
            var currentAssetPath = AssetDatabase.GetAssetPath(selectedPath);
            if (string.IsNullOrEmpty(currentAssetPath)) return;

            // Generate new path with the new name
            var directory = Path.GetDirectoryName(currentAssetPath);
            var newAssetPath = $"{directory}/{evt.newValue}.asset";

            // Check if target path already exists
            if (File.Exists(newAssetPath) && newAssetPath != currentAssetPath)
            {
                EditorUtility.DisplayDialog("Rename Failed",
                    $"Cannot rename path to '{evt.newValue}' because an asset with that name already exists.", "OK");
                pathNameField.SetValueWithoutNotify(selectedPath.name);
                return;
            }

            // Rename the asset
            AssetDatabase.RenameAsset(currentAssetPath, evt.newValue);
            EditorUtility.SetDirty(selectedPath);

            // Update the header title to reflect the new name
            var headerTitle = root.Q<Label>(className: "unity-label");
            if (headerTitle != null && headerTitle.parent.ClassListContains("header"))
                headerTitle.text = $"Editing: {evt.newValue}";
        });
        pathNameField.style.marginBottom = 10;
        pathProperties.Add(pathNameField);

        // Is Loop toggle
        var isLoopToggle = new Toggle("Is Loop Path");
        isLoopToggle.value = selectedPath.IsLoop;
        isLoopToggle.tooltip = "When enabled, connects the last waypoint back to the first";
        isLoopToggle.style.marginBottom = 10;
        isLoopToggle.RegisterValueChangedCallback(evt =>
        {
            selectedPath.IsLoop = evt.newValue;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });
        pathProperties.Add(isLoopToggle);

        // Flip Direction button
        var flipDirectionButton = new Button(() => OnFlipPath?.Invoke());
        flipDirectionButton.text = "↑↓ Flip Path Direction";
        flipDirectionButton.tooltip = "Reverses the order of waypoints (start becomes end)";
        ApplyButtonStyle(flipDirectionButton, _secondaryActionColor);
        pathProperties.Add(flipDirectionButton);
        
        // Add delete path button
        var deletePathButton = new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Delete Path",
                    $"Are you sure you want to delete '{selectedPath.name}'? This cannot be undone.",
                    "Delete", "Cancel"))
            {
                var assetPath = AssetDatabase.GetAssetPath(selectedPath);
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
                OnGoBack?.Invoke(); // Return to main screen
            }
        });
        deletePathButton.text = "🗑️ Delete Path";
        ApplyButtonStyle(deletePathButton, _dangerColor);
        pathProperties.Add(deletePathButton);
        
        // END OF PROPERTIES SECTION
        pathProperties = CreateSection(pathProperties, "Path Properties");

        // START OF NAV SECTION
        var navSection = new VisualElement();
        navSection.style.flexDirection = FlexDirection.Row;
        navSection.style.justifyContent = Justify.Center; // Center horizontally
        navSection.style.alignItems = Align.Center; // Center vertically

        var prevButton = new Button(() =>
        {
            if (selectedPath.Waypoints != null && selectedPath.Waypoints.Length > 0)
            {
                var newIndex = selectedWaypointIndex <= 0
                    ? selectedPath.Waypoints.Length - 1
                    : selectedWaypointIndex - 1;
                OnSelectWaypoint?.Invoke(newIndex);
            }
        });
        prevButton.text = "◄ Previous";
        ApplyButtonStyle(prevButton, _secondaryActionColor);
        prevButton.style.marginRight = 20; // Add space between buttons
        prevButton.style.width = 200; // Make wider
        prevButton.style.height = 30; // Make taller
        navSection.Add(prevButton);

        var nextButton = new Button(() =>
        {
            if (selectedPath.Waypoints != null && selectedPath.Waypoints.Length > 0)
            {
                var newIndex = selectedWaypointIndex >= selectedPath.Waypoints.Length - 1
                    ? 0
                    : selectedWaypointIndex + 1;
                OnSelectWaypoint?.Invoke(newIndex);
            }
        });
        nextButton.text = "Next ►";
        ApplyButtonStyle(nextButton, _secondaryActionColor);
        nextButton.style.width = 200; // Make wider
        nextButton.style.height = 30; // Make taller
        navSection.Add(nextButton);
        
        navSection = CreateSection(navSection, $"Navigation");
        // END OF NAV SECTION
        
        // START OF MANAGE POINTS SECTION
        var managePoints = new VisualElement();
        managePoints.style.flexDirection = FlexDirection.Column;

        // Add waypoint buttons
        if (selectedWaypointIndex >= 0 && selectedWaypointIndex < selectedPath.Waypoints.Length)
        {
            // Add the relative buttons when waypoint is selected
            var addButtonsRow = new VisualElement();
            addButtonsRow.style.flexDirection = FlexDirection.Row;
            addButtonsRow.style.marginTop = 5;
            addButtonsRow.style.height = 36; // Taller buttons
            managePoints.Add(addButtonsRow);

            var addBeforeButton = new Button(() => OnAddWaypointRelative?.Invoke(true));
            addBeforeButton.text = "◄+ Add Before";
            ApplyButtonStyle(addBeforeButton, _insertBeforeColor, true);
            addBeforeButton.style.flexGrow = 1;
            addButtonsRow.Add(addBeforeButton);

            var addAfterButton = new Button(() => OnAddWaypointRelative?.Invoke(false));
            addAfterButton.text = "Add After +►";
            ApplyButtonStyle(addAfterButton, _insertAfterColor, true);
            addAfterButton.style.flexGrow = 1;
            addButtonsRow.Add(addAfterButton);
            
            // Add delete button after the add buttons
            var deleteButton = new Button(() => OnRemoveWaypoint?.Invoke(selectedWaypointIndex));
            deleteButton.text = "🗑 Delete Waypoint";
            ApplyButtonStyle(deleteButton, _dangerColor);
            deleteButton.style.marginTop = 8;
            managePoints.Add(deleteButton);
        }
        else
        {
            // Standard add waypoint button
            var addWaypointButton = new Button(() => OnAddWaypoint?.Invoke());
            addWaypointButton.text = "＋ Add Waypoint";
            ApplyButtonStyle(addWaypointButton, _primaryActionColor, true);
            addWaypointButton.style.height = 36; // Make it taller
            managePoints.Add(addWaypointButton);
        }

        managePoints = CreateSection(managePoints, "Manage Waypoints");
        // END OF MANAGE POINTS SECTION
        
        // START OF WAYPOINT PROPERTIES SECTION
        var waypointProperties = new VisualElement();
        
        // Selected waypoint details
        var helpSection = new VisualElement();
        if (selectedWaypointIndex >= 0 && selectedWaypointIndex < selectedPath.Waypoints.Length)
        {
            BuildSelectedWaypointUI(waypointProperties, selectedPath, selectedWaypointIndex);
        }
        else
        {
            var helpBox = new HelpBox("Select a waypoint in the Scene view to edit its properties",
                HelpBoxMessageType.Info);
            helpSection.Add(helpBox);
        }
        
        // EVERYTHING IN THE UI
        allContainer.Add(header);
        allContainer.Add(topActions);
        allContainer.Add(pathProperties);
        allContainer.Add(navSection);
        allContainer.Add(managePoints);
        allContainer.Add(waypointProperties);
        
        root.Add(allContainer);
        root.Add(helpSection);
    }

    // Build the UI for the selected waypoint
    private void BuildSelectedWaypointUI(VisualElement root, NpcPath selectedPath, int selectedWaypointIndex)
    {
        var waypoint = selectedPath.Waypoints[selectedWaypointIndex];

        // START OF WAYPOINT SECTION
        var waypointSection = new VisualElement();
        waypointSection.style.marginTop = 10;
        
        // Position field
        var positionField = new Vector3Field();
        positionField.value = waypoint.Point;
        positionField.style.marginBottom = 10;
        positionField.RegisterValueChangedCallback(evt =>
        {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].Point = evt.newValue;
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });
        waypointSection.Add(positionField);

        // Waypoint properties in foldout
        var propertiesGroup = new Foldout();
        propertiesGroup.text = "Waypoint Behavior";
        propertiesGroup.value = true; // Expanded by default
        propertiesGroup.style.marginBottom = 10;
        waypointSection.Add(propertiesGroup);

        // Has Stop toggle
        var hasStopToggle = new Toggle("Has Stop");
        hasStopToggle.value = waypoint.HasStop;
        hasStopToggle.tooltip = "NPCs will stop at this waypoint";
        hasStopToggle.style.marginBottom = 5;
        propertiesGroup.Add(hasStopToggle);

        // Stop Time field with slider
        var stopTimeContainer = new VisualElement();
        stopTimeContainer.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        stopTimeContainer.style.marginLeft = 16;
        propertiesGroup.Add(stopTimeContainer);

        var timeLabel = new Label("Stop Time (seconds)");
        stopTimeContainer.Add(timeLabel);

        var timeControls = new VisualElement();
        timeControls.style.flexDirection = FlexDirection.Row;
        timeControls.style.alignItems = Align.Center;
        stopTimeContainer.Add(timeControls);

        var timeSlider = new Slider(0, 10);
        timeSlider.value = waypoint.StopTime;
        timeSlider.style.flexGrow = 1;
        timeControls.Add(timeSlider);

        var timeField = new FloatField();
        timeField.value = waypoint.StopTime;
        timeField.style.width = 50;
        timeField.style.marginLeft = 5;
        timeControls.Add(timeField);

        // Link slider and field
        timeSlider.RegisterValueChangedCallback(evt =>
        {
            timeField.value = evt.newValue;
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].StopTime = evt.newValue;
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        timeField.RegisterValueChangedCallback(evt =>
        {
            timeSlider.value = evt.newValue;
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].StopTime = Mathf.Max(0, evt.newValue);
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // Has Direction toggle
        var hasDirectionToggle = new Toggle("Has Direction");
        hasDirectionToggle.value = waypoint.HasDirection;
        hasDirectionToggle.tooltip = "NPCs will face this direction when stopping";
        hasDirectionToggle.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        hasDirectionToggle.style.marginTop = 10;
        propertiesGroup.Add(hasDirectionToggle);

        var directionField = new Vector3Field("Direction");
        directionField.value = waypoint.Direction;
        directionField.style.display =
            waypoint.HasStop && waypoint.HasDirection ? DisplayStyle.Flex : DisplayStyle.None;
        directionField.style.marginLeft = 16;
        propertiesGroup.Add(directionField);

        // Has Animation toggle
        var hasAnimationToggle = new Toggle("Has Animation");
        hasAnimationToggle.value = waypoint.HasAnimation;
        hasAnimationToggle.tooltip = "NPC will play animation at this waypoint";
        hasAnimationToggle.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        hasAnimationToggle.style.marginTop = 10;
        propertiesGroup.Add(hasAnimationToggle);

        // Animation type field
        var animationTypeField = new EnumField("Animation Type", waypoint.Animation);
        animationTypeField.style.display =
            waypoint.HasStop && waypoint.HasAnimation ? DisplayStyle.Flex : DisplayStyle.None;
        animationTypeField.style.marginLeft = 16;
        propertiesGroup.Add(animationTypeField);

        // Register callbacks
        RegisterWaypointCallbacks(
            selectedPath, selectedWaypointIndex,
            hasStopToggle, hasDirectionToggle, hasAnimationToggle,
            stopTimeContainer, directionField, animationTypeField);
        
        waypointSection = CreateSection(waypointSection, $"Waypoint {selectedWaypointIndex + 1} Properties");
        root.Add(waypointSection);
        // END OF WAYPOINT SECTION
    }

    // Register all callbacks for the waypoint properties
    private void RegisterWaypointCallbacks(
        NpcPath selectedPath, int selectedWaypointIndex,
        Toggle hasStopToggle, Toggle hasDirectionToggle, Toggle hasAnimationToggle,
        VisualElement stopTimeContainer, VisualElement directionField, VisualElement animationTypeField)
    {
        // HasStop toggle callback
        hasStopToggle.RegisterValueChangedCallback(evt =>
        {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].HasStop = evt.newValue;

            // If HasStop is turned off, also disable HasDirection and HasAnimation
            if (!evt.newValue)
            {
                if (waypoints[selectedWaypointIndex].HasDirection)
                {
                    waypoints[selectedWaypointIndex].HasDirection = false;
                    hasDirectionToggle.value = false;
                }

                if (waypoints[selectedWaypointIndex].HasAnimation)
                {
                    waypoints[selectedWaypointIndex].HasAnimation = false;
                    hasAnimationToggle.value = false;
                }
            }

            // Update UI visibility with current toggle values
            stopTimeContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            hasDirectionToggle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            hasAnimationToggle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            directionField.style.display =
                evt.newValue && hasDirectionToggle.value ? DisplayStyle.Flex : DisplayStyle.None;
            animationTypeField.style.display =
                evt.newValue && hasAnimationToggle.value ? DisplayStyle.Flex : DisplayStyle.None;

            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // HasDirection toggle callback
        hasDirectionToggle.RegisterValueChangedCallback(evt =>
        {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].HasDirection = evt.newValue;
            selectedPath.Waypoints = waypoints;

            // Use current toggle value instead of old waypoint value
            directionField.style.display = hasStopToggle.value && evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;

            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // Direction field callback
        ((Vector3Field)directionField).RegisterValueChangedCallback(evt =>
        {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].Direction = evt.newValue.normalized;
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // HasAnimation toggle callback
        hasAnimationToggle.RegisterValueChangedCallback(evt =>
        {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].HasAnimation = evt.newValue;
            selectedPath.Waypoints = waypoints;

            // Use current toggle value instead of old waypoint value
            animationTypeField.style.display =
                hasStopToggle.value && evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;

            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // AnimationType field callback
        ((EnumField)animationTypeField).RegisterValueChangedCallback(evt =>
        {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].Animation = (Waypoint.AnimationType)evt.newValue;
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });
    }

    // Helper methods for creating UI elements
    private VisualElement CreateHeader(string title, string subtitle = null)
    {
        var header = new VisualElement();
        header.AddToClassList("header"); // Add a class for identification
        header.style.backgroundColor = _headerBgColor;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
        header.style.paddingLeft = 16;
        header.style.paddingRight = 16;
        header.style.paddingTop = 12;
        header.style.paddingBottom = 12;
        header.style.marginBottom = 16;

        var headerTitle = new Label(title);
        headerTitle.style.fontSize = 16;
        headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerTitle.style.color = Color.white;
        header.Add(headerTitle);

        if (!string.IsNullOrEmpty(subtitle))
        {
            var headerSubtitle = new Label(subtitle);
            headerSubtitle.style.fontSize = 11;
            headerSubtitle.style.color = new Color(0.7f, 0.7f, 0.7f);
            headerSubtitle.style.marginTop = 2;
            header.Add(headerSubtitle);
        }

        return header;
    }

    private VisualElement CreateSection(VisualElement content, string title = null)
    {
        var section = new VisualElement
        {
            style =
            {
                marginBottom = 10
            }
        };
        
        var sectionContainer = new Box
        {
            style =
            {
                backgroundColor = _sectionCColor,
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4,
                paddingLeft = 12,
                paddingRight = 12,
                paddingTop = 12,
                paddingBottom = 12
            }
        };

        var sectionHeader = new Label(title)
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 13,
                marginBottom = 8
            }
        };
        if(title != null)
            sectionContainer.Add(sectionHeader);

        var sectionContent = new Box
        {
            style =
            {
                backgroundColor = _sectionBgColor,
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4,
                paddingLeft = 12,
                paddingRight = 12,
                paddingTop = 12,
                paddingBottom = 12
            }
        };
        sectionContent.Add(content);
        sectionContainer.Add(sectionContent);
        section.Add(sectionContainer);

        return section;
    }

    private void ApplyButtonStyle(Button button, Color baseColor, bool isPrimary = false)
    {
        button.style.backgroundColor = baseColor;
        button.style.color = Color.white;
        button.style.borderTopLeftRadius = 4;
        button.style.borderTopRightRadius = 4;
        button.style.borderBottomLeftRadius = 4;
        button.style.borderBottomRightRadius = 4;
        button.style.borderTopWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.paddingTop = 6;
        button.style.paddingBottom = 6;
        button.style.paddingLeft = isPrimary ? 16 : 12;
        button.style.paddingRight = isPrimary ? 16 : 12;
        button.style.marginTop = 2;
        button.style.marginBottom = 2;

        // Add hover effect using pseudo states
        button.RegisterCallback<MouseEnterEvent>(evt =>
        {
            button.style.backgroundColor = new Color(
                baseColor.r * 1.1f,
                baseColor.g * 1.1f,
                baseColor.b * 1.1f
            );
        });

        button.RegisterCallback<MouseLeaveEvent>(evt => { button.style.backgroundColor = baseColor; });
    }

    private void ApplyBaseStyles(VisualElement root)
    {
        root.style.paddingBottom = 16;
    }
}