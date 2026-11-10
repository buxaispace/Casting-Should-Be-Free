using System;
using System.Collections.Generic;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public enum UnityLayer
{
    Default = 0,
    GorillaInteractable = 18,
    FirstPersonOnly = 19,
    MirrorOnly = 22,
}

[Flags]
public enum UnityLayerMask
{
    Nothing = 0,
    GorillaInteractable = 0x40000,
}

public static class UnityLayerExtensions
{
    public static int ToLayerIndex(this UnityLayer self) => (int)self;
    public static bool IsOnLayer(this GameObject obj, UnityLayer layer) => obj.layer == (int)layer;
    public static void SetLayer(this GameObject obj, UnityLayer layer) => obj.layer = (int)layer;
}

public static class ComponentAddExtensions
{
    public static T AddComponent<T>(this Component component) where T : Component =>
            component.gameObject.AddComponent<T>();
}

namespace UnityEngine
{
    public static class GUI
    {
        public static void Label(Rect position, string text) { }
    }
}

public static class EnumUtilExt
{
    public static int GetIndex<TEnum>(this TEnum e) where TEnum : struct, Enum => Convert.ToInt32(e);
}

public enum GTZone
{
    forest = 0,
    canyon = 1,
    city = 2,
}

public static class ZoneManagement
{
    public static void SetActiveZone(GTZone zone) { }
}

public class NetPlayer
{
    public string NickName;
    public Player GetPlayerRef() => null;
}

public class VRMap
{
    public Transform rigTarget;
}

public class CosmeticSet
{
    public GorillaNetworking.CosmeticsController.CosmeticItem[] items =
            Array.Empty<GorillaNetworking.CosmeticsController.CosmeticItem>();
}

public class CosmeticItemInstance
{
    public void EnableItem(GorillaNetworking.CosmeticsController.CosmeticSlots slot, VRRig rig) { }
    public void DisableItem(GorillaNetworking.CosmeticsController.CosmeticSlots slot) { }
}

public class CosmeticsObjectRegistry
{
    public CosmeticItemInstance Cosmetic(string displayName) => new CosmeticItemInstance();
}

public class VRRig : MonoBehaviour
{
    public static VRRig LocalRig;

    public int setMatIndex;
    public Color playerColor;
    public TMP_Text playerText1;
    public NetPlayer OwningNetPlayer;
    public NetPlayer creator;
    public GameObject headMesh;
    public Renderer bodyRenderer;
    public bool isLocal;
    public int fps;
    public float lerpValueBody;
    public float lerpValueFingers;
    public CosmeticSet cosmeticSet = new CosmeticSet();
    public CosmeticsObjectRegistry cosmeticsObjectRegistry = new CosmeticsObjectRegistry();
    public List<string> _playerOwnedCosmetics = new List<string>();
    public VRMap head = new VRMap();

    public void ChangeMaterialLocal(int materialIndex) { }
    public void SetCosmeticsActive() { }
    public void SetColor(Color color) { }
    public void SetNameTagText(string name) { }
    public void PostTick() { }
    public void OnDisable() { }
    public Vector3 LatestVelocity() => Vector3.zero;
}

public interface IUserCosmeticsCallback
{
    void OnGetUserCosmetics();
}

public class GorillaTagger : MonoBehaviour
{
    public static GorillaTagger Instance;
    public GameObject thirdPersonCamera;
    public Rigidbody rigidbody;
    public Collider headCollider;
    public GameObject rightHandTriggerCollider;

    public static void OnPlayerSpawned(Action callback) => callback?.Invoke();
    public void StartVibration(bool isLeftHand, float amplitude, float duration) { }
}

public class GorillaTriggerColliderHandIndicator : MonoBehaviour
{
    public bool isLeftHand;
}

public class GorillaParent : MonoBehaviour
{
    public static bool hasInstance;
}

public class VRRigCache : MonoBehaviour
{
    public static IEnumerable<VRRig> ActiveRigs;
}

public class BetterDayNightManager : MonoBehaviour
{
    public static BetterDayNightManager instance;
    public enum WeatherType { None, Raining }
    public WeatherType[] weatherCycle = Array.Empty<WeatherType>();
    public float[] timeOfDayRange = Array.Empty<float>();
    public void SetTimeOfDay(int value) { }
}

public class NetworkSystem
{
    public static NetworkSystem Instance = new NetworkSystem();
    public void ReturnToSinglePlayer() { }
}

public class ControllerInputPoller
{
    public static ControllerInputPoller instance = new ControllerInputPoller();
    public bool leftControllerPrimaryButton;
    public bool leftGrab;
    public bool rightGrab;
}

namespace GorillaNetworking
{
    public enum JoinType { Solo }

    public class PhotonNetworkController : MonoBehaviour
    {
        public static PhotonNetworkController Instance;
        public bool disableAFKKick;
        public void AttemptToJoinSpecificRoom(string roomName, JoinType joinType) { }
    }

    public class GorillaComputer : MonoBehaviour
    {
        public static GorillaComputer instance;
        public bool CheckAutoBanListForName(string roomName) => true;
    }

    public class CosmeticsController
    {
        public enum CosmeticCategory { Face, Hat }
        public enum CosmeticSlots { Face, Hat }

        public struct CosmeticItem
        {
            public CosmeticCategory itemCategory;
            public string displayName;
        }
    }
}

namespace GorillaLocomotion
{
    public class GTPlayer : MonoBehaviour
    {
        public static GTPlayer Instance;
        public Collider bodyCollider;
        public Collider headCollider;
        public HandRef leftHand;
        public HandRef rightHand;
    }

    public class HandRef
    {
        public Transform controllerTransform;
    }
}
