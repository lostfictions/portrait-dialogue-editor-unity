using UnityEngine;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using Newtonsoft.Json;

namespace DialogueEditor
{
    /// Overarching schema for the JSON file.
    [Serializable]
    public class Dialogue
    {
        public string script;
        public Question[] questions;
        public Clip[] clips;
    }

    /// A choice of lines of dialogue that the user can pose to the character.
    [Serializable]
    public class Question : IDialogueElement
    {
        public int id;
        [JsonIgnore]
        public int Id { get { return id; } }

        public float x;
        public float y;
        [JsonIgnore]
        public Vector2 Position { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }

        public Choice[] choices;

        /// The video that should play while this question is
        /// being displayed to the user.
        public Video video;
    }

    /// A video of the character that is shown to the user following a choice, a script
    /// or another clip.
    [Serializable]
    public class Clip : IDialogueElement
    {
        public int id;
        [JsonIgnore]
        public int Id { get { return id; } }

        public float x;
        public float y;
        [JsonIgnore]
        public Vector2 Position { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }

        /// Identifies what should be displayed after this clip has finished
        /// playing. Should be in the format of either "clip:{id}" or
        /// "question:{id}", where {id} is an integer indicating the id value
        /// of the next clip or question to show.
        /// 
        /// There is also a third possible value: "script:{scriptname};{arguments}"
        /// Currently the only available script is "time".
        public string next;

        public Video video;

        /// Here's the hairy part. This is a map of language codes ("en", "fr",
        /// "de", etc.) to timestamped captions. The value of each language code
        /// is an array of timestamped strings, where each timestamped string is
        /// itself an array of two values: the timestamp (an int) and the caption
        /// proper. (For example: [ 1, "Our world was built in a day." ] )
        public Dictionary<string, object[][]> strings;
    }

//    public class Script : IDialogueElement
//    {
//        public int id;
//        [JsonIgnore]
//        public int Id { get { return id; } }

//        public float x;
//        public float y;
//        [JsonIgnore]        
//        public Vector2 Position { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }
//    }

    [Serializable]
    public class Choice
    {
        /// Maps language codes ("en", "fr", "de", etc.) to choice strings.
        /// A special case is the initial question, where all answer strings
        /// have a key of "*" and an associated "lang" field. The latter is
        /// used to set the language for the subsequent dialogues.
        public Dictionary<string, string> strings;

        /// An id indicating the clip that should play when this choice is selected.
        public int answer;

        /// See documentation for "strings" field above. Only used for the initial
        /// question, in which each choice sets the language.
        public string lang;
    }

    [Serializable]
    public class Video
    {
        public string src;
        public bool loop;
    }

    [Serializable]
    public class Caption
    {
        public int timestamp;
        public string caption;
    }
    
    /// An element that can follow a clip -- currently either a question, another
    /// clip, or a script.
    public interface IDialogueElement
    {
        int Id { get; }
        Vector2 Position { get; set; }
    }
}
