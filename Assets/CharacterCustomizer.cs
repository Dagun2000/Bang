using UnityEngine;

public class CharacterCustomizer : MonoBehaviour
{
    private BmanCustomize bmanCustomize;
    
    void Awake()
    {
        bmanCustomize = GetComponent<BmanCustomize>();
    }
    
    public void ApplyCustomization(CharacterCustomData data)
    {
        if (bmanCustomize == null)
        {
            bmanCustomize = GetComponent<BmanCustomize>();
            if (bmanCustomize == null)
            {
                Debug.LogError("BmanCustomize를 찾을 수 없습니다!");
                return;
            }
        }
        
        // 값 계산 (범위 체크 포함)
        int faceTy = Mathf.Clamp(data.facePresetIndex, 0, 4);
        int skinTy = faceTy;
        int eyeCo = 0;  // 눈 색상 고정
        int glassesTy = Mathf.Clamp(data.glassesIndex + 1, 0, 2);  // 0=No, 1=Glasses, 2=SunGlasses
        int hairTy = Mathf.Clamp(data.hairIndex, 0, 7);
        int hairCo = Mathf.Clamp(data.hairColorIndex, 0, 4);
        int jacketTy = Mathf.Clamp(data.jacketType, 0, 2);  // 0=Open, 1=Closed, 2=No
        int waistcoatTy = data.waistcoatType == 0 ? 0 : 1;  // 0=Yes, 1=No
        int tieTy = Mathf.Clamp(data.tieType, 0, 2);
        int beltTy = data.beltType == 0 ? 0 : 1;
        int handkerchiefTy = 1;  // 손수건 항상 없음
        int jacketCo = Mathf.Clamp(data.jacketColorIndex, 0, 9);
        int shirtCo = Mathf.Clamp(data.shirtColorIndex, 0, 7);
        int waistcoatCo = Mathf.Clamp(data.waistcoatColorIndex, 0, 9);
        int tieCo = Mathf.Clamp(data.tieColorIndex, 0, 9);
        int pantsCo = Mathf.Clamp(data.pantsColorIndex, 0, 9);
        int shoesCo = Mathf.Clamp(data.shoesColorIndex, 0, 3);
        int handkerchiefCo = 0;  // 손수건 색상 고정
        
        bmanCustomize.charCustomize(
            faceTy, skinTy, eyeCo, glassesTy, hairTy, hairCo,
            jacketTy, waistcoatTy, tieTy, beltTy, handkerchiefTy,
            jacketCo, shirtCo, waistcoatCo, tieCo, pantsCo, shoesCo, handkerchiefCo
        );
    }
}