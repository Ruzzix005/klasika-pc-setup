using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace KlasikaPCSetup;

public sealed class MainForm : Form
{
    private const int NoApplicableUpdate = unchecked((int)0x8A15002B);
    private readonly CheckBox chrome = NewOption("Namesti ali posodobi Google Chrome");
    private readonly CheckBox sevenZip = NewOption("Namesti ali posodobi 7-Zip");
    private readonly CheckBox adobe = NewOption("Namesti ali posodobi Adobe Acrobat Reader (64-bit)");
    private readonly CheckBox powerPlan = NewOption("Ustvari in aktiviraj nacrt ReadyForge - visoka ucinkovitost");
    private readonly CheckBox devicePower = NewOption("Izklopi varcevanje USB in aktivnih mreznih kartic");
    private readonly CheckBox cleanup = NewOption("Preglej in odstrani programe iz Programi in funkcije");
    private readonly RichTextBox log = new() { ReadOnly = true, BackColor = AppTheme.Background, ForeColor = Color.Gainsboro, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9.5f) };
    private readonly Label status = new() { Text = "Pripravljeno", AutoSize = true, ForeColor = AppTheme.Muted };
    private readonly Button start = new() { Text = "ZAČNI   →", Width = 158, Height = 46 };
    private readonly Button cancel = new() { Text = "×   PREKLIČI", Width = 142, Height = 46, Enabled = false };
    private CancellationTokenSource? currentRun;
    private readonly string logPath;

    public MainForm()
    {
        Text = "ReadyForge 2.0";
        ClientSize = new Size(940, 760);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = AppTheme.Background;
        Font = new Font("Segoe UI", 10);

        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ReadyForge", "Logs");
        Directory.CreateDirectory(logDirectory);
        logPath = Path.Combine(logDirectory, $"ReadyForge-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        var header = new Panel { Location = Point.Empty, Size = new Size(940, 128), BackColor = AppTheme.Charcoal };
        var logoTile = new Label { Text = "R", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Black", 29), ForeColor = Color.White, BackColor = AppTheme.Accent, Location = new Point(34, 28), Size = new Size(62, 62) };
        var title = new Label { Text = "ReadyForge", Font = new Font("Segoe UI Semibold", 28), ForeColor = Color.White, AutoSize = true, Location = new Point(116, 25) };
        var version = new Label { Text = "2.0", Font = new Font("Segoe UI Semibold", 10), ForeColor = Color.White, BackColor = AppTheme.Accent, AutoSize = true, Padding = new Padding(7, 4, 7, 4), Location = new Point(322, 38) };
        var subtitle = new Label { Text = "Hitra priprava Windows racunalnika", ForeColor = AppTheme.Muted, AutoSize = true, Location = new Point(119, 78) };
        var utility = new Label { Text = "WINDOWS DEPLOYMENT UTILITY", Font = new Font("Segoe UI Semibold", 9), ForeColor = AppTheme.Accent, AutoSize = true, Location = new Point(712, 55) };
        var accent = new Panel { Location = new Point(0, 125), Size = new Size(940, 3), BackColor = AppTheme.Accent };
        header.Controls.AddRange([logoTile, title, version, subtitle, utility, accent]);
        var group = new ForgeCard { Location = new Point(34, 148), Size = new Size(872, 296) };
        var groupTitle = new Label { Text = "IZBOR OPRAVIL", Font = new Font("Segoe UI Semibold", 14), ForeColor = AppTheme.AccentBright, AutoSize = true, Location = new Point(28, 18) };
        group.Controls.Add(groupTitle);
        var boxes = new[] { chrome, sevenZip, adobe, powerPlan, devicePower, cleanup };
        string[] rowMarks = ["WEB", "7Z", "PDF", "PERF", "USB", "DEL"];
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i].Location = new Point(30, 55 + i * 38); boxes[i].Width = 730; group.Controls.Add(boxes[i]);
            var mark = new Label { Text = rowMarks[i], Font = new Font("Segoe UI Semibold", 8), ForeColor = AppTheme.AccentBright, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(780, 62 + i * 38), Size = new Size(55, 24) };
            group.Controls.Add(mark);
            if (i < boxes.Length - 1) group.Controls.Add(new Panel { BackColor = Color.FromArgb(24, 39, 43), Location = new Point(67, 92 + i * 38), Size = new Size(768, 1) });
        }

        var outputCard = new ForgeCard { Location = new Point(34, 462), Size = new Size(872, 190) };
        var selectAll = new ForgeCheckBox { Text = "IZBERI VSE", Checked = true, Font = new Font("Segoe UI Semibold", 11.5f), ForeColor = AppTheme.AccentBright, Location = new Point(25, 12), Width = 250 };
        selectAll.CheckedChanged += (_, _) => { foreach (var box in boxes) box.Checked = selectAll.Checked; };
        log.Location = new Point(25, 52); log.Size = new Size(822, 116);
        outputCard.Controls.AddRange([selectAll, log]);
        status.Location = new Point(50, 697);
        cancel.Location = new Point(580, 678); start.Location = new Point(748, 678);
        AppTheme.PrimaryButton(start); AppTheme.SecondaryButton(cancel);
        start.Click += async (_, _) => await StartSetupAsync(boxes, selectAll);
        cancel.Click += (_, _) => { cancel.Enabled = false; status.Text = "Preklicujem ..."; currentRun?.Cancel(); };

        Controls.AddRange([header, group, outputCard, status, cancel, start]);
        Shown += (_, _) => AppTheme.ApplyDarkTitleBar(this);
        WriteLog("Program je pripravljen. Izberi opravila in klikni ZACNI.");
    }

    private static CheckBox NewOption(string text) => new ForgeCheckBox { Text = text, Checked = true };

    private async Task StartSetupAsync(CheckBox[] boxes, CheckBox selectAll)
    {
        if (!boxes.Any(x => x.Checked)) { MessageBox.Show("Izberi vsaj eno opravilo.", Text); return; }
        start.Enabled = false; cancel.Enabled = true; selectAll.Enabled = false;
        foreach (var box in boxes) box.Enabled = false;
        currentRun = new CancellationTokenSource();
        var failures = new List<string>();
        var tasks = new List<(string Name, Func<CancellationToken, Task> Run)>();
        if (chrome.Checked) tasks.Add(("Google Chrome", ct => InstallPackageAsync("Google.Chrome", "Google Chrome", ct)));
        if (sevenZip.Checked) tasks.Add(("7-Zip", ct => InstallPackageAsync("7zip.7zip", "7-Zip", ct)));
        if (adobe.Checked) tasks.Add(("Adobe Acrobat Reader", ct => InstallPackageAsync("Adobe.Acrobat.Reader.64-bit", "Adobe Acrobat Reader", ct)));
        if (powerPlan.Checked) tasks.Add(("Nacrt porabe energije", SetPowerPlanAsync));
        if (devicePower.Checked) tasks.Add(("Varcevanje naprav", DisableDevicePowerAsync));
        if (cleanup.Checked) tasks.Add(("Odstranjevanje programov", ShowCleanupAsync));

        try
        {
            foreach (var task in tasks)
            {
                currentRun.Token.ThrowIfCancellationRequested();
                try { await task.Run(currentRun.Token); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { failures.Add(task.Name); WriteLog($"{task.Name}: {ex.Message}", "ERROR"); }
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
        if (result.ExitCode != 0) throw new InvalidOperationException($"winget koda {result.ExitCode}. {result.Output}");
        WriteLog($"{name} je pripravljen.", "OK");
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
        WriteLog($"Nacrt je ustvarjen oziroma posodobljen in aktiviran ({guid}).", "OK");
    }

    private async Task DisableDevicePowerAsync(CancellationToken ct)
    {
        WriteLog("Izklapljam varcevanje USB in mreznih kartic ...");
        const string script = "$ids=@(); $ids += Get-PnpDevice -Class USB -Status OK -ErrorAction SilentlyContinue | % InstanceId; $ids += Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ? Status -ne 'Disabled' | % PnPDeviceID; $p=Get-CimInstance -Namespace root/wmi -ClassName MSPower_DeviceEnable -ErrorAction Stop; $n=0; foreach($id in ($ids|?{$_}|sort -Unique)){ $x=$id.Replace('\\','_'); $p|?{(($_.InstanceName -like \"$id*\") -or ($_.InstanceName -like \"$x*\")) -and $_.Enable}|%{Set-CimInstance -InputObject $_ -Property @{Enable=$false} -ErrorAction Stop|Out-Null;$n++} }; Write-Output $n";
        var result = await RunCheckedAsync("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], false, ct);
        WriteLog($"Spremenjenih power-management vnosov: {result.Output.Trim()}.", "OK");
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
