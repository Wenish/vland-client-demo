using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NPCBehaviour.EditorTools
{
    /// <summary>
    /// Lightweight editor window to author behaviour profiles, states, transitions, and conditions.
    /// Focuses on clarity and keeps all runtime logic inside the existing ScriptableObjects.
    /// </summary>
    public class BehaviourAuthoringWindow : EditorWindow
    {
        private readonly List<BehaviourProfile> _profiles = new List<BehaviourProfile>();
        private BehaviourProfile _selectedProfile;
        private BehaviourState _selectedState;

        private Editor _profileEditor;
        private Editor _stateEditor;

        private Vector2 _profileListScroll;
        private Vector2 _detailScroll;
        private Vector2 _flowScroll;

        private string _profileSearch = string.Empty;

        private const string DefaultFolder = "Assets/Scripts/NPCBehaviour";

        [MenuItem("Tools/Shadow Infection/Behaviour Authoring", priority = 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<BehaviourAuthoringWindow>(false, "Behaviour Authoring", true);
            window.minSize = new Vector2(900f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshProfiles();
        }

        private void OnDisable()
        {
            if (_profileEditor != null) DestroyImmediate(_profileEditor);
            if (_stateEditor != null) DestroyImmediate(_stateEditor);
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawProfileList();
                DrawProfileDetail();
            }
        }

        #region Toolbar

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    RefreshProfiles();
                }

                GUILayout.Space(6f);

                if (GUILayout.Button("New Profile", EditorStyles.toolbarButton))
                {
                    CreateAsset(typeof(BehaviourProfile), "NewBehaviourProfile.asset", obj =>
                    {
                        SelectProfile(obj as BehaviourProfile);
                    });
                }

                if (GUILayout.Button("New State", EditorStyles.toolbarButton))
                {
                    ShowCreateMenuForType(typeof(BehaviourState));
                }

                if (GUILayout.Button("New Transition", EditorStyles.toolbarButton))
                {
                    CreateAsset(typeof(BehaviourTransition), "NewTransition.asset");
                }

                if (GUILayout.Button("New Condition", EditorStyles.toolbarButton))
                {
                    ShowCreateMenuForType(typeof(BehaviourCondition));
                }

                GUILayout.FlexibleSpace();

                _profileSearch = GUILayout.TextField(_profileSearch, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
            }
        }

        #endregion

        #region Profile List

        private void DrawProfileList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
            {
                EditorGUILayout.LabelField("Behaviour Profiles", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Pick a profile to edit its states, transitions, and conditions.", MessageType.None);

                _profileListScroll = EditorGUILayout.BeginScrollView(_profileListScroll, GUILayout.ExpandHeight(true));
                foreach (var profile in _profiles)
                {
                    if (!PassesSearch(profile)) continue;

                    bool isSelected = profile == _selectedProfile;
                    using (new EditorGUILayout.HorizontalScope(isSelected ? "RL Element" : GUIStyle.none))
                    {
                        if (GUILayout.Button(profile.name, isSelected ? EditorStyles.boldLabel : EditorStyles.label))
                        {
                            SelectProfile(profile);
                        }

                        if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                        {
                            EditorGUIUtility.PingObject(profile);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private bool PassesSearch(BehaviourProfile profile)
        {
            if (string.IsNullOrWhiteSpace(_profileSearch)) return true;
            string term = _profileSearch.Trim();
            return profile != null && (profile.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       (!string.IsNullOrEmpty(profile.profileName) && profile.profileName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private void SelectProfile(BehaviourProfile profile)
        {
            _selectedProfile = profile;
            _selectedState = profile != null ? profile.initialState : null;
            if (_profileEditor != null) DestroyImmediate(_profileEditor);
            if (_stateEditor != null) DestroyImmediate(_stateEditor);
        }

        private void RefreshProfiles()
        {
            _profiles.Clear();
            string[] guids = AssetDatabase.FindAssets("t:BehaviourProfile");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<BehaviourProfile>(path);
                if (profile != null)
                {
                    _profiles.Add(profile);
                }
            }
            _profiles.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            if (_selectedProfile != null && !_profiles.Contains(_selectedProfile))
            {
                SelectProfile(null);
            }
        }

        #endregion

        #region Profile Detail

        private void DrawProfileDetail()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                if (_selectedProfile == null)
                {
                    EditorGUILayout.HelpBox("Select or create a behaviour profile to start editing.", MessageType.Info);
                    return;
                }

                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                DrawProfileHeader();
                EditorGUILayout.Space();
                DrawStateList();
                EditorGUILayout.Space();
                DrawStateInspector();
                EditorGUILayout.Space();
                DrawTransitions();
                EditorGUILayout.Space();
                DrawFlowOverview();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawProfileHeader()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Asset", _selectedProfile, typeof(BehaviourProfile), false);

                SerializedObject so = new SerializedObject(_selectedProfile);
                so.Update();

                EditorGUILayout.PropertyField(so.FindProperty("profileName"));
                EditorGUILayout.PropertyField(so.FindProperty("profileDescription"));
                EditorGUILayout.PropertyField(so.FindProperty("initialState"));
                EditorGUILayout.PropertyField(so.FindProperty("availableStates"), new GUIContent("Available States"), true);
                EditorGUILayout.PropertyField(so.FindProperty("globalTransitions"), new GUIContent("Global Transitions"), true);

                if (so.ApplyModifiedProperties())
                {
                    if (_selectedProfile.initialState != null)
                    {
                        _selectedState = _selectedProfile.initialState;
                    }
                }
            }
        }

        private void DrawStateList()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("States in Profile", EditorStyles.boldLabel);

                var states = _selectedProfile.availableStates ?? new List<BehaviourState>();
                if (states.Count == 0)
                {
                    EditorGUILayout.HelpBox("No states assigned. Add states to the profile to start building behaviour.", MessageType.Warning);
                    return;
                }

                foreach (var state in states)
                {
                    if (state == null) continue;

                    bool isSelected = state == _selectedState;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(isSelected, FormatStateLabel(state), "Button"))
                        {
                            _selectedState = state;
                            if (_stateEditor != null) DestroyImmediate(_stateEditor);
                        }

                        if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                        {
                            EditorGUIUtility.PingObject(state);
                        }
                    }
                }
            }
        }

        private void DrawStateInspector()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("State Properties", EditorStyles.boldLabel);

                if (_selectedState == null)
                {
                    EditorGUILayout.HelpBox("Select a state from the profile to edit its properties.", MessageType.Info);
                    return;
                }

                EditorGUILayout.ObjectField("Asset", _selectedState, typeof(BehaviourState), false);

                Editor.CreateCachedEditor(_selectedState, null, ref _stateEditor);
                _stateEditor.OnInspectorGUI();
            }
        }

        private void DrawTransitions()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

                SerializedObject profileSO = new SerializedObject(_selectedProfile);
                SerializedProperty globalTransitions = profileSO.FindProperty("globalTransitions");

                EditorGUILayout.LabelField("Global Transitions (any state)", EditorStyles.miniBoldLabel);
                DrawTransitionList(globalTransitions);

                if (_selectedState == null)
                {
                    EditorGUILayout.HelpBox("Select a state to view its transitions.", MessageType.Info);
                    return;
                }

                var stateSO = new SerializedObject(_selectedState);
                SerializedProperty stateTransitions = stateSO.FindProperty("transitions");

                if (stateTransitions != null)
                {
                    EditorGUILayout.LabelField($"{_selectedState.name} Transitions", EditorStyles.miniBoldLabel);
                    DrawTransitionList(stateTransitions);
                }
                else
                {
                    EditorGUILayout.HelpBox("This state does not expose a 'transitions' list. Add one to enable editor support.", MessageType.None);
                }
            }
        }

        private void DrawTransitionList(SerializedProperty transitionsProp)
        {
            if (transitionsProp == null)
            {
                EditorGUILayout.HelpBox("No transition list found.", MessageType.Info);
                return;
            }

            transitionsProp.serializedObject.Update();

            for (int i = 0; i < transitionsProp.arraySize; i++)
            {
                SerializedProperty element = transitionsProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.PropertyField(element, new GUIContent($"Transition {i + 1}"));

                    if (element.objectReferenceValue is BehaviourTransition transition)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            SerializedObject transitionSO = new SerializedObject(transition);
                            transitionSO.Update();
                            EditorGUILayout.PropertyField(transitionSO.FindProperty("targetState"));
                            EditorGUILayout.PropertyField(transitionSO.FindProperty("priority"));
                            EditorGUILayout.PropertyField(transitionSO.FindProperty("conditions"), true);
                            transitionSO.ApplyModifiedProperties();
                        }
                    }
                }
            }

            if (GUILayout.Button("Add Transition"))
            {
                transitionsProp.InsertArrayElementAtIndex(transitionsProp.arraySize);
            }

            transitionsProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawFlowOverview()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Flow Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Quick view of how states connect. Global transitions are marked as ANY.", MessageType.None);

                _flowScroll = EditorGUILayout.BeginScrollView(_flowScroll, GUILayout.MinHeight(140f));

                // Global transitions
                if (_selectedProfile.globalTransitions != null && _selectedProfile.globalTransitions.Count > 0)
                {
                    EditorGUILayout.LabelField("ANY", EditorStyles.miniBoldLabel);
                    foreach (var transition in _selectedProfile.globalTransitions)
                    {
                        DrawFlowLine("ANY", transition);
                    }
                }

                // State-specific transitions
                foreach (var state in _selectedProfile.availableStates)
                {
                    if (state == null) continue;
                    var transitions = GetTransitionsFromState(state);
                    if (transitions == null || transitions.Count == 0) continue;

                    string stateLabel = FormatStateLabel(state);
                    foreach (var transition in transitions)
                    {
                        DrawFlowLine(stateLabel, transition);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawFlowLine(string fromState, BehaviourTransition transition)
        {
            if (transition == null) return;

            string target = transition.targetState != null ? FormatStateLabel(transition.targetState) : "(none)";
            int conditionCount = transition.conditions != null ? transition.conditions.Count : 0;
            string line = $"{fromState}  →  {target}  [priority {transition.priority}, {conditionCount} condition{(conditionCount == 1 ? string.Empty : "s")}]";
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        }

        #endregion

        #region Utilities

        private void CreateAsset(Type type, string suggestedName, Action<UnityEngine.Object> onCreated = null)
        {
            EnsureFolderExists(DefaultFolder);

            string path = EditorUtility.SaveFilePanelInProject("Create Asset", suggestedName, "asset", "Choose location for the new asset", DefaultFolder);
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);
            onCreated?.Invoke(asset);
            RefreshProfiles();
        }

        private void ShowCreateMenuForType(Type baseType)
        {
            var menu = new GenericMenu();
            var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ScriptableObject).IsAssignableFrom(t))
                .OrderBy(t => t.Name);

            foreach (var type in derivedTypes)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    CreateAsset(type, $"New{type.Name}.asset");
                });
            }

            if (!derivedTypes.Any())
            {
                menu.AddDisabledItem(new GUIContent($"No {baseType.Name} types found"));
            }

            menu.ShowAsContext();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        private static List<BehaviourTransition> GetTransitionsFromState(BehaviourState state)
        {
            if (state == null) return null;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo field = state.GetType().GetField("transitions", flags);
            if (field != null && typeof(List<BehaviourTransition>).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(state) as List<BehaviourTransition>;
            }
            return null;
        }

        private static string FormatStateLabel(BehaviourState state)
        {
            if (state == null) return "(null)";
            if (!string.IsNullOrEmpty(state.stateId)) return state.stateId;
            return state.name;
        }

        #endregion
    }
}
