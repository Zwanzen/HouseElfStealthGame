using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.UIElements;

public class PathRenderer
{
    // Add maximum size constants
    private const float MAX_DOT_SIZE = 0.1f;       // Maximum dot size
    private const float MAX_ARROW_SIZE = 1.5f;   // Maximum arrow size
    private const float MAX_HANDLE_SIZE = 2f;    // Maximum handle size for calculations
    private const float MAX_DISC_SIZE = 1.5f;    // Maximum disc size for direction handles
    private const float LABEL_FADE_START_DISTANCE = 20f;  // Distance at which labels start fading
    private const float LABEL_FADE_END_DISTANCE = 30f;    // Distance at which labels become invisible


    
    // Colors for styling
    private readonly Color _dotColor = new Color(0f, 1f, 1f, 1f);
    private readonly Color _insertBeforeColor = new Color(0.2f, 0.6f, 1f, 1f);
    private readonly Color _insertAfterColor = new Color(0.2f, 1f, 0.6f, 1f);
    private readonly Color _pathColor = new Color(0f, 1f, 1f, 0.3f);
    private readonly Color _loopLineColor = new Color(0.8f, 0.8f, 1f,0.3f);
    private readonly Color _directionHandleColor = Color.yellow;
    private readonly Color _stopColor = new Color(1f, 0.5f, 0f);
    private readonly Color _selectedColor = Color.white;
    private readonly Color _animationColor = new Color(0f, 1f, 0.5f);
    private readonly Color _lineArrowColor = new Color(0.4f, 1f, 1f);
    private readonly Color _loopArrowColor = new Color(0.9f, 0.9f, 1f);

    // Helper method to get size with maximum limit
    private float GetLimitedHandleSize(Vector3 position)
    {
        return Mathf.Min(HandleUtility.GetHandleSize(position), MAX_HANDLE_SIZE);
    }
    
    // Helper method to calculate label fade factor based on distance
    private float GetLabelFadeFactor(Vector3 position)
    {
        Camera camera = SceneView.currentDrawingSceneView?.camera;
        if (camera == null) return 1f;
    
        float distance = Vector3.Distance(camera.transform.position, position);
    
        // Full opacity when close
        if (distance < LABEL_FADE_START_DISTANCE) return 1f;
    
        // Completely invisible when too far
        if (distance > LABEL_FADE_END_DISTANCE) return 0f;
    
        // Linear fade between start and end distances
        return 1f - ((distance - LABEL_FADE_START_DISTANCE) / 
                     (LABEL_FADE_END_DISTANCE - LABEL_FADE_START_DISTANCE));
    }
    
private void DrawWaypointInsertionPoints(NpcPath path, int selectedWaypointIndex)
{
    if (selectedWaypointIndex < 0 || selectedWaypointIndex >= path.Waypoints.Length)
        return;

    Vector3 selectedPos = path.Waypoints[selectedWaypointIndex].Point;
    float handleSize = GetLimitedHandleSize(selectedPos);

    // Determine if we can add before/after
    bool canAddBefore = path.IsLoop || selectedWaypointIndex > 0;
    bool canAddAfter = path.IsLoop || selectedWaypointIndex < path.Waypoints.Length - 1;

    // Only calculate and draw the "Before" indicator if we can add before
    if (canAddBefore)
    {
        Vector3 beforePos;
        if (selectedWaypointIndex > 0)
        {
            beforePos = Vector3.Lerp(path.Waypoints[selectedWaypointIndex - 1].Point, selectedPos, 0.5f);
        }
        else // This is the first point, and path is a loop
        {
            beforePos = Vector3.Lerp(path.Waypoints[^1].Point, selectedPos, 0.5f);
        }

        // Draw the "Before" indicator
        Handles.color = _insertBeforeColor;
        float size = Mathf.Min(handleSize * 0.07f, MAX_HANDLE_SIZE * 0.25f);
        Handles.DotHandleCap(0, beforePos, Quaternion.identity, size, EventType.Repaint);
        
        // Draw "BEFORE" text
        Handles.color = new Color(_insertBeforeColor.r, _insertBeforeColor.g, _insertBeforeColor.b, 1f);
        Vector3 labelPos = beforePos + Vector3.up * handleSize * 0.4f;
        Handles.Label(labelPos, "BEFORE");
    }

    // Only calculate and draw the "After" indicator if we can add after
    if (canAddAfter)
    {
        Vector3 afterPos;
        if (selectedWaypointIndex < path.Waypoints.Length - 1)
        {
            afterPos = Vector3.Lerp(selectedPos, path.Waypoints[selectedWaypointIndex + 1].Point, 0.5f);
        }
        else // This is the last point, and path is a loop
        {
            afterPos = Vector3.Lerp(selectedPos, path.Waypoints[0].Point, 0.5f);
        }

        // Draw the "After" indicator
        Handles.color = _insertAfterColor;
        float size = Mathf.Min(handleSize * 0.07f, MAX_HANDLE_SIZE * 0.25f);
        Handles.DotHandleCap(0, afterPos, Quaternion.identity, size, EventType.Repaint);
        
        // Draw "AFTER" text
        Handles.color = new Color(_insertAfterColor.r, _insertAfterColor.g, _insertAfterColor.b, 1f);
        Vector3 labelPos = afterPos + Vector3.up * handleSize * 0.4f;
        Handles.Label(labelPos, "AFTER");
    }
}
    
    public void RenderPath(NpcPath path, int selectedWaypointIndex, SceneView sceneView, Action<int> waypointSelectedCallback)
    {
        Event e = Event.current;
        bool isHandleHot = GUIUtility.hotControl != 0;
        int directionHandleId = -1;

        HandleKeyboardInput(e, path, selectedWaypointIndex, sceneView);
        DrawPathLines(path, selectedWaypointIndex);
        HandleWaypointManipulation(e, path, selectedWaypointIndex, sceneView, out directionHandleId);

        DrawWaypointDirections(path, selectedWaypointIndex);
        DrawWaypointVisuals(path, selectedWaypointIndex);
        DrawWaypointLabels(path, selectedWaypointIndex);
    
        // Add this line to draw insertion points when a waypoint is selected
        if (selectedWaypointIndex >= 0)
            DrawWaypointInsertionPoints(path, selectedWaypointIndex);
        
        HandleWaypointSelection(e, path, selectedWaypointIndex, isHandleHot, waypointSelectedCallback);
    }
    
    private void HandleKeyboardInput(Event e, NpcPath path, int selectedWaypointIndex, SceneView sceneView)
    {
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F &&
            selectedWaypointIndex >= 0 && selectedWaypointIndex < path.Waypoints.Length)
        {
            Vector3 focusPoint = path.Waypoints[selectedWaypointIndex].Point;
            sceneView.Frame(new Bounds(focusPoint, Vector3.one * 3f), false);
            e.Use();
        }
    }

    private void DrawPathLines(NpcPath path, int selectedWaypointIndex)
    {
        if (path.Waypoints.Length < 2)
            return;

        // Draw regular path segments
        for (int i = 0; i < path.Waypoints.Length - 1; i++)
        {
            Vector3 startPoint = path.Waypoints[i].Point;
            Vector3 endPoint = path.Waypoints[i + 1].Point;
            Vector3 direction = (endPoint - startPoint).normalized;
            float distance = Vector3.Distance(startPoint, endPoint);

            // Draw main line
            Handles.color = _pathColor;
            Handles.DrawLine(startPoint, endPoint, 2f);

            // Draw direction indicators if the line doesn't connect to the selected waypoint
            if (path.IsLoop && distance > 0.1f && i != selectedWaypointIndex && (i + 1) != selectedWaypointIndex)
            {
                DrawLineDirectionIndicator(startPoint, direction, distance, _lineArrowColor);
            }
        }

        // Draw loop line if enabled
        if (path.IsLoop && path.Waypoints.Length > 2)
        {
            Vector3 lastPoint = path.Waypoints[^1].Point;
            Vector3 firstPoint = path.Waypoints[0].Point;
            float loopDistance = Vector3.Distance(lastPoint, firstPoint);
        
            Handles.color = _loopLineColor;
            Handles.DrawLine(lastPoint, firstPoint, 2f);

            // Only draw direction indicator if neither endpoint is selected
            if (loopDistance > 0.1f && selectedWaypointIndex != (path.Waypoints.Length - 1) && selectedWaypointIndex != 0)
            {
                Vector3 loopDirection = (firstPoint - lastPoint).normalized;
                DrawLineDirectionIndicator(lastPoint, loopDirection, loopDistance, _loopArrowColor);
            }
        }
    }

    private void DrawLineDirectionIndicator(Vector3 startPoint, Vector3 direction, float distance, Color arrowColor)
    {
        Vector3 midPoint = startPoint + direction * (distance * 0.5f);
        float arrowSize = Mathf.Min(GetLimitedHandleSize(midPoint) * 0.2f, MAX_ARROW_SIZE * 0.2f);

        Handles.color = arrowColor;
        Handles.DrawLine(midPoint, midPoint - direction * arrowSize + Vector3.Cross(Vector3.up, direction).normalized * arrowSize * 0.5f, 2f);
        Handles.DrawLine(midPoint, midPoint - direction * arrowSize - Vector3.Cross(Vector3.up, direction).normalized * arrowSize * 0.5f, 2f);
    }

    private void DrawLoopLine(Vector3 lastPoint, Vector3 firstPoint)
    {
        Vector3 direction = (firstPoint - lastPoint).normalized;
        float distance = Vector3.Distance(lastPoint, firstPoint);

        Handles.color = _loopLineColor;
        Handles.DrawLine(lastPoint, firstPoint, 2f);

        if (distance > 0.1f)
        {
            DrawLineDirectionIndicator(lastPoint, direction, distance, _loopArrowColor);
        }
    }

    private void DrawWaypointDirections(NpcPath path, int selectedWaypointIndex)
    {
        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            Waypoint waypoint = path.Waypoints[i];
            if (waypoint.HasStop && waypoint.HasDirection)
            {
                // Set color based on waypoint state
                if (i == selectedWaypointIndex)
                    Handles.color = _selectedColor;
                else if (waypoint.HasAnimation)
                    Handles.color = _animationColor;
                else
                    Handles.color = _stopColor;

                DrawDirectionArrow(waypoint.Point, waypoint.Direction.normalized);
            }
        }
    }

    private void DrawDirectionArrow(Vector3 position, Vector3 direction)
    {
        float handleSize = GetLimitedHandleSize(position);
        float arrowLength = Mathf.Min(handleSize, MAX_ARROW_SIZE);

        Handles.DrawLine(position, position + direction * arrowLength, 2f);

        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * Mathf.Min(handleSize * 0.2f, MAX_ARROW_SIZE * 0.2f);
        Vector3 arrowEnd = position + direction * arrowLength;
        Handles.DrawLine(arrowEnd, arrowEnd - direction * handleSize * 0.2f + right, 2f);
        Handles.DrawLine(arrowEnd, arrowEnd - direction * handleSize * 0.2f - right, 2f);
    }

    private void HandleWaypointManipulation(Event e, NpcPath path, int selectedWaypointIndex, SceneView sceneView, out int directionHandleId)
    {
        directionHandleId = -1;

        if (selectedWaypointIndex < 0 || selectedWaypointIndex >= path.Waypoints.Length)
            return;

        // Handle Shift+left click for surface snapping
        if (e.type == EventType.MouseDrag && e.shift && e.button == 0)
        {
            HandleSurfaceSnapping(e, path, selectedWaypointIndex, sceneView);
        }

        // Handle direction control
        if (path.Waypoints[selectedWaypointIndex].HasDirection &&
            path.Waypoints[selectedWaypointIndex].HasStop)
        {
            directionHandleId = HandleDirectionControl(path, selectedWaypointIndex);
        }

        // Only show position handle if we're not manipulating the direction handle
        bool isDirectionHandleActive = (GUIUtility.hotControl == directionHandleId);
        if (!isDirectionHandleActive)
        {
            HandlePositionControl(path, selectedWaypointIndex);
        }
    }

    private void HandleSurfaceSnapping(Event e, NpcPath path, int selectedWaypointIndex, SceneView sceneView)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Undo.RecordObject(path, "Snap Waypoint");
            var waypoints = path.Waypoints;
            waypoints[selectedWaypointIndex].Point = hit.point;
            path.Waypoints = waypoints;
            EditorUtility.SetDirty(path);
            e.Use();
            sceneView.Repaint();
        }
    }

    private int HandleDirectionControl(NpcPath path, int selectedWaypointIndex)
    {
        Handles.color = _directionHandleColor;
        Vector3 waypointPos = path.Waypoints[selectedWaypointIndex].Point;
        Vector3 direction = path.Waypoints[selectedWaypointIndex].Direction.normalized;
        float handleSize = GetLimitedHandleSize(waypointPos);
    
        // Apply maximum size limit to disc handle
        float discSize = Mathf.Min(handleSize, MAX_DISC_SIZE);

        float currentYRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion currentRotation = Quaternion.Euler(0, currentYRotation, 0);

        // Generate a unique control ID for the direction handle
        int directionHandleId = GUIUtility.GetControlID(FocusType.Passive);

        EditorGUI.BeginChangeCheck();
        // Use discSize instead of handleSize
        Quaternion newRotation = Handles.Disc(directionHandleId, currentRotation, waypointPos, Vector3.up, discSize, false, 10f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(path, "Rotate Waypoint Direction");

            Vector3 newDirection = newRotation * Vector3.forward;
            newDirection.y = 0;
            newDirection.Normalize();

            var waypoints = path.Waypoints;
            waypoints[selectedWaypointIndex].Direction = newDirection;
            path.Waypoints = waypoints;

            EditorUtility.SetDirty(path);
        }

        return directionHandleId;
    }

    private void HandlePositionControl(NpcPath path, int selectedWaypointIndex)
    {
        // Generate a control ID for the position handle
        int positionHandleId = GUIUtility.GetControlID(FocusType.Passive);
        
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(path.Waypoints[selectedWaypointIndex].Point, Quaternion.identity);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(path, "Move Waypoint");
            Waypoint[] waypoints = path.Waypoints;
            waypoints[selectedWaypointIndex].Point = newPosition;
            path.Waypoints = waypoints;
            EditorUtility.SetDirty(path);
        }
    }
    
    private void DrawWaypointVisuals(NpcPath path, int selectedWaypointIndex)
    {
        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            Waypoint waypoint = path.Waypoints[i];
            Vector3 waypointPos = waypoint.Point;
            float handleSize = GetLimitedHandleSize(waypointPos);
        
            // Use correct color based on waypoint state
            if (i == selectedWaypointIndex)
                Handles.color = _selectedColor;
            else if (waypoint.HasAnimation)
                Handles.color = _animationColor;
            else if(waypoint.HasStop)
                Handles.color = _stopColor;
            else
                Handles.color = _dotColor;
        
            // Use capped dot size
            // Reduce the multiplier for selected waypoints from 0.15f to 0.12f
            float dotSize = Mathf.Min((i == selectedWaypointIndex ? 0.08f : 0.1f) * handleSize, MAX_DOT_SIZE);

            // Draw the waypoint dot
            Handles.DotHandleCap(0, waypointPos, Quaternion.identity, dotSize, EventType.Repaint);

            // Draw dot
            Handles.DotHandleCap(
                0,
                waypointPos,
                Quaternion.identity,
                dotSize,
                EventType.Repaint
            );
        }
    }

    private void DrawWaypointLabels(NpcPath path, int selectedWaypointIndex)
    {
        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            Waypoint waypoint = path.Waypoints[i];
            Vector3 waypointPos = waypoint.Point;
            float handleSize = GetLimitedHandleSize(waypointPos);
            float fadeFactor = GetLabelFadeFactor(waypointPos);
        
            // Skip drawing if fully faded out
            if (fadeFactor <= 0) continue;

            // Draw stop time indicator
            if (waypoint.HasStop && waypoint.StopTime > 0)
            {
                Vector3 timePos = waypointPos + Vector3.up * handleSize * 0.5f;

                Color timeColor;
                if (i == selectedWaypointIndex)
                    timeColor = _selectedColor;
                else if (waypoint.HasAnimation)
                    timeColor = _animationColor;
                else
                    timeColor = _stopColor;
            
                // Apply fade factor to color
                timeColor.a *= fadeFactor;
                Handles.color = timeColor;

                string timeLabel = $"◷ {waypoint.StopTime:F1}s";
                Handles.Label(timePos, timeLabel);

                DrawClockVisualization(waypointPos, handleSize, waypoint.StopTime, fadeFactor);
            }

            // Draw animation type indicator
            if (waypoint.HasAnimation)
            {
                Vector3 animPos = waypointPos + Vector3.up * handleSize * (waypoint.HasStop ? 0.7f : 0.5f);
                Color animColor = (i == selectedWaypointIndex) ? _selectedColor : _animationColor;
            
                // Apply fade factor to color
                animColor.a *= fadeFactor;
                Handles.color = animColor;
            
                string animLabel = $"⚡ {waypoint.Animation}";
                Handles.Label(animPos, animLabel);
            }
        }
    }

    private void DrawClockVisualization(Vector3 position, float handleSize, float stopTime, float fadeFactor)
    {
        float limitedHandleSize = GetLimitedHandleSize(position);
        float circleRadius = Mathf.Min(limitedHandleSize * 0.3f, MAX_DOT_SIZE * 4f);
    
        // Apply fade factor to current color
        Color currentColor = Handles.color;
        currentColor.a *= fadeFactor;
        Handles.color = currentColor;
    
        Handles.DrawWireDisc(position, Vector3.up, circleRadius);

        float angle = 90 - (stopTime % 12) * 30;
        Vector3 hand = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            0,
            Mathf.Sin(angle * Mathf.Deg2Rad)
        ) * circleRadius * 0.8f;

        Handles.DrawLine(position, position + hand, 2f);
    }

    private void HandleWaypointSelection(Event e, NpcPath path, int selectedWaypointIndex, bool isHandleHot, Action<int> selectionCallback)
    {
        if (e.type != EventType.MouseDown || e.button != 0 || isHandleHot)
            return;

        bool clickedOnWaypoint = false;

        // Check if clicked on any waypoint
        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            Vector3 waypointPos = path.Waypoints[i].Point;
            if (Vector3.Distance(HandleUtility.WorldToGUIPoint(waypointPos), e.mousePosition) < 10f)
            {
                clickedOnWaypoint = true;
                selectionCallback?.Invoke(i);
                e.Use();
                SceneView.RepaintAll();
                break;
            }
        }

        if (!clickedOnWaypoint)
        {
            selectionCallback?.Invoke(-1);
            e.Use();
            SceneView.RepaintAll();
        }
    }
}