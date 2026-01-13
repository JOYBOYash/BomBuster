using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    Transform camTransform;
    Vector3 originalPos;
    Coroutine shakeRoutine;

    void Awake()
    {
        Instance = this;
        camTransform = transform;
        originalPos = camTransform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            Vector3 offset = Random.insideUnitSphere * intensity;
            camTransform.localPosition = originalPos + offset;

            yield return null;
        }

        camTransform.localPosition = originalPos;
        shakeRoutine = null;
    }
}
