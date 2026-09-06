using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGunGenerator : MonoBehaviour
{
    public bool assembleOnStart;
    public bool forceReassemble;

    public enum BodyType { Random, SingleAction, Revolver, BoltAction, SemiAuto, Automatic}
    public enum HandleType { Random, Broom, Pistol, Stock }
    public enum BarrelType { Random, Short, Medium, Long, HandguardShort, HandguardLong, WaterCooledShort, WaterCooledLong }
    public enum AimType { Random, Simple, Reflex, Scope }

    public BodyType body;
    public HandleType handle;
    public BarrelType barrel;
    public AimType aim;

    public ProceduralGunParts partsList;

    public void AssembleWeapon()
    {
        ProceduralGunBody gunBody;

        if(body == 0)
        {
            gunBody = GameObject.Instantiate(
                partsList.BodyList[Random.Range(0, partsList.BodyList.Length)],
                transform.position,
                transform.rotation,
                transform
                ).GetComponent<ProceduralGunBody>();
        } else
        {
            gunBody = GameObject.Instantiate(
                partsList.BodyList[(int)body-1],
                transform.position,
                transform.rotation,
                transform
                ).GetComponent<ProceduralGunBody>();
        }

        if(handle == 0)
        {
            GameObject.Instantiate(
                partsList.HandleList[Random.Range(0, partsList.HandleList.Length)],
                gunBody.handleParent.position,
                gunBody.handleParent.rotation,
                gunBody.handleParent
                );
        }
        else
        {
            GameObject.Instantiate(
                partsList.HandleList[(int)handle - 1], 
                gunBody.handleParent.position, 
                gunBody.handleParent.rotation, 
                gunBody.handleParent
                );
        }

        if (barrel == 0)
        {
            GameObject.Instantiate(
                partsList.BarrelList[Random.Range(0, partsList.BarrelList.Length)],
                gunBody.barrelParent.position,
                gunBody.barrelParent.rotation,
                gunBody.barrelParent
                );
        }
        else
        {
            GameObject.Instantiate(
                partsList.BarrelList[(int)barrel - 1],
                gunBody.barrelParent.position,
                gunBody.barrelParent.rotation,
                gunBody.barrelParent
                );
        }

        if (aim == 0)
        {
            GameObject.Instantiate(
                partsList.AimList[Random.Range(0, partsList.AimList.Length)],
                gunBody.aimParent.position,
                gunBody.aimParent.rotation,
                gunBody.aimParent
                );
        }
        else
        {
            GameObject.Instantiate(
                partsList.AimList[(int)aim - 1],
                gunBody.aimParent.position,
                gunBody.aimParent.rotation,
                gunBody.aimParent
                );
        }
    }

    void ForceReassemble()
    {
        if(transform.childCount > 0)
            Destroy(transform.GetChild(0).gameObject);
        AssembleWeapon();
    }

    private void Start()
    {
        if (assembleOnStart)
            AssembleWeapon();
    }

    private void Update()
    {
        if (forceReassemble)
        {
            ForceReassemble();
            forceReassemble = false;            
        }

    }

}
