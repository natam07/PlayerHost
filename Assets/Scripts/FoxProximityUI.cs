using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FoxProximityUI : MonoBehaviour
{
    [Header("UI")]
    public Image directionArrow;
    public float detectionRange;

    private Transform nearestRabbit;
    private FPSController controller;

    void Start()
    {
        controller = GetComponent<FPSController>();

        if (controller == null || controller.Role != "FOX")
        {
            enabled = false;
            return;
        }

        if (directionArrow == null)
        {
            GameObject arrowObj = GameObject.Find("DirectionArrow");
            if (arrowObj != null)
            {
                directionArrow = arrowObj.GetComponent<Image>();
            }
        }
    }

    void Update()
    {
        FindNearestRabbit();
        if (directionArrow == null || nearestRabbit == null) return;

        Vector3 dir = nearestRabbit.position - transform.position;
        dir.y = 0;

        if (dir.magnitude <= detectionRange)
        {
            directionArrow.gameObject.SetActive(true);

            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            directionArrow.rectTransform.rotation = Quaternion.Euler(0, 0, -angle);
        }
        else
        {
            directionArrow.gameObject.SetActive(false);
        }
    }

    void FindNearestRabbit()
    {
        GameObject[] rabbits = GameObject.FindGameObjectsWithTag("Rabbit");

        if (rabbits.Length > 0)
        {
            nearestRabbit = rabbits
                .Select(r => r.transform)
                .OrderBy(t => Vector3.Distance(transform.position, t.position))
                .FirstOrDefault();
        }
        else
        {
            nearestRabbit = null;
        }
    }
}
