using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;

public class StateEditorWindow : EditorWindow
{
    private List<State> _selectedStates = new List<State>();

    private bool _connectionsEditMode = false;

    private TextureSourceEditor _textureSourceEditor = new TextureSourceEditor();

    private Vector2 _connectionsListScroll = Vector2.zero;

    private readonly GroupConnectionEditor groupConnectionEditor = new GroupConnectionEditor();
    private readonly ContentEditor contentEditor = new ContentEditor();

    private bool connectionsOpened = true;
    private bool groupConnectionsOpened = true;
    private bool itemsOpened = true;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {

        // Draw pages
        if (_selectedStates.Count == 0 || _selectedStates.Count > 2)
        {
            DrawIdlePageGUI();
        }
        else if (_selectedStates.Count == 1)
        {
            DrawStateEditPageGUI();
        }
        else
        {
            DrawConnectionEditGUI();
        }

        // Force update scene on change (for savig)
        if (GUI.changed)
        {
            foreach (var selection in _selectedStates)
            {
                EditorUtility.SetDirty(selection);
                EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);
            }
        }

        // Draw state creation button
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create new state", GUILayout.Height(50)))
        {
            var newObject = PrefabUtility.InstantiatePrefab(TourEditor.StatePrefab) as GameObject;
            SelectObject(newObject);
            Undo.RegisterCreatedObjectUndo(newObject, "Undo state creation");
        }

        EditorGUILayout.Space();

        // Force redraw all
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        _selectedStates = GetSlectedStates();
    }

    private void DrawIdlePageGUI()
    {
        GUILayout.Label("Select one state for editing it", EditorStyles.boldLabel);
        GUILayout.Label("Select two states for editing connections", EditorStyles.boldLabel);
    }

    private void DrawStateEditPageGUI()
    {
        if (EditorApplication.isPlaying)
            return;

        var state = _selectedStates[0];

        if (_connectionsEditMode)
            TourEditor.StateGraphRenderer.targetState = state;

        // Draw title edit field
        GUILayout.Label("State title: ", EditorStyles.boldLabel);
        state.title = EditorGUILayout.TextField(state.title);
        EditorGUILayout.Space();

        // Draw panorama texture edit field
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Panorama: ", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Change texture source"))
            _textureSourceEditor.ShowContextMenu(state);

        EditorGUILayout.EndHorizontal();

        _textureSourceEditor.Draw(state);

        EditorGUILayout.Space();

        // Draw panorama preview

        var previewTexture = state.GetComponent<TextureSource>().LoadedTexture;
        if (previewTexture == null)
        {
            previewTexture = EditorGUIUtility.whiteTexture;
        }
        EditorGUI.DrawPreviewTexture(EditorGUILayout.GetControlRect(false, 150.0f), previewTexture, null, ScaleMode.ScaleToFit);


        EditorGUILayout.Space();

        GUILayout.Label("Actions: ", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Focus camera", GUILayout.Height(50)))
        {
            FocusCamera(state.gameObject);
            SelectObject(state.gameObject);
        }



        GUILayout.EndHorizontal();

        // Draw edit mode toggle
        GUIStyle editModeButtonStyle = _connectionsEditMode ? Styles.ToggleButtonStyleToggled : Styles.ToggleButtonStyleNormal;
        if (GUILayout.Button("Edit mode", editModeButtonStyle, GUILayout.Height(50)))
        {
            if (_connectionsEditMode)
            {
                TourEditor.StateGraphRenderer.targetState = null;
            }
            else
            {
                TourEditor.StateGraphRenderer.targetState = state;
            }

            _connectionsEditMode = !_connectionsEditMode;
        }

        EditorGUILayout.Space();

        // Draw connections list
        connectionsOpened = EditorGUILayout.Foldout(connectionsOpened, "Connections: ", true);
        if (connectionsOpened)
        {
            itemsOpened = false;

            _connectionsListScroll = EditorGUILayout.BeginScrollView(_connectionsListScroll);

            var connections = state.GetComponents<Connection>();

            foreach (var connection in connections)
            {
                if (connection.Destination == null)
                    continue;
                GUILayout.Label(connection.Destination.title, EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("move to"))
                {
                    FocusCamera(connection.Destination.gameObject);
                    SelectObject(connection.Destination.gameObject);
                }

                var buttonStyle = Styles.ToggleButtonStyleNormal;
                if (StateItemPlaceEditor.EditableItem == connection)
                    buttonStyle = Styles.ToggleButtonStyleToggled;

                if (GUILayout.Button("edit", buttonStyle))
                {
                    if (StateItemPlaceEditor.EditableItem == connection)
                    {
                        StateItemPlaceEditor.CleadEditing();
                    }
                    else
                    {
                        StateItemPlaceEditor.EnableEditing(state, connection, Color.green);
                    }
                }
                EditorGUILayout.EndHorizontal();

                var schemes = Tour.Instance.colorSchemes;
                var schemeNames = schemes.Select(s => s.name).ToArray();
                connection.colorScheme = EditorGUILayout.Popup(new GUIContent("Color scheme"), connection.colorScheme, schemeNames.Select(sn => new GUIContent(sn)).ToArray());
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        groupConnectionsOpened = EditorGUILayout.Foldout(groupConnectionsOpened, "Group connections", true);
        if (groupConnectionsOpened)
        {
            groupConnectionEditor.Draw(state);
        }

        // TODO Content draw
        //itemsOpened = EditorGUILayout.Foldout(itemsOpened, "Items: ", true);
        //if (itemsOpened)
        //{
        //    connectionsOpened = false;
        //    _connectionsEditMode = false;
        //    TourEditor.StateGraphRenderer.targetState = null;
        //    itemsEditor.Draw(state);
        //}

        EditorGUILayout.Space();

        GUILayout.FlexibleSpace();
    }

    void DrawConnectionEditGUI()
    {
        GUILayout.FlexibleSpace();

        foreach (var item in _selectedStates)
        {
            GUILayout.Label(item.title);
        }

        if (GUILayout.Button("Toggle connection", GUILayout.Height(50)))
        {
            CreateConnection(_selectedStates[0], _selectedStates[1]);
        }
    }

    public static Vector3 ReflectDirection(Vector3 inDirection, Vector3 normal)
    {
        var length = inDirection.magnitude;
        inDirection.Normalize();
        normal.Normalize();

        return (2 * (Vector3.Dot(inDirection, normal) * normal) - inDirection).normalized * -length;
    }

    public void OnInspectorUpdate()
    {
        Repaint();
    }

    public static void FocusCamera(GameObject obj)
    {
        var sceneView = SceneView.lastActiveSceneView;

        var offset = sceneView.pivot - sceneView.camera.transform.position;
        var cameraDistance = offset.magnitude;
        sceneView.pivot = obj.transform.position + sceneView.camera.transform.forward * cameraDistance;
    }

    private void SelectObject(GameObject obj)
    {
        Selection.objects = new Object[] { obj };
    }

    private List<State> GetSlectedStates()
    {
        var result = new List<State>();

        foreach (var selection in Selection.gameObjects)
        {
            var state = selection.GetComponent<State>();
            if (state != null)
                result.Add(state);
        }

        return result;
    }

    private void CreateConnection(State firstState, State secondState)
    {
        var firstStateConnections = firstState.GetComponents<Connection>();
        var seconsStateConnections = secondState.GetComponents<Connection>();

        Connection connectionFirst = null;
        foreach (var connecton in firstStateConnections)
        {
            if (connecton.Destination == null)
                continue;

            if (connecton.Destination == secondState)
                connectionFirst = connecton;
        }

        Connection connectionSecond = null;
        foreach (var connecton in seconsStateConnections)
        {
            if (connecton.Destination == null)
                continue;

            if (connecton.Destination == firstState)
                connectionSecond = connecton;
        }

        if (connectionFirst == null && connectionSecond == null)
        {
            var directionToSecond = secondState.transform.position - firstState.transform.position;

            connectionFirst = Undo.AddComponent<Connection>(firstState.gameObject);
            connectionSecond = Undo.AddComponent<Connection>(secondState.gameObject);

            connectionFirst.Destination = secondState;
            connectionFirst.orientation = Quaternion.FromToRotation(Vector3.forward, directionToSecond);

            connectionSecond.Destination = firstState;
            connectionSecond.orientation = Quaternion.FromToRotation(Vector3.forward, -directionToSecond);
        }
        else
        {
            if (connectionFirst != null)
                Undo.DestroyObjectImmediate(connectionFirst);

            if (connectionSecond != null)
                Undo.DestroyObjectImmediate(connectionSecond);
        }
    }
}

#endif
