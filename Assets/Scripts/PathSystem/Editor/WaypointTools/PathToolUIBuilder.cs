using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class PathToolUIBuilder
{
    // Colors for styling
    private readonly Color _insertBeforeColor = new Color(0.2f, 0.6f, 1f);
    private readonly Color _insertAfterColor = new Color(0.2f, 1f, 0.6f);
    private readonly Color _headerBgColor = new Color(0.22f, 0.22f, 0.25f);
    private readonly Color _sectionBgColor = new Color(0.18f, 0.18f, 0.21f);
    private readonly Color _buttonHoverColor = new Color(0.25f, 0.25f, 0.28f);
    private readonly Color _primaryActionColor = new Color(0.35f, 0.55f, 0.9f);
    private readonly Color _secondaryActionColor = new Color(0.4f, 0.4f, 0.45f);
    private readonly Color _dangerColor = new Color(0.95f, 0.3f, 0.3f);

    // Events
    public event Action<NpcPath> OnPathSelected;
    public event Action OnAddWaypoint;
    public event Action<int> OnRemoveWaypoint;
    public event Action OnFlipPath;
    public event Action<int> OnSelectWaypoint;
    public event Action OnSaveChanges;
    public event Action<bool> OnAddWaypointRelative;

    // Build the initial UI for path selection or creation
    public void BuildInitialUI(VisualElement root)
    {
        root.Clear();
        ApplyBaseStyles(root);

        // Create header
        var header = CreateHeader("Path Creation Tool", "Create or select a path to edit");
        root.Add(header);

        // Select path section
        var selectSection = CreateSection("Select Existing Path");
        root.Add(selectSection);

        ObjectField npcPathField = new ObjectField();
        npcPathField.objectType = typeof(NpcPath);
        npcPathField.label = "Path Asset";
        npcPathField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == null) return;

            // Get the asset path
            string assetPath = AssetDatabase.GetAssetPath(evt.newValue);

            // Unload the asset and force reload it completely
            Resources.UnloadAsset(evt.newValue);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // Load a fresh copy of the asset
            NpcPath selectedPath = AssetDatabase.LoadAssetAtPath<NpcPath>(assetPath);
            OnPathSelected?.Invoke(selectedPath);
        });
        selectSection.Add(npcPathField);

        // Create path section
        var createSection = CreateSection("Create New Path");
        root.Add(createSection);

        TextField nameField = new TextField("Name");
        nameField.value = "New Path";
        nameField.style.marginBottom = 10;
        createSection.Add(nameField);

        Button createPathButton = new Button(() =>
        {
            string path = "Assets/Scripts/PathSystem/Paths";
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            if (string.IsNullOrEmpty(nameField.value))
                nameField.value = "New Path";

            NpcPath npcPath = ScriptableObject.CreateInstance<NpcPath>();
            npcPath.Waypoints = new Waypoint[0];

            string pathToSave = AssetDatabase.GenerateUniqueAssetPath(path + "/" + nameField.value + ".asset");
            AssetDatabase.CreateAsset(npcPath, pathToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            OnPathSelected?.Invoke(npcPath);
        });
        createPathButton.text = "Create Path";
        ApplyButtonStyle(createPathButton, _primaryActionColor, true);
        createSection.Add(createPathButton);

        // Footer with info
        var footer = new VisualElement();
        footer.style.marginTop = 20;
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

        // Create header
        var header = CreateHeader($"Editing: {selectedPath.name}", "Configure waypoints and path properties");
        root.Add(header);

        // Top action bar
        var topActions = new VisualElement();
        topActions.style.flexDirection = FlexDirection.Row;
        topActions.style.marginBottom = 16;
        root.Add(topActions);

        // Back button
        Button backButton = new Button(() => {
            BuildInitialUI(root);
        });
        backButton.text = "← Back";
        ApplyButtonStyle(backButton, _secondaryActionColor);
        backButton.style.width = 80;
        topActions.Add(backButton);

        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        topActions.Add(spacer);

        // Save button - keep it always visible
        Button saveButton = new Button(() => OnSaveChanges?.Invoke());
        saveButton.text = "💾 Save Changes";
        ApplyButtonStyle(saveButton, _primaryActionColor);
        topActions.Add(saveButton);

        // Ensure the waypoints array is initialized
        if (selectedPath.Waypoints == null)
        {
            selectedPath.Waypoints = new Waypoint[0];
            EditorUtility.SetDirty(selectedPath);
        }

        // Path Properties section
        var propertiesSection = CreateSection("Path Properties");
        root.Add(propertiesSection);

        // Is Loop toggle
        Toggle isLoopToggle = new Toggle("Is Loop Path");
        isLoopToggle.value = selectedPath.IsLoop;
        isLoopToggle.tooltip = "When enabled, connects the last waypoint back to the first";
        isLoopToggle.style.marginBottom = 10;
        isLoopToggle.RegisterValueChangedCallback(evt => {
            selectedPath.IsLoop = evt.newValue;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });
        propertiesSection.Add(isLoopToggle);

        // Flip Direction button
        Button flipDirectionButton = new Button(() => OnFlipPath?.Invoke());
        flipDirectionButton.text = "↑↓ Flip Path Direction";
        flipDirectionButton.tooltip = "Reverses the order of waypoints (start becomes end)";
        ApplyButtonStyle(flipDirectionButton, _secondaryActionColor);
        propertiesSection.Add(flipDirectionButton);

        // Waypoints section
        var waypointsSection = CreateSection($"Waypoints ({selectedPath.Waypoints?.Length ?? 0})");
        root.Add(waypointsSection);

        // Waypoint navigation and management
        var navContainer = new Box();
        navContainer.style.flexDirection = FlexDirection.Column;
        navContainer.style.marginBottom = 12;
        waypointsSection.Add(navContainer);

        // Nav buttons row
        var navigationRow = new VisualElement();
        navigationRow.style.flexDirection = FlexDirection.Row;
        navigationRow.style.marginBottom = 10;
        navContainer.Add(navigationRow);

        Button prevButton = new Button(() => {
            if (selectedPath.Waypoints != null && selectedPath.Waypoints.Length > 0) {
                int newIndex = selectedWaypointIndex <= 0 ? 
                    selectedPath.Waypoints.Length - 1 : 
                    selectedWaypointIndex - 1;
                OnSelectWaypoint?.Invoke(newIndex);
            }
        });
        prevButton.text = "◄ Previous";
        ApplyButtonStyle(prevButton, _secondaryActionColor);
        prevButton.style.flexGrow = 1;
        navigationRow.Add(prevButton);

        Button nextButton = new Button(() => {
            if (selectedPath.Waypoints != null && selectedPath.Waypoints.Length > 0) {
                int newIndex = selectedWaypointIndex >= selectedPath.Waypoints.Length - 1 ? 
                    0 : 
                    selectedWaypointIndex + 1;
                OnSelectWaypoint?.Invoke(newIndex);
            }
        });
        nextButton.text = "Next ►";
        ApplyButtonStyle(nextButton, _secondaryActionColor);
        nextButton.style.flexGrow = 1;
        navigationRow.Add(nextButton);

        // Add waypoint buttons
        if (selectedWaypointIndex >= 0 && selectedWaypointIndex < selectedPath.Waypoints.Length)
        {
            // Add the relative buttons when waypoint is selected
            var addButtonsRow = new VisualElement();
            addButtonsRow.style.flexDirection = FlexDirection.Row;
            addButtonsRow.style.marginTop = 5;
            addButtonsRow.style.height = 36; // Taller buttons
            navContainer.Add(addButtonsRow);

            Button addBeforeButton = new Button(() => OnAddWaypointRelative?.Invoke(true));
            addBeforeButton.text = "◄+ Add Before";
            ApplyButtonStyle(addBeforeButton, _insertBeforeColor, true);
            addBeforeButton.style.flexGrow = 1;
            addBeforeButton.style.marginRight = 5;
            addButtonsRow.Add(addBeforeButton);

            Button addAfterButton = new Button(() => OnAddWaypointRelative?.Invoke(false));
            addAfterButton.text = "Add After +►";
            ApplyButtonStyle(addAfterButton, _insertAfterColor, true);
            addAfterButton.style.flexGrow = 1;
            addAfterButton.style.marginLeft = 5;
            addButtonsRow.Add(addAfterButton);
        }
        else
        {
            // Standard add waypoint button
            Button addWaypointButton = new Button(() => OnAddWaypoint?.Invoke());
            addWaypointButton.text = "＋ Add Waypoint";
            ApplyButtonStyle(addWaypointButton, _primaryActionColor, true);
            addWaypointButton.style.height = 36; // Make it taller
            navContainer.Add(addWaypointButton);
        }

        // Selected waypoint details
        if (selectedWaypointIndex >= 0 && selectedWaypointIndex < selectedPath.Waypoints.Length)
        {
            BuildSelectedWaypointUI(waypointsSection, selectedPath, selectedWaypointIndex);
        }
        else
        {
            var helpBox = new HelpBox("Select a waypoint in the Scene view to edit its properties", HelpBoxMessageType.Info);
            waypointsSection.Add(helpBox);
        }
    }

    // Build the UI for the selected waypoint
    private void BuildSelectedWaypointUI(VisualElement root, NpcPath selectedPath, int selectedWaypointIndex)
    {
        var waypointSection = CreateSection($"Waypoint {selectedWaypointIndex + 1} Properties");
        waypointSection.style.marginTop = 16;
        root.Add(waypointSection);

        Waypoint waypoint = selectedPath.Waypoints[selectedWaypointIndex];

        // Position field
        Vector3Field positionField = new Vector3Field("Position");
        positionField.value = waypoint.Point;
        positionField.style.marginBottom = 10;
        positionField.RegisterValueChangedCallback(evt => {
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
        Toggle hasStopToggle = new Toggle("Has Stop");
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
        timeSlider.RegisterValueChangedCallback(evt => {
            timeField.value = evt.newValue;
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].StopTime = evt.newValue;
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        timeField.RegisterValueChangedCallback(evt => {
            timeSlider.value = evt.newValue;
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].StopTime = Mathf.Max(0, evt.newValue);
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // Has Direction toggle
        Toggle hasDirectionToggle = new Toggle("Has Direction");
        hasDirectionToggle.value = waypoint.HasDirection;
        hasDirectionToggle.tooltip = "NPCs will face this direction when stopping";
        hasDirectionToggle.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        hasDirectionToggle.style.marginTop = 10;
        propertiesGroup.Add(hasDirectionToggle);

        Vector3Field directionField = new Vector3Field("Direction");
        directionField.value = waypoint.Direction;
        directionField.style.display = (waypoint.HasStop && waypoint.HasDirection) ? DisplayStyle.Flex : DisplayStyle.None;
        directionField.style.marginLeft = 16;
        propertiesGroup.Add(directionField);

        // Has Animation toggle
        Toggle hasAnimationToggle = new Toggle("Has Animation");
        hasAnimationToggle.value = waypoint.HasAnimation;
        hasAnimationToggle.tooltip = "NPC will play animation at this waypoint";
        hasAnimationToggle.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        hasAnimationToggle.style.marginTop = 10;
        propertiesGroup.Add(hasAnimationToggle);

        // Animation type field
        EnumField animationTypeField = new EnumField("Animation Type", waypoint.Animation);
        animationTypeField.style.display = (waypoint.HasStop && waypoint.HasAnimation) ? DisplayStyle.Flex : DisplayStyle.None;
        animationTypeField.style.marginLeft = 16;
        propertiesGroup.Add(animationTypeField);

        // Register callbacks
        RegisterWaypointCallbacks(
            selectedPath, selectedWaypointIndex,
            hasStopToggle, hasDirectionToggle, hasAnimationToggle,
            stopTimeContainer, directionField, animationTypeField);

        // Delete button - moved to bottom and styled as a danger action
        Button deleteButton = new Button(() => OnRemoveWaypoint?.Invoke(selectedWaypointIndex));
        deleteButton.text = "🗑 Delete Waypoint";
        ApplyButtonStyle(deleteButton, _dangerColor);
        deleteButton.style.marginTop = 12;
        waypointSection.Add(deleteButton);
    }

    // Register all callbacks for the waypoint properties
    private void RegisterWaypointCallbacks(
        NpcPath selectedPath, int selectedWaypointIndex,
        Toggle hasStopToggle, Toggle hasDirectionToggle, Toggle hasAnimationToggle,
        VisualElement stopTimeContainer, VisualElement directionField, VisualElement animationTypeField)
    {
        // HasStop toggle callback
        hasStopToggle.RegisterValueChangedCallback(evt => {
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
            directionField.style.display = (evt.newValue && hasDirectionToggle.value) ?
                DisplayStyle.Flex : DisplayStyle.None;
            animationTypeField.style.display = (evt.newValue && hasAnimationToggle.value) ?
                DisplayStyle.Flex : DisplayStyle.None;

            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // HasDirection toggle callback
        hasDirectionToggle.RegisterValueChangedCallback(evt => {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].HasDirection = evt.newValue;
            selectedPath.Waypoints = waypoints;

            // Use current toggle value instead of old waypoint value
            directionField.style.display = (hasStopToggle.value && evt.newValue) ?
                DisplayStyle.Flex : DisplayStyle.None;

            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // Direction field callback
        ((Vector3Field)directionField).RegisterValueChangedCallback(evt => {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].Direction = evt.newValue.normalized;
            selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // HasAnimation toggle callback
        hasAnimationToggle.RegisterValueChangedCallback(evt => {
            var waypoints = selectedPath.Waypoints;
            waypoints[selectedWaypointIndex].HasAnimation = evt.newValue;
            selectedPath.Waypoints = waypoints;

            // Use current toggle value instead of old waypoint value
            animationTypeField.style.display = (hasStopToggle.value && evt.newValue) ?
                DisplayStyle.Flex : DisplayStyle.None;

            EditorUtility.SetDirty(selectedPath);
            SceneView.RepaintAll();
        });

        // AnimationType field callback
        ((EnumField)animationTypeField).RegisterValueChangedCallback(evt => {
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

    private VisualElement CreateSection(string title)
    {
        var section = new VisualElement();
        section.style.marginBottom = 16;

        var sectionHeader = new Label(title);
        sectionHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        sectionHeader.style.fontSize = 13;
        sectionHeader.style.marginBottom = 8;
        section.Add(sectionHeader);

        var sectionContent = new Box();
        sectionContent.style.backgroundColor = _sectionBgColor;
        sectionContent.style.borderTopLeftRadius = 4;
        sectionContent.style.borderTopRightRadius = 4;
        sectionContent.style.borderBottomLeftRadius = 4;
        sectionContent.style.borderBottomRightRadius = 4;
        sectionContent.style.paddingLeft = 12;
        sectionContent.style.paddingRight = 12;
        sectionContent.style.paddingTop = 12;
        sectionContent.style.paddingBottom = 12;
        section.Add(sectionContent);

        return sectionContent;
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
        button.RegisterCallback<MouseEnterEvent>((evt) => {
            button.style.backgroundColor = new Color(
                baseColor.r * 1.1f,
                baseColor.g * 1.1f,
                baseColor.b * 1.1f
            );
        });

        button.RegisterCallback<MouseLeaveEvent>((evt) => {
            button.style.backgroundColor = baseColor;
        });
    }
    private void ApplyBaseStyles(VisualElement root)
    {
        root.style.paddingBottom = 16;
    }
}