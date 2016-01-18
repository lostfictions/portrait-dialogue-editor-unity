using System.IO;
using System.Linq;
using System.Text;
using DialogueEditor;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using UnityEditor;

public class DialogueGraph : EditorWindow
{
    static readonly Color shadowCol = new Color(0, 0, 0, 0.06f);
    static readonly Vector2 dialogueElementSize = new Vector2(200f, 120f);


    Vector2 scrollPosition;


    Texture2D background;

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


        //make sure clips and questions don't have the same ids, so we can use a single identifier for both
        var usedIds = new HashSet<int>();
        foreach(var q in dialogue.questions) {
            usedIds.Add(q.id);
        }
        foreach(var c in dialogue.clips) {
            if(usedIds.Contains(c.id)) {
                int newId = 0;
                while(usedIds.Contains(newId)) {
                    newId++;
                }
                foreach(var q in dialogue.questions) {
                    foreach(var ch in q.choices) {
                        if(ch.answer == c.id) {
                            ch.answer = newId;
                        }
                    }
                }
                foreach(var c2 in dialogue.clips) {
                    var next = c2.next.Split(':');
                    if(next[0] == "clip") {
                        int id;
                        if(int.TryParse(next[1], out id)) {
                            if(id == c.id) {
                                c2.next = "clip:" + newId;
                            }
                        }
                    }
                }
                c.id = newId;
                usedIds.Add(newId);
            }
        }
        
        
        questions = dialogue.questions.ToDictionary(q => q.id);
        clips = dialogue.clips.ToDictionary(c => c.id);


        var allDialogueElements = dialogue.questions.Concat<IDialogueElement>(dialogue.clips).ToDictionary(de => de.Id);

//        Debug.Log(allDialogueElements.Keys.Log(", "));

        var unexplored = new Dictionary<int, IDialogueElement>(allDialogueElements);
        var explored = new HashSet<int>();
        
        var elementsToExplore = new Queue<int>(new[] { 0 });
        int rank = 0;
        while(unexplored.Count > 0) {
            IDialogueElement de;
            if(elementsToExplore.Any()) {
                if(!unexplored.TryGetValue(elementsToExplore.Dequeue(), out de)) {
                    continue;
                }
            }
            else {
                de = unexplored.First().Value;
//                Debug.Log("grabbing from unexplored... " + de.Id);
                if(de.Position.sqrMagnitude < 1f) {
//                    Debug.Log("setting position... " + de.Id);
                    allDialogueElements[de.Id].Position = new Vector2(rank * (dialogueElementSize.x + 10f) + 10f, 10f);
                    rank++;
                }
            }

            unexplored.Remove(de.Id);
            explored.Add(de.Id);

            var q = de as Question;
            if(q != null) {
                for(int i = 0; i < q.choices.Length; i++) {
                    var nextId = q.choices[i].answer;
                    if(!explored.Contains(nextId)) {
                        elementsToExplore.Enqueue(nextId);
                        allDialogueElements[nextId].Position = new Vector2(rank * (dialogueElementSize.x + 10f) + 10f, i * (dialogueElementSize.y + 10f) + 10f);
                    }
                }
            }
            else {
                var c = (Clip)de;
                var next = c.next.Split(':');

                if(next[0] == "question") {
                    int nextId;
                    if(int.TryParse(next[1], out nextId)) {
                        if(!explored.Contains(nextId)) {
                            elementsToExplore.Enqueue(nextId);
                            allDialogueElements[nextId].Position = new Vector2(rank * (dialogueElementSize.x + 10f) + 10f, 10f);
                        }
                    }
                }
                else if(next[0] == "clip") {
                    int nextId;
                    if(int.TryParse(next[1], out nextId)) {
                        if(!explored.Contains(nextId)) {
                            elementsToExplore.Enqueue(nextId);
                            allDialogueElements[nextId].Position = new Vector2(rank * (dialogueElementSize.x + 10f) + 10f, 10f);
                        }
                    }
                }
            }
            rank++;
        }

        questionRects = dialogue.questions.ToDictionary(q => q.id, q => new Rect(q.Position, dialogueElementSize)).ToReactiveDictionary();
        clipRects = dialogue.clips.ToDictionary(c => c.id, c => new Rect(c.Position, dialogueElementSize)).ToReactiveDictionary();

//        questionRects = dialogue.questions.ToDictionary(q => q.id, q => new Rect(new Vector2(q.id % 30 * 250f, q.id / 30 * 300f + 100f), dialogueElementSize)).ToReactiveDictionary();
//        clipRects = dialogue.clips.ToDictionary(c => c.id, c => new Rect(new Vector2(c.id % 30 * 250f + 25f, c.id / 30 * 300f + 190f), dialogueElementSize)).ToReactiveDictionary();

        languages = dialogue.questions.SelectMany(q => q.choices).SelectMany(c => c.strings.Keys).Distinct().ToArray();
        currentLanguage = languages.First(l => l != "*");

        background = Resources.Load<Texture2D>("background");
        Assert.IsNotNull(background);
    }

    void OnGUI()
    {
        if(dialogueFile == null) {
//            Debug.Log("df null");
            Init();
        }
        if(dialogue == null) {
//            Debug.Log("dia null");
            Init();
        }

        if(questions == null) {
//            Debug.Log("qs null");
            Init();
        }
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
            if(GUILayout.Button("Current dialogue: " + dialogueFile.name, EditorStyles.toolbarButton, GUILayout.MinWidth(120f))) {
                Debug.Log("Clicked");
            }

            GUILayout.Space(5f);

            currentLanguage = languages[EditorGUILayout.Popup(Array.IndexOf(languages, currentLanguage), languages, EditorStyles.toolbarPopup, GUILayout.MaxWidth(40f))];

            GUILayout.Space(5f);
            
            if(GUILayout.Button("Save", EditorStyles.toolbarButton)) {
                SaveGraph();
            }
            GUILayout.FlexibleSpace();
        }

        var scrollViewLayoutRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        var rects = questionRects.Values.Concat(clipRects.Values).ToArray();
        var maxX = Mathf.Max(rects.Max(rect => rect.x + rect.width), scrollViewLayoutRect.width);
        var maxY = Mathf.Max(rects.Max(rect => rect.y + rect.height), scrollViewLayoutRect.height);

        var scrollViewInnerRect = new Rect(0, 0, maxX, maxY);
        try {
            using(var sv = new GUI.ScrollViewScope(scrollViewLayoutRect, scrollPosition, scrollViewInnerRect)) {
                GUI.DrawTextureWithTexCoords(scrollViewInnerRect, background, new Rect(0, 0, maxX / background.width, maxY / background.height));

                foreach(var q in dialogue.questions) {
                    foreach(var c in q.choices) {
                        DrawNodeCurve(questionRects[q.id], clipRects[c.answer]); // Here the curve is drawn under the windows
                    }
                }
                foreach(var c in dialogue.clips) {
                    var next = c.next.Split(':');

                    Rect? targetRect = null;

                    if(next[0] == "question") {
                        int id;
                        if(int.TryParse(next[1], out id)) {
                            targetRect = questionRects[id];
                        }
                    }
                    else if(next[0] == "clip") {
                        int id;
                        if(int.TryParse(next[1], out id)) {
                            targetRect = clipRects[id];
                        }
                    }

                    if(targetRect.HasValue) {
                        DrawNodeCurve(clipRects[c.id], targetRect.Value);
                    }
                }

                BeginWindows();
                foreach(var q in dialogue.questions) {
                    var newRect = GUI.Window(q.Id, questionRects[q.Id], DrawQuestionWindow, "Question " + q.Id);
                    if(newRect != questionRects[q.id]) {
                        newRect.x = Mathf.Max(0, newRect.x);
                        newRect.y = Mathf.Max(0, newRect.y);
                        questionRects[q.id] = newRect;
                    }
                }
                foreach(var c in dialogue.clips) {
                    var newRect = GUI.Window(c.Id, clipRects[c.Id], DrawClipWindow, "Clip " + c.Id);
                    if(newRect != clipRects[c.id]) {
                        newRect.x = Mathf.Max(0, newRect.x);
                        newRect.y = Mathf.Max(0, newRect.y);
                        clipRects[c.id] = newRect;
                    }
                }
                EndWindows();
                scrollPosition = sv.scrollPosition;
            }
        }
        catch(Exception) {
            Close();
            throw;
        }


    }

    void DrawQuestionWindow(int id)
    {
        using(new GUILayout.VerticalScope()) {
            foreach(var c in questions[id].choices) {
                string choice;
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


    void DrawClipWindow(int id)
    {
        using(new GUILayout.VerticalScope()) {
            object[][] captions;
            if(!clips[id].strings.TryGetValue(currentLanguage, out captions)) {
                EditorGUILayout.LabelField("No captions for this language");
            }
            else {
                foreach(var o in captions) {
                    using(new GUILayout.HorizontalScope()) {
                        EditorGUILayout.IntField((int)(long)o[0], GUILayout.Width(25f));
                        EditorGUILayout.TextField((string)o[1]);
                    }
                }
            }

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
        var output = JsonConvert.SerializeObject(dialogue, Formatting.Indented);
        var path = AssetDatabase.GetAssetPath(dialogueFile);
        File.WriteAllText(path, output);
        Debug.Log("Saved!");
    }
}

