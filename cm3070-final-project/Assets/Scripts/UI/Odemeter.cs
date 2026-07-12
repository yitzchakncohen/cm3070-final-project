using TMPro;
using UnityEngine;

public class Odemeter : MonoBehaviour
{
    private const float NEEDLE_SPEED = 60f;
    [SerializeField] private RectTransform indicators;
    [SerializeField] private RectTransform indicatorPrefab;
    [SerializeField] private RectTransform needle;
    [SerializeField] private TMP_Text unitsText;
    [SerializeField] private float startingAngle = 130f;
    [SerializeField] private float endingAngle = -130f;
    [SerializeField] private int increments = 17;
    private float incrementValue = 20;

    public void Init(string units, float values)
    {
        incrementValue = values;
        for(int i = 0; i < indicators.childCount; i++)
        {
            DestroyImmediate(indicators.GetChild(i));
        }

        unitsText.text = units;

        float degreeIncrements = (endingAngle - startingAngle) / increments;
        for (int i = 0; i < increments; i++)
        {
            RectTransform indicator = Instantiate(indicatorPrefab, indicators);
            indicator.localEulerAngles = new Vector3(0, 0, startingAngle + i*degreeIncrements);
            TMP_Text value = indicator.GetComponentInChildren<TMP_Text>();
            value.text = (incrementValue * i).ToString();
            value.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, -(startingAngle + i*degreeIncrements));
        }
    }

    public void UpdateNeedle(float currentValue)
    {
        float percent = currentValue / (increments * incrementValue);
        float targetAngle = Mathf.Lerp(startingAngle, endingAngle, percent);
        float angle = Mathf.MoveTowardsAngle(needle.localEulerAngles.z, targetAngle, NEEDLE_SPEED * Time.deltaTime);
        needle.localEulerAngles = new Vector3(0, 0, angle);
    }
}
