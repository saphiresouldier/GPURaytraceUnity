﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaytracingController : MonoBehaviour {

    public ComputeShader RayTraceShader;
    public Texture SkyboxTex;

    private RenderTexture _targetTex;
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
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
        Graphics.Blit(_targetTex, dest);
    }

    private void InitRenderTexture()
    {
        if(_targetTex == null || _targetTex.width != Screen.width || _targetTex.height != Screen.height)
        {
            //release old render texture
            if (_targetTex != null) _targetTex.Release();

            //Create render texture for raytracing
            _targetTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _targetTex.enableRandomWrite = true;
            _targetTex.Create();
        }
    }

}