using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;

public class LMKPrefabWrapper : MonoBehaviour, ILMKListener
{
    public string AddressableName;
    
    private GameObject _instantiatedObject;
    private LMKBroadcaster _broadcaster;
    private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle _operationHandle;

    private bool _loaded = false;
    private bool _shouldBeLoaded = false;
    private bool _busy = false;

    // Start is called before the first frame update
    void Start()
    {
        _broadcaster = GameObject.FindObjectOfType<LMKBroadcaster>();

        if (_broadcaster)
            _broadcaster.RegisterListener(this);

        Load();
    }

    private void Stop()
    {
        if (_broadcaster)
            _broadcaster.DeregisterListener(this);
    }

    void Update()
    {
        if (_shouldBeLoaded != _loaded)
        {
            if (_busy) return;
            
            if (_shouldBeLoaded)
            {
                _busy = true;
                Addressables.LoadAssetAsync<GameObject>(AddressableName).Completed += OnLoadDone;
            }
            else
            {
                Destroy(_instantiatedObject);
                _instantiatedObject = null;
                Addressables.Release(_operationHandle);
                _loaded = false;
            }
        }
    }

    private void OnLoadDone(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        _operationHandle = obj;
        _instantiatedObject = Instantiate(obj.Result, transform);
        _loaded = true;
        _busy = false;
    }

    public void OnLowMemory()
    {
        Unload();
    }
    public void OnHighMemory()
    {
        Load();
    }

    public void Load()
    {
        _shouldBeLoaded = true;
    }

    public void Unload()
    {
        _shouldBeLoaded = false;
    }

}
