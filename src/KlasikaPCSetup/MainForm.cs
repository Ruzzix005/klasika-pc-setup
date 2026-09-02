using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace KlasikaPCSetup;

public sealed class MainForm : Form
{
    private const int NoApplicableUpdate = unchecked((int)0x8A15002B);
    private const int InstallerHashMismatch = unchecked((int)0x8A150011);
    private readonly CheckBox chrome = NewOption("Google Chrome");
    private readonly CheckBox sevenZip = NewOption("7-Zip");
    private readonly CheckBox adobe = NewOption("Adobe Acrobat Reader");
    private readonly CheckBox powerPlan = NewOption("Visoka ucinkovitost in Fast Startup");
    private readonly CheckBox devicePower = NewOption("USB in mrezne kartice");
    private readonly CheckBox cleanup = NewOption("Odstranjevanje programov");
    private readonly CheckBox windowsUpdate = NewOption("Windows Update");
    private readonly CheckBox drivers = NewOption("Pregled gonilnikov");
    private readonly RichTextBox log = new() { ReadOnly = true, BackColor = Color.FromArgb(10, 14, 18), ForeColor = Color.FromArgb(218, 225, 229), BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9.5f) };
    private readonly Label status = new() { Text = "Pripravljeno", AutoSize = true, ForeColor = AppTheme.Muted };
    private readonly Button start = new ForgeButton { Text = "ZAČNI   →", Width = 158, Height = 46 };
    private readonly Button cancel = new ForgeButton { Text = "×   PREKLIČI", Width = 142, Height = 46, Enabled = false };
    private readonly Label systemSummary = new() { AutoSize = false, ForeColor = Color.Gainsboro, Font = new Font("Segoe UI", 9.5f) };
    private readonly Label progressText = new() { Text = "0 %", AutoSize = true, ForeColor = AppTheme.Muted };
    private readonly Panel progressTrack = new() { BackColor = Color.FromArgb(26, 39, 43), Height = 5 };
    private readonly Panel progressFill = new() { BackColor = AppTheme.AccentBright, Height = 5, Width = 0 };
    private readonly Dictionary<CheckBox, Label> taskStates = [];
    private CancellationTokenSource? currentRun;
    private readonly string logPath;

    public MainForm()
    {
        Text = "ReadyForge 2.4";
        ClientSize = new Size(1120, 760);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = AppTheme.Background;
        Font = new Font("Segoe UI", 10);

        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ReadyForge", "Logs");
        Directory.CreateDirectory(logDirectory);
        logPath = Path.Combine(logDirectory, $"ReadyForge-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        var sidebar = new Panel { Location = Point.Empty, Size = new Size(218, 760), BackColor = AppTheme.Charcoal };
        var logoTile = new Label { Text = "R", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Black", 22), ForeColor = Color.White, BackColor = AppTheme.Accent, Location = new Point(24, 28), Size = new Size(48, 48) };
        var brand = new Label { Text = "ReadyForge", Font = new Font("Segoe UI Semibold", 17), ForeColor = Color.White, AutoSize = true, Location = new Point(84, 31) };
        var version = new Label { Text = "VERSION 2.4", Font = new Font("Segoe UI Semibold", 8), ForeColor = AppTheme.AccentBright, AutoSize = true, Location = new Point(86, 61) };
        var navTitle = new Label { Text = "DELOVNI PROSTOR", Font = new Font("Segoe UI Semibold", 8), ForeColor = Color.FromArgb(92, 103, 112), AutoSize = true, Location = new Point(24, 125) };
        var activeNav = new Panel { Location = new Point(12, 151), Size = new Size(194, 46), BackColor = AppTheme.SurfaceRaised };
        var activeLine = new Panel { Location = Point.Empty, Size = new Size(3, 46), BackColor = AppTheme.Accent };
        var activeText = new Label { Text = "  Priprava racunalnika", Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.White, AutoSize = true, Location = new Point(18, 13) };
        activeNav.Controls.AddRange([activeLine, activeText]);
        var section1 = new Label { Text = "01   Pregled sistema", ForeColor = AppTheme.Muted, AutoSize = true, Location = new Point(30, 220) };
        var section2 = new Label { Text = "02   Izbor opravil", ForeColor = AppTheme.Muted, AutoSize = true, Location = new Point(30, 257) };
        var section3 = new Label { Text = "03   Dnevnik izvajanja", ForeColor = AppTheme.Muted, AutoSize = true, Location = new Point(30, 294) };
        var github = new LinkLabel { Text = "GitHub repozitorij  ↗", LinkColor = AppTheme.AccentBright, ActiveLinkColor = Color.White, AutoSize = true, Location = new Point(27, 690) };
        github.Click += (_, _) => Process.Start(new ProcessStartInfo("https://github.com/Ruzzix005/klasika-pc-setup") { UseShellExecute = true });
        var sideNote = new Label { Text = "Windows 10 / 11  •  x64", ForeColor = Color.FromArgb(88, 99, 107), AutoSize = true, Location = new Point(27, 724) };
        sidebar.Controls.AddRange([logoTile, brand, version, navTitle, activeNav, section1, section2, section3, github, sideNote]);

        var pageTitle = new Label { Text = "Priprava racunalnika", Font = new Font("Segoe UI Semibold", 25), ForeColor = Color.White, AutoSize = true, Location = new Point(252, 24) };
        var pageSubtitle = new Label { Text = "Izberi opravila in spremljaj potek priprave sistema.", ForeColor = AppTheme.Muted, AutoSize = true, Location = new Point(255, 65) };

        var systemCard = new ForgeCard { Location = new Point(252, 98), Size = new Size(834, 92) };
        var systemBadge = new Label { Text = "PC", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 10), ForeColor = AppTheme.AccentBright, BackColor = AppTheme.SurfaceRaised, Location = new Point(20, 20), Size = new Size(52, 52) };
        var systemTitle = new Label { Text = "Ta racunalnik", Font = new Font("Segoe UI Semibold", 11), ForeColor = Color.White, AutoSize = true, Location = new Point(91, 17) };
        systemSummary.Text = "Berem podatke o racunalniku ..."; systemSummary.Location = new Point(91, 42); systemSummary.Size = new Size(720, 40);
        systemCard.Controls.AddRange([systemBadge, systemTitle, systemSummary]);

        var groupTitle = new Label { Text = "Opravila", Font = new Font("Segoe UI Semibold", 16), ForeColor = Color.White, AutoSize = true, Location = new Point(252, 211) };
        var boxes = new[] { chrome, sevenZip, adobe, powerPlan, devicePower, cleanup, windowsUpdate, drivers };
        for (var i = 0; i < boxes.Length; i++)
        {
            var column = i % 2; var row = i / 2;
            var tile = new ForgeCard { Location = new Point(252 + column * 421, 250 + row * 65), Size = new Size(405, 54) };
            boxes[i].Location = new Point(15, 8); boxes[i].Width = 300; boxes[i].Height = 38; tile.Controls.Add(boxes[i]);
            var state = new Label { Text = "CAKA", Font = new Font("Segoe UI Semibold", 7.5f), ForeColor = AppTheme.Muted, TextAlign = ContentAlignment.MiddleRight, Location = new Point(316, 15), Size = new Size(70, 24) };
            taskStates[boxes[i]] = state; tile.Controls.Add(state); Controls.Add(tile);
        }

        var selectAll = new ForgeCheckBox { Text = "Izberi vse", Checked = true, Font = new Font("Segoe UI Semibold", 10), ForeColor = AppTheme.AccentBright, Location = new Point(940, 207), Width = 145 };
        selectAll.CheckedChanged += (_, _) => { foreach (var box in boxes) box.Checked = selectAll.Checked; };

        var outputCard = new ForgeCard { Location = new Point(252, 525), Size = new Size(834, 130) };
        var outputTitle = new Label { Text = "Dnevnik izvajanja", Font = new Font("Segoe UI Semibold", 11), ForeColor = Color.White, AutoSize = true, Location = new Point(18, 14) };
        progressText.Location = new Point(775, 16);
        progressTrack.Location = new Point(540, 26); progressTrack.Width = 215; progressTrack.Controls.Add(progressFill);
        log.Location = new Point(18, 46); log.Size = new Size(798, 66);
        outputCard.Controls.AddRange([outputTitle, progressTrack, progressText, log]);

        var footer = new Panel { Location = new Point(218, 680), Size = new Size(902, 80), BackColor = Color.FromArgb(10, 14, 18) };
        status.Location = new Point(34, 31);
        cancel.Location = new Point(572, 17); start.Location = new Point(738, 17);
        AppTheme.PrimaryButton(start); AppTheme.SecondaryButton(cancel);
        start.Click += async (_, _) => await StartSetupAsync(boxes, selectAll);
        cancel.Click += (_, _) => { cancel.Enabled = false; status.Text = "Preklicujem ..."; currentRun?.Cancel(); };
        footer.Controls.AddRange([status, cancel, start]);

        Controls.AddRange([sidebar, pageTitle, pageSubtitle, systemCard, groupTitle, selectAll, outputCard, footer]);
        Shown += async (_, _) => { AppTheme.ApplyDarkTitleBar(this); await LoadSystemSummaryAsync(); };
        WriteLog("Program je pripravljen. Izberi opravila in klikni ZACNI.");
    }

    private static CheckBox NewOption(string text) => new ForgeCheckBox { Text = text, Checked = true };

    private async Task LoadSystemSummaryAsync()
    {
        try
        {
            const string script = "$cs=Get-CimInstance Win32_ComputerSystem;$os=Get-CimInstance Win32_OperatingSystem;$lic=Get-CimInstance SoftwareLicensingProduct|?{$_.ApplicationID -eq '55c92734-d682-4d71-983e-d6ec3f16059f' -and $_.PartialProductKey}|sort LicenseStatus -Descending|select -First 1;$restart=(Test-Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired') -or (Test-Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending');'{0}|{1}|{2}|{3}|{4}' -f $cs.Manufacturer,$cs.Model,$os.Caption,[math]::Round($cs.TotalPhysicalMemory/1GB),$(if($lic.LicenseStatus -eq 1){'aktiviran'}else{'ni potrjeno'});'RESTART='+$restart";
            var result = await RunAsync("powershell.exe", ["-NoProfile", "-Command", script], false, TimeSpan.FromSeconds(30), CancellationToken.None);
            if (result.ExitCode != 0) throw new InvalidOperationException(result.Output);
            var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var values = lines.FirstOrDefault()?.Split('|') ?? [];
            var pc = values.Length >= 5 ? $"{values[0]} {values[1]}  •  {values[2]}  •  {values[3]} GB RAM  •  Windows {values[4]}" : Environment.MachineName;
            var restart = lines.Any(x => x.Contains("RESTART=True", StringComparison.OrdinalIgnoreCase)) ? "potreben" : "ne";
            var internet = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() ? "na voljo" : "ni povezave";
            var winget = CommandExists("winget.exe") ? "pripravljen" : "manjka";
            systemSummary.Text = pc + Environment.NewLine + $"Internet: {internet}  •  winget: {winget}  •  ponovni zagon: {restart}";
            WriteLog("Predhodni pregled racunalnika je koncan.", "OK");
        }
        catch (Exception ex)
        {
            systemSummary.Text = "Podatkov o racunalniku ni bilo mogoce v celoti prebrati: " + ex.Message;
            WriteLog("Predhodni pregled: " + ex.Message, "WARN");
        }
    }

    private void SetTaskState(CheckBox box, string text, Color color)
    {
        if (!taskStates.TryGetValue(box, out var label)) return;
        label.Text = text; label.ForeColor = color;
    }

    private void UpdateProgress(int completed, int total)
    {
        var percent = total == 0 ? 0 : completed * 100 / total;
        progressText.Text = percent + " %";
        progressFill.Width = progressTrack.Width * percent / 100;
    }

    private async Task StartSetupAsync(CheckBox[] boxes, CheckBox selectAll)
    {
        if (!boxes.Any(x => x.Checked)) { MessageBox.Show("Izberi vsaj eno opravilo.", Text); return; }
        start.Enabled = false; cancel.Enabled = true; selectAll.Enabled = false;
        foreach (var box in boxes) box.Enabled = false;
        currentRun = new CancellationTokenSource();
        var failures = new List<string>();
        var tasks = new List<(string Name, CheckBox Box, Func<CancellationToken, Task> Run)>();
        if (chrome.Checked) tasks.Add(("Google Chrome", chrome, ct => InstallPackageAsync("Google.Chrome", "Google Chrome", ct)));
        if (sevenZip.Checked) tasks.Add(("7-Zip", sevenZip, ct => InstallPackageAsync("7zip.7zip", "7-Zip", ct)));
        if (adobe.Checked) tasks.Add(("Adobe Acrobat Reader", adobe, ct => InstallPackageAsync("Adobe.Acrobat.Reader.64-bit", "Adobe Acrobat Reader", ct)));
        if (powerPlan.Checked) tasks.Add(("Nacrt porabe energije", powerPlan, SetPowerPlanAsync));
        if (devicePower.Checked) tasks.Add(("Varcevanje naprav", devicePower, DisableDevicePowerAsync));
        if (cleanup.Checked) tasks.Add(("Odstranjevanje programov", cleanup, ShowCleanupAsync));
        if (windowsUpdate.Checked) tasks.Add(("Windows Update", windowsUpdate, InstallWindowsUpdatesAsync));
        if (drivers.Checked) tasks.Add(("Pregled gonilnikov", drivers, CheckDriversAsync));

        foreach (var pair in taskStates) SetTaskState(pair.Key, pair.Key.Checked ? "CAKA" : "IZPUSCENO", AppTheme.Muted);
        UpdateProgress(0, tasks.Count);

        try
        {
            for (var index = 0; index < tasks.Count; index++)
            {
                var task = tasks[index];
                currentRun.Token.ThrowIfCancellationRequested();
                SetTaskState(task.Box, "IZVAJAM", AppTheme.AccentBright);
                status.Text = "Izvajam: " + task.Name;
                try { await task.Run(currentRun.Token); SetTaskState(task.Box, "USPELO", Color.FromArgb(83, 205, 137)); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { failures.Add(task.Name); SetTaskState(task.Box, "NAPAKA", Color.FromArgb(239, 101, 101)); WriteLog($"{task.Name}: {ex.Message}", "ERROR"); }
                UpdateProgress(index + 1, tasks.Count);
            }

            if (failures.Count == 0) { status.Text = "Vsa opravila so koncana."; WriteLog("Vsa izbrana opravila so bila uspesno koncana.", "OK"); MessageBox.Show("Racunalnik je uspesno pripravljen.", Text); }
            else { status.Text = "Koncano z napakami - preveri dnevnik."; MessageBox.Show("Napake:\n- " + string.Join("\n- ", failures), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        catch (OperationCanceledException) { status.Text = "Izvajanje je bilo preklicano."; WriteLog("Izvajanje je bilo preklicano.", "WARN"); }
        finally
        {
            currentRun.Dispose(); currentRun = null; start.Enabled = true; cancel.Enabled = false; selectAll.Enabled = true;
            foreach (var box in boxes) box.Enabled = true;
        }
    }

    private async Task InstallPackageAsync(string id, string name, CancellationToken ct)
    {
        if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "winget.exe")) && !CommandExists("winget.exe"))
            throw new InvalidOperationException("winget ni namescen. Namesti App Installer iz Microsoft Store.");

        WriteLog($"Preverjam {name} ...");
        var listed = await RunAsync("winget.exe", ["list", "--id", id, "-e", "--source", "winget", "--accept-source-agreements", "--disable-interactivity"], false, TimeSpan.FromMinutes(2), ct);
        ProcessResult result;
        if (listed.ExitCode == 0)
        {
            WriteLog($"{name} je namescen; preverjam posodobitev.");
            status.Text = $"Preverjanje posodobitve: {name}";
            result = await RunAsync("winget.exe", ["upgrade", "--id", id, "-e", "--source", "winget", "--interactive", "--accept-package-agreements", "--accept-source-agreements"], true, TimeSpan.FromMinutes(15), ct);
            if (result.ExitCode == NoApplicableUpdate || result.Output.Contains("No available upgrade", StringComparison.OrdinalIgnoreCase) || result.Output.Contains("No newer package", StringComparison.OrdinalIgnoreCase))
            { WriteLog($"{name} je ze na najnovejsi verziji.", "OK"); return; }
        }
        else
        {
            WriteLog($"Namescam {name}; spremljaj namestitveno okno ...");
            status.Text = $"Namescanje: {name} - dokoncaj korake v installerju";
            result = await RunAsync("winget.exe", ["install", "--id", id, "-e", "--source", "winget", "--interactive", "--accept-package-agreements", "--accept-source-agreements"], true, TimeSpan.FromMinutes(15), ct);
        }
        if (result.ExitCode == InstallerHashMismatch && id == "Google.Chrome")
        {
            WriteLog("WinGet manifest za Chrome ima napacen hash. Preklapljam na uradni Google MSI ...", "WARN");
            await InstallChromeFromOfficialMsiAsync(ct);
            return;
        }
        if (result.ExitCode != 0)
        {
            var description = await DescribeWingetErrorAsync(result.ExitCode, ct);
            throw new InvalidOperationException($"winget koda {result.ExitCode} (0x{unchecked((uint)result.ExitCode):X8}): {description} {result.Output}".Trim());
        }
        WriteLog($"{name} je pripravljen.", "OK");
    }

    private async Task InstallChromeFromOfficialMsiAsync(CancellationToken ct)
    {
        var msiPath = Path.Combine(Path.GetTempPath(), $"ReadyForge-Chrome-{Guid.NewGuid():N}.msi");
        try
        {
            status.Text = "Google Chrome: prenasam uradni Google MSI ...";
            using (var client = new HttpClient())
            using (var response = await client.GetAsync("https://dl.google.com/dl/chrome/install/googlechromestandaloneenterprise64.msi", HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(ct);
                await using var target = File.Create(msiPath);
                await source.CopyToAsync(target, ct);
            }

            status.Text = "Google Chrome: namescam uradni MSI ...";
            var install = await RunAsync("msiexec.exe", ["/i", msiPath, "/qn", "/norestart"], false, TimeSpan.FromMinutes(15), ct);
            if (install.ExitCode != 0 && install.ExitCode != 3010)
                throw new InvalidOperationException($"Google MSI namestitev ni uspela (koda {install.ExitCode}). {install.Output}");

            var chrome64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
            var chrome32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
            if (!File.Exists(chrome64) && !File.Exists(chrome32))
                throw new InvalidOperationException("MSI je koncal brez napake, vendar chrome.exe ni bil najden.");
            WriteLog("Google Chrome je namescen z uradnim Google Enterprise MSI in preverjen.", "OK");
        }
        finally
        {
            try { if (File.Exists(msiPath)) File.Delete(msiPath); } catch { }
        }
    }

    private async Task<string> DescribeWingetErrorAsync(int exitCode, CancellationToken ct)
    {
        try
        {
            var result = await RunAsync("winget.exe", ["error", $"0x{unchecked((uint)exitCode):X8}"], false, TimeSpan.FromSeconds(20), ct);
            return string.IsNullOrWhiteSpace(result.Output) ? "Neznana WinGet napaka." : result.Output;
        }
        catch { return "Neznana WinGet napaka."; }
    }

    private async Task SetPowerPlanAsync(CancellationToken ct)
    {
        WriteLog("Nastavljam nacrt porabe energije ...");
        var list = await RunCheckedAsync("powercfg.exe", ["/list"], false, ct);
        var match = Regex.Match(list.Output, @"([0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}).*(?:ReadyForge|Klasika) - visoka ucinkovitost", RegexOptions.IgnoreCase);
        string guid;
        if (match.Success) guid = match.Groups[1].Value;
        else
        {
            // Uporabimo nespremenljivi GUID nacrta High performance, ker alias
            // SCHEME_MIN na nekaterih OEM racunalnikih vrne Invalid Parameters.
            var duplicate = await RunCheckedAsync("powercfg.exe", ["/duplicatescheme", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"], false, ct);
            var guidMatch = Regex.Match(duplicate.Output, @"[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}", RegexOptions.IgnoreCase);
            if (!guidMatch.Success) throw new InvalidOperationException("GUID novega nacrta ni bil najden.");
            guid = guidMatch.Value;
        }

        await RunCheckedAsync("powercfg.exe", ["/changename", guid, "ReadyForge - visoka ucinkovitost"], false, ct);
        await RunCheckedAsync("powercfg.exe", ["/setactive", guid], false, ct);

        // /change je na Windows 10/11 bolj zdruzljiv od lokaliziranih aliasov
        // SUB_VIDEO, SUB_DISK in SUB_SLEEP.
        string[][] required = [
            ["/change", "monitor-timeout-ac", "0"],
            ["/change", "disk-timeout-ac", "0"],
            ["/change", "standby-timeout-ac", "0"],
            ["/change", "hibernate-timeout-ac", "0"],
            ["/hibernate", "off"]
        ];
        foreach (var command in required) await RunCheckedAsync("powercfg.exe", command, false, ct);

        // Hiter zagon (Fast Startup) uporablja hiberboot tudi takrat, ko je
        // nacrt porabe nastavljen na Nikoli. Izklopimo ga se eksplicitno.
        await RunCheckedAsync("reg.exe", [
            "add",
            @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power",
            "/v", "HiberbootEnabled",
            "/t", "REG_DWORD",
            "/d", "0",
            "/f"
        ], false, ct);
        WriteLog("Hiter zagon (Fast Startup) je izklopljen.", "OK");

        // Ti nastavitvi na nekaterih OEM/Modern Standby sistemih ne obstajata.
        // Uporabimo uradne GUID-e in nepodprto nastavitev obravnavamo kot opozorilo.
        string[][] optional = [
            ["/setacvalueindex", guid, "2a737441-1930-4402-8d77-b2bebba308a3", "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", "0"],
            ["/setacvalueindex", guid, "501a4d13-42af-4429-9fd1-a8218c268e20", "ee12f906-d277-404b-b6da-e5fa1a576df5", "0"]
        ];
        foreach (var command in optional)
        {
            var result = await RunAsync("powercfg.exe", command, false, TimeSpan.FromMinutes(1), ct);
            if (result.ExitCode != 0) WriteLog($"Opcijska nastavitev ni podprta: powercfg {string.Join(' ', command)}", "WARN");
        }
        await RunCheckedAsync("powercfg.exe", ["/setactive", guid], false, ct);
        await VerifyPowerSettingsAsync(guid, ct);
        WriteLog($"Nacrt je ustvarjen oziroma posodobljen in aktiviran ({guid}).", "OK");
    }

    private async Task VerifyPowerSettingsAsync(string guid, CancellationToken ct)
    {
        var active = await RunCheckedAsync("powercfg.exe", ["/getactivescheme"], false, ct);
        if (!active.Output.Contains(guid, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ReadyForge nacrt po nastavitvi ni aktiven.");

        string[][] settings = [
            ["7516b95f-f776-4464-8c53-06167f40cc99", "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e"],
            ["0012ee47-9041-4b5d-9b77-535fba8b1442", "6738e2c4-e8a5-4a42-b16a-e040e769756e"],
            ["238c9fa8-0aad-41ed-83f4-97be242c8f20", "29f6c1db-86da-48c5-9fdb-f2b67b1f44da"],
            ["238c9fa8-0aad-41ed-83f4-97be242c8f20", "9d7815a6-7ee4-497e-8888-515a05f02364"]
        ];
        foreach (var setting in settings)
        {
            var query = await RunCheckedAsync("powercfg.exe", ["/query", guid, setting[0], setting[1]], false, ct);
            var values = Regex.Matches(query.Output, @"0x([0-9a-f]{8})", RegexOptions.IgnoreCase);
            if (values.Count == 0 || values[0].Groups[1].Value != "00000000")
                throw new InvalidOperationException("Ena od AC nastavitev porabe energije ni nastavljena na Nikoli.");
        }

        var fastStartup = await RunCheckedAsync("reg.exe", ["query", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "/v", "HiberbootEnabled"], false, ct);
        if (!Regex.IsMatch(fastStartup.Output, @"HiberbootEnabled\s+REG_DWORD\s+0x0\b", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Hiter zagon po spremembi ni izklopljen.");
        WriteLog("Preverjeno: nacrt je aktiven, AC casovne omejitve so Nikoli in Fast Startup je izklopljen.", "OK");
    }

    private async Task DisableDevicePowerAsync(CancellationToken ct)
    {
        WriteLog("Izklapljam varcevanje USB in mreznih kartic ...");
        const string script = "$ids=@(); $ids += Get-PnpDevice -Class USB -Status OK -ErrorAction SilentlyContinue | % InstanceId; $ids += Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ? Status -ne 'Disabled' | % PnPDeviceID; $p=Get-CimInstance -Namespace root/wmi -ClassName MSPower_DeviceEnable -ErrorAction Stop; $n=0; foreach($id in ($ids|?{$_}|sort -Unique)){ $x=$id.Replace('\\','_'); $p|?{(($_.InstanceName -like \"$id*\") -or ($_.InstanceName -like \"$x*\")) -and $_.Enable}|%{Set-CimInstance -InputObject $_ -Property @{Enable=$false} -ErrorAction Stop|Out-Null;$n++} }; Write-Output $n";
        var result = await RunCheckedAsync("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], false, ct);
        WriteLog($"Spremenjenih power-management vnosov: {result.Output.Trim()}.", "OK");
    }

    private async Task InstallWindowsUpdatesAsync(CancellationToken ct)
    {
        WriteLog("Iscem Windows posodobitve; pregled lahko traja vec minut ...");
        status.Text = "Windows Update: iskanje posodobitev ...";
        const string script = "$s=New-Object -ComObject Microsoft.Update.Session;$r=$s.CreateUpdateSearcher().Search(\"IsInstalled=0 and IsHidden=0 and Type='Software'\");$c=New-Object -ComObject Microsoft.Update.UpdateColl;foreach($u in $r.Updates){if(-not $u.EulaAccepted){$u.AcceptEula()};[void]$c.Add($u)};if($c.Count -eq 0){'COUNT=0';exit 0};$d=$s.CreateUpdateDownloader();$d.Updates=$c;[void]$d.Download();$ready=New-Object -ComObject Microsoft.Update.UpdateColl;foreach($u in $c){if($u.IsDownloaded){[void]$ready.Add($u)}};if($ready.Count -eq 0){throw 'Posodobitev ni bilo mogoce prenesti.'};$i=$s.CreateUpdateInstaller();$i.Updates=$ready;$x=$i.Install();'COUNT='+$ready.Count;'RESULT='+$x.ResultCode;'REBOOT='+$x.RebootRequired";
        var result = await RunAsync("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], false, TimeSpan.FromMinutes(60), ct);
        if (result.ExitCode != 0) throw new InvalidOperationException(result.Output);
        var count = Regex.Match(result.Output, @"COUNT=(\d+)").Groups[1].Value;
        if (count == "0") WriteLog("Windows Update: novih programskih posodobitev ni.", "OK");
        else
        {
            var reboot = result.Output.Contains("REBOOT=True", StringComparison.OrdinalIgnoreCase);
            WriteLog($"Windows Update: obdelanih posodobitev {count}. Ponovni zagon: {(reboot ? "DA" : "ne")}.", "OK");
        }
    }

    private async Task CheckDriversAsync(CancellationToken ct)
    {
        WriteLog("Osvezujem naprave in preverjam manjkajoce gonilnike ...");
        await RunAsync("pnputil.exe", ["/scan-devices"], false, TimeSpan.FromMinutes(3), ct);
        const string script = "$x=Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue|?{$_.Status -ne 'OK'}|select FriendlyName,Class,Problem,InstanceId;if($x){$x|ConvertTo-Json -Compress}else{'NONE'}";
        var result = await RunCheckedAsync("powershell.exe", ["-NoProfile", "-Command", script], false, ct);
        if (result.Output.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            WriteLog("Ni zaznanih naprav z manjkajocimi ali okvarjenimi gonilniki.", "OK");
            return;
        }
        WriteLog("Zaznane so naprave, ki zahtevajo pregled gonilnikov: " + result.Output, "WARN");
        if (MessageBox.Show("Zaznane so naprave z napako ali manjkajocim gonilnikom. Odprem Windows Update > Izbirne posodobitve?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            Process.Start(new ProcessStartInfo("ms-settings:windowsupdate-optionalupdates") { UseShellExecute = true });
    }

    private Task ShowCleanupAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var dialog = new CleanupForm();
        dialog.ShowDialog(this);
        return Task.CompletedTask;
    }

    private async Task<ProcessResult> RunCheckedAsync(string file, IEnumerable<string> args, bool interactive, CancellationToken ct)
    {
        var result = await RunAsync(file, args, interactive, TimeSpan.FromMinutes(5), ct);
        if (result.ExitCode != 0) throw new InvalidOperationException($"{file} ni uspel (koda {result.ExitCode}): {result.Output}");
        return result;
    }

    private async Task<ProcessResult> RunAsync(string file, IEnumerable<string> args, bool interactive, TimeSpan timeout, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);
        var psi = new ProcessStartInfo(file) {
            UseShellExecute = false,
            CreateNoWindow = !interactive,
            RedirectStandardOutput = !interactive,
            RedirectStandardError = !interactive
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = psi };
        process.Start();
        var outputTask = interactive ? Task.FromResult(string.Empty) : process.StandardOutput.ReadToEndAsync();
        var errorTask = interactive ? Task.FromResult(string.Empty) : process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(linked.Token); }
        catch (OperationCanceledException) { try { process.Kill(true); } catch { } throw; }
        return new ProcessResult(process.ExitCode, ((await outputTask) + Environment.NewLine + (await errorTask)).Trim());
    }

    private static bool CommandExists(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        return path.Split(Path.PathSeparator).Any(p => File.Exists(Path.Combine(p, command)));
    }

    private void WriteLog(string message, string level = "INFO")
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        File.AppendAllText(logPath, line + Environment.NewLine, Encoding.UTF8);
        log.AppendText(line + Environment.NewLine); log.SelectionStart = log.TextLength; log.ScrollToCaret();
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
