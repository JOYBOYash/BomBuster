using UnityEngine;

public class WeaponClippingHandler : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform weaponRoot;

    [Header("Clipping Detection")]
    public LayerMask blockingLayers;
    public float clipCheckDistance = 0.8f;
    public float minDistance = 0.15f;

    [Header("Forward Direction Control")]
    public WeaponForwardAxis forwardAxis = WeaponForwardAxis.ZForward;

    [Header("Smoothing")]
    public float clipInSpeed = 14f;
    public float clipOutSpeed = 10f;

    Vector3 defaultLocalPosition;

    void Awake()
    {
        if (weaponRoot == null)
            weaponRoot = transform;

        defaultLocalPosition = weaponRoot.localPosition;
    }

    void LateUpdate()
    {
        if (weaponRoot == null)
            return;

        HandleWeaponClipping();
    }

void HandleWeaponClipping()
{
    // 🔒 STABLE ORIGIN (camera, not weapon)
    Vector3 rayOrigin = playerCamera != null
        ? playerCamera.transform.position
        : weaponRoot.position;

    Vector3 rayDirection = GetWeaponForward();

    float desiredZ = defaultLocalPosition.z;
    bool isBlocked = false;

    if (Physics.Raycast(
        rayOrigin,
        rayDirection,
        out RaycastHit hit,
        clipCheckDistance,
        blockingLayers,
        QueryTriggerInteraction.Ignore))
    {
        float pushBack = Mathf.Max(hit.distance - 0.05f, minDistance);
        desiredZ = -pushBack;
        isBlocked = true;
    }

    // 🔥 HYSTERESIS (prevents flicker near threshold)
    float deadZone = 0.03f;
    if (!isBlocked && Mathf.Abs(weaponRoot.localPosition.z - defaultLocalPosition.z) < deadZone)
    {
        desiredZ = defaultLocalPosition.z;
    }

    float speed = desiredZ < weaponRoot.localPosition.z
        ? clipInSpeed
        : clipOutSpeed;

    float newZ = Mathf.MoveTowards(
        weaponRoot.localPosition.z,
        desiredZ,
        speed * Time.deltaTime
    );

    weaponRoot.localPosition = new Vector3(
        defaultLocalPosition.x,
        defaultLocalPosition.y,
        newZ
    );
}


    // ================= AXIS RESOLUTION =================

    Vector3 GetWeaponForward()
    {
        switch (forwardAxis)
        {
            case WeaponForwardAxis.ZForward:  return weaponRoot.forward;
            case WeaponForwardAxis.ZBackward: return -weaponRoot.forward;
            case WeaponForwardAxis.XForward:  return weaponRoot.right;
            case WeaponForwardAxis.XBackward: return -weaponRoot.right;
            case WeaponForwardAxis.YForward:  return weaponRoot.up;
            case WeaponForwardAxis.YBackward: return -weaponRoot.up;
            default: return weaponRoot.forward;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (weaponRoot == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            weaponRoot.position,
            GetWeaponForward() * clipCheckDistance
        );
    }
#endif
}

public enum WeaponForwardAxis
{
    ZForward,
    ZBackward,
    XForward,
    XBackward,
    YForward,
    YBackward
}
