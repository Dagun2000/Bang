using UnityEngine;

[CreateAssetMenu(fileName = "NewParts", menuName = "Sursson/ProceduralGunParts", order = 1)]
public class ProceduralGunParts : ScriptableObject
{
    public GameObject[] BodyList;
    public GameObject[] HandleList;
    public GameObject[] BarrelList;
    public GameObject[] AimList;
}
