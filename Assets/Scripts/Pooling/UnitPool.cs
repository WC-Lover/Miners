using System.Collections.Generic;
using Assets.Scripts.Unit;
using UnityEngine;

public class UnitPool : MonoBehaviour
{
    [SerializeField] private int unitPoolSize;

    private Vector3 spawnPosition;

    private Queue<Unit> pool = new Queue<Unit>();

    public void PredefineUnitPoolByHost(Unit unitPrefab, Vector3 spawnPosition)
    {
        this.spawnPosition = spawnPosition;

        for (int i = 0; i < unitPoolSize; i++)
        {
            Unit unit = Instantiate(unitPrefab);
            unit.gameObject.SetActive(false);
            pool.Enqueue(unit);
        }
    }

    public Unit GetUnit()
    {
        if (pool.Count == 0) return null;
        
        Unit unit = pool.Dequeue();
        unit.transform.position = spawnPosition;
        unit.gameObject.SetActive(true);

        return unit;
    }

    public void ReturnUnit(Unit unit)
    {
        unit.gameObject.SetActive(false);
        pool.Enqueue(unit);
    }
}
