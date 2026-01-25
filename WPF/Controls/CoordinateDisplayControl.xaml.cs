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
    public partial class CoordinateDisplayControl : System.Windows.Controls.UserControl, ICNCEventListener
    {
        public CoordinateDisplayControl()
        {
            InitializeComponent();
            DataContext = new CoordinateDisplayViewModel();

            // Register as event listener with CNCJobInfoListener
            CNCJobInfoListener.AddListener(this);
        }

        /// <summary>
        /// Receives and processes CNC events for coordinate updates
        /// </summary>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            if (centroidEvent is DROEvent droEvent && DataContext is CoordinateDisplayViewModel vm)
            {
                // Update UI on the main thread using Dispatcher
                Dispatcher.Invoke(() =>
                {
                    vm.UpdateCoordinates(droEvent.Axis1, droEvent.Axis2, droEvent.Axis3);
                });
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Unregister from event listener when control is unloaded
            CNCJobInfoListener.RemoveListener(this);
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
