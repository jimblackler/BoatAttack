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
            JSONObject params_ = new JSONObject(json);
            JSONObject flattened = FlattenParams(params_);
            Debug.LogFormat("$$ flattened {0}", flattened.Print(true));

            //Debug.LogFormat("$$ testing " + test.GetField("hello").n);

        }
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
            int n = (int) coordinates[coordinateNumber].i;
            JSONObject jsonObject = jsonArray[n];
            foreach (string key in jsonObject.keys)
            { 
                p.SetField(key, jsonObject[key]);
            }
        }

        return p;
    }

}
