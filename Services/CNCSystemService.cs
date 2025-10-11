using HavenCNCServer.Services;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Clean CNC System service interface
    /// </summary>
    public interface ICNCSystemService
    {
        // UI Control
        Task<bool> EnterFullScreenAsync();
        Task<bool> ExitFullScreenAsync();
        bool IsFullScreen { get; }

        // System Control
        void Shutdown();
        void RestartCentroid();

        // Machine State Control
        void EmergencyStop();
        bool IsHomed();
        void UnhomeMachine();
        void HomeMachine();
        string[]? GetCurrentErrorState();
        void ResetErrorState();
        void ResetMachine();
        bool HasCurrentErrors();
    }

    /// <summary>
    /// Implementation of clean CNC System service
    /// </summary>
    public class CNCSystemService : ICNCSystemService
    {
        // UI Control
        public async Task<bool> EnterFullScreenAsync()
        {
            return await UIControlService.EnterFullScreenAsync();
        }

        public async Task<bool> ExitFullScreenAsync()
        {
            return await UIControlService.ExitFullScreenAsync();
        }

        public bool IsFullScreen => UIControlService.IsFullScreen;

        // System Control
        public void Shutdown()
        {
            // TODO: Implement shutdown functionality
        }

        public void RestartCentroid()
        {
            // TODO: Implement Centroid restart functionality
        }

        // Machine State Control
        public void EmergencyStop()
        {
            // TODO: Implement emergency stop functionality
        }

        public bool IsHomed()
        {
            // TODO: Implement homed check
            return true;
        }

        public void UnhomeMachine()
        {
            // TODO: Implement unhome machine functionality
        }

        public void HomeMachine()
        {
            // TODO: Implement home machine functionality
        }

        public string[]? GetCurrentErrorState()
        {
            // TODO: Implement get current error state
            return null;
        }

        public void ResetErrorState()
        {
            // TODO: Implement reset error state functionality
        }

        public void ResetMachine()
        {
            // TODO: Implement reset machine functionality
        }

        public bool HasCurrentErrors()
        {
            // TODO: Implement has current errors check
            return false;
        }
    }
}