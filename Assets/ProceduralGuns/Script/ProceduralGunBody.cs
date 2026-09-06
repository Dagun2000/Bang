using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGunBody : MonoBehaviour
{
    public enum AimType { Round, Flat}
    public AimType aimType;

    public Transform handleParent;
    public Transform barrelParent;
    public Transform aimParent;
    public Transform magazineParent;

}
