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
    private readonly CheckBox powerPlan = NewOption("Ustvari in aktiviraj nacrt Klasika - visoka ucinkovitost");
    private readonly CheckBox devicePower = NewOption("Izklopi varcevanje USB in aktivnih mreznih kartic");
    private readonly CheckBox cleanup = NewOption("Preglej in odstrani programe iz Programi in funkcije");
    private readonly RichTextBox log = new() { ReadOnly = true, BackColor = Color.White, Font = new Font("Consolas", 9) };
    private readonly Label status = new() { Text = "Pripravljeno", AutoSize = true };
    private readonly Button start = new() { Text = "ZACNI", Width = 145, Height = 42 };
    private readonly Button cancel = new() { Text = "PREKLICI", Width = 125, Height = 42, Enabled = false };
    private CancellationTokenSource? currentRun;
    private readonly string logPath;

    public MainForm()
    {
        Text = "Klasika PC Setup 2.0";
        ClientSize = new Size(760, 650);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10);

        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KlasikaPCSetup", "Logs");
        Directory.CreateDirectory(logDirectory);
        logPath = Path.Combine(logDirectory, $"Klasika-PC-Setup-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        var title = new Label { Text = "Klasika PC Setup", Font = new Font("Segoe UI Semibold", 20), AutoSize = true, Location = new Point(24, 18) };
        var subtitle = new Label { Text = "Izberi programe in sistemske nastavitve, nato klikni ZACNI.", ForeColor = Color.DimGray, AutoSize = true, Location = new Point(27, 60) };
        var group = new GroupBox { Text = "Izbor opravil", Location = new Point(24, 92), Size = new Size(710, 220) };
        var boxes = new[] { chrome, sevenZip, adobe, powerPlan, devicePower, cleanup };
        for (var i = 0; i < boxes.Length; i++) { boxes[i].Location = new Point(20, 30 + i * 32); group.Controls.Add(boxes[i]); }

        var selectAll = new CheckBox { Text = "Izberi vse", Checked = true, AutoSize = true, Location = new Point(24, 325) };
        selectAll.CheckedChanged += (_, _) => { foreach (var box in boxes) box.Checked = selectAll.Checked; };
        log.Location = new Point(24, 356); log.Size = new Size(710, 190); log.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        status.Location = new Point(24, 570);
        cancel.Location = new Point(455, 558); start.Location = new Point(589, 558);
        start.BackColor = Color.FromArgb(0, 120, 215); start.ForeColor = Color.White; start.FlatStyle = FlatStyle.Flat;
        start.Click += async (_, _) => await StartSetupAsync(boxes, selectAll);
        cancel.Click += (_, _) => { cancel.Enabled = false; status.Text = "Preklicujem ..."; currentRun?.Cancel(); };

        Controls.AddRange([title, subtitle, group, selectAll, log, status, cancel, start]);
        WriteLog("Program je pripravljen. Izberi opravila in klikni ZACNI.");
    }

    private static CheckBox NewOption(string text) => new() { Text = text, Checked = true, AutoSize = true };

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

            if (failures.Count == 0) { status.Text = "Vsa opravila so koncana."; WriteLog("Vsa izbrana opravila so bila uspesno koncana.", "OK"); MessageBox.Show("Klasika je uspesno pripravljena.", Text); }
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
        var match = Regex.Match(list.Output, @"([0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}).*Klasika - visoka ucinkovitost", RegexOptions.IgnoreCase);
        string guid;
        if (match.Success) guid = match.Groups[1].Value;
        else
        {
            var duplicate = await RunCheckedAsync("powercfg.exe", ["/duplicatescheme", "SCHEME_MIN"], false, ct);
            var guidMatch = Regex.Match(duplicate.Output, @"[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}", RegexOptions.IgnoreCase);
            if (!guidMatch.Success) throw new InvalidOperationException("GUID novega nacrta ni bil najden.");
            guid = guidMatch.Value;
        }
        string[][] commands = [
            ["/changename", guid, "Klasika - visoka ucinkovitost", "Brez spanja in varcevanja na namiznem racunalniku."],
            ["/setacvalueindex", guid, "SUB_VIDEO", "VIDEOIDLE", "0"], ["/setacvalueindex", guid, "SUB_DISK", "DISKIDLE", "0"],
            ["/setacvalueindex", guid, "SUB_SLEEP", "STANDBYIDLE", "0"], ["/setacvalueindex", guid, "SUB_SLEEP", "HIBERNATEIDLE", "0"],
            ["/setacvalueindex", guid, "SUB_USB", "USBSELECTIVE", "0"], ["/setacvalueindex", guid, "SUB_PCIEXPRESS", "ASPM", "0"],
            ["/setactive", guid], ["/hibernate", "off"] ];
        foreach (var command in commands) await RunCheckedAsync("powercfg.exe", command, false, ct);
        WriteLog($"Nacrt je ustvarjen oziroma posodobljen in aktiviran ({guid}).", "OK");
    }

    private async Task DisableDevicePowerAsync(CancellationToken ct)
    {
        WriteLog("Izklapljam varcevanje USB in mreznih kartic ...");
        const string script = "$ids=@(); $ids += Get-PnpDevice -Class USB -Status OK -ErrorAction SilentlyContinue | % InstanceId; $ids += Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ? Status -ne 'Disabled' | % PnPDeviceID; $p=Get-CimInstance -Namespace root/wmi -ClassName MSPower_DeviceEnable -ErrorAction Stop; $n=0; foreach($id in ($ids|?{$_}|sort -Unique)){ $x=$id.Replace('\\','_'); $p|?{$_.InstanceName -like \"$x*\" -and $_.Enable}|%{Set-CimInstance -InputObject $_ -Property @{Enable=$false} -ErrorAction Stop|Out-Null;$n++} }; Write-Output $n";
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
        var psi = new ProcessStartInfo(file) { UseShellExecute = false, CreateNoWindow = !interactive, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = psi };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(); var errorTask = process.StandardError.ReadToEndAsync();
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
