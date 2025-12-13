using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CastingShouldBeFree.Core.Interface.Panel_Handlers;

public class PlayerHandler : PanelHandlerBase
{
    private const string RigLerpKey        = "RigLerpingValueOooooooo";
    private const string RigLerpEnabledKey = "RigLerpingEnabled";

    private const float RigLerpBody    = 0.155f;
    private const float RigLerpFingers = 0.34f;

    private float currentRigLerpValue; // autcomplete wanted lastHealth

    private bool rigLerpingEnabled = true;

    protected override void Start()
    {
        SetUpRigLerpingChangerThingy();
        base.Start();
    }

    private void SetUpRigLerpingChangerThingy()
    {
        Transform sliderHolder = transform.Find("RigLerpThang");
        float     currentLerp  = PlayerPrefs.GetFloat(RigLerpKey, RigLerpBody);
        rigLerpingEnabled = PlayerPrefs.GetInt(RigLerpEnabledKey, 1) == 1;

        sliderHolder.GetComponentInChildren<Slider>().onValueChanged.AddListener(value =>
        {
            currentLerp = value;
            PlayerPrefs.SetFloat(RigLerpKey, value);
            sliderHolder.GetChild(1).GetComponent<TextMeshProUGUI>().text =
                    $"Rig Lerping: {value.ToString("F", CultureInfo.InvariantCulture)}";
        });

        sliderHolder.GetComponentInChildren<Slider>().value = currentLerp;
        sliderHolder.GetComponentInChildren<Slider>().onValueChanged?.Invoke(currentLerp);

        sliderHolder.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text =
                rigLerpingEnabled ? "<color=green>Enabled</color>" : "<color=red>Disabled</color>";

        sliderHolder.GetChild(2).GetComponent<Button>().onClick.AddListener(() =>
                                                                            {
                                                                                rigLerpingEnabled = !rigLerpingEnabled;
                                                                                PlayerPrefs.SetInt(RigLerpEnabledKey,
                                                                                        rigLerpingEnabled ? 1 : 0);

                                                                                sliderHolder.GetChild(2)
                                                                                       .GetComponentInChildren<
                                                                                                TextMeshProUGUI>()
                                                                                       .text = rigLerpingEnabled
                                                                                        ? "<color=green>Enabled</color>"
                                                                                        : "<color=red>Disabled</color>";
                                                                            });

        Plugin.Instance.OnFixedUpdate += () =>
                                         {
                                             if (!GorillaParent.hasInstance || GorillaParent.instance.vrrigs == null)
                                                 return;

                                             if (rigLerpingEnabled)
                                                 foreach (VRRig rig in GorillaParent.instance.vrrigs.Where(rig =>
                                                                  !Mathf.Approximately(rig.lerpValueBody,
                                                                          currentRigLerpValue) ||
                                                                  !Mathf.Approximately(rig.lerpValueFingers,
                                                                          currentRigLerpValue)))
                                                 {
                                                     rig.lerpValueBody    = currentLerp;
                                                     rig.lerpValueFingers = currentLerp;
                                                 }
                                             else
                                                 foreach (VRRig rig in GorillaParent.instance.vrrigs.Where(rig =>
                                                                  !Mathf.Approximately(rig.lerpValueBody,
                                                                          RigLerpBody) ||
                                                                  !Mathf.Approximately(rig.lerpValueFingers,
                                                                          RigLerpFingers)))
                                                 {
                                                     rig.lerpValueBody    = RigLerpBody;
                                                     rig.lerpValueFingers = RigLerpFingers;
                                                 }
                                         };
    }
}