using UnityEngine;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DialogueEditor
{
    [Serializable]
    public class Dialogue
    {
        public string script;
        public Question[] questions;
        public Clip[] clips;
    }

    [Serializable]
    public class Question
    {
        public int id;

        public Choice[] choices;

        /// The video that should play while this question is
        /// being displayed to the user.
        public Video video;
    }

    [Serializable]
    public class Clip
    {
        public int id;

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
        public Dictionary<string, object[]> strings;
    }

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
}
