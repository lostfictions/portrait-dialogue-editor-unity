using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Linq;
using UnityEngine.UI;
using DialogueEditor;
using UnityEngine.Assertions;
using UniRx;

public class DialoguePlayer : MonoBehaviour
{
    [Header("Resources")]
    public TextAsset dialogueJson;

    [Header("Prefabs")]
    public Button choicePrefab;

    [Header("Scene Targets")]
    public RawImage videoTarget;
    public RectTransform choiceContainer;
    public Text caption;

    Dictionary<string, MovieTexture> videos;
    Dictionary<int, Question> questions;
    Dictionary<int, Clip> clips;
    AudioSource aud;

    GameObject choiceView;

    ReactiveProperty<Question> currentQuestion;
    ReactiveProperty<Clip> currentClip = new ReactiveProperty<Clip>();

    string lang = "*";

    void Start()
    {
        Assert.IsTrue(System.IO.Directory.Exists("Assets/Resources/video"),
            "Video clips for the dialogue JSON file must be placed in a folder " +
            "named 'video' in the project's 'Resources' folder.");

        choiceView = choiceContainer.GetComponentInParent<ScrollRect>().gameObject;

        aud = GetComponent<AudioSource>();
        Assert.IsNotNull(aud);

        var dialogue = Newtonsoft.Json.JsonConvert.DeserializeObject<Dialogue>(dialogueJson.text);
        questions = dialogue.questions.ToDictionary(q => q.id);
        clips = dialogue.clips.ToDictionary(c => c.id);

        //Load any videos we might need.
        videos = dialogue.clips
            .Select(c => Resources.Load<MovieTexture>("video/" + c.video.src))
            .Where(mt => mt != null)
            .Distinct()
            .ToDictionary(mt => mt.name);

        //Set up observable.
        var videoEnded = Observable.EveryLateUpdate()
            .Select(_ => {
                var mt = videoTarget.texture as MovieTexture;
                return mt != null && !mt.isPlaying;
            })
            .DistinctUntilChanged()
            .Where(b => b)
            .Select(b => Unit.Default);

        currentQuestion = new ReactiveProperty<Question>(questions[0]);

        currentQuestion
            .Subscribe(q => {
                if(q.video != null) {
                    SetVideo(q.video.src, true);
                }
                SetupChoices(q.choices);
            }).AddTo(this);

        currentClip
            .Subscribe(c => {
                if(c != null) {
                    SetVideo(c.video.src);
                    ShowCaption("");

                    var captions = c.strings[lang];

                    var w = gameObject.AddComponent<Waiter>();
                    long lastTime = 0;
                    foreach(var o in captions) {
                        object[] oo = o; //mono compiler silliness
                        long timestamp = (long)o[0];

                        //HACK: timestamps seem to be offset by a second.
                        timestamp = Math.Max(timestamp - 1, 0);

                        w.ThenWait(timestamp - lastTime).Then(() => ShowCaption((string)oo[1]));
                        lastTime = timestamp;
                    }

                    var next = c.next.Split(':');
                    switch(next[0]) {
                        case "question":
                            videoEnded.Take(1).Subscribe(_ => currentQuestion.Value = questions[int.Parse(next[1])]);
                            break;
                        case "clip":
                            videoEnded.Take(1).Subscribe(_ => currentClip.Value = clips[int.Parse(next[1])]);
                            break;
                        case "script":
                            var scriptData = next[1].Split(';');
                            var nextClip = DialogueScripts.scripts[scriptData[0]](scriptData[1]);
                            videoEnded.Take(1).Subscribe(_ => currentClip.Value = clips[nextClip]);
                            break;
                        default:
                            throw new NotImplementedException("Clip 'next' value '" +
                                                              next[0] +"' hasn't been implemented yet!");
                    }
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

    void SetupChoices(IEnumerable<Choice> choices)
    {
        choiceView.gameObject.SetActive(true);
        caption.gameObject.SetActive(false);
        choiceContainer.Cast<Transform>().Each(child => Destroy(child.gameObject));

        choiceContainer.anchoredPosition = Vector2.zero; //force scroll to top of list.

        foreach(var c in choices) {
            var cp = Instantiate(choicePrefab);
            cp.transform.SetParent(choiceContainer, false);
            cp.GetComponentInChildren<Text>().text = c.strings[lang];

            var cc = c; //work around mono compiler being silly :'(
            cp.OnClickAsObservable().Take(1).Subscribe(_ => {
                if(!string.IsNullOrEmpty(cc.lang)) {
                    lang = cc.lang;
                }
                currentClip.Value = clips[cc.answer];
            });
        }
    }

    void ShowCaption(string text)
    {
        choiceView.gameObject.SetActive(false);
        caption.gameObject.SetActive(true);
        caption.text = text;
    }
}
