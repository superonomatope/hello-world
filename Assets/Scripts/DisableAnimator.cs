using UnityEngine;

public class DisableAnimator : MonoBehaviour
{
    public Animator _animator;
    public bool isHideWeapon;
    public bool isTpose = true;
    private bool _isTposeTrigger = false;
    private AnimationRandomOffset _randomOffset;


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _randomOffset = GetComponent<AnimationRandomOffset>();
        _randomOffset.enabled = false;
        _animator.SetFloat("Offset", 0f);
    }
    private void Start()
    {
        if (isTpose == true)
            _animator.enabled = false;
        if (isHideWeapon)
            HideWeapons();
    }

    private void LateUpdate()
    {
        if (isTpose == false)
            if (!_isTposeTrigger)
            {
                _isTposeTrigger = true;
                _animator.enabled = false;
            }
    }
    private void HideWeapons()
    {
        int a = GetComponents<HideWeapon>().Length;
        for (int i = 0; i < a; i++)
            GetComponents<HideWeapon>()[i].Hide();
    }
}
