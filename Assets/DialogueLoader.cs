using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using DialogueEditor;
using UnityEngine.Assertions;

public class DialogueLoader : MonoBehaviour
{
    public TextAsset dialogueJson;

    Dictionary<string, MovieTexture> clips;
    
    void Start()
    {
        var dialogue = Newtonsoft.Json.JsonConvert.DeserializeObject<Dialogue>(dialogueJson.text);

        Assert.IsTrue(System.IO.Directory.Exists("Assets/Resources/" + dialogueJson.name),
            "Video clips for dialogue JSON file must be placed in a folder " +
            "with the same name as the file (excluding the extension.)");

//        clips = Resources
//            .LoadAll<MovieTexture>(dialogueJson.name)
//            .ToDictionary(mt => mt.name);
    }
    
    void Update()
    {
        
    }
}
