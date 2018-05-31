using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaytracingController : MonoBehaviour {

    public ComputeShader RayTraceShader;
    public Texture SkyboxTex;
    public Light DirectionalLight;

    [SerializeField]
    private float _skyboxMultiplicator = 1.0f;
    private RenderTexture _targetTex;
    private Camera _camera;
    private uint _currentSample = 0;
    private Material _addMaterial;

    public float SkyboxMultiplicator
    {
        get { return _skyboxMultiplicator; }
        set {
            _skyboxMultiplicator = value;
            RestartSampling();
        }
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        DetectTransformChanged(transform);
        DetectTransformChanged(DirectionalLight.transform);
    }

    private void DetectTransformChanged(Transform t)
    {
        if(t.hasChanged)
        {
            RestartSampling();
            t.hasChanged = false;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void SetShaderParameters()
    {
        RayTraceShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTraceShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTraceShader.SetTexture(0, "_SkyboxTex", SkyboxTex);
        RayTraceShader.SetFloat("_SkyboxTexFactor", _skyboxMultiplicator);
        RayTraceShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
    }

    private void Render(RenderTexture dest)
    {
        //Do we have a render target?
        InitRenderTexture();

        //set target, dispatch computeshader
        RayTraceShader.SetTexture(0, "Result", _targetTex);
        int threadGroupAmountX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupAmountY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTraceShader.Dispatch(0, threadGroupAmountX, threadGroupAmountY, 1);

        //show resulting texture
        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_targetTex, dest, _addMaterial);
        _currentSample++;
        //Graphics.Blit(_targetTex, dest);
    }

    private void InitRenderTexture()
    {
        if(_targetTex == null || _targetTex.width != Screen.width || _targetTex.height != Screen.height)
        {
            //Restart with sample 0
            _currentSample = 0;

            //release old render texture
            if (_targetTex != null) _targetTex.Release();

            //Create render texture for raytracing
            _targetTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _targetTex.enableRandomWrite = true;
            _targetTex.Create();
        }
    }

    private void RestartSampling()
    {
        _currentSample = 0;
    }
}
