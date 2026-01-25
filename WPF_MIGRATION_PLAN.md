# HavenCNCServer - WPF Migration Plan

**Document Version:** 1.0  
**Date:** January 24, 2026  
**Status:** Ready for Migration

---

## Executive Summary

This document outlines the complete migration plan from WinForms to WPF for HavenCNCServer. The application has been audited and refactored to extract all business logic from UI components. The codebase is now architecturally ready for UI framework migration with minimal risk.

**Migration Readiness:** ✅ **100%**

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Architecture Assessment](#architecture-assessment)
3. [Migration Strategy](#migration-strategy)
4. [Implementation Plan](#implementation-plan)
5. [Risk Assessment & Mitigation](#risk-assessment--mitigation)
6. [Timeline & Effort Estimates](#timeline--effort-estimates)
7. [Testing Strategy](#testing-strategy)
8. [Rollback Plan](#rollback-plan)

---

## Current State Analysis

### Application Overview

**Type:** ASP.NET Core Web API + WinForms UI Host  
**Primary Function:** CNC machine control interface for Centroid CNC12  
**Technology Stack:**
- ASP.NET Core 8.0 Web API
- Windows Forms (current UI)
- SignalR for real-time updates
- CentroidAPI for CNC integration

### Forms Inventory

| Form Name | Purpose | UI Elements | Complexity | Readiness |
|-----------|---------|-------------|------------|-----------|
| **MainForm.cs** | Main application host | 6 buttons + menu + layout | Low | 100% |
| **BrowserForm.cs** | WebView2 browser | 1 WebView2 control | Very Low | 100% |
| **GCodeTestDialog.cs** | G-code testing | 5 buttons + textbox | Low | 100% |
| **SettingsForm.cs** | Application settings | 3 textboxes + 1 button | Very Low | 100% |
| **MessagesForm.cs** | CNC message display | 1 button + component | Very Low | 100% |
| **LogsForm.cs** | Application logs | 1 button + component | Very Low | 100% |
| **GCodeForm.cs** | G-code viewer | 1 component (read-only) | Very Low | 100% |

**Total Forms:** 7  
**Complexity Assessment:** All forms are simple with minimal UI elements and no business logic

### Component Inventory

| Component Name | WinForms Complexity | WPF Equivalent | Time to Recreate |
|----------------|---------------------|----------------|------------------|
| **CoordinateDisplayComponent** | 3 Labels (X/Y/Z) | 3 `TextBlock`s in a `Grid` | 1 hour |
| **MessageDisplayComponent** | RichTextBox + Clear button | `RichTextBox` + `Button` | 1 hour |
| **GCodeViewerComponent** | RichTextBox with line coloring | `RichTextBox` with `TextRange` coloring | 1 hour |
| **FlickerFreeLogViewer** | Custom RichTextBox | `RichTextBox` with `Dispatcher.Invoke` | 1 hour |

**Total Components:** 4  
**Total Time to Recreate:** 4 hours (not 2 weeks!)

**Decision:** Skip `WindowsFormsHost` entirely - create native WPF controls from the start.

---

## Architecture Assessment

### ✅ Strengths

1. **Clean Separation of Concerns**
   - All CNC control logic in Controllers/
   - All business services in Services/
   - All data models in Models/
   - UI is purely presentational

2. **Framework-Independent Backend**
   - ASP.NET Core Web API
   - Controllers have no UI dependencies
   - Services are pure C# classes
   - Models are POCOs

3. **Recent Refactoring Completed**
   - ✅ Script deployment extracted to `ScriptDeploymentService`
   - ✅ Step run logic extracted to `StepRunService`
   - ✅ G-code validation extracted to `GCodeValidationService`
   - ✅ All forms use service layer

4. **Minimal UI Business Logic**
   - Forms are thin coordination layers
   - No direct CNC API calls from UI
   - Event-driven architecture already in place

### ⚠️ Minimal Challenges

1. **Threading Pattern Update**
   - WinForms uses `Invoke/InvokeRequired`
   - WPF uses `Dispatcher.Invoke`
   - Straightforward find-and-replace in event handlers

2. **WebView2 Migration**
   - BrowserForm uses WinForms WebView2
   - WPF has native `Microsoft.Web.WebView2.Wpf` control
   - Drop-in replacement, same API

**Note:** No theming dependencies to migrate - Krypton Toolkit is WinForms-only visual styling that won't be needed. Material Design provides superior theming in WPF from day one.

---

## Migration Strategy

### Approach: **Incremental Parallel Migration**

We will run WinForms and WPF side-by-side during migration, allowing:
- Testing of WPF UI without disrupting production
- Gradual feature migration
- Easy rollback if issues arise
- Continuous delivery during migration

### Phase-Based Rollout

```
Phase 1: Foundation & Core UI (Week 1)
├── Create WPF project structure
├── Link backend (Controllers, Services, Models, Centroid)
├── Set up MVVM framework (Community Toolkit)
├── Implement base ViewModels and MainViewModel
├── Create MainWindow (6 buttons + menu + layout)
├── Create native WPF controls (simple!)
│   ├── CoordinateDisplayControl (3 TextBlocks) - 1 hour
│   ├── MessageDisplayControl (RichTextBox + button) - 1 hour
│   ├── GCodeViewerControl (RichTextBox) - 1 hour
│   └── LogViewerControl (RichTextBox) - 1 hour
└── Test basic CNC operations (Reset/Stop/Start)

Phase 2: Dialog Migration (Week 2)
├── Migrate GCodeTestDialog (5 buttons + textbox) - 1 day
├── Migrate SettingsWindow (3 textboxes + button) - 0.5 day
├── Migrate MessagesWindow (wrapper around control) - 0.5 day
├── Migrate LogsWindow (wrapper around control) - 0.5 day
├── Migrate GCodeWindow (wrapper around control) - 0.5 day
├── Migrate BrowserWindow (WebView2) - 0.5 day
└── Integration testing - 1 day

Phase 3: Polish & Production (Week 3)
├── Apply consistent theming (Material Design) - 1 day
├── Add polish (animations, transitions) - 1 day
├── Performance optimization and testing - 1 day
├── Documentation updates - 1 day
└── Final production testing - 1 day
```

**Key Change:** Skip `WindowsFormsHost` entirely! The components are so simple (just formatted TextBlocks/RichTextBoxes) that creating native WPF versions takes minutes, not days.

---

## Implementation Plan

### Step 1: Project Setup

**Create WPF Application Project**

```bash
dotnet new wpf -n HavenCNCServer.WPF -f net8.0-windows
cd HavenCNCServer.WPF
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Web.WebView2.Wpf
dotnet add package MaterialDesignThemes  # or ModernWpf
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

**Project Structure:**
```
HavenCNCServer.WPF/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Views/
│   ├── SettingsWindow.xaml
│   ├── MessagesWindow.xaml
│   ├── LogsWindow.xaml
│   ├── GCodeWindow.xaml
│   └── GCodeTestWindow.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── MessagesViewModel.cs
│   ├── LogsViewModel.cs
│   ├── GCodeViewModel.cs
│   └── GCodeTestViewModel.cs
├── Controls/
│   ├── CoordinateDisplayControl.xaml
│   ├── MessageDisplayControl.xaml
│   ├── GCodeViewerControl.xaml
│   └── LogViewerControl.xaml
├── Services/ (link to existing)
├── Controllers/ (link to existing)
├── Models/ (link to existing)
└── Centroid/ (link to existing)
```

### Step 2: Shared Backend Setup

**Link Existing Backend Files:**

Edit `HavenCNCServer.WPF.csproj`:

```xml
<ItemGroup>
  <!-- Link shared backend code -->
  <Compile Include="..\Controllers\**\*.cs" LinkBase="Controllers" />
  <Compile Include="..\Services\**\*.cs" LinkBase="Services" />
  <Compile Include="..\Models\**\*.cs" LinkBase="Models" />
  <Compile Include="..\Centroid\**\*.cs" LinkBase="Centroid" />
  
  <!-- Copy CentroidAPI.dll -->
  <None Include="..\CentroidAPI.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Step 3: MVVM Base Classes

**Create BaseViewModel.cs:**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HavenCNCServer.WPF.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? statusMessage;

        protected void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
    }
}
```

### Step 4: MainViewModel Implementation

**Create MainViewModel.cs:**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid;
using System.Windows;

namespace HavenCNCServer.WPF.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool isConnected;

        [ObservableProperty]
        private bool isCnc12Running;

        [ObservableProperty]
        private int connectionRetryCount;

        [ObservableProperty]
        private bool isAlwaysOnTop;

        public MainViewModel()
        {
            // Subscribe to events
            CNCConnectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            ApiManager.StatusChanged += OnApiStatusChanged;
        }

        [RelayCommand]
        private void ToggleAlwaysOnTop()
        {
            IsAlwaysOnTop = !IsAlwaysOnTop;
            Application.Current.MainWindow.Topmost = IsAlwaysOnTop;
        }

        [RelayCommand]
        private async Task ResetButton()
        {
            await Task.Run(() => CNCUtils.StartSkinEvent(SkinEvent.ResetButtonPressed));
            await Task.Delay(100);
            await Task.Run(() => CNCUtils.StopSkinEvent(SkinEvent.ResetButtonPressed));
        }

        [RelayCommand]
        private async Task StopButton()
        {
            await Task.Run(() => CNCUtils.StartSkinEvent(SkinEvent.CycleCancel));
            await Task.Delay(100);
            await Task.Run(() => CNCUtils.StopSkinEvent(SkinEvent.CycleCancel));
        }

        [RelayCommand]
        private async Task StartButton()
        {
            await Task.Run(() => CNCUtils.StartSkinEvent(SkinEvent.CycleStart));
            await Task.Delay(100);
            await Task.Run(() => CNCUtils.StopSkinEvent(SkinEvent.CycleStart));
        }

        private void OnConnectionStatusChanged(bool connected, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = connected;
                UpdateStatus(message);
            });
        }

        private void OnApiStatusChanged(string status, System.Drawing.Color color)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateStatus(status);
            });
        }
    }
}
```

### Step 5: MainWindow.xaml Implementation

**Create MainWindow.xaml:**

```xml
<Window x:Class="HavenCNCServer.WPF.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:HavenCNCServer.WPF.ViewModels"
        Title="HavenCNC Server" Height="800" Width="1400"
        Topmost="{Binding IsAlwaysOnTop}">
    
    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>
    
    <DockPanel>
        <!-- Top Menu Bar -->
        <Menu DockPanel.Dock="Top">
            <MenuItem Header="File">
                <MenuItem Header="Settings" Command="{Binding OpenSettingsCommand}" />
                <Separator />
                <MenuItem Header="Exit" Command="{Binding ExitCommand}" />
            </MenuItem>
            <MenuItem Header="View">
                <MenuItem Header="Always on Top" 
                          IsCheckable="True"
                          IsChecked="{Binding IsAlwaysOnTop}"
                          Command="{Binding ToggleAlwaysOnTopCommand}" />
                <Separator />
                <MenuItem Header="Logs" Command="{Binding ShowLogsCommand}" />
                <MenuItem Header="Messages" Command="{Binding ShowMessagesCommand}" />
                <MenuItem Header="G-Code" Command="{Binding ShowGCodeCommand}" />
            </MenuItem>
            <MenuItem Header="Tools">
                <MenuItem Header="G-Code Test" Command="{Binding ShowGCodeTestCommand}" />
                <MenuItem Header="Open Browser UI" Command="{Binding OpenBrowserUICommand}" />
                <MenuItem Header="Open Swagger" Command="{Binding OpenSwaggerCommand}" />
            </MenuItem>
            <MenuItem Header="Admin">
                <MenuItem Header="Open Data Folder" Command="{Binding OpenDataFolderCommand}" />
            </MenuItem>
        </Menu>
        
        <!-- Status Bar -->
        <StatusBar DockPanel.Dock="Bottom" Height="30">
            <StatusBarItem>
                <TextBlock Text="{Binding StatusMessage}" />
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="CNC12: " />
                    <TextBlock Text="{Binding IsCnc12Running, Converter={StaticResource BoolToStatusConverter}}" 
                               Foreground="{Binding IsCnc12Running, Converter={StaticResource BoolToColorConverter}}" />
                    <TextBlock Text="  |  Connection: " Margin="10,0,0,0" />
                    <TextBlock Text="{Binding IsConnected, Converter={StaticResource BoolToStatusConverter}}" 
                               Foreground="{Binding IsConnected, Converter={StaticResource BoolToColorConverter}}" />
                </StackPanel>
            </StatusBarItem>
        </StatusBar>
        
        <!-- Main Content -->
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="400" />
            </Grid.ColumnDefinitions>
            
            <!-- Left Panel - Controls -->
            <StackPanel Grid.Column="0" Margin="10">
                <TextBlock Text="CNC Control" FontSize="20" FontWeight="Bold" Margin="0,0,0,20" />
                
                <!-- Control Buttons -->
                <UniformGrid Columns="3" Rows="1" Height="60">
                    <Button Content="RESET" 
                            Command="{Binding ResetButtonCommand}"
                            Margin="5"
                            Background="Orange" />
                    <Button Content="STOP" 
                            Command="{Binding StopButtonCommand}"
                            Margin="5"
                            Background="Red" />
                    <Button Content="START" 
                            Command="{Binding StartButtonCommand}"
                            Margin="5"
                            Background="Green" />
                </UniformGrid>
                
                <!-- Tabs for Messages/Logs/GCode -->
                <TabControl Margin="0,20,0,0">
                    <TabItem Header="Messages">
                        <!-- Host WinForms MessageDisplayComponent initially -->
                        <WindowsFormsHost>
                            <!-- MessageDisplayComponent will be hosted here -->
                        </WindowsFormsHost>
                    </TabItem>
                    <TabItem Header="Logs">
                        <!-- Host WinForms FlickerFreeLogViewer initially -->
                        <WindowsFormsHost>
                            <!-- FlickerFreeLogViewer will be hosted here -->
                        </WindowsFormsHost>
                    </TabItem>
                    <TabItem Header="G-Code">
                        <!-- Host WinForms GCodeViewerComponent initially -->
                        <WindowsFormsHost>
                            <!-- GCodeViewerComponent will be hosted here -->
                        </WindowsFormsHost>
                    </TabItem>
                </TabControl>
            </StackPanel>
            
            <GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Center" VerticalAlignment="Stretch" />
            
            <!-- Right Panel - DRO -->
            <Border Grid.Column="2" BorderBrush="Gray" BorderThickness="1" Margin="10">
                <!-- Host WinForms CoordinateDisplayComponent initially -->
                <WindowsFormsHost>
                    <!-- CoordinateDisplayComponent will be hosted here -->
                </WindowsFormsHost>
            </Border>
        </Grid>
    </DockPanel>
</Window>
```

### Step 6: WinForms Component Hosting

**Create helper class for WinForms hosting:**

```csharp
using System.Windows.Forms.Integration;
using HavenCNCServer.Components;

namespace HavenCNCServer.WPF.Helpers
{
    public static class WinFormsComponentHosting
    {
        public static WindowsFormsHost CreateCoordinateDisplayHost()
        {
            var host = new WindowsFormsHost();
            host.Child = new CoordinateDisplayComponent();
            return host;
        }

        public static WindowsFormsHost CreateMessageDisplayHost()
        {
            var host = new WindowsFormsHost();
            host.Child = new MessageDisplayComponent();
            return host;
        }

        public static WindowsFormsHost CreateGCodeViewerHost()
        {
            var host = new WindowsFormsHost();
            host.Child = new GCodeViewerComponent();
            return host;
        }

        public static WindowsFormsHost CreateLogViewerHost()
        {
            var host = new WindowsFormsHost();
            var logViewer = new FlickerFreeLogViewer();
            var logTarget = new LoggingService.FlickerFreeLogTarget(logViewer, null);
            LoggingService.AddTarget(logTarget);
            host.Child = logViewer;
            return host;
        }
    }
}
```

---

## Risk Assessment & Mitigation

### High-Risk Items

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **CentroidAPI compatibility issues** | High | Low | Test early, CentroidAPI is COM-based and framework-independent |
| **Performance degradation with WindowsFormsHost** | Medium | Medium | Profile early, plan native WPF components in Phase 5 |
| **Threading/synchronization issues** | Medium | Low | All backend already thread-safe, just UI dispatcher changes |
| **Loss of functionality during migration** | High | Low | Run both UIs in parallel, incremental migration |

### Medium-Risk Items

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Learning curve for WPF/MVVM** | Low | High | Use Community Toolkit, extensive documentation available |
| **UI layout/styling differences** | Low | Medium | Iterate on design, get user feedback early |
| **WebView2 migration issues** | Low | Low | WPF has native WebView2, straightforward |

### Low-Risk Items

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Build/deployment changes** | Low | Low | Document new build process |
| **Configuration file changes** | Low | Low | Same appsettings.json, no changes needed |

---

## Timeline & Effort Estimates

### Overall Timeline: **3 weeks**

**Breakthrough Realization:** The "complex" components are actually trivial:
- **CoordinateDisplayComponent**: 3 labels (X/Y/Z values)
- **MessageDisplayComponent**: RichTextBox + Clear button
- **GCodeViewerComponent**: RichTextBox with line highlighting
- **FlickerFreeLogViewer**: RichTextBox with color text

These are **30 minutes each** to recreate in WPF, not days. No need for `WindowsFormsHost` at all!

| Phase | Duration | Effort (Hours) | Deliverable |
|-------|----------|----------------|-------------|
| **Phase 1: Foundation & Core** | 1 week | 35-40 | Complete MainWindow + all native controls |
| **Phase 2: Dialog Migration** | 1 week | 35-40 | All 6 dialogs migrated and tested |
| **Phase 3: Polish & Production** | 1 week | 30-40 | Themed, polished, production-ready |

**Total Estimated Effort:** 100-120 hours (2.5-3 weeks of full-time work)

### Detailed Breakdown

**Week 1 - Foundation:**
- Project setup: 4 hours
- MainViewModel + commands: 6 hours
- MainWindow XAML: 8 hours
- **Native controls (all 4): 4 hours total**
  - CoordinateDisplayControl: 1 hour (3 `TextBlock`s)
  - MessageDisplayControl: 1 hour (`RichTextBox` + button)
  - GCodeViewerControl: 1 hour (`RichTextBox` with highlighting)
  - LogViewerControl: 1 hour (`RichTextBox` with colors)
- Wire up events/bindings: 8 hours
- Testing: 6 hours

**Week 2 - Dialogs:**
- 6 simple dialogs × 4 hours each: 24 hours
- Integration and testing: 12 hours

**Week 3 - Polish:**
- Material Design theming: 8 hours
- Animations/transitions: 8 hours
- Performance tuning: 8 hours
- Documentation: 8 hours
- Final testing: 8 hours

### Milestones

- **End of Week 1:** Full working UI with all controls native WPF
- **End of Week 2:** Feature parity with WinForms
- **End of Week 3:** Polished, production-ready WPF application

---

## Testing Strategy

### Unit Testing

✅ **Already in place** - Services are testable without UI

**No changes needed:**
- Controllers already have no UI dependencies
- Services are pure C# classes
- Models are POCOs

### Integration Testing

**Test Areas:**
1. CentroidAPI integration (same as current)
2. SignalR event handling (verify Dispatcher.Invoke works)
3. API endpoint calls (same as current)
4. File I/O operations (same as current)

### UI Testing

**Manual Testing Checklist:**
- [ ] All buttons trigger correct CNC commands
- [ ] DRO updates in real-time
- [ ] Messages display correctly
- [ ] Logs display correctly
- [ ] G-code viewer updates
- [ ] Settings save/load correctly
- [ ] Always-on-top works
- [ ] Window resize/positioning
- [ ] Multi-monitor support
- [ ] WebView2 browser opens correctly

**Automated UI Testing (Optional):**
- Consider using FlaUI or WPF Test Automation Framework

### Performance Testing

**Metrics to Monitor:**
- UI responsiveness (target: <16ms frame time)
- Memory usage (should be similar to WinForms)
- CPU usage during real-time updates
- SignalR message latency

---

## Rollback Plan

### Scenario 1: Critical Bug in WPF Version

**Action:** Revert to WinForms immediately

1. Keep WinForms project intact during entire migration
2. Can switch back to WinForms with single configuration change
3. No backend changes = no data loss

### Scenario 2: Performance Issues

**Action:** Continue using WinForms while optimizing WPF

1. WinForms remains production
2. Fix WPF performance in parallel
3. Switch when performance acceptable

### Scenario 3: User Rejection

**Action:** Gather feedback, iterate on WPF design

1. Run both UIs in parallel
2. Allow users to switch between them
3. Incorporate feedback into WPF version

---

## Success Criteria

### Must-Have (Go/No-Go)

✅ All CNC control functions work identically  
✅ Real-time DRO updates with no lag  
✅ Messages and logs display correctly  
✅ G-code execution works  
✅ Settings persist correctly  
✅ No crashes or hangs  
✅ Performance equal to or better than WinForms  

### Nice-to-Have

🎨 Modern, polished UI design  
🎨 Smooth animations and transitions  
🎨 Consistent theming throughout  
🎨 Better touch support (if applicable)  
🎨 Improved layout for different screen sizes  

---

## Post-Migration Tasks

1. **Update Documentation**
   - User manual with new UI screenshots
   - Developer guide for WPF architecture
   - Update README.md

2. **Remove WinForms Dependencies**
   - Archive old WinForms project
   - Remove WinForms NuGet packages
   - Clean up unused code

3. **Performance Optimization**
   - Profile and optimize hot paths
   - Optimize XAML rendering
   - Consider virtualization for long lists

4. **User Training**
   - Create quick-start guide
   - Record demo videos
   - Provide migration FAQ

---

## Appendix A: Technology Decisions

### MVVM Framework: **Community Toolkit MVVM**

**Rationale:**
- Official Microsoft recommendation
- Source generators for reduced boilerplate
- Excellent documentation
- Active development

**Alternatives Considered:**
- Prism (too heavy for our needs)
- Caliburn.Micro (less active development)
- ReactiveUI (steeper learning curve)

### UI Framework: **Material Design In XAML Toolkit**

**Rationale:**
- Modern, professional look
- Extensive component library
- Good documentation
- Active community

**Alternatives Considered:**
- ModernWpf (Windows 11 style)
- MahApps.Metro (older style)
- Custom theming (too much work)

### Dependency Injection: **Microsoft.Extensions.DependencyInjection**

**Rationale:**
- Already used in ASP.NET Core backend
- Consistent pattern across application
- Built-in support for ViewModels

---

## Appendix B: Key Dependencies

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="MaterialDesignThemes" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
<PackageReference Include="Microsoft.Web.WebView2.Wpf" Version="1.0.2210.55" />
```

---

## Appendix C: Contact & Resources

**Project Lead:** [Your Name]  
**Repository:** [Repository URL]  
**Documentation:** [Documentation URL]  

**Useful Resources:**
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Community Toolkit MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Material Design In XAML](http://materialdesigninxaml.net/)
- [WPF Tutorial](https://wpf-tutorial.com/)

---

**End of Migration Plan Document**
