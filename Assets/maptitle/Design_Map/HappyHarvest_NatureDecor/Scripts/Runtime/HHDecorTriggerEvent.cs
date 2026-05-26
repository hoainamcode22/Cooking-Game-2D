using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class HHDecorTriggerEvent : MonoBehaviour
{
    public UnityEvent OnEnter;
    public UnityEvent OnExit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        OnEnter?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        OnExit?.Invoke();
    }
}
