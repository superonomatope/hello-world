using UnityEngine;
public class AnimationRandomOffset : MonoBehaviour
{
    void Start() => GetComponent<Animator>().SetFloat("Offset", Random.Range(0.0f, 1.0f));
}