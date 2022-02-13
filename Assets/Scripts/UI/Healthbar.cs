using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class Healthbar : MonoBehaviour
{

    [SerializeField] [Range(0.01f, 1f)] private float fillSmoothness = 0.01f;
    private Slider slider;

    private float health;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void SetMaxHealth(float maxHealth)
    {
        slider.maxValue = maxHealth;
        health = maxHealth;
    }

    public void SetHealth(float _health)
    {
        health = _health;
    }

    private void Update()
    {
        slider.value = Mathf.Lerp(slider.value, health, fillSmoothness);
    }
}
