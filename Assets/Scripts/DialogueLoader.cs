using UnityEngine;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Linq;
using UnityEngine.UI;
using DialogueEditor;
using UnityEngine.Assertions;
using UniRx;

public class DialogueLoader : MonoBehaviour
{
    [Header("Resources")]
    public TextAsset dialogueJson;

    [Header("Prefabs")]
    public Button choicePrefab;

    [Header("Scene Targets")]
    public RawImage videoTarget;
    public Transform choiceContainer;

    Dictionary<string, MovieTexture> videos;
    Dictionary<int, Question> questions;
    Dictionary<int, Clip> clips;
    AudioSource aud;

    ReactiveProperty<Question> currentQuestion;
    ReactiveProperty<Clip> currentClip = new ReactiveProperty<Clip>();

    string lang = "*";

    void Start()
    {
        Assert.IsTrue(System.IO.Directory.Exists("Assets/Resources/" + dialogueJson.name),
            "Video clips for dialogue JSON file must be placed in a folder " +
            "with the same name as the file (excluding the extension.)");

        videos = Resources
            .LoadAll<MovieTexture>(dialogueJson.name)
            .ToDictionary(mt => mt.name);

        aud = GetComponent<AudioSource>();
        Assert.IsNotNull(aud);

        var dialogue = Newtonsoft.Json.JsonConvert.DeserializeObject<Dialogue>(dialogueJson.text);
        questions = dialogue.questions.ToDictionary(q => q.id);
        clips = dialogue.clips.ToDictionary(c => c.id);

        //Set up observable.
        var videoEnded = Observable.EveryLateUpdate()
            .Select(_ => {
                var mt = videoTarget.texture as MovieTexture;
                return mt != null && !mt.isPlaying;
            })
            .DistinctUntilChanged()
            .Where(b => b)
            .Select(b => Unit.Default);

//        videoEnded.Subscribe(_ => Debug.Log("Video ended!"));
//        SetVideo("P22");
//        Waiters.Wait(4f, gameObject).Then(() => SetVideo("P32"));


        currentQuestion = new ReactiveProperty<Question>(questions[0]);

        currentQuestion
            .Subscribe(q => {
                Debug.Log(q);
                if(q.video != null) {
                    SetVideo(q.video.src, true);
                }
                SetupChoices(q.choices);
            }).AddTo(this);

        currentClip
            .Subscribe(c => {
                if(c != null) {
                    
                }

            }).AddTo(this);
    }

    void SetVideo(string videoName, bool loop = false)
    {
        var mt = videos[videoName];

        var currentMt = videoTarget.texture as MovieTexture;
        if(currentMt != null) {
            currentMt.Stop();
            aud.Stop();
        }

        mt.loop = loop;
        aud.loop = loop;

        videoTarget.texture = mt;
        aud.clip = mt.audioClip;
        mt.Play();
        aud.Play();
    }

    void SetupChoices(Choice[] choices)
    {
        choiceContainer.Cast<Transform>().Each(child => Destroy(child.gameObject));

        foreach(var c in choices) {

            var cp = Instantiate(choicePrefab);
            cp.transform.SetParent(choiceContainer, false);
            cp.GetComponentInChildren<Text>().text = c.strings[lang];

            var cc = c; //work around mono compiler being silly :'(
            cp.OnClickAsObservable().Take(1).Subscribe(_ => {
                Debug.Log("Choice: " + cc.strings[lang]);
                if(!string.IsNullOrEmpty(cc.lang)) {
                    lang = cc.lang;
                }
                currentClip.Value = clips[cc.answer];
            });
        }
    }

    void ShowCaption(string text)
    {
        
    }
}
