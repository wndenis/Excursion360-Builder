﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

namespace TourConfig
{
    [Serializable]
    class SphericalDirection
    {
        public float f = 0.0f;
        public float o = 0.0f;
    }

    [Serializable]
    class StateLink : SphericalDirection
    {
        public string id = "state";
    }

    [Serializable]
    class State
    {
        public string id = "state";
        public string title = "";
        public string url = "";
        public string type = "image";
        public SphericalDirection viewDirection = new SphericalDirection();
        public Vector3 rotation = Vector3.zero;

        public List<StateLink> links = new List<StateLink>();
    }

    [Serializable]
    class Tour
    {
        public string firstStateId = "state";
        public List<State> states = new List<State>();
    }
}

[ExecuteInEditMode]
public class StateExporter : EditorWindow
{
    private UnityEngine.Object _initialStateObject;

    [MenuItem("VR Tour/Export")]
    static void ShowWindow()
    {
        var window = GetWindowWithRect<StateExporter>(new Rect(0, 0, 250, 100));
        window.Show();
    }

    void ExportTour(State initialState)
    {
        State[] states = FindObjectsOfType<State>();

        if (states.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "There is no states on this scene to export!", "Ok");
            return;
        }

        // Input path
        string path = EditorUtility.OpenFolderPanel("Select folder for tour", "", "");
        if (path.Length == 0)
        {
            //EditorUtility.DisplayDialog("Error", "Invalid path!", "Ok");
            return;
        }

        // Pre process states
        Dictionary<string, string> titleToId = new Dictionary<string, string>();

        UpdateProcess(0, states.Length);

        TourConfig.Tour tour = new TourConfig.Tour();

        for (int i = 0; i < states.Length; ++i)
        {
            var state = states[i];

            TourConfig.State exportedState = new TourConfig.State
            {
                id = "state_" + i,
                title = state.title,
                rotation = state.transform.rotation.eulerAngles
            };

            tour.states.Add(exportedState);
            titleToId.Add(state.title, exportedState.id);

            string sourcePath = AssetDatabase.GetAssetPath(state.panoramaTexture);

            exportedState.url = "state_" + i + Path.GetExtension(sourcePath);

            File.Copy(sourcePath, path + "/" + exportedState.url);

            UpdateProcess(i + 1, states.Length);
        }

        titleToId.TryGetValue(initialState.title, out tour.firstStateId);

        // Process links
        for (int i = 0; i < states.Length; ++i)
        {
            var state = states[i];
            var exportedState = tour.states[i];

            Connection[] connections = state.gameObject.GetComponents<Connection>();
            if (connections == null)
            {
                continue;
            }

            foreach (var connection in connections)
            {
                if (!titleToId.TryGetValue(connection.state.title, out string otherId))
                {
                    continue;
                }

                Vector3 direction = (connection.orientation * Vector3.forward).normalized;

                exportedState.links.Add(new TourConfig.StateLink()
                {
                    id = otherId,
                    o = Mathf.Acos(direction.z),
                    f = Mathf.Atan2(direction.y, direction.x)
                });
            }
        }

        File.WriteAllText(path + "/tour.json", JsonUtility.ToJson(tour));

        EditorUtility.ClearProgressBar();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Initial state");
        _initialStateObject = EditorGUILayout.ObjectField(_initialStateObject, typeof(State), true);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Export"))
        {
            if (_initialStateObject == null) {
                ShowNotification(new GUIContent("No initial state selected"));
            }
            else
            {
                ExportTour(_initialStateObject as State);
            }
        }
    }

    static void UpdateProcess(int current, int target)
    {
        EditorUtility.DisplayProgressBar("Exporting", "Please wait...", (float)current/ (float)target);
    }
}

#endif
