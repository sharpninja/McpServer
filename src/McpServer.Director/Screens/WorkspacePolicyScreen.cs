using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen that binds to <see cref="WorkspacePolicyViewModel"/>.
/// Provides a form for editing workspace compliance policy (ban lists).
/// </summary>
internal sealed class WorkspacePolicyScreen : View
{
    private readonly WorkspacePolicyViewModel _vm;
    private readonly ViewModelBinder _binder = new();

    public WorkspacePolicyScreen(WorkspacePolicyViewModel vm)
    {
        _vm = vm;
        Title = "Compliance Policy";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        BuildUi();
    }

    private void BuildUi()
    {
        // Workspace path
        var pathLabel = new Label { X = 0, Y = 0, Text = "Workspace Path:" };
        var pathField = new TextField
        {
            X = 17,
            Y = 0,
            Width = Dim.Fill(),
            Text = _vm.WorkspacePath,
            ReadOnly = true,
        };
        Add(pathLabel, pathField);

        _binder.BindProperty(_vm, nameof(_vm.WorkspacePath), () =>
        {
            var current = pathField.Text ?? "";
            if (string.Equals(current, _vm.WorkspacePath, StringComparison.Ordinal))
                return;

            pathField.Text = _vm.WorkspacePath;
        });

        // Ban lists — each as a labeled text area
        var y = 2;
        var licensesView = AddBanListSection(ref y, "Banned Licenses:", _vm.BannedLicenses);
        var countriesView = AddBanListSection(ref y, "Banned Countries:", _vm.BannedCountriesOfOrigin);
        var orgsView = AddBanListSection(ref y, "Banned Orgs:", _vm.BannedOrganizations);
        var individualsView = AddBanListSection(ref y, "Banned Individuals:", _vm.BannedIndividuals);

        // Status / error
        var statusLabel = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(statusLabel);

        // Save button
        var saveBtn = new Button
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Text = "Save Policy",
        };
        Add(saveBtn);

        // Bindings
        _binder.BindProperty(_vm, nameof(_vm.IsSaving), () =>
        {
            saveBtn.Enabled = !_vm.IsSaving;
            if (_vm.IsSaving)
                statusLabel.Text = "⏳ Saving...";
        });

        _binder.BindProperty(_vm, nameof(_vm.SaveSucceeded), () =>
        {
            if (_vm.SaveSucceeded)
                statusLabel.Text = "✓ Policy saved successfully.";
        });

        _binder.BindProperty(_vm, nameof(_vm.ErrorMessage), () =>
        {
            if (!string.IsNullOrEmpty(_vm.ErrorMessage))
                statusLabel.Text = $"✗ {_vm.ErrorMessage}";
        });

        _binder.BindButton(saveBtn, async () =>
        {
            // Sync text areas back to ViewModel collections before saving
            SyncTextToCollection(licensesView, _vm.BannedLicenses);
            SyncTextToCollection(countriesView, _vm.BannedCountriesOfOrigin);
            SyncTextToCollection(orgsView, _vm.BannedOrganizations);
            SyncTextToCollection(individualsView, _vm.BannedIndividuals);
            await _vm.SaveAsync().ConfigureAwait(true);
        });
    }

    private TextView AddBanListSection(
        ref int y,
        string label,
        System.Collections.ObjectModel.ObservableCollection<string> collection)
    {
        var lbl = new Label { X = 0, Y = y, Text = label };
        Add(lbl);
        y++;

        var textView = new TextView
        {
            X = 0,
            Y = y,
            Width = Dim.Fill(),
            Height = 4,
            Text = string.Join("\n", collection),
            WordWrap = true,
        };
        Add(textView);
        y += 5;

        return textView;
    }

    private static void SyncTextToCollection(
        TextView textView,
        System.Collections.ObjectModel.ObservableCollection<string> collection)
    {
        collection.Clear();
        var text = textView.Text ?? "";
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(line))
                collection.Add(line);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _binder.Dispose();
        base.Dispose(disposing);
    }
}
