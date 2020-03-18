using System;
using UnityEngine;
using Firebase.TestLab;
using System.IO;

public class TestLoops : MonoBehaviour
{
    private TestLabManager _testLabManager;
    private long _startTime;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("$$ Starting");
        _startTime = DateTime.Now.Ticks;
        _testLabManager = TestLabManager.Instantiate();

    
        using (StreamReader r = new StreamReader(File.OpenRead("/sdcard/params.json")))
        {
            JSONObject params1 = new JSONObject(r.ReadToEnd());

            JSONObject report = JSONObject.obj;
            report["params"] = params1;
            _testLabManager.LogToResults(report.Print() + Environment.NewLine);

            JSONObject flattened = FlattenParams(params1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_testLabManager.IsTestingScenario) { return; }

#if UNITY_ANDROID
        using (AndroidJavaClass debug = new AndroidJavaClass("android.os.Debug"))
        {
            JSONObject report = JSONObject.obj;
            report["time"] = JSONObject.Create(DateTime.Now.Ticks - _startTime);
            report["nativeAllocated"] = 
                JSONObject.Create(debug.CallStatic<long>("getNativeHeapAllocatedSize"));
            _testLabManager.LogToResults(report.Print() + Environment.NewLine);
        }
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
