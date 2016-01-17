using System.IO;
using System.Linq;
using System.Text;
using DialogueEditor;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using UnityEditor;

public class DialogueGraph : EditorWindow
{
    static readonly Color shadowCol = new Color(0, 0, 0, 0.06f);
    static readonly Vector2 dialogueElementSize = new Vector2(250f, 250f);

    [SerializeField]
    TextAsset dialogueFile;

    Dialogue dialogue;

    Dictionary<int, Question> questions;
    Dictionary<int, Clip> clips;

    string currentLanguage;
    string[] languages;
    
    ReactiveDictionary<int, Rect> questionRects;
    ReactiveDictionary<int, Rect> clipRects;

            
    [MenuItem("Window/Dialogue Graph")]
    static void ShowWindow()
    {
        var editor = GetWindow<DialogueGraph>();
        editor.Init();
    }

    void Init()
    {
        if(dialogueFile == null) {
            dialogueFile = FindObjectOfType<DialogueLoader>().dialogueJson;
        }
        else {
            Debug.Log("dialogue file already referenced on init!");
        }

        dialogue = JsonConvert.DeserializeObject<Dialogue>(dialogueFile.text);

        questions = dialogue.questions.ToDictionary(q => q.id);
        clips = dialogue.clips.ToDictionary(c => c.id);

//        questionRects = dialogue.questions.ToDictionary(q => q.id, q => new Rect(q.Position, dialogueElementSize)).ToReactiveDictionary();
        questionRects = dialogue.questions.ToDictionary(q => q.id, q => new Rect(new Vector2(Random.Range(0f, 1300f), Random.Range(0f, 900f)), dialogueElementSize)).ToReactiveDictionary();
//        clipRects = dialogue.clips.ToDictionary(c => c.id, c => new Rect(c.Position, dialogueElementSize)).ToReactiveDictionary();
        clipRects = dialogue.clips.ToDictionary(c => c.id, c => new Rect(new Vector2(Random.Range(0f, 1300f), Random.Range(0f, 900f)), dialogueElementSize)).ToReactiveDictionary();

        languages = dialogue.questions.SelectMany(q => q.choices).SelectMany(c => c.strings.Keys).Distinct().ToArray();
        currentLanguage = languages.First(l => l != "*");
    }

    void OnGUI()
    {
        if(dialogueFile == null) {
            Debug.Log("df null");
            Init();
        }
        if(dialogue == null) {
            Debug.Log("dia null");
            Init();
        }

//        DrawNodeCurve(window1, window2); // Here the curve is drawn under the windows

        BeginWindows();
        foreach(var q in dialogue.questions) {
            questionRects[q.Id] = GUI.Window(q.Id, questionRects[q.Id], DrawQuestionWindow, "Question " + q.Id);
        }

        foreach(var c in dialogue.clips) {
            clipRects[c.Id] = GUI.Window(int.MaxValue - c.Id, clipRects[c.Id], DrawNodeWindow, "Clip " + c.Id);
        }
        EndWindows();

        using(new GUILayout.HorizontalScope(EditorStyles.toolbar)) {
            if(GUILayout.Button(dialogueFile.name, EditorStyles.toolbarButton)) {
                Debug.Log("Clicked");
            }

            currentLanguage = languages[EditorGUILayout.Popup(Array.IndexOf(languages, currentLanguage), languages)];

            GUILayout.Space(15f);
            if(GUILayout.Button("Save", EditorStyles.toolbarButton)) {
                SaveGraph();
            }
            GUILayout.FlexibleSpace();
        }
    }

    void DrawQuestionWindow(int id)
    {
        using(new GUILayout.VerticalScope()) {
            foreach(var c in questions[id].choices) {
                string choice = "";
                if(c.strings.TryGetValue(currentLanguage, out choice)) {
                    EditorGUILayout.TextField(choice);
                }
                else {
                    EditorGUILayout.LabelField("No choice for this language");
                }
            }
        }
        GUI.DragWindow();
    }


    void DrawNodeWindow(int id)
    {
        using(new GUILayout.VerticalScope()) {
            
        }
        GUI.DragWindow();
    }

    void DrawNodeCurve(Rect start, Rect end)
    {
        var startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
        var endPos = new Vector3(end.x, end.y + end.height / 2, 0);
        var startTan = startPos + Vector3.right * 50;
        var endTan = endPos + Vector3.left * 50;

        // Draw a shadow
        for(int i = 0; i < 3; i++) {
            Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
        }
        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 1);
    }

    void SaveGraph()
    {
        var output = Newtonsoft.Json.JsonConvert.SerializeObject(dialogue, Formatting.Indented);
        var path = AssetDatabase.GetAssetPath(dialogueFile);
        File.WriteAllText(path, output);
        Debug.Log("Saved!");
    }
}

