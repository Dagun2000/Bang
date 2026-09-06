using UnityEngine;

[System.Serializable]
public class CharacterCustomData
{
    // 1. 얼굴+피부 세트
    public int facePresetIndex = 0;      // 0~4
    
    // 2. 머리
    public int hairIndex = 0;            // 0~7
    public int hairColorIndex = 0;       // 0~4
    
    // 3. 안경
    public int glassesIndex = -1;        // -1=없음, 0=Glasses, 1=SunGlasses
    
    // 4. 벨트
    public int beltType = 0;             // 0=Belt, 1=Loops
    
    // 5. 자켓
    public int jacketType = 0;           // 0=Open, 1=Closed, 2=No
    public int jacketColorIndex = 0;     // 0~9
    
    // 6. 셔츠 색상 (종류는 자켓에 따라 자동)
    public int shirtColorIndex = 0;      // 0~7
    
    // 7. 넥타이
    public int tieType = 0;              // 0=Tie, 1=Butterfly, 2=No
    public int tieColorIndex = 0;        // 0~9
    
    // 8. 조끼
    public int waistcoatType = 1;        // 0=Yes, 1=No
    public int waistcoatColorIndex = 0;  // 0~9
    
    // 9. 바지
    public int pantsColorIndex = 0;      // 0~9
    
    // 10. 신발
    public int shoesColorIndex = 0;      // 0~3
    
    public string Serialize()
    {
        return $"{facePresetIndex},{hairIndex},{hairColorIndex},{glassesIndex},{beltType}," +
               $"{jacketType},{jacketColorIndex},{shirtColorIndex}," +
               $"{tieType},{tieColorIndex},{waistcoatType},{waistcoatColorIndex}," +
               $"{pantsColorIndex},{shoesColorIndex}";
    }
    
    public static CharacterCustomData Deserialize(string data)
    {
        var d = new CharacterCustomData();
        if (string.IsNullOrEmpty(data)) return d;
        
        var parts = data.Split(',');
        if (parts.Length >= 14)
        {
            d.facePresetIndex = int.Parse(parts[0]);
            d.hairIndex = int.Parse(parts[1]);
            d.hairColorIndex = int.Parse(parts[2]);
            d.glassesIndex = int.Parse(parts[3]);
            d.beltType = int.Parse(parts[4]);
            d.jacketType = int.Parse(parts[5]);
            d.jacketColorIndex = int.Parse(parts[6]);
            d.shirtColorIndex = int.Parse(parts[7]);
            d.tieType = int.Parse(parts[8]);
            d.tieColorIndex = int.Parse(parts[9]);
            d.waistcoatType = int.Parse(parts[10]);
            d.waistcoatColorIndex = int.Parse(parts[11]);
            d.pantsColorIndex = int.Parse(parts[12]);
            d.shoesColorIndex = int.Parse(parts[13]);
        }
        return d;
    }
}