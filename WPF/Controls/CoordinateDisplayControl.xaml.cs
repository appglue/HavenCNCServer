using System;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.WPF.Controls
{
    /// <summary>
    /// WPF control for displaying machine coordinates (DRO - Digital Readout)
    /// </summary>
    public partial class CoordinateDisplayControl : System.Windows.Controls.UserControl, IEventSubscriber
    {
        public CoordinateDisplayControl()
        {
            InitializeComponent();
            DataContext = new CoordinateDisplayViewModel();

            // Subscribe to CNCEventBus for position updates
            CNCEventBus.Instance.Subscribe(this);
        }

        public EventTypeFlags GetSubscribedEvents()
        {
            return EventTypeFlags.Position; // Only subscribe to position/DRO updates
        }

        public void OnPositionUpdate(DROEvent position)
        {
            try
            {
                // Update UI on the main thread using Dispatcher
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (DataContext is CoordinateDisplayViewModel vm)
                        {
                            vm.UpdateCoordinates(position.Axis1, position.Axis2, position.Axis3);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error updating coordinates on UI thread: {ex.Message}", "CoordinateDisplay");
                    }
                }));
            }
            catch (Exception ex)
            {
                LogError($"Error in OnPositionUpdate: {ex.Message}", "CoordinateDisplay");
            }
        }

        public void OnLogMessage(LogEvent log)
        {
            // Not interested in log messages
        }

        public void OnCNCMessage(ICentroidEvent message)
        {
            // Not interested in CNC messages
        }

        public void OnServerStatus(ServerStatusEvent status)
        {
            // Not interested in server status
        }

        /// <summary>
        /// Receives and processes CNC events for coordinate updates
        /// </summary>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            try
            {
                if (centroidEvent is DROEvent droEvent)
                {
                    // Update UI on the main thread using Dispatcher
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (DataContext is CoordinateDisplayViewModel vm)
                            {
                                vm.UpdateCoordinates(droEvent.Axis1, droEvent.Axis2, droEvent.Axis3);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error updating coordinates on UI thread: {ex.Message}", "CoordinateDisplay");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in EventReceived: {ex.Message}", "CoordinateDisplay");
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Unsubscribe from event bus when control is unloaded
            CNCEventBus.Instance.Unsubscribe(this);
        }
    }

    /// <summary>
    /// ViewModel for coordinate display
    /// </summary>
    public partial class CoordinateDisplayViewModel : ObservableObject
    {
        [ObservableProperty]
        private double xValue = 0.0;

        [ObservableProperty]
        private double yValue = 0.0;

        [ObservableProperty]
        private double zValue = 0.0;

        public void UpdateCoordinates(double x, double y, double z)
        {
            try
            {
                XValue = x;
                YValue = y;
                ZValue = z;
            }
            catch (Exception ex)
            {
                LogError($"Error updating coordinate display: {ex.Message}", "CoordinateDisplay");
            }
        }
    }
}
