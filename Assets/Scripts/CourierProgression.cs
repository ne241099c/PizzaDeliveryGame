using UnityEngine;
public sealed class CourierProgression : MonoBehaviour
{
    [SerializeField,Min(1)] private float experiencePerLevel=200;
    [SerializeField,Range(0,1)] private float growthPerLevel=.02f;
    public float ExperiencePerLevel=>experiencePerLevel;
    public float GrowthPerLevel=>growthPerLevel;
}
