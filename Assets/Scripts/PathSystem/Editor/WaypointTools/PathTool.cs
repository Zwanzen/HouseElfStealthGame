using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class PathTool : EditorWindow
{
    [MenuItem("Tools/PathTool")]
    public static void ShowExample()
    {
        PathTool wnd = GetWindow<PathTool>();
        wnd.titleContent = new GUIContent("Path Tool");
    }

    private NpcPath _selectedPath;
    private VisualElement _root;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private int _selectedWaypointIndex = -1; // -1 means no waypoint is selected

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_selectedPath == null || _selectedPath.Waypoints == null)
            return;

        Event e = Event.current;
        bool isHandleHot = GUIUtility.hotControl != 0;
        int directionHandleId = -1;  // Will store the direction handle's control ID
        
        // Add F key focus functionality
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && 
            _selectedWaypointIndex >= 0 && _selectedWaypointIndex < _selectedPath.Waypoints.Length)
        {
            Vector3 focusPoint = _selectedPath.Waypoints[_selectedWaypointIndex].Point;
        
            // Frame the selected waypoint
            sceneView.Frame(new Bounds(focusPoint, Vector3.one * 3f), false);
        
            // Consume the event
            e.Use();
        }
        
        // Draw lines between waypoints if we have at least 2
        if (_selectedPath.Waypoints.Length >= 2)
        {
            Handles.color = Color.cyan;
            for (int i = 0; i < _selectedPath.Waypoints.Length - 1; i++)
            {
                Vector3 startPoint = _selectedPath.Waypoints[i].Point;
                Vector3 endPoint = _selectedPath.Waypoints[i + 1].Point;
                Handles.DrawLine(startPoint, endPoint, 2f);
            }
        
            // Draw loop line if enabled
            if (_selectedPath.IsLoop && _selectedPath.Waypoints.Length > 2)
            {
                Vector3 lastPoint = _selectedPath.Waypoints[^1].Point;
                Vector3 firstPoint = _selectedPath.Waypoints[0].Point;
                Handles.color = new Color(0.8f, 0.8f, 1f); // Slightly different color for loop line
                Handles.DrawLine(lastPoint, firstPoint, 2f);
            }
        }
        
        // Draw direction indicators for all waypoints with HasStop and HasDirection
        for (int i = 0; i < _selectedPath.Waypoints.Length; i++)
        {
            Waypoint waypoint = _selectedPath.Waypoints[i];
            if (waypoint.HasStop && waypoint.HasDirection)
            {
                // Use white for selected waypoint's direction arrow, orange for others
                Handles.color = (i == _selectedWaypointIndex) ? Color.white : new Color(1f, 0.5f, 0f);
                Vector3 waypointPos = waypoint.Point;
                Vector3 direction = waypoint.Direction.normalized;
            
                // Scale direction arrow by handle size
                float handleSize = HandleUtility.GetHandleSize(waypointPos);
                float arrowLength = handleSize;
            
                // Draw direction arrow with consistent size
                Handles.DrawLine(waypointPos, waypointPos + direction * arrowLength, 2f);

                // Draw arrowhead with consistent size
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * handleSize * 0.2f;
                Vector3 arrowEnd = waypointPos + direction * arrowLength;
                Handles.DrawLine(arrowEnd, arrowEnd - direction * handleSize * 0.2f + right, 2f);
                Handles.DrawLine(arrowEnd, arrowEnd - direction * handleSize * 0.2f - right, 2f);
            }
        }

        // Only show position handle for selected waypoint
        if (_selectedWaypointIndex >= 0 && _selectedWaypointIndex < _selectedPath.Waypoints.Length)
        {
            // Handle Shift+left click for surface snapping
            if (e.type == EventType.MouseDrag && e.shift && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Undo.RecordObject(_selectedPath, "Snap Waypoint");
                    var waypoints = _selectedPath.Waypoints;
                    waypoints[_selectedWaypointIndex].Point = hit.point;
                    _selectedPath.Waypoints = waypoints;
                    EditorUtility.SetDirty(_selectedPath);
                    e.Use();
                    sceneView.Repaint();
                }
            }
    
            // Add direction handle for selected waypoint first to get its control ID
            if (_selectedPath.Waypoints[_selectedWaypointIndex].HasDirection &&
                _selectedPath.Waypoints[_selectedWaypointIndex].HasStop)
            {
                Handles.color = Color.yellow;
                Vector3 waypointPos = _selectedPath.Waypoints[_selectedWaypointIndex].Point;
                Vector3 direction = _selectedPath.Waypoints[_selectedWaypointIndex].Direction.normalized;
                float handleSize = HandleUtility.GetHandleSize(waypointPos);

                // Create a rotation based only on the Y axis direction
                float currentYRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                Quaternion currentRotation = Quaternion.Euler(0, currentYRotation, 0);

                // Generate a unique control ID for the direction handle
                directionHandleId = GUIUtility.GetControlID(FocusType.Passive);
                
                EditorGUI.BeginChangeCheck();
                // Scale disc size by handle size for consistent appearance
                Quaternion newRotation = Handles.Disc(directionHandleId, currentRotation, waypointPos, Vector3.up, handleSize, false, 10f);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_selectedPath, "Rotate Waypoint Direction");

                    Vector3 newDirection = newRotation * Vector3.forward;
                    newDirection.y = 0;
                    newDirection.Normalize();

                    var waypoints = _selectedPath.Waypoints;
                    waypoints[_selectedWaypointIndex].Direction = newDirection;
                    _selectedPath.Waypoints = waypoints;

                    EditorUtility.SetDirty(_selectedPath);
                    sceneView.Repaint();
                }
            }

            // Only show position handle if we're not manipulating the direction handle
            bool isDirectionHandleActive = (GUIUtility.hotControl == directionHandleId);
            if (!isDirectionHandleActive)
            {
                // Use constant size position handle
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(_selectedPath.Waypoints[_selectedWaypointIndex].Point, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_selectedPath, "Move Waypoint");
                    var waypoints = _selectedPath.Waypoints;
                    waypoints[_selectedWaypointIndex].Point = newPosition;
                    _selectedPath.Waypoints = waypoints;
                    EditorUtility.SetDirty(_selectedPath);
                }
            }
        }
        
        // Draw clickable dots for all waypoints with consistent size
        for (int i = 0; i < _selectedPath.Waypoints.Length; i++)
        {
            Waypoint waypoint = _selectedPath.Waypoints[i];
            Vector3 waypointPos = waypoint.Point;
            float handleSize = HandleUtility.GetHandleSize(waypointPos);

            // Set color based on waypoint state
            if (i == _selectedWaypointIndex)
                Handles.color = Color.white;
            else if (waypoint.HasStop)
                Handles.color = new Color(1f, 0.5f, 0f);
            else
                Handles.color = Color.cyan;

            // Use consistent size based on handle size
            float dotSize = (i == _selectedWaypointIndex ? 0.1f : 0.07f) * handleSize;

            // Draw dot with consistent size (Selection handling moved to the beginning of the method)
            Handles.DotHandleCap(
                0,
                waypointPos,
                Quaternion.identity,
                dotSize,
                EventType.Repaint
            );

            // Draw label with consistent size
            Handles.Label(waypointPos + Vector3.up * handleSize * 0.3f, $"Waypoint {i+1}");
        }
        
        // Display stop time for waypoints with HasStop enabled
        for (int i = 0; i < _selectedPath.Waypoints.Length; i++)
        {
            Waypoint waypoint = _selectedPath.Waypoints[i];
            if (waypoint.HasStop && waypoint.StopTime > 0)
            {
                Vector3 waypointPos = waypoint.Point;
                float handleSize = HandleUtility.GetHandleSize(waypointPos);
        
                // Position the time text below the waypoint number
                Vector3 timePos = waypointPos + Vector3.up * handleSize * 0.5f;
        
                // Set the color - use the same color as the waypoint for consistency
                Handles.color = (i == _selectedWaypointIndex) ? Color.white : new Color(1f, 0.5f, 0f);
        
                // Draw stopwatch icon with time value
                string timeLabel = $"◷ {waypoint.StopTime:F1}s";
                Handles.Label(timePos, timeLabel);
        
                // Draw a clock visualization - a circle with a radius based on stop time
                float circleRadius = handleSize * 0.15f;
                Handles.DrawWireDisc(waypointPos, Vector3.up, circleRadius);
        
                // Draw a "hand" on the clock
                float angle = 90 - (waypoint.StopTime % 12) * 30; // 30 degrees per "hour"
                Vector3 hand = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * circleRadius * 0.8f;
        
                Handles.DrawLine(waypointPos, waypointPos + hand, 2f);
            }
        }
        
        // Prevent selection of scene objects when clicking
        if (e.type == EventType.MouseDown && e.button == 0 && !isHandleHot)
        {
            bool clickedOnWaypoint = false;

            // Check if clicked on any waypoint first
            for (int i = 0; i < _selectedPath.Waypoints.Length; i++)
            {
                Vector3 waypointPos = _selectedPath.Waypoints[i].Point;
                if (Vector3.Distance(HandleUtility.WorldToGUIPoint(waypointPos), e.mousePosition) < 10f)
                {
                    clickedOnWaypoint = true;
                    _selectedWaypointIndex = i;
                    BuildPathDetailsUI();
                    e.Use();
                    SceneView.RepaintAll();
                    break;
                }
            }

            // If not clicked on waypoint, just consume the event to prevent selection
            if (!clickedOnWaypoint)
            {
                _selectedWaypointIndex = -1;
                BuildPathDetailsUI();
                e.Use();
                SceneView.RepaintAll();
            }
        }
    }
    
    public void CreateGUI()
    {
        _root = rootVisualElement;
        BuildInitialUI();
    }

    private void BuildInitialUI()
    {
        _root.Clear();

        string path = "Assets/Scripts/PathSystem/Paths";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        Label label = new Label("Insert Path or Create New Path");
        _root.Add(label);

        ObjectField npcPathField = new ObjectField();
        npcPathField.objectType = typeof(NpcPath);
        npcPathField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == null) return;
        
            // Get the asset path
            string assetPath = AssetDatabase.GetAssetPath(evt.newValue);
        
            // Unload the asset and force reload it completely
            Resources.UnloadAsset(evt.newValue);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        
            // Load a fresh copy of the asset
            _selectedPath = AssetDatabase.LoadAssetAtPath<NpcPath>(assetPath);
        
            if (_selectedPath != null)
            {
                // Reset selected waypoint
                _selectedWaypointIndex = -1;
                BuildPathDetailsUI();
                SceneView.RepaintAll();
            }
        });
        _root.Add(npcPathField);

        TextField nameField = new TextField("Name");
        nameField.value = "New Path";
        _root.Add(nameField);

        Button createPathButton = new Button(() =>
        {
            if (string.IsNullOrEmpty(nameField.value))
                nameField.value = "New Path";

            NpcPath npcPath = ScriptableObject.CreateInstance<NpcPath>();
            npcPath.Waypoints = new Waypoint[0];
            
            string pathToSave = AssetDatabase.GenerateUniqueAssetPath(path + "/" + nameField.value + ".asset");
            AssetDatabase.CreateAsset(npcPath, pathToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selectedPath = npcPath;
            BuildPathDetailsUI();
        });
        createPathButton.text = "Create Path";
        _root.Add(createPathButton);
    }

private void BuildPathDetailsUI()
{
    _root.Clear();

    // Ensure the selectedPath is valid
    if (_selectedPath == null)
    {
        BuildInitialUI();
        return;
    }

    // Path header
    Label header = new Label($"Editing Path: {_selectedPath.name}");
    header.style.fontSize = 16;
    header.style.marginBottom = 10;
    _root.Add(header);

    // Back button
    Button backButton = new Button(() => {
        _selectedPath = null;
        BuildInitialUI();
    });
    backButton.text = "Select Different Path";
    _root.Add(backButton);

    // Ensure the waypoints array is initialized
    if (_selectedPath.Waypoints == null)
    {
        _selectedPath.Waypoints = Array.Empty<Waypoint>();
        EditorUtility.SetDirty(_selectedPath);
    }

    // Is Loop toggle - add this new control
    Toggle isLoopToggle = new Toggle("Is Loop Path");
    isLoopToggle.value = _selectedPath.IsLoop;
    isLoopToggle.tooltip = "When enabled, connects the last waypoint back to the first";
    isLoopToggle.style.marginTop = 10;
    isLoopToggle.style.marginBottom = 5;
    isLoopToggle.RegisterValueChangedCallback(evt => {
        _selectedPath.IsLoop = evt.newValue;
        EditorUtility.SetDirty(_selectedPath);
        SceneView.RepaintAll();
    });
    _root.Add(isLoopToggle);

    // Waypoints info section 
    Label waypointsHeader = new Label($"Waypoints: {_selectedPath.Waypoints?.Length ?? 0}");
    waypointsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
    _root.Add(waypointsHeader);

    // Selected waypoint section
    if (_selectedWaypointIndex >= 0 && _selectedWaypointIndex < _selectedPath.Waypoints.Length)
    {
        // Display selected waypoint details
        Box selectedWaypointBox = new Box();
        selectedWaypointBox.style.marginTop = 10;
        selectedWaypointBox.style.marginBottom = 10;
        _root.Add(selectedWaypointBox);

        Label selectedLabel = new Label($"Selected: Waypoint {_selectedWaypointIndex + 1}");
        selectedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        selectedWaypointBox.Add(selectedLabel);

        Waypoint waypoint = _selectedPath.Waypoints[_selectedWaypointIndex];

        // Position field
        Vector3Field positionField = new Vector3Field("Position");
        positionField.value = waypoint.Point;
        positionField.RegisterValueChangedCallback(evt => {
            var waypoints = _selectedPath.Waypoints;
            waypoints[_selectedWaypointIndex].Point = evt.newValue;
            _selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(_selectedPath);
            SceneView.RepaintAll();
        });
        selectedWaypointBox.Add(positionField);

        // Has Stop toggle
        Toggle hasStopToggle = new Toggle("Has Stop");
        hasStopToggle.value = waypoint.HasStop;
        selectedWaypointBox.Add(hasStopToggle);
        
        // Stop Time field (only visible when HasStop is true)
        FloatField stopTimeField = new FloatField("Stop Time (seconds)");
        stopTimeField.value = waypoint.StopTime;
        stopTimeField.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        selectedWaypointBox.Add(stopTimeField);

        // Register the callback
        stopTimeField.RegisterValueChangedCallback(evt => {
            var waypoints = _selectedPath.Waypoints;
            waypoints[_selectedWaypointIndex].StopTime = Mathf.Max(0, evt.newValue); // Ensure positive value
            _selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(_selectedPath);
            SceneView.RepaintAll();
        });
        
        // Has Direction toggle and direction field
        Toggle hasDirectionToggle = new Toggle("Has Direction");
        hasDirectionToggle.value = waypoint.HasDirection;
        // Initially hide or show based on HasStop value
        hasDirectionToggle.style.display = waypoint.HasStop ? DisplayStyle.Flex : DisplayStyle.None;
        selectedWaypointBox.Add(hasDirectionToggle);

        Vector3Field directionField = new Vector3Field("Direction");
        directionField.value = waypoint.Direction;
        // Hide or show direction field based on both HasStop and HasDirection
        directionField.style.display = (waypoint.HasStop && waypoint.HasDirection) ? DisplayStyle.Flex : DisplayStyle.None;
        selectedWaypointBox.Add(directionField);

        // Register HasStop toggle callback
        hasStopToggle.RegisterValueChangedCallback(evt => {
            var waypoints = _selectedPath.Waypoints;
            waypoints[_selectedWaypointIndex].HasStop = evt.newValue;

            // If HasStop is turned off, also disable HasDirection
            if (!evt.newValue && waypoints[_selectedWaypointIndex].HasDirection)
            {
                waypoints[_selectedWaypointIndex].HasDirection = false;
                hasDirectionToggle.value = false;
            }

            // Update UI visibility
            stopTimeField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            hasDirectionToggle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            directionField.style.display = (evt.newValue && waypoints[_selectedWaypointIndex].HasDirection) ? 
                DisplayStyle.Flex : DisplayStyle.None;

            _selectedPath.Waypoints = waypoints;
            EditorUtility.SetDirty(_selectedPath);
        });

        // Register HasDirection toggle callback
        hasDirectionToggle.RegisterValueChangedCallback(evt => {
            var waypoints = _selectedPath.Waypoints;
            waypoints[_selectedWaypointIndex].HasDirection = evt.newValue;
            _selectedPath.Waypoints = waypoints;
            directionField.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            EditorUtility.SetDirty(_selectedPath);
        });

        // Delete button
        Button deleteButton = new Button(() => RemoveWaypoint(_selectedWaypointIndex));
        deleteButton.text = "Delete Waypoint";
        selectedWaypointBox.Add(deleteButton);
    }
    else
    {
        Label selectPrompt = new Label("Select a waypoint in the Scene view to edit it");
        selectPrompt.style.marginTop = 10;
        _root.Add(selectPrompt);
    }

    // Waypoint navigation buttons
    Box navigationBox = new Box();
    navigationBox.style.flexDirection = FlexDirection.Row;
    _root.Add(navigationBox);
    
    Button prevButton = new Button(() => {
        if (_selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0) {
            _selectedWaypointIndex = (_selectedWaypointIndex <= 0) ? 
                _selectedPath.Waypoints.Length - 1 : _selectedWaypointIndex - 1;
            BuildPathDetailsUI();
            SceneView.RepaintAll();
        }
    });
    prevButton.text = "Previous";
    navigationBox.Add(prevButton);
    
    Button nextButton = new Button(() => {
        if (_selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0) {
            _selectedWaypointIndex = (_selectedWaypointIndex >= _selectedPath.Waypoints.Length - 1 || _selectedWaypointIndex < 0) ? 
                0 : _selectedWaypointIndex + 1;
            BuildPathDetailsUI();
            SceneView.RepaintAll();
        }
    });
    nextButton.text = "Next";
    navigationBox.Add(nextButton);

    // Add waypoint button
    Button addWaypointButton = new Button(AddWaypoint);
    addWaypointButton.text = "Add Waypoint";
    _root.Add(addWaypointButton);

    // Save changes button
    Button saveButton = new Button(() => {
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(_selectedPath);
    });
    saveButton.text = "Save Changes";
    _root.Add(saveButton);
}

    private void AddWaypoint()
    {
        Vector3 newPosition = Vector3.zero;
    
        // If there are existing waypoints, use the position of the last one
        if (_selectedPath.Waypoints != null && _selectedPath.Waypoints.Length > 0)
        {
            newPosition = _selectedPath.Waypoints[^1].Point;
        }
    
        Waypoint newWaypoint = new Waypoint
        {
            Point = newPosition,
            HasStop = false,
            HasDirection = false,
            Direction = Vector3.forward
        };

        Waypoint[] waypoints = _selectedPath.Waypoints ?? Array.Empty<Waypoint>();
        Waypoint[] newWaypoints = new Waypoint[waypoints.Length + 1];
        Array.Copy(waypoints, newWaypoints, waypoints.Length);
        newWaypoints[waypoints.Length] = newWaypoint;

        _selectedPath.Waypoints = newWaypoints;
        _selectedWaypointIndex = waypoints.Length; // Select the new waypoint
        EditorUtility.SetDirty(_selectedPath);
        BuildPathDetailsUI();
    }

    private void RemoveWaypoint(int index)
    {
        Waypoint[] waypoints = _selectedPath.Waypoints;
        Waypoint[] newWaypoints = new Waypoint[waypoints.Length - 1];
        
        Array.Copy(waypoints, 0, newWaypoints, 0, index);
        Array.Copy(waypoints, index + 1, newWaypoints, index, waypoints.Length - index - 1);
        
        _selectedPath.Waypoints = newWaypoints;
        EditorUtility.SetDirty(_selectedPath);
        BuildPathDetailsUI();
    }
}