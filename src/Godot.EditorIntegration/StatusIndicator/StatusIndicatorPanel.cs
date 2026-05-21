using System;
using System.IO;
using System.Linq;
using Godot.EditorIntegration.Internals;

namespace Godot.EditorIntegration;

[GodotClass]
internal sealed partial class StatusIndicatorPanel : VBoxContainer
{
    private PopupPanel? _panelParent;

#nullable disable
    private Label _dotnetSdkInfoLabel;
    private LinkButton _dotnetSdkInfoButton;

    private Label _assemblyInfoLabel;
    private TextureRect _assemblyInfoIcon;

    private LinkButton _buildButton;
#nullable restore

    public StatusIndicatorPanel()
    {
        EditorInternal.StatusPanelSetContent(this);
    }

    protected override void _Ready()
    {
        // Find the parent PopupPanel to be able to hide it as a response to certain actions.
        {
            var current = GetParent();
            while (current is not null)
            {
                if (current is PopupPanel popupPanel)
                {
                    _panelParent = popupPanel;
                    break;
                }

                current = current.GetParent();
            }
        }

        // Godot.EditorIntegration line.
        {
            var hbox = new HBoxContainer();
            AddChild(hbox);

            var versionLabel = new Label();
            versionLabel.Text = SR.StatusIndicatorPanel_EditorIntegrationVersionLabel;
            hbox.AddChild(versionLabel);

            hbox.AddSpacer(begin: false);

            // If the the version is a prerelease, it may contain additional metadata after a '+' character,
            // which ends up being extremely long so we hide it from the UI. It will still be copied to the
            // clipboard when the user clicks the button.
            string version = EditorIntegrationState.Version;
            if (version.Contains('+'))
            {
                version = version.Substring(0, version.IndexOf('+'));
            }

            var versionInfoButton = new LinkButton()
            {
                Text = version,
                TooltipText = SR.StatusIndicatorPanel_ClickToCopyVersion,
            };
            versionInfoButton.Pressed += CopyEditorIntegrationVersionToClipboard;
            versionInfoButton.SetVSizeFlags(SizeFlags.ShrinkCenter);
            hbox.AddChild(versionInfoButton);
        }

        // .NET SDK line.
        {
            var hbox = new HBoxContainer();
            AddChild(hbox);

            var sdkLabel = new Label()
            {
                Text = ".NET SDK",
            };
            hbox.AddChild(sdkLabel);

            hbox.AddSpacer(begin: false);

            _dotnetSdkInfoLabel = new Label()
            {
                Text = EditorIntegrationState.DotNetSdkVersion,
            };
            hbox.AddChild(_dotnetSdkInfoLabel);

            _dotnetSdkInfoButton = new LinkButton()
            {
                Text = EditorIntegrationState.DotNetSdkVersion,
                TooltipText = SR.StatusIndicatorPanel_ClickToCopyVersion,
            };
            _dotnetSdkInfoButton.Pressed += CopyDotNetSdkToClipboard;
            _dotnetSdkInfoButton.SetVSizeFlags(SizeFlags.ShrinkCenter);
            _dotnetSdkInfoButton.Hide();
            hbox.AddChild(_dotnetSdkInfoButton);
        }

        // Assembly line.
        {
            var hbox = new HBoxContainer();
            AddChild(hbox);

            var assemblyIcon = new TextureRect()
            {
                StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            };
            _assemblyInfoIcon = new TextureRect()
            {
                StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            };
            _assemblyInfoIcon.Hide();
            hbox.AddChild(_assemblyInfoIcon);

            _assemblyInfoLabel = new Label()
            {
                Text = SR.StatusIndicatorPanel_AssemblyLoadState_None,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(512, 1),
            };
            _assemblyInfoLabel.SetVSizeFlags(SizeFlags.ShrinkCenter);
            hbox.AddChild(_assemblyInfoLabel);

            hbox.AddSpacer(begin: false);

            _buildButton = new LinkButton()
            {
                Text = SR.StatusIndicatorPanel_ProjectBuild,
            };
            _buildButton.Pressed += BuildAssembly;
            _buildButton.SetVSizeFlags(SizeFlags.ShrinkCenter);
            _buildButton.Hide();
            hbox.AddChild(_buildButton);
        }
    }

    // IMPORTANT: Do not rename this method, it's called from the .NET module
    // using the name "update" and changing it will break.
    [BindMethod(Name = "update")]
    public void Update()
    {
        UpdateContents();
    }

    private struct Severity
    {
        public StatusIndicatorSeverity Value { get; private set; }

        public void Update(StatusIndicatorSeverity newSeverity)
        {
            // We only want to update the severity if it's worse,
            // so we don't end up showing a warning icon when there's
            // already an error. The highest severity wins.
            if (newSeverity > Value)
            {
                Value = newSeverity;
            }
        }
    }

    private void UpdateContents()
    {
        var severity = new Severity();

        EditorInternal.ModuleGetAssemblyLoadState(out string? loadedAssemblyName, out AssemblyLoadState assemblyLoadState);

        // .NET SDK line.
        if (string.IsNullOrEmpty(EditorIntegrationState.DotNetSdkPath))
        {
            _dotnetSdkInfoLabel.Show();
            _dotnetSdkInfoButton.Hide();
        }
        else
        {
            _dotnetSdkInfoButton.Text = EditorIntegrationState.DotNetSdkVersion;
            _dotnetSdkInfoLabel.Hide();
            _dotnetSdkInfoButton.Show();
        }

        // Assembly line.
        {
            _buildButton.Hide();
            _assemblyInfoIcon.Hide();

            switch (assemblyLoadState)
            {
                case AssemblyLoadState.NotLoaded:
                case AssemblyLoadState.ProjectNotFound:
                {
                    _assemblyInfoLabel.Text = SR.StatusIndicatorPanel_AssemblyLoadState_None;
                    break;
                }

                case AssemblyLoadState.Loaded:
                {
                    _assemblyInfoLabel.Text = !string.IsNullOrEmpty(loadedAssemblyName)
                        ? $"{loadedAssemblyName}.dll"
                        : SR.StatusIndicatorPanel_AssemblyLoadState_Unknown;
                    break;
                }

                case AssemblyLoadState.DllNotFound:
                {
                    // We found a .csproj in the expected location, but no DLL.
                    // This likely means the user hasn't built the project yet, so it couldn't be loaded,
                    // which is needed to register C# classes and run editor classes.
                    // We let the user know about it, so they don't get confused about why their C# classes
                    // don't show up in the editor, and show a build button to fix it.
                    severity.Update(StatusIndicatorSeverity.Warning);
                    _assemblyInfoIcon.Show();
                    _assemblyInfoIcon.Texture = GetThemeIcon(EditorThemeNames.StatusWarning, EditorThemeNames.EditorIcons);
                    _assemblyInfoLabel.Text = SR.StatusIndicatorPanel_AssemblyLoadState_DllNotFound;
                    _buildButton.Show();
                    break;
                }

                case AssemblyLoadState.FailedToLoad:
                {
                    severity.Update(StatusIndicatorSeverity.Error);
                    _assemblyInfoIcon.Show();
                    _assemblyInfoIcon.Texture = GetThemeIcon(EditorThemeNames.StatusError, EditorThemeNames.EditorIcons);
                    _assemblyInfoLabel.Text = SR.StatusIndicatorPanel_AssemblyLoadState_Failed;
                    _buildButton.Show();
                    break;
                }

                case AssemblyLoadState.FailedToResolveGDExtensionEntryPoint:
                {
                    severity.Update(StatusIndicatorSeverity.Error);
                    _assemblyInfoIcon.Show();
                    _assemblyInfoIcon.Texture = GetThemeIcon(EditorThemeNames.StatusError, EditorThemeNames.EditorIcons);
                    _assemblyInfoLabel.Text = SR.StatusIndicatorPanel_AssemblyLoadState_FailedToResolveGDExtensionEntryPoint;
                    _buildButton.Show();
                    break;
                }


                case AssemblyLoadState.FailedToInitializeGDExtension:
                {
                    severity.Update(StatusIndicatorSeverity.Error);
                    _assemblyInfoIcon.Show();
                    _assemblyInfoIcon.Texture = GetThemeIcon(EditorThemeNames.StatusError, EditorThemeNames.EditorIcons);
                    _assemblyInfoLabel.Text = SR.StatusIndicatorPanel_AssemblyLoadState_FailedToInitializeGDExtension;
                    _buildButton.Show();
                    break;
                }
            }
        }

        EditorInternal.StatusIndicatorUpdateSeverity(severity.Value);
    }

    private static void CopyEditorIntegrationVersionToClipboard()
    {
        DisplayServer.Singleton.ClipboardSet($"Godot.EditorIntegration {EditorIntegrationState.Version}");
    }

    private static void CopyDotNetSdkToClipboard()
    {
        DisplayServer.Singleton.ClipboardSet($"{EditorIntegrationState.DotNetSdkVersion} [{EditorIntegrationState.DotNetSdkPath}]");
    }

    private void BuildAssembly()
    {
        _panelParent?.Hide();
        DotNetEditorPlugin.Singleton.BuildProjectPressed();
    }

    protected override void Dispose(bool disposing)
    {
        EditorInternal.StatusPanelSetContent(null);
        base.Dispose(disposing);
    }
}
