using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class Energybar : MonoBehaviour
{

    [SerializeField] [Range(0.01f, 1f)] private float fillSmoothness = 0.01f;
    [SerializeField] private Gradient gradient;
    [SerializeField] private Image fill;

    private Slider slider;

    private float energy;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void SetMaxEnergy(float maxEnergy)
    {
        slider.maxValue = maxEnergy;
        energy = maxEnergy;

        fill.color = gradient.Evaluate(1f);
    }

    public void SetEnergy(float _energy)
    {
        energy = _energy;        
    }

    private void Update()
    {
        slider.value = Mathf.Lerp(slider.value, energy, fillSmoothness);
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
}
