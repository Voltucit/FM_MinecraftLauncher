namespace FMLauncher;

public class LauncherSettings
{
    public int LoginMode { get; set; } = 1;
    public string Playername { get; set; } = "";
    public string GameVersion { get; set; } = ""; 
    public string JavaPath { get; set; } = "";
    
    public double Memory { get; set; } = 4;
  
    public  string OnlineUserName { get; set; }
    public  string OnlineUserUUID { get; set; }
    
    public  string AccessToken { get; set; }
    public   string RefreshToken { get; set; }
    public DateTime TokenExpiry { get; set; }
    public int ExpiresIn { get; set; }
}