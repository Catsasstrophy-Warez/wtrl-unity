using UnityEngine;

namespace WTRL.UI
{
    /// <summary>
    /// The simplest possible chase camera -- exists only so the vertical
    /// slice scene has something other than a static viewpoint. Not
    /// Cinemachine-based despite `com.unity.cinemachine` being installed:
    /// configuring a real Cinemachine rig well requires visual iteration
    /// this pass can't do blind, so a plain lerp-follow is the honest
    /// placeholder instead of a half-configured Cinemachine setup nobody
    /// has looked at.
    /// </summary>
    public sealed class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0, 4, -8);
        [SerializeField] private float positionLerpSpeed = 5f;
        [SerializeField] private float rotationLerpSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null) return;
            var desiredPosition = target.position + target.rotation * offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerpSpeed * Time.deltaTime);

            var lookRotation = Quaternion.LookRotation(target.position - transform.position + Vector3.up, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationLerpSpeed * Time.deltaTime);
        }
    }
}
