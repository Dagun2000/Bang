using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Threading.Tasks;

public class SteamAvatarLoader : MonoBehaviour
{
    public RawImage avatarImage;
    
    private Texture2D avatarTexture;
    
    public async void LoadAvatar(SteamId steamId)
    {
        if (avatarImage == null) return;
        
        var image = await SteamFriends.GetLargeAvatarAsync(steamId);
        
        if (image.HasValue)
        {
            avatarTexture = new Texture2D((int)image.Value.Width, (int)image.Value.Height, TextureFormat.RGBA32, false);
            avatarTexture.LoadRawTextureData(image.Value.Data);
            avatarTexture.Apply();
            
            avatarImage.texture = avatarTexture;
            
            // Y축 뒤집기
            avatarImage.uvRect = new Rect(0, 1, 1, -1);
        }
    }
    
    public void LoadMyAvatar()
    {
        LoadAvatar(SteamClient.SteamId);
    }
    
    void OnDestroy()
    {
        if (avatarTexture != null)
        {
            Destroy(avatarTexture);
        }
    }
}