using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CastingShouldBeFree.Core.VisualPresets;

/// <summary>
/// Applies the Light visual preset only to cameras the casting mod owns
/// (PC spectator camera and the VR preview render-texture camera).
/// Headset / gameplay cameras are left untouched. Global RenderSettings
/// (fog, ambient) are swapped only for the duration of those cameras' renders.
/// </summary>
public static class VisualPresetApplier
{
    public const string PresetKey    = "VisualPreset";
    public const string IntensityKey = "VisualIntensity";

    private static readonly List<Camera> CastingCameras = new();

    private static VisualPresetDriver driver;
    private static Volume             volume;
    private static VolumeProfile      profile;

    private static ColorAdjustments colorAdjustments;
    private static WhiteBalance     whiteBalance;
    private static Tonemapping      tonemapping;
    private static Vignette         vignette;

    private static VisualPreset currentPreset = VisualPreset.Off;
    private static float        intensity     = 1f;
    private static bool         hooksActive;

    public static VisualPreset CurrentPreset => currentPreset;
    public static float        Intensity     => intensity;

    public static void LoadFromPlayerPrefs()
    {
        int stored = PlayerPrefs.GetInt(PresetKey, (int)VisualPreset.Off);
        currentPreset = stored == (int)VisualPreset.Light ? VisualPreset.Light : VisualPreset.Off;
        intensity     = Mathf.Clamp01(PlayerPrefs.GetFloat(IntensityKey, 1f));
        Apply();
    }

    public static void SetPreset(VisualPreset preset)
    {
        currentPreset = preset == VisualPreset.Light ? VisualPreset.Light : VisualPreset.Off;
        PlayerPrefs.SetInt(PresetKey, (int)currentPreset);
        PlayerPrefs.Save();
        Apply();
    }

    public static void CyclePreset()
    {
        SetPreset(currentPreset == VisualPreset.Off ? VisualPreset.Light : VisualPreset.Off);
    }

    public static void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(IntensityKey, intensity);
        PlayerPrefs.Save();
        if (currentPreset == VisualPreset.Light)
            ApplyLightProfile();
    }

    public static void Register(Camera camera)
    {
        if (camera == null)
            return;

        EnsureInitialized();

        if (!CastingCameras.Contains(camera))
            CastingCameras.Add(camera);

        ApplyCameraState(camera);
    }

    private static void EnsureInitialized()
    {
        if (driver != null)
            return;

        GameObject host = new("CastingShouldBeFree Visual Presets");
        Object.DontDestroyOnLoad(host);
        host.hideFlags = HideFlags.HideAndDontSave;

        volume              = host.AddComponent<Volume>();
        volume.isGlobal     = true;
        volume.priority     = 1000f;
        volume.weight       = 1f;
        volume.enabled      = false;

        profile              = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags    = HideFlags.HideAndDontSave;
        profile.name         = "CastingShouldBeFree Light Profile";
        volume.sharedProfile = profile;

        colorAdjustments = profile.Add<ColorAdjustments>(true);
        whiteBalance     = profile.Add<WhiteBalance>(true);
        tonemapping      = profile.Add<Tonemapping>(true);
        vignette         = profile.Add<Vignette>(true);

        driver = host.AddComponent<VisualPresetDriver>();
        ApplyLightProfile();
    }

    private static void Apply()
    {
        EnsureInitialized();
        PruneCameras();

        bool light = currentPreset == VisualPreset.Light;
        if (light)
        {
            ApplyLightProfile();
            EnableHooks();
        }
        else
        {
            DisableHooks();
            volume.enabled = false;
            driver.RestoreRenderSettingsIfNeeded();
        }

        foreach (Camera camera in CastingCameras)
            ApplyCameraState(camera);
    }

    private static void ApplyLightProfile()
    {
        if (colorAdjustments == null)
            return;

        float i = intensity;

        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.12f * i);
        colorAdjustments.contrast.Override(14f * i);
        colorAdjustments.saturation.Override(10f * i);
        colorAdjustments.hueShift.Override(0f);
        colorAdjustments.colorFilter.Override(Color.Lerp(Color.white, new Color(1f, 0.98f, 0.94f), i));

        whiteBalance.active = true;
        whiteBalance.temperature.Override(10f * i);
        whiteBalance.tint.Override(3f * i);

        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.Neutral);

        vignette.active = true;
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(0.26f * i);
        vignette.smoothness.Override(0.42f);
        vignette.rounded.Override(false);
    }

    private static void ApplyCameraState(Camera camera)
    {
        if (camera == null)
            return;

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        driver.RememberCamera(camera, data);
        data.renderPostProcessing = currentPreset == VisualPreset.Light ||
                                    driver.WasPostProcessingEnabled(camera);
    }

    private static void EnableHooks()
    {
        if (hooksActive)
            return;

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering   += OnEndCameraRendering;
        hooksActive = true;
    }

    private static void DisableHooks()
    {
        if (!hooksActive)
            return;

        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering   -= OnEndCameraRendering;
        hooksActive = false;
        if (volume != null)
            volume.enabled = false;
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (currentPreset != VisualPreset.Light || !IsCastingCamera(camera))
            return;

        driver.CaptureAndApplyRenderSettings(intensity);
        volume.enabled = true;
    }

    private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!IsCastingCamera(camera))
            return;

        volume.enabled = false;
        driver.RestoreRenderSettingsIfNeeded();
    }

    private static bool IsCastingCamera(Camera camera)
    {
        if (camera == null)
            return false;

        for (int i = CastingCameras.Count - 1; i >= 0; i--)
        {
            if (CastingCameras[i] == null)
            {
                CastingCameras.RemoveAt(i);
                continue;
            }

            if (CastingCameras[i] == camera)
                return true;
        }

        return false;
    }

    private static void PruneCameras()
    {
        for (int i = CastingCameras.Count - 1; i >= 0; i--)
            if (CastingCameras[i] == null)
                CastingCameras.RemoveAt(i);
    }

    internal sealed class VisualPresetDriver : MonoBehaviour
    {
        private readonly Dictionary<Camera, bool> originalPostProcessing = new();

        private bool            hasCapture;
        private bool            capturedFog;
        private Color           capturedFogColor;
        private FogMode         capturedFogMode;
        private float           capturedFogDensity;
        private float           capturedFogStart;
        private float           capturedFogEnd;
        private Color           capturedAmbientLight;
        private float           capturedAmbientIntensity;
        private AmbientMode     capturedAmbientMode;
        private Color           capturedSky;
        private Color           capturedEquator;
        private Color           capturedGround;

        public void RememberCamera(Camera camera, UniversalAdditionalCameraData data)
        {
            if (camera == null || originalPostProcessing.ContainsKey(camera))
                return;

            originalPostProcessing[camera] = data.renderPostProcessing;
        }

        public bool WasPostProcessingEnabled(Camera camera) =>
                originalPostProcessing.TryGetValue(camera, out bool enabled) && enabled;

        public void CaptureAndApplyRenderSettings(float presetIntensity)
        {
            if (!hasCapture)
            {
                capturedFog              = RenderSettings.fog;
                capturedFogColor         = RenderSettings.fogColor;
                capturedFogMode          = RenderSettings.fogMode;
                capturedFogDensity       = RenderSettings.fogDensity;
                capturedFogStart         = RenderSettings.fogStartDistance;
                capturedFogEnd           = RenderSettings.fogEndDistance;
                capturedAmbientLight     = RenderSettings.ambientLight;
                capturedAmbientIntensity = RenderSettings.ambientIntensity;
                capturedAmbientMode      = RenderSettings.ambientMode;
                capturedSky              = RenderSettings.ambientSkyColor;
                capturedEquator          = RenderSettings.ambientEquatorColor;
                capturedGround           = RenderSettings.ambientGroundColor;
                hasCapture               = true;
            }

            float i = presetIntensity;

            RenderSettings.fog         = true;
            RenderSettings.fogMode     = FogMode.ExponentialSquared;
            RenderSettings.fogColor    = Color.Lerp(capturedFogColor, new Color(0.42f, 0.52f, 0.68f), 0.35f * i);
            RenderSettings.fogDensity  = Mathf.Lerp(capturedFog ? capturedFogDensity : 0.006f, 0.012f, i);
            RenderSettings.ambientLight = Color.Lerp(capturedAmbientLight,
                    capturedAmbientLight * 1.06f + new Color(0.03f, 0.035f, 0.05f, 0f), i);
            RenderSettings.ambientIntensity = capturedAmbientIntensity * (1f + 0.1f * i);
        }

        public void RestoreRenderSettingsIfNeeded()
        {
            if (!hasCapture)
                return;

            RenderSettings.fog               = capturedFog;
            RenderSettings.fogColor          = capturedFogColor;
            RenderSettings.fogMode           = capturedFogMode;
            RenderSettings.fogDensity        = capturedFogDensity;
            RenderSettings.fogStartDistance  = capturedFogStart;
            RenderSettings.fogEndDistance    = capturedFogEnd;
            RenderSettings.ambientLight      = capturedAmbientLight;
            RenderSettings.ambientIntensity  = capturedAmbientIntensity;
            RenderSettings.ambientMode       = capturedAmbientMode;
            RenderSettings.ambientSkyColor   = capturedSky;
            RenderSettings.ambientEquatorColor = capturedEquator;
            RenderSettings.ambientGroundColor  = capturedGround;
            hasCapture = false;
        }

        private void OnDisable()
        {
            DisableHooks();
            RestoreRenderSettingsIfNeeded();
            if (volume != null)
                volume.enabled = false;
        }

        private void OnDestroy()
        {
            DisableHooks();
            RestoreRenderSettingsIfNeeded();
            if (profile != null)
                Destroy(profile);
        }
    }
}
