using UnityEngine;
using Firebase.TestLab;
using System.IO;

public class TestLoops : MonoBehaviour
{
    private TestLabManager _testLabManager;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("$$ Starting");
        _testLabManager = TestLabManager.Instantiate();

        using (StreamReader r = new StreamReader(File.OpenRead("/sdcard/params.json")))
        {
            string json = r.ReadToEnd();
            Debug.Log("$$ rawJson " + json);
            JSONObject params1 = new JSONObject(json);
            JSONObject flattened = FlattenParams(params1);
            Debug.LogFormat("$$ flattened {0}", flattened.Print(true));
            _testLabManager.LogToResults(flattened.Print(true));
            //Debug.LogFormat("$$ testing " + test.GetField("hello").n);
        }
        
        JSONObject o = JSONObject.obj;
        o["testingOutput"] = JSONObject.Create(false);
        o["test2"] = JSONObject.Create(99);
        _testLabManager.LogToResults("test");
        _testLabManager.LogToResults(o.Print(true));
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!_testLabManager.IsTestingScenario) { return; }

        Debug.Log("$$ Scenario number: " + _testLabManager.ScenarioNumber);
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
