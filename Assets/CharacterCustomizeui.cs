using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCustomizeUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject customizePanel;
    
    [Header("Preview")]
    public GameObject previewCharacter;
    public Camera previewCamera;
    private CharacterCustomizer previewCustomizer;
    
    [Header("Value Texts")]
    public TextMeshProUGUI faceValueText;
    public TextMeshProUGUI hairValueText;
    public TextMeshProUGUI hairColorValueText;
    public TextMeshProUGUI glassesValueText;
    public TextMeshProUGUI jacketTypeValueText;
    public TextMeshProUGUI jacketColorValueText;
    public TextMeshProUGUI shirtColorValueText;
    public TextMeshProUGUI tieTypeValueText;
    public TextMeshProUGUI tieColorValueText;
    public TextMeshProUGUI waistcoatValueText;
    public TextMeshProUGUI waistcoatColorValueText;
    public TextMeshProUGUI beltTypeValueText;
    public TextMeshProUGUI pantsColorValueText;
    public TextMeshProUGUI shoesColorValueText;
    
    // 최대값
    private readonly int maxFace = 5;
    private readonly int maxHair = 8;
    private readonly int maxHairColor = 5;
    private readonly int maxGlasses = 2;      // 0=Glasses, 1=SunGlasses (없음은 -1)
    private readonly int maxJacketType = 3;   // 0=Open, 1=Closed, 2=No
    private readonly int maxJacketColor = 10;
    private readonly int maxShirtColor = 8;
    private readonly int maxTieType = 3;      // 0=Tie, 1=Butterfly, 2=No
    private readonly int maxTieColor = 10;
    private readonly int maxWaistcoatType = 2; // 0=Yes, 1=No
    private readonly int maxWaistcoatColor = 10;
    private readonly int maxBeltType = 2;     // 0=Belt, 1=No
    private readonly int maxPantsColor = 10;
    private readonly int maxShoesColor = 4;
    
    private CharacterCustomData tempData = new CharacterCustomData();
    private CharacterCustomData savedData = new CharacterCustomData();
    
    void Start()
    {
        if (customizePanel != null)
            customizePanel.SetActive(false);
            
        if (previewCharacter != null)
        {
            previewCustomizer = previewCharacter.GetComponent<CharacterCustomizer>();
            previewCharacter.SetActive(false);
        }
        
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(false);
            
        LoadSavedData();
    }
    
    void LoadSavedData()
    {
        string saved = PlayerPrefs.GetString("MyCharacterCustom", "");
        savedData = CharacterCustomData.Deserialize(saved);
        tempData = CharacterCustomData.Deserialize(saved);
    }
    
    public void OpenCustomizePanel()
    {
        if (customizePanel == null) return;
        
        tempData = CharacterCustomData.Deserialize(savedData.Serialize());
        customizePanel.SetActive(true);
        
        if (previewCharacter != null)
            previewCharacter.SetActive(true);
            
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(true);
            
        UpdateAllUI();
        ApplyPreview();
    }
    
    public void CloseCustomizePanel()
    {
        if (customizePanel != null)
            customizePanel.SetActive(false);
            
        if (previewCharacter != null)
            previewCharacter.SetActive(false);
            
        if (previewCamera != null)
            previewCamera.gameObject.SetActive(false);
    }
    
    public void ConfirmCustomization()
    {
        savedData = CharacterCustomData.Deserialize(tempData.Serialize());
        
        PlayerPrefs.SetString("MyCharacterCustom", savedData.Serialize());
        PlayerPrefs.Save();
        
        if (SteamLobby.Instance != null && SteamLobby.Instance.CurrentLobby.HasValue)
        {
            SteamLobby.Instance.CurrentLobby.Value.SetMemberData("CharacterCustom", savedData.Serialize());
        }
        
        CloseCustomizePanel();
    }
    
    // ============ 버튼 핸들러 ============
    
    // 얼굴+피부
    public void OnFaceLeft() { tempData.facePresetIndex = Wrap(tempData.facePresetIndex - 1, maxFace); OnValueChanged(); }
    public void OnFaceRight() { tempData.facePresetIndex = Wrap(tempData.facePresetIndex + 1, maxFace); OnValueChanged(); }
    
    // 머리
    public void OnHairLeft() { tempData.hairIndex = Wrap(tempData.hairIndex - 1, maxHair); OnValueChanged(); }
    public void OnHairRight() { tempData.hairIndex = Wrap(tempData.hairIndex + 1, maxHair); OnValueChanged(); }
    public void OnHairColorLeft() { tempData.hairColorIndex = Wrap(tempData.hairColorIndex - 1, maxHairColor); OnValueChanged(); }
    public void OnHairColorRight() { tempData.hairColorIndex = Wrap(tempData.hairColorIndex + 1, maxHairColor); OnValueChanged(); }
    
    // 안경
    public void OnGlassesLeft() { tempData.glassesIndex = WrapWithNone(tempData.glassesIndex - 1, maxGlasses); OnValueChanged(); }
    public void OnGlassesRight() { tempData.glassesIndex = WrapWithNone(tempData.glassesIndex + 1, maxGlasses); OnValueChanged(); }
    
    // 자켓
    public void OnJacketTypeLeft() { tempData.jacketType = Wrap(tempData.jacketType - 1, maxJacketType); OnValueChanged(); }
    public void OnJacketTypeRight() { tempData.jacketType = Wrap(tempData.jacketType + 1, maxJacketType); OnValueChanged(); }
    public void OnJacketColorLeft() { tempData.jacketColorIndex = Wrap(tempData.jacketColorIndex - 1, maxJacketColor); OnValueChanged(); }
    public void OnJacketColorRight() { tempData.jacketColorIndex = Wrap(tempData.jacketColorIndex + 1, maxJacketColor); OnValueChanged(); }
    
    // 셔츠 (색상만)
    public void OnShirtColorLeft() { tempData.shirtColorIndex = Wrap(tempData.shirtColorIndex - 1, maxShirtColor); OnValueChanged(); }
    public void OnShirtColorRight() { tempData.shirtColorIndex = Wrap(tempData.shirtColorIndex + 1, maxShirtColor); OnValueChanged(); }
    
    // 넥타이
    public void OnTieTypeLeft() { tempData.tieType = Wrap(tempData.tieType - 1, maxTieType); OnValueChanged(); }
    public void OnTieTypeRight() { tempData.tieType = Wrap(tempData.tieType + 1, maxTieType); OnValueChanged(); }
    public void OnTieColorLeft() { tempData.tieColorIndex = Wrap(tempData.tieColorIndex - 1, maxTieColor); OnValueChanged(); }
    public void OnTieColorRight() { tempData.tieColorIndex = Wrap(tempData.tieColorIndex + 1, maxTieColor); OnValueChanged(); }
    
    // 조끼
    public void OnWaistcoatLeft() { tempData.waistcoatType = Wrap(tempData.waistcoatType - 1, maxWaistcoatType); OnValueChanged(); }
    public void OnWaistcoatRight() { tempData.waistcoatType = Wrap(tempData.waistcoatType + 1, maxWaistcoatType); OnValueChanged(); }
    public void OnWaistcoatColorLeft() { tempData.waistcoatColorIndex = Wrap(tempData.waistcoatColorIndex - 1, maxWaistcoatColor); OnValueChanged(); }
    public void OnWaistcoatColorRight() { tempData.waistcoatColorIndex = Wrap(tempData.waistcoatColorIndex + 1, maxWaistcoatColor); OnValueChanged(); }
    
    // 벨트
    public void OnBeltTypeLeft() { tempData.beltType = Wrap(tempData.beltType - 1, maxBeltType); OnValueChanged(); }
    public void OnBeltTypeRight() { tempData.beltType = Wrap(tempData.beltType + 1, maxBeltType); OnValueChanged(); }
    
    // 바지
    public void OnPantsColorLeft() { tempData.pantsColorIndex = Wrap(tempData.pantsColorIndex - 1, maxPantsColor); OnValueChanged(); }
    public void OnPantsColorRight() { tempData.pantsColorIndex = Wrap(tempData.pantsColorIndex + 1, maxPantsColor); OnValueChanged(); }
    
    // 신발
    public void OnShoesColorLeft() { tempData.shoesColorIndex = Wrap(tempData.shoesColorIndex - 1, maxShoesColor); OnValueChanged(); }
    public void OnShoesColorRight() { tempData.shoesColorIndex = Wrap(tempData.shoesColorIndex + 1, maxShoesColor); OnValueChanged(); }
    
    // ============ 헬퍼 ============
    
    void OnValueChanged()
    {
        UpdateAllUI();
        ApplyPreview();
    }
    
    void ApplyPreview()
    {
        if (previewCustomizer != null)
            previewCustomizer.ApplyCustomization(tempData);
    }
    
    int Wrap(int value, int max)
    {
        if (value < 0) return max - 1;
        if (value >= max) return 0;
        return value;
    }
    
    int WrapWithNone(int value, int max)
    {
        // -1 = 없음, 0 ~ max-1 = 있음
        if (value < -1) return max - 1;
        if (value >= max) return -1;
        return value;
    }
    
    void UpdateAllUI()
    {
        SetText(faceValueText, $"{tempData.facePresetIndex + 1}/{maxFace}");
        SetText(hairValueText, $"{tempData.hairIndex + 1}/{maxHair}");
        SetText(hairColorValueText, $"{tempData.hairColorIndex + 1}/{maxHairColor}");
        SetText(glassesValueText, $"{tempData.glassesIndex + 2}/3");  // -1→1, 0→2, 1→3
        SetText(jacketTypeValueText, GetJacketTypeName(tempData.jacketType));
        SetText(jacketColorValueText, $"{tempData.jacketColorIndex + 1}/{maxJacketColor}");
        SetText(shirtColorValueText, $"{tempData.shirtColorIndex + 1}/{maxShirtColor}");
        SetText(tieTypeValueText, GetTieTypeName(tempData.tieType));
        SetText(tieColorValueText, $"{tempData.tieColorIndex + 1}/{maxTieColor}");
        SetText(waistcoatValueText, $"{tempData.waistcoatType + 1}/{maxWaistcoatType}");
        SetText(waistcoatColorValueText, $"{tempData.waistcoatColorIndex + 1}/{maxWaistcoatColor}");
        SetText(beltTypeValueText, $"{tempData.beltType + 1}/{maxBeltType}");
        SetText(pantsColorValueText, $"{tempData.pantsColorIndex + 1}/{maxPantsColor}");
        SetText(shoesColorValueText, $"{tempData.shoesColorIndex + 1}/{maxShoesColor}");
    }
    
    void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null) text.text = value;
    }
    
    // 0=Open, 1=Closed, 2=No
    string GetJacketTypeName(int type) => type switch { 0 => "Open", 1 => "Closed", 2 => "없음", _ => "Open" };
    
    // 0=Tie, 1=Butterfly, 2=No
    string GetTieTypeName(int type) => type switch { 0 => "Tie", 1 => "Butterfly", 2 => "없음", _ => "Tie" };
    
    public CharacterCustomData GetSavedData()
    {
        return savedData;
    }
}