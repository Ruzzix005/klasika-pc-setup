using Microsoft.Win32;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace KlasikaPCSetup;

public sealed class CleanupForm : Form
{
    private readonly DataGridView grid = new();
    private readonly Button remove = new() { Text = "ODSTRANI IZBRANO", Width = 170, Height = 40 };
    private readonly Button removeOffice = new() { Text = "ODSTRANI CEL OFFICE", Width = 180, Height = 40 };
    private readonly Button close = new() { Text = "ZAPRI", Width = 100, Height = 40 };
    private readonly Label status = new() { AutoSize = true, Text = "Berem seznam Programi in funkcije ..." };
    private CancellationTokenSource? removal;

    public CleanupForm()
    {
        Text = "Pregled in odstranitev programov";
        ClientSize = new Size(920, 590);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);
        MinimizeBox = false;

        var info = new Label {
            Text = "Oznaci samo programe, ki jih res zelis odstraniti. Predizbrana sta znana navlaka in Office; gonilniki ter OEM updaterji niso.",
            AutoSize = true, Location = new Point(18, 16)
        };
        grid.Location = new Point(18, 48); grid.Size = new Size(884, 470);
        grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Remove", HeaderText = "Odstrani", FillWeight = 18 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Program", ReadOnly = true, FillWeight = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Publisher", HeaderText = "Izdajatelj", ReadOnly = true, FillWeight = 55 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Version", HeaderText = "Razlicica", ReadOnly = true, FillWeight = 30 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Note", HeaderText = "Opomba", ReadOnly = true, FillWeight = 40 });

        status.Location = new Point(18, 542); removeOffice.Location = new Point(432, 532); remove.Location = new Point(621, 532); close.Location = new Point(800, 532);
        remove.Click += async (_, _) => await RemoveSelectedAsync();
        removeOffice.Click += async (_, _) => await RemoveAllOfficeAsync();
        close.Click += (_, _) => { if (removal is null) Close(); else removal.Cancel(); };
        Controls.AddRange([info, grid, status, removeOffice, remove, close]);
        Shown += (_, _) => LoadPrograms();
    }

    private void LoadPrograms()
    {
        try
        {
            var apps = ReadInstalledPrograms().OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            foreach (var app in apps)
            {
                var row = grid.Rows[grid.Rows.Add(app.Recommended, app.Name, app.Publisher, app.Version, app.Note)];
                row.Tag = app;
                if (app.IsOffice) { row.Cells["Remove"].ReadOnly = true; row.DefaultCellStyle.BackColor = Color.AliceBlue; }
                if (app.Protected) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
            }
            status.Text = $"Najdenih programov: {apps.Count}. Rumeno oznaceni zahtevajo posebno previdnost.";
        }
        catch (Exception ex) { status.Text = "Napaka pri branju: " + ex.Message; remove.Enabled = false; }
    }

    private static IEnumerable<InstalledProgram> ReadInstalledPrograms()
    {
        var found = new Dictionary<string, InstalledProgram>(StringComparer.OrdinalIgnoreCase);
        var locations = new[] {
            (RegistryHive.LocalMachine, RegistryView.Registry64), (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Registry64), (RegistryHive.CurrentUser, RegistryView.Registry32)
        };
        foreach (var (hive, view) in locations)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null) continue;
            foreach (var keyName in uninstall.GetSubKeyNames())
            {
                using var key = uninstall.OpenSubKey(keyName);
                if (key is null) continue;
                var name = key.GetValue("DisplayName")?.ToString()?.Trim();
                var command = (key.GetValue("UninstallString")?.ToString() ?? key.GetValue("QuietUninstallString")?.ToString())?.Trim();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command)) continue;
                if (Convert.ToInt32(key.GetValue("SystemComponent", 0)) == 1) continue;
                if (key.GetValue("ParentKeyName") is not null) continue;
                var releaseType = key.GetValue("ReleaseType")?.ToString() ?? "";
                if (releaseType.Contains("Update", StringComparison.OrdinalIgnoreCase) || releaseType.Contains("Hotfix", StringComparison.OrdinalIgnoreCase)) continue;

                var publisher = key.GetValue("Publisher")?.ToString()?.Trim() ?? "";
                var version = key.GetValue("DisplayVersion")?.ToString()?.Trim() ?? "";
                var isOffice = IsOffice(name, publisher);
                var protectedApp = IsProtected(name, publisher);
                var recommended = !isOffice && !protectedApp && IsRecommendedRemoval(name, publisher);
                var note = isOffice ? "Uporabi gumb CEL OFFICE" : protectedApp ? "PREVIDNO" : recommended ? "Predlagana odstranitev" : "";
                var id = $"{name}|{version}|{publisher}";
                found.TryAdd(id, new InstalledProgram(name, publisher, version, command, recommended, protectedApp, isOffice, note));
            }
        }
        return found.Values;
    }

    private static bool IsRecommendedRemoval(string name, string publisher)
    {
        var text = $"{name} {publisher}";
        string[] patterns = [
            "McAfee", "Norton", "WildTangent", "ExpressVPN",
            "Dropbox Promotion", "Booking.com", "Amazon", "Dell SupportAssist", "Dell Optimizer", "Dell Digital Delivery",
            "Dell Customer Connect", "HP Wolf Security", "HP JumpStarts", "Lenovo App Explorer"
        ];
        return patterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOffice(string name, string publisher)
    {
        if (!publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) return false;
        return name.Contains("Microsoft 365", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Office 365", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Microsoft Office", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Microsoft OneNote -", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProtected(string name, string publisher)
    {
        var text = $"{name} {publisher}";
        string[] patterns = [
            "Microsoft Visual C++", ".NET", "Windows Driver", "Chipset", "Realtek", "NVIDIA", "AMD Software",
            "Intel(R)", "Bluetooth", "Wireless", "Audio Driver", "Graphics Driver", "Dell Command | Update",
            "Dell Command Update", "Lenovo Vantage", "Lenovo System Update", "HP Image Assistant", "HP Support Assistant",
            "Hotkey", "System Event Utility"
        ];
        return patterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task RemoveSelectedAsync()
    {
        var selected = grid.Rows.Cast<DataGridViewRow>()
            .Where(r => Convert.ToBoolean(r.Cells["Remove"].Value ?? false))
            .Select(r => (Row: r, App: (InstalledProgram)r.Tag!)).ToList();
        if (selected.Count == 0) { MessageBox.Show("Oznaci vsaj en program.", Text); return; }
        if (selected.Any(x => x.App.Protected) && MessageBox.Show("Izbral si tudi rumeno oznacene programe. Ti lahko vsebujejo gonilnike ali uporabna sistemska orodja. Nadaljujem?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (MessageBox.Show($"Res zelis odstraniti {selected.Count} programov? Vsak uradni uninstaller se lahko odpre v svojem oknu.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        removal = new CancellationTokenSource(); remove.Enabled = false; grid.Enabled = false; close.Text = "PREKLICI";
        var failed = 0;
        foreach (var item in selected)
        {
            if (removal.IsCancellationRequested) break;
            status.Text = "Odstranjujem: " + item.App.Name;
            try
            {
                var exit = await RunUninstallerAsync(item.App.UninstallCommand, removal.Token);
                if (exit == 0 || exit == 1605 || exit == 3010) { item.Row.DefaultCellStyle.BackColor = Color.Honeydew; item.Row.Cells["Note"].Value = exit == 3010 ? "Odstranjeno - potreben restart" : "Odstranjeno"; }
                else { failed++; item.Row.DefaultCellStyle.BackColor = Color.MistyRose; item.Row.Cells["Note"].Value = $"Napaka {exit}"; }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { failed++; item.Row.DefaultCellStyle.BackColor = Color.MistyRose; item.Row.Cells["Note"].Value = ex.Message; }
        }
        status.Text = removal.IsCancellationRequested ? "Odstranjevanje je bilo preklicano." : $"Koncano. Neuspesnih: {failed}.";
        removal.Dispose(); removal = null; remove.Enabled = true; grid.Enabled = true; close.Text = "ZAPRI";
    }

    private async Task RemoveAllOfficeAsync()
    {
        if (MessageBox.Show("To bo z Microsoftovim uradnim pomocnikom odstranilo vse različice Office, Microsoft 365, Visio, Project in jezikovne pakete. Pred nadaljevanjem zapri Word, Excel, Outlook, Teams in druge Office programe. Nadaljujem?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        removal = new CancellationTokenSource(); remove.Enabled = false; removeOffice.Enabled = false; grid.Enabled = false; close.Text = "PREKLICI";
        var workDirectory = Path.Combine(Path.GetTempPath(), "KlasikaOfficeRemoval-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(workDirectory);
            var zipPath = Path.Combine(workDirectory, "GetHelpCmd.zip");
            status.Text = "Prenašam uradni Microsoft Get Help pomocnik ...";
            using (var client = new HttpClient())
            using (var response = await client.GetAsync("https://aka.ms/SaRA_EnterpriseVersionFiles", HttpCompletionOption.ResponseHeadersRead, removal.Token))
            {
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(removal.Token);
                await using var target = File.Create(zipPath);
                await source.CopyToAsync(target, removal.Token);
            }

            status.Text = "Pripravljam Microsoft Office Uninstall pomocnik ...";
            var extractPath = Path.Combine(workDirectory, "GetHelpCmd");
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            var executable = Directory.GetFiles(extractPath, "GetHelpCmd.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new FileNotFoundException("GetHelpCmd.exe ni bil najden v Microsoftovem paketu.");

            status.Text = "Odstranjujem celoten Office; to lahko traja vec minut ...";
            var psi = new ProcessStartInfo(executable) { UseShellExecute = true };
            psi.ArgumentList.Add("-S"); psi.ArgumentList.Add("OfficeScrubScenario"); psi.ArgumentList.Add("-AcceptEula"); psi.ArgumentList.Add("-OfficeVersion"); psi.ArgumentList.Add("All");
            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Microsoftovega pomocnika ni bilo mogoce zagnati.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(removal.Token); timeout.CancelAfter(TimeSpan.FromMinutes(45));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { try { process.Kill(true); } catch { } throw; }

            if (process.ExitCode == 0)
            {
                status.Text = "Office je odstranjen. Ponovno zazeni racunalnik.";
                MessageBox.Show("Microsoftov pomocnik je odstranitev uspešno koncal. Za dokoncanje čiščenja ponovno zazeni racunalnik.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else throw new InvalidOperationException($"Microsoft Get Help je vrnil kodo {process.ExitCode}. Preveri njegovo CMD okno.");
        }
        catch (OperationCanceledException) { status.Text = "Odstranjevanje Office je bilo preklicano."; }
        catch (Exception ex) { status.Text = "Office ni bil odstranjen: " + ex.Message; MessageBox.Show(status.Text, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            try { if (Directory.Exists(workDirectory)) Directory.Delete(workDirectory, true); } catch { }
            removal?.Dispose(); removal = null; remove.Enabled = true; removeOffice.Enabled = true; grid.Enabled = true; close.Text = "ZAPRI";
        }
    }

    private static async Task<int> RunUninstallerAsync(string command, CancellationToken ct)
    {
        command = Environment.ExpandEnvironmentVariables(command.Trim());
        ProcessStartInfo psi;

        // Programi in funkcije pogosto shrani MSI ukaz kot /I, ki pomeni
        // vzdrzevanje. Za odstranitev ga moramo zagnati kot /X.
        var msi = Regex.Match(command, @"(?i)msiexec(?:\.exe)?\s+/(?:I|X)\s*(\{[0-9a-f-]+\})");
        if (msi.Success)
        {
            psi = new ProcessStartInfo("msiexec.exe") { UseShellExecute = true };
            psi.ArgumentList.Add("/x");
            psi.ArgumentList.Add(msi.Groups[1].Value);
        }
        else
        {
            string executable;
            string arguments;
            if (command.StartsWith('"'))
            {
                var endQuote = command.IndexOf('"', 1);
                if (endQuote < 2) throw new InvalidOperationException("Napacen UninstallString.");
                executable = command[1..endQuote];
                arguments = command[(endQuote + 1)..].Trim();
            }
            else
            {
                var exeEnd = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (exeEnd < 0) throw new InvalidOperationException("Pot do uninstallerja ni bila prepoznana.");
                exeEnd += 4;
                executable = command[..exeEnd].Trim();
                arguments = command[exeEnd..].Trim();
            }
            psi = new ProcessStartInfo(executable) { UseShellExecute = true, Arguments = arguments };
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Uninstallerja ni bilo mogoce zagnati.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromMinutes(20));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { try { process.Kill(true); } catch { } throw; }
        return process.ExitCode;
    }

    private sealed record InstalledProgram(string Name, string Publisher, string Version, string UninstallCommand, bool Recommended, bool Protected, bool IsOffice, string Note);
}
