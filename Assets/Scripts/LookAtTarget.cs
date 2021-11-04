using UnityEngine;
[ExecuteInEditMode]
public class LookAtTarget : MonoBehaviour
{
    [SerializeField] private GameObject _target;
    [SerializeField] private Camera _eventCamera;
    private void LateUpdate()
    {
        if (_target && _eventCamera)
            _eventCamera.transform.LookAt(_target.transform);
    }
}
