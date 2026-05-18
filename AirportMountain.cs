using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Management; // Untuk HWID
using System.Text;
using System.IO; // Ditambahkan untuk simpan Alias
using GTA;
using GTA.Math;
using GTA.UI;
using GTA.Native;

public class CoordinateProgressBar : Script
{
    // ==============================================================
    // ⚙️ PENGATURAN
    // ==============================================================
    private string serverUrl = "https://script.google.com/macros/s/AKfycby8v4KIkwS2Goenxr-CLJr0FwQRulFQISvkVnVSY2gA4eYYEcCfwy6oMtrFGImGKjQw5Q/exec"; 
    private const string currentVersion = "1.0.7"; // Disamakan dengan server agar tidak muncul notifikasi update
    private Vector3 startCoord = new Vector3(-1388.600f, -3111.610f, 13.9426f); 
    private Vector3 targetCoord = new Vector3(501.7521f, 5601.2607f, 796.6726f); 
    // ==============================================================

    private bool isTracking = true; 
    private float totalDistance = 0f;
    private int winTimer = 0;
    private int finishCountdown = 0; // Target time for 5s countdown
    private int countdownDuration = 5;
    private int winCount = 0;
    private bool isInitialized = false;
    
    // License Status
    private enum LicenseStatus { Checking, Unauthorized, Pending, Active, Cheating }
    private LicenseStatus currentStatus = LicenseStatus.Checking;
    private string myHwid = ""; // HWID PC asli
    private string storedHwidHash = ""; // Hash HWID yang terdaftar di .ini
    private string myAlias = "";
    private bool showHealthPercentage = true; 
    private string deathTextMsg = "YAAHH TURU DEK!!!";

    private bool isChecking = false;
    private int nextCheckTime = 0;
    private bool isUpdateDownloaded = false;
    private string newVersionAvailable = "";

    public CoordinateProgressBar()
    {
        TryApplyUpdate(); 
        
        myHwid = GetMachineId();
        LoadSettings(); 
        
        // Cek jika Hash HWID di .ini berbeda dengan Hash HWID PC asli (berarti file di-copy)
        string currentHwidHash = GetHash(myHwid);
        if (!string.IsNullOrEmpty(storedHwidHash) && storedHwidHash != currentHwidHash)
        {
            currentStatus = LicenseStatus.Cheating;
        }
        else
        {
            CheckLicenseAsync(); 
        }

        // Tampilkan Notifikasi untuk memastikan user tahu versi yang jalan
        Notification.Show($"~g~Tamsfinity Script Loaded~n~~w~Version: {currentVersion}-Safe~n~ID: {GetShortHwid()}");

        Tick += OnTick;
    }

    private string GetShortHwid()
    {
        if (myHwid.Length > 8) return myHwid.Substring(0, 8) + "...";
        return myHwid;
    }

    private void TryApplyUpdate()
    {
        try {
            string currentPath = "scripts/AirportMountain.dll";
            string newPath = "scripts/AirportMountain.dll.new";
            string oldPath = "scripts/AirportMountain.dll.old";

            // Jika ada file .new (artinya download sukses sebelum restart)
            if (File.Exists(newPath))
            {
                // Hapus file .old jika masih ada
                if (File.Exists(oldPath)) File.Delete(oldPath);
                
                // Rename diri kita sendiri ke .old (diperbolehkan Windows saat running)
                if (File.Exists(currentPath)) File.Move(currentPath, oldPath);
                
                // Pindahkan file .new ke nama utama
                File.Move(newPath, currentPath);
                
                // File sekarang sudah ter-swap untuk game boot berikutnya!
            }
        } catch { }
    }

    private string GetHash(string input)
    {
        using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input + "TAMSFINTY_SALT"));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    private void LoadSettings()
    {
        string filePath = "scripts/AirportMountain.ini";
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (line.StartsWith("Alias=")) myAlias = line.Replace("Alias=", "").Trim();
                if (line.StartsWith("ShowPercentage=")) bool.TryParse(line.Replace("ShowPercentage=", "").Trim(), out showHealthPercentage);
                if (line.StartsWith("HID=")) storedHwidHash = line.Replace("HID=", "").Trim();
                if (line.StartsWith("DeathText=")) deathTextMsg = line.Replace("DeathText=", "").Trim();
                if (line.StartsWith("CountdownDuration=")) int.TryParse(line.Replace("CountdownDuration=", "").Trim(), out countdownDuration);
                if (line.StartsWith("WinCount=")) int.TryParse(line.Replace("WinCount=", "").Trim(), out winCount);
            }
        }

        // Jika baru/kosong, simpan Hash HWID asli ke .ini
        if (string.IsNullOrEmpty(storedHwidHash))
        {
            storedHwidHash = GetHash(myHwid);
            SaveSettings();
        }

        if (string.IsNullOrEmpty(myAlias))
        {
            Random rnd = new Random();
            myAlias = "User-" + rnd.Next(1000, 9999).ToString();
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        string filePath = "scripts/AirportMountain.ini";
        try
        {
            string content = $"Alias={myAlias}\nShowPercentage={showHealthPercentage}\nHID={storedHwidHash}\nDeathText={deathTextMsg}\nCountdownDuration={countdownDuration}\nWinCount={winCount}";
            File.WriteAllText(filePath, content);
        }
        catch { }
    }

    private string GetMachineId()
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            // 1. CPU ID
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc) {
                sb.Append(mo.Properties["ProcessorId"]?.Value?.ToString());
                break;
            }

            // 2. BaseBoard (Motherboard) Serial
            mc = new ManagementClass("Win32_BaseBoard");
            moc = mc.GetInstances();
            foreach (ManagementObject mo in moc) {
                sb.Append(mo.Properties["SerialNumber"]?.Value?.ToString());
                break;
            }

            // 3. Disk Serial (Paling unik)
            mc = new ManagementClass("Win32_DiskDrive");
            moc = mc.GetInstances();
            foreach (ManagementObject mo in moc) {
                sb.Append(mo.Properties["SerialNumber"]?.Value?.ToString());
                break;
            }
        }
        catch { }

        string rawId = sb.ToString().Replace(" ", "");
        if (rawId.Length < 10) rawId += Environment.MachineName + Environment.UserName;
        
        // Return Hash agar rapi di Sheet
        return GetHash(rawId).Substring(0, 20).ToUpper();
    }

    private async void CheckLicenseAsync()
    {
        if (isChecking) return;
        isChecking = true;

        try
        {
            // SANGAT PENTING: Gunakan TLS 1.2 agar bisa konek ke server Google/Modern
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Tamsfinity-GTA-License-System");
                client.Timeout = TimeSpan.FromSeconds(15);
                
                // Gunakan FormUrlEncoded agar lebih kompatibel dengan Google Apps Script default
                var postData = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "hwid", myHwid },
                    { "alias", myAlias }
                };

                var content = new FormUrlEncodedContent(postData);
                var response = await client.PostAsync(serverUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    
                    // Cek jika response memang JSON (paling tidak mengandung bracket)
                    if (!rawJson.Trim().StartsWith("{")) {
                        currentStatus = LicenseStatus.Unauthorized;
                        return;
                    }

                    // Simple JSON Parser (Tanpa Dependency)
                    string status = GetJsonValue(rawJson, "status");
                    string latestVer = GetJsonValue(rawJson, "version");
                    string downloadUrl = GetJsonValue(rawJson, "url");
                    string confirmedHwid = GetJsonValue(rawJson, "hwid");

                    // SECURITY FIX: Pastikan HWID yang dibalas server sama dengan HWID kita
                    // Dan status harus "active" secara eksplisit
                    if (status == "active" && confirmedHwid == myHwid) 
                    {
                        currentStatus = LicenseStatus.Active;
                    }
                    else if (status == "pending")
                    {
                        currentStatus = LicenseStatus.Pending;
                    }
                    else if (status == "unauthorized" || status == "error")
                    {
                        currentStatus = LicenseStatus.Unauthorized;
                    }
                    else if (status == "cheating")
                    {
                        currentStatus = LicenseStatus.Cheating;
                    }
                    else
                    {
                        // Jika HWID tidak cocok atau status aneh, tolak akses
                        currentStatus = LicenseStatus.Unauthorized;
                    }

                    // Cek Update
                    if (!string.IsNullOrEmpty(latestVer) && latestVer != currentVersion && !isUpdateDownloaded)
                    {
                        newVersionAvailable = latestVer;
                        DownloadUpdateAsync(downloadUrl);
                    }
                }
                else
                {
                    currentStatus = LicenseStatus.Unauthorized;
                }
            }
        }
        catch { }
        finally { isChecking = false; }
    }

    private string GetJsonValue(string json, string key)
    {
        try {
            string searchKey = $"\"{key}\":\"";
            int start = json.IndexOf(searchKey);
            if (start == -1) return "";
            start += searchKey.Length;
            int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start);
        } catch { return ""; }
    }

    private async void DownloadUpdateAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Tamsfinity-GTA-Updater");
                byte[] data = await client.GetByteArrayAsync(url);
                string path = "scripts/AirportMountain.dll.new";
                File.WriteAllBytes(path, data);
                isUpdateDownloaded = true;
            }
        }
        catch { }
    }

    private void SetupDistance()
    {
        Vector3 start2D = startCoord;
        start2D.Z = 0;
        Vector3 target2D = targetCoord;
        target2D.Z = 0;
        totalDistance = start2D.DistanceTo(target2D);
        if (totalDistance < 1.0f) totalDistance = 1.0f; 
        isInitialized = true;
    }

    private void OnTick(object sender, EventArgs e)
    {
        try
        {
            if (currentStatus == LicenseStatus.Cheating)
            {
                DrawLicenseOverlay();
                return;
            }

            if (currentStatus != LicenseStatus.Active)
            {
                DrawLicenseOverlay();
                if (Game.GameTime > nextCheckTime)
                {
                    CheckLicenseAsync();
                    nextCheckTime = Game.GameTime + 10000; 
                }
                return;
            }

            if (Game.Player == null || Game.Player.Character == null || !Game.Player.Character.Exists()) return;

            // Cek jika mati
            if (Game.Player.Character.IsDead)
            {
                // Menghentikan script 'wasted' bawaan dan sembunyikan HUD
                Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, "wasted");
                Function.Call(Hash.DISPLAY_HUD, false); 
                DrawDeathText();
                return;
            }
            else
            {
                Function.Call(Hash.DISPLAY_HUD, true);
            }

            if (!isInitialized) SetupDistance();

            DrawHealthBar();
            DrawWinCountDisplay();

            // CELEBRATION MODE
            if (Game.GameTime < winTimer)
            {
                DrawWinText();
                SpawnFireworks(); 

                if (winTimer - Game.GameTime < 100)
                {
                    ResetGame();
                }
                return; 
            }

            if (!isTracking) return;

            Vector3 currentPos = Game.Player.Character.Position;
            currentPos.Z = 0;
            Vector3 target2D = targetCoord;
            target2D.Z = 0;
            float currentDistance = currentPos.DistanceTo(target2D);

            float progress = 1.0f - (currentDistance / totalDistance);
            if (progress < 0f) progress = 0f;
            if (progress > 1f) progress = 1f;

            if (currentDistance < 30.0f)
            {
                if (finishCountdown == 0)
                {
                    finishCountdown = Game.GameTime + (countdownDuration * 1000);
                }

                if (Game.GameTime >= finishCountdown)
                {
                    isTracking = false;
                    finishCountdown = 0;
                    winCount++;
                    SaveSettings();
                    winTimer = Game.GameTime + 15000;
                    Audio.PlaySoundFrontendAndForget("Mission_Pass_Notify", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS");
                    return;
                }

                DrawFinishCountdown();
            }
            else
            {
                finishCountdown = 0; // Reset jika keluar radius
            }

            if (isTracking) DrawProgressBar(progress, currentDistance);
            if (isUpdateDownloaded) DrawUpdateNotification();
        }
        catch { }
    }

    private void DrawUpdateNotification()
    {
        Function.Call(Hash.DRAW_RECT, 1.0f - 0.08f, 0.04f, 0.15f, 0.04f, 184, 134, 11, 200, 0); // Gold Box
        var updateText = new TextElement($"UPDATE v{newVersionAvailable} READY!", new PointF(1280f - 105f, 18f), 0.28f);
        updateText.Alignment = Alignment.Center;
        updateText.Color = Color.Black;
        updateText.Outline = true;
        updateText.Draw();

        var subUpdate = new TextElement("Restart game to apply", new PointF(1280f - 105f, 35f), 0.22f);
        subUpdate.Alignment = Alignment.Center;
        subUpdate.Color = Color.White;
        subUpdate.Draw();
    }

    private void ResetGame()
    {
        Ped playerChar = Game.Player.Character;
        playerChar.Position = startCoord;
        isTracking = true;
        isInitialized = false; 
    }

    private int nextFireworkTime = 0;
    private string[] fireworkEffects = { 
        "scr_indep_firework_shotburst", 
        "scr_indep_firework_starburst", 
        "scr_indep_firework_trailburst",
        "scr_indep_firework_trailburst_spawn",
        "scr_indep_firework_fountain"
    };

    private void SpawnFireworks()
    {
        if (Game.GameTime < nextFireworkTime) return;
        
        Random rnd = new Random();
        nextFireworkTime = Game.GameTime + rnd.Next(200, 500); 

        string ptfxAsset = "scr_indep_fireworks";
        Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, ptfxAsset);
        if (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, ptfxAsset)) return;

        Ped player = Game.Player.Character;
        int burstCount = rnd.Next(2, 5);
        for (int i = 0; i < burstCount; i++)
        {
            string ptfxName = fireworkEffects[rnd.Next(fireworkEffects.Length)];
            float offsetX = (float)(rnd.NextDouble() * 40.0 - 20.0);
            float offsetY = (float)(rnd.NextDouble() * 40.0 - 20.0);
            float offsetZ = (float)(rnd.NextDouble() * 20.0 + 5.0);
            Vector3 fireworkPos = player.Position + new Vector3(offsetX, offsetY, offsetZ);

            Function.Call(Hash.USE_PARTICLE_FX_ASSET, ptfxAsset);
            float scale = (float)(rnd.NextDouble() * 2.5 + 0.8); 
            
            Function.Call(Hash.START_NETWORKED_PARTICLE_FX_NON_LOOPED_AT_COORD, 
                ptfxName, fireworkPos.X, fireworkPos.Y, fireworkPos.Z, 
                0.0f, 0.0f, 0.0f, scale, false, false, false, false);
            
            string[] sounds = { "Firework_Rocket_Explosion", "Firework_Rocket_Explosion_Secondary" };
            Audio.PlaySoundFrontendAndForget(sounds[rnd.Next(sounds.Length)], "DLC_WRS_FIREWORK_SOUNDS");
        }
    }

    private void DrawLicenseOverlay()
    {
        string msg = "";
        Color msgColor = Color.White;

        switch (currentStatus)
        {
            case LicenseStatus.Checking: msg = "CHECKING LICENSE..."; break;
            case LicenseStatus.Pending: msg = "LICENSE PENDING - WAITING FOR CONFIRMATION"; break;
            case LicenseStatus.Unauthorized: msg = "UNAUTHORIZED - FAILED TO CONTACT SERVER"; break;
            case LicenseStatus.Cheating: 
                msg = "1 PC 1 LISENSI YA JANGAN CURANG!"; 
                msgColor = Color.Red;
                break;
        }

        var text = new TextElement(msg, new PointF(1280f / 2f, 720f / 2f), 0.5f);
        text.Alignment = Alignment.Center;
        text.Color = msgColor;
        text.Outline = true;
        text.Shadow = true;
        text.Draw();

        if (currentStatus != LicenseStatus.Cheating)
        {
            var aliasText = new TextElement($"YOUR LICENSE ID: {myAlias}", new PointF(1280f / 2f, 720f / 2f + 30f), 0.4f);
            aliasText.Alignment = Alignment.Center;
            aliasText.Color = Color.Gold;
            aliasText.Outline = true;
            aliasText.Draw();

            // SANGAT PENTING: Watermark Debug untuk PC No. 5
            var debugInfo = new TextElement($"SERVER: {serverUrl.Substring(0, 30)}...", new PointF(1280f / 2f, 720f - 100f), 0.22f);
            debugInfo.Alignment = Alignment.Center;
            debugInfo.Color = Color.Gray;
            debugInfo.Draw();

            var hwidInfo = new TextElement($"MY HWID: {myHwid} | v{currentVersion}", new PointF(1280f / 2f, 720f - 75f), 0.3f);
            hwidInfo.Alignment = Alignment.Center;
            hwidInfo.Color = Color.White;
            hwidInfo.Draw();

            var statusInfo = new TextElement($"LAST STATUS: {currentStatus} | IS_CHECKING: {isChecking}", new PointF(1280f / 2f, 720f - 50f), 0.3f);
            statusInfo.Alignment = Alignment.Center;
            statusInfo.Color = Color.Aqua;
            statusInfo.Draw();
        }
    }

    private void DrawProgressBar(float progress, float remainingDistance)
    {
        float screenX = 0.5f; 
        float screenY = 0.07f; // Turunkan sedikit agar teks besar tidak terpotong atas
        float barWidth = 0.45f; // Diperlebar dari 0.35
        float barHeight = 0.025f; // Dipertebal dari 0.018

        Function.Call(Hash.DRAW_RECT, screenX, screenY, barWidth + 0.004f, barHeight + 0.006f, 0, 0, 0, 200, 0);
        Function.Call(Hash.DRAW_RECT, screenX, screenY, barWidth, barHeight, 40, 40, 40, 180, 0);
        float currentBarWidth = barWidth * progress;
        float fgX = (screenX - barWidth / 2f) + (currentBarWidth / 2f);
        if (currentBarWidth > 0)
        {
            Function.Call(Hash.DRAW_RECT, fgX, screenY, currentBarWidth, barHeight, 34, 139, 34, 255, 0); 
            Function.Call(Hash.DRAW_RECT, fgX, screenY, currentBarWidth, barHeight * 0.4f, 50, 205, 50, 150, 0);
        }

        int percent = (int)(progress * 100f);
        string text = $"PROGRES: {percent}%  |  DISTANCE: {Math.Round(remainingDistance, 0)}m";
        var distanceText = new TextElement(text, new PointF(1280f / 2f, 720f * screenY + 25f), 0.8f); // Diperbesar ke 0.8
        distanceText.Alignment = Alignment.Center;
        distanceText.Color = Color.White;
        distanceText.Outline = true;
        distanceText.Draw();

        var creditsText = new TextElement("by : tamsfinity", new PointF(1280f / 2f, 720f * screenY - 6f), 0.22f);
        creditsText.Alignment = Alignment.Center;
        creditsText.Color = Color.FromArgb(220, 255, 255, 255);
        creditsText.Outline = true;
        creditsText.Draw();
    }

    private void DrawHealthBar()
    {
        float screenX = 0.5f; 
        float screenY = 0.92f; 
        float barWidth = 0.25f; 
        float barHeight = 0.045f; // Dipertebal ke 0.045 agar boks lebih lega untuk teks 0.8f

        Ped playerChar = Game.Player.Character;
        float health = (float)playerChar.Health;
        float maxHealth = (float)playerChar.MaxHealth;
        float healthProgress = health / maxHealth;
        if (healthProgress < 0f) healthProgress = 0f;
        if (healthProgress > 1f) healthProgress = 1f;

        // Background
        Function.Call(Hash.DRAW_RECT, screenX, screenY, barWidth + 0.003f, barHeight + 0.005f, 0, 0, 0, 200, 0);
        Function.Call(Hash.DRAW_RECT, screenX, screenY, barWidth, barHeight, 40, 40, 40, 180, 0);
        
        float currentBarWidth = barWidth * healthProgress;
        float fgX = (screenX - barWidth / 2f) + (currentBarWidth / 2f);
        if (currentBarWidth > 0)
        {
            int r, g;
            if (healthProgress > 0.5f) {
                r = (int)(255 * (1 - healthProgress) * 2);
                g = 255;
            } else {
                r = 255;
                g = (int)(255 * healthProgress * 2);
            }
            
            Function.Call(Hash.DRAW_RECT, fgX, screenY, currentBarWidth, barHeight, r, g, 0, 255, 0); 
            Function.Call(Hash.DRAW_RECT, fgX, screenY, currentBarWidth, barHeight * 0.4f, 255, 255, 255, 100, 0); 
        }

        string healthMsg = showHealthPercentage ? $"{(int)(healthProgress * 100f)}%" : $"{(int)health}/{(int)maxHealth}";
        // Posisi Y -22f terbukti lebih Center untuk teks skala 0.8f pada bar setebal 0.045
        var healthText = new TextElement(healthMsg, new PointF(1280f / 2f, 720f * screenY - 22f), 0.8f); 
        healthText.Alignment = Alignment.Center;
        healthText.Color = Color.White;
        healthText.Outline = true;
        healthText.Draw();
    }

    private void DrawWinText()
    {
        float wave = (float)Math.Sin(Game.GameTime * 0.005f); 
        float pulseScale = 1.0f + (wave * 0.05f); 
        
        int r = 255;
        int g = (int)(200 + (wave * 30)); 
        int b = 0;
        Color dynamicColor = Color.FromArgb(r, g, b);

        var winText = new TextElement("YOU WIN!", new PointF(1280f / 2f, (720f / 2f) - 50f), 2.8f * pulseScale);
        winText.Alignment = Alignment.Center;
        winText.Color = dynamicColor;
        winText.Outline = true;
        winText.Shadow = true;
        winText.Draw();

        int secondsLeft = Math.Max(0, (winTimer - Game.GameTime) / 1000);
        // Moved up (0.86 instead of 0.95), scaled up (0.6 instead of 0.38), and text changed
        var resetText = new TextElement($"Back To Airport In {secondsLeft}s...", new PointF(1280f / 2f, 720f * 0.86f - 35f), 0.6f);
        resetText.Alignment = Alignment.Center;
        resetText.Color = Color.Gold;
        resetText.Outline = true;
        resetText.Draw();
    }

    private void DrawDeathText()
    {
        // 1. Naikkan Layer agar di atas Scaleform "WASTED" bawaan game (Layer 4 adalah HUD/Big Message)
        Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 4); 

        // 2. Overlay Hitam Lebih Gelap & Pekat agar menutupi teks asli di belakang
        Function.Call(Hash.DRAW_RECT, 0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, 180, 0);

        // 3. Teks Utama dengan Font Pricedown
        var deathText = new TextElement(deathTextMsg, new PointF(1280f / 2f, 720f / 2f - 60f), 3.5f);
        deathText.Alignment = Alignment.Center;
        deathText.Color = Color.FromArgb(200, 0, 0); 
        deathText.Font = GTA.UI.Font.Pricedown;
        deathText.Outline = true;
        deathText.Shadow = true;
        deathText.Draw();

        // 4. Reset Layer ke default agar tidak merusak UI lain
        Function.Call(Hash.SET_SCRIPT_GFX_DRAW_ORDER, 0); 
    }

    private void DrawWinCountDisplay()
    {
        var winCountText = new TextElement($"WIN: {winCount}", new PointF(1280f / 2f, 110f), 0.8f);
        winCountText.Alignment = Alignment.Center;
        winCountText.Color = Color.Gold;
        winCountText.Font = GTA.UI.Font.Pricedown;
        winCountText.Outline = true;
        winCountText.Shadow = true;
        winCountText.Draw();
    }

    private void DrawFinishCountdown()
    {
        int timeLeft = Math.Max(0, (finishCountdown - Game.GameTime) / 1000) + 1;
        if (timeLeft > countdownDuration) timeLeft = countdownDuration;

        var countdownText = new TextElement(timeLeft.ToString(), new PointF(1280f / 2f, 720f / 2f - 50f), 5.0f);
        countdownText.Alignment = Alignment.Center;
        countdownText.Color = Color.White;
        countdownText.Font = GTA.UI.Font.Pricedown;
        countdownText.Outline = true;
        countdownText.Shadow = true;
        countdownText.Draw();
    }
}
