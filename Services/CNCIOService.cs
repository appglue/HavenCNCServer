using HavenCNCServer.Models;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Clean CNC IO service with simple method signatures
    /// </summary>
    public interface ICNCIOService
    {
        /// <summary>
        /// Get available input port numbers
        /// </summary>
        int[] GetAvailableInputs();

        /// <summary>
        /// Get available output port numbers
        /// </summary>
        int[] GetAvailableOutputs();

        /// <summary>
        /// Get current input states
        /// </summary>
        Dictionary<int, bool> GetCurrentInputs();

        /// <summary>
        /// Get current output states
        /// </summary>
        Dictionary<int, bool> GetCurrentOutputs();

        /// <summary>
        /// Check if specific input is active
        /// </summary>
        bool IsInputActive(int inputNumber);

        /// <summary>
        /// Check if specific output is active
        /// </summary>
        bool IsOutputActive(int outputNumber);

        /// <summary>
        /// Set output state
        /// </summary>
        void SetOutputState(int outputNumber, bool state);

        /// <summary>
        /// Override input for testing
        /// </summary>
        void OverrideInput(int inputNumber, bool value);

        /// <summary>
        /// Override output for testing
        /// </summary>
        void OverrideOutput(int outputNumber, bool value);

        /// <summary>
        /// Reset all input overrides
        /// </summary>
        void ResetInputOverrides();

        /// <summary>
        /// Reset all output overrides
        /// </summary>
        void ResetOutputOverrides();

        /// <summary>
        /// Check if input port is available
        /// </summary>
        bool IsInputAvailable(int inputNumber);

        /// <summary>
        /// Check if output port is available
        /// </summary>
        bool IsOutputAvailable(int outputNumber);

        /// <summary>
        /// Get system information
        /// </summary>
        string GetSystemInfo();

        /// <summary>
        /// Invert input polarity
        /// </summary>
        bool InvertInput(int inputNumber, bool invert = true);

        /// <summary>
        /// Invert multiple inputs
        /// </summary>
        bool InvertInputs(Dictionary<int, bool> inputSettings);
    }

    /// <summary>
    /// Implementation of clean CNC IO service
    /// </summary>
    public class CNCIOService : ICNCIOService
    {
        public int[] GetAvailableInputs()
        {
            try
            {
                return HavenCNCServer.CentriodAPI.CNCUtils.GetAvailableInputPorts();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available inputs: {ex.Message}", ex);
            }
        }

        public int[] GetAvailableOutputs()
        {
            try
            {
                return HavenCNCServer.CentriodAPI.CNCUtils.GetAvailableOutputPorts();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available outputs: {ex.Message}", ex);
            }
        }

        public Dictionary<int, bool> GetCurrentInputs()
        {
            // TODO: Implement actual CNC input reading
            return new Dictionary<int, bool>
            {
                { 1, true },
                { 2, false },
                { 3, true }
            };
        }

        public Dictionary<int, bool> GetCurrentOutputs()
        {
            // TODO: Implement actual CNC output reading
            return new Dictionary<int, bool>
            {
                { 1, false },
                { 2, true },
                { 3, false }
            };
        }

        public bool IsInputActive(int inputNumber)
        {
            if (inputNumber <= 0)
                throw new ArgumentException("Input number must be greater than 0");

            // TODO: Implement actual input check
            return false;
        }

        public bool IsOutputActive(int outputNumber)
        {
            if (outputNumber <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            // TODO: Implement actual output check
            return false;
        }

        public void SetOutputState(int outputNumber, bool state)
        {
            if (outputNumber <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            // TODO: Implement actual output setting
        }

        public void OverrideInput(int inputNumber, bool value)
        {
            if (inputNumber <= 0)
                throw new ArgumentException("Input number must be greater than 0");

            // TODO: Implement input override
        }

        public void OverrideOutput(int outputNumber, bool value)
        {
            if (outputNumber <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            // TODO: Implement output override
        }

        public void ResetInputOverrides()
        {
            // TODO: Implement reset input overrides
        }

        public void ResetOutputOverrides()
        {
            // TODO: Implement reset output overrides
        }

        public bool IsInputAvailable(int inputNumber)
        {
            try
            {
                return HavenCNCServer.CentriodAPI.CNCUtils.IsInputAvailable(inputNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check input availability: {ex.Message}", ex);
            }
        }

        public bool IsOutputAvailable(int outputNumber)
        {
            try
            {
                return HavenCNCServer.CentriodAPI.CNCUtils.IsOutputAvailable(outputNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check output availability: {ex.Message}", ex);
            }
        }

        public string GetSystemInfo()
        {
            try
            {
                return HavenCNCServer.CentriodAPI.CNCUtils.GetSystemInfo();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system info: {ex.Message}", ex);
            }
        }

        public bool InvertInput(int inputNumber, bool invert = true)
        {
            try
            {
                // TODO: Fix reference to CentroidConfigUtil
                // return CentroidConfigUtil.InvertInput(inputNumber, invert);
                return true; // Placeholder
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invert input: {ex.Message}", ex);
            }
        }

        public bool InvertInputs(Dictionary<int, bool> inputSettings)
        {
            try
            {
                // TODO: Fix reference to CentroidConfigUtil
                // return CentroidConfigUtil.InvertInputs(inputSettings);
                return true; // Placeholder
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invert inputs: {ex.Message}", ex);
            }
        }
    }
}