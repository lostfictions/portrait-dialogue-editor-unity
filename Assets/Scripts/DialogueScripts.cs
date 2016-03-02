using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class DialogueScripts
{
    public static readonly Dictionary<string, Func<string, int>> scripts = new Dictionary<string, Func<string, int>> {
        {
            "time", values => {
                var clipIds = ParseArray<int>(values);

                var time = DateTime.Now;

                int hour = time.Hour;
                if(time.Minute > 30) {
                    hour++;
                }
                if(hour == 24) {
                    hour = 0;
                }
                else if(hour > 12) {
                    hour -= 12;
                }

                return clipIds[hour];
            } 
        },
    };

    static T[] ParseArray<T>(string array)
    {
        Assert.IsTrue(array.StartsWith("[") && array.EndsWith("]"));

        return array
            .Substr(1, array.Length - 1)
            .Split(',')
            .Select(value => (T)Convert.ChangeType(value, typeof(T)))
            .ToArray();
    }
}
