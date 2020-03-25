using System;
using UnityEngine;
using Firebase.TestLab;
using System.IO;

public class TestLoops : MonoBehaviour
{
    private TestLabManager _testLabManager;
    private AndroidJavaObject _info;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("$$ Starting");
        _testLabManager = TestLabManager.Instantiate();

        using (StreamReader r = new StreamReader(File.OpenRead("/sdcard/params.json")))
        {
            JSONObject params1 = new JSONObject(r.ReadToEnd());
            JSONObject first = JSONObject.obj;

            using (AndroidJavaClass helper =
                new AndroidJavaClass("com.google.android.apps.internal.games.helperlibrary.Helper"))
            {
                first["build"] = JSONObject.Create(helper.CallStatic<string>("getBuild"));
            }

            first["params"] = params1;
            _testLabManager.LogToResults(first.Print(false) + Environment.NewLine);
            _info =
                new AndroidJavaObject("com.google.android.apps.internal.games.helperlibrary.Info");
            
            JSONObject flattened = FlattenParams(params1);
            if (flattened.HasField("quality"))
            {
                int quality = (int) flattened.GetField("quality").i;
                Debug.LogFormat("$$ Quality {0}", quality);
                QualitySettings.SetQualityLevel(quality);    
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_testLabManager.IsTestingScenario)
        {
            return;
        }

#if UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
        
        JSONObject report = JSONObject.Create(_info.Call<string>("standardInfo", context));
        _testLabManager.LogToResults(report.Print() + Environment.NewLine);
      #endif
    }

    private static JSONObject FlattenParams(JSONObject params1)
    {
        JSONObject p = JSONObject.obj;
        JSONObject coordinates = params1["coordinates"];
        JSONObject tests = params1["tests"];

        for (int coordinateNumber = 0; coordinateNumber != coordinates.Count; coordinateNumber++)
        {
            JSONObject jsonArray = tests[coordinateNumber];
            JSONObject jsonObject = jsonArray[(int) coordinates[coordinateNumber].i];
            foreach (string key in jsonObject.keys)
            {
                p[key] = jsonObject[key];
            }
        }
        return p;
    }
}