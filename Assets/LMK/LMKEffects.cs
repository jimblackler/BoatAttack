using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LMKEffects : MonoBehaviour, ILMKListener
{
    public GameObject ParticleFXPrefab;
    
    private GameObject _particleFXInit;
    private LMKBroadcaster _broadcaster;

    // Start is called before the first frame update
    void Start()
    {
        _broadcaster = GameObject.FindObjectOfType<LMKBroadcaster>();

        if (_broadcaster)
            _broadcaster.RegisterListener(this);

        EnableFX();
    }

    private void Stop()
    {
        if (_broadcaster)
            _broadcaster.DeregisterListener(this);
    }

    public void OnLowMemory()
    {
        DisableFX();
    }
    public void OnHighMemory()
    {
        EnableFX();
    }

    public void EnableFX()
    {
        _particleFXInit = GameObject.Instantiate(ParticleFXPrefab, transform);
    }

    public void DisableFX()
    {
        DestroyImmediate(_particleFXInit);
    }

}
