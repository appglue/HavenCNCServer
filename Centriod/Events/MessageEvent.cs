using System;
using System.Text.RegularExpressions;
using CentroidAPI;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Centriod.Events
{
    /// <summary>
    /// Event containing a CNC message with error code and classification
    /// </summary>
    public class MessageEvent : ICentroidEvent 
    {
        /// <summary>
        /// Timestamp when the message event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Numeric error or message code from the CNC system
        /// </summary>
        public int EventCode { get; set; }
        
        /// <summary>
        /// Message text content
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Classified type of the message event
        /// </summary>
        public MessageEventType EventType { get; set; }

        /// <summary>
        /// Process MESSAGE_WINDOW_MESSAGE - Contains message text
        /// </summary>
        public static MessageEvent? ProcessMessage(CNCPipe.InboundComm.CommPacket packet, System.Action<string> logToFile, System.Action<ICentroidEvent, string> storeMessage, System.Action<ICentroidEvent> notifyListeners)
        {
            logToFile($"    Message Window Event:");
            
            // According to API docs: "Message - the added message"
            var message = GetPacketProperty<string>(packet, "Message", "");
            if (!string.IsNullOrWhiteSpace(message))
            {
                logToFile($"    Message: {message}");
            }
            
            // Try other common message property names
            var text = GetPacketProperty<string>(packet, "Text", "");
            var content = GetPacketProperty<string>(packet, "Content", "");
            
            if (!string.IsNullOrWhiteSpace(text)) logToFile($"    Text: {text}");
            if (!string.IsNullOrWhiteSpace(content)) logToFile($"    Content: {content}");

            // Determine the final message
            var finalMessage = message;
            if (string.IsNullOrWhiteSpace(finalMessage)) finalMessage = text;
            if (string.IsNullOrWhiteSpace(finalMessage)) finalMessage = content;
            
            if (!string.IsNullOrWhiteSpace(finalMessage))
            {
                // Extract error code and determine event type based on Centroid documentation
                var (eventCode, eventType) = ClassifyMessage(finalMessage);

                var messageEvent = new MessageEvent
                {
                    Timestamp = DateTime.Now,
                    EventCode = eventCode,
                    Message = finalMessage,
                    EventType = eventType
                };
                
                // Store the message event in history
                storeMessage(messageEvent, "MESSAGE_WINDOW_MESSAGE");
                
                // Notify listeners
                notifyListeners(messageEvent);
                
                return messageEvent;
            }
            
            return null;
        }

        /// <summary>
        /// Classify a CNC message based on error codes and content according to Centroid documentation
        /// </summary>
        /// <param name="message">The message to classify</param>
        /// <returns>Tuple of (EventCode, EventType)</returns>
        public static (int eventCode, MessageEventType eventType) ClassifyMessage(string message)
        {
            var eventCode = ExtractErrorCode(message);
            var eventType = ClassifyByErrorCode(eventCode, message);
            return (eventCode, eventType);
        }

        /// <summary>
        /// Extract numeric error code from message text
        /// </summary>
        /// <param name="message">Message text</param>
        /// <returns>Error code or 0 if not found</returns>
        private static int ExtractErrorCode(string message)
        {
            // Look for patterns like "Error 123" or "123:" or message starting with digits
            var patterns = new[]
            {
                @"(?:Error|Code|Fault)\s*(\d+)", // "Error 123", "Code 123", "Fault 123"
                @"^(\d{3,4})\s*:", // "123:" at start of message
                @"^(\d{3,4})\s+", // "123 " at start of message
                @"\b(\d{3,4})\b" // Any 3-4 digit number
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int code))
                {
                    return code;
                }
            }

            return 0; // No code found
        }

        /// <summary>
        /// Classify message type based on error code ranges from Centroid documentation
        /// </summary>
        /// <param name="errorCode">Numeric error code</param>
        /// <param name="message">Original message for fallback classification</param>
        /// <returns>Message event type</returns>
        private static MessageEventType ClassifyByErrorCode(int errorCode, string message)
        {
            // Classify based on Centroid error code ranges
            if (errorCode == 0)
            {
                return ClassifyByContent(message);
            }
            
            // Configuration messages (special codes)
            if (errorCode == 111 || errorCode == 444 || errorCode == 555 || errorCode == 556 || 
                errorCode == 777 || errorCode == 888 || errorCode == 999)
            {
                return MessageEventType.ConfigurationChange;
            }
            
            // Startup errors and messages (100-199)
            if (errorCode >= 102 && errorCode <= 106)
            {
                return MessageEventType.StartupError;
            }
            if (errorCode == 199)
            {
                return MessageEventType.StatusMessage; // "CNC started"
            }
            
            // Exit messages (200-299)
            if (errorCode >= 201 && errorCode <= 204 || errorCode == 222)
            {
                return errorCode == 222 ? MessageEventType.StatusMessage : MessageEventType.ExitMessage;
            }
            
            // Status messages (300-399)
            if (errorCode >= 301 && errorCode <= 347)
            {
                return ClassifyStatusMessage(errorCode);
            }
            
            // Faults and abnormal stops (400-499)
            if (errorCode >= 401 && errorCode <= 490)
            {
                return ClassifyFaultMessage(errorCode);
            }
            
            // Syntax errors (500-599)
            if (errorCode >= 501 && errorCode <= 552)
            {
                return MessageEventType.SyntaxError;
            }
            
            // Cutter compensation errors (600-699)
            if (errorCode >= 601 && errorCode <= 608)
            {
                return MessageEventType.CutterCompensationError;
            }
            
            // Parameter setting errors (700-799)
            if (errorCode >= 701 && errorCode <= 705)
            {
                return MessageEventType.ParameterSettingError;
            }
            
            // Canned cycle errors (800-899)
            if (errorCode >= 801 && errorCode <= 807)
            {
                return MessageEventType.CannedCycleError;
            }
            
            // Miscellaneous errors (900-999)
            if (errorCode >= 901 && errorCode <= 949)
            {
                return MessageEventType.MiscellaneousError;
            }
            
            // Scaling/mirroring errors (1000-1199)
            if (errorCode >= 1001 && errorCode <= 1199)
            {
                return MessageEventType.ScalingError;
            }
            
            // Unknown error code
            return MessageEventType.Unknown;
        }

        /// <summary>
        /// Classify status messages (300-399 range) into more specific types
        /// </summary>
        private static MessageEventType ClassifyStatusMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 301:
                    return MessageEventType.StatusMessage;    // "Stopped"
                case 302:
                    return MessageEventType.StatusMessage;    // "Moving..."
                case 303:
                    return MessageEventType.StatusMessage;    // "Paused..."
                case 304:
                    return MessageEventType.StatusMessage;    // "MDI..."
                case 305:
                    return MessageEventType.StatusMessage;    // "Processing..."
                case 306:
                    return MessageEventType.JobCompleted;     // "Job Finished"
                case 307:
                    return MessageEventType.JobCancelled;     // "Operator abort: job canceled"
                case 338:
                    return MessageEventType.JobCancelled;     // "Job Cancelled"
                default:
                    // Various probing errors (318-337)
                    if (errorCode >= 318 && errorCode <= 337)
                    {
                        return MessageEventType.ProbeError;
                    }
                    // Various cancellation reasons (320-330) - note overlap with probe range
                    if (errorCode >= 320 && errorCode <= 330)
                    {
                        return MessageEventType.SystemFault;
                    }
                    return MessageEventType.StatusMessage;
            }
        }

        /// <summary>
        /// Classify fault messages (400-499 range) into more specific types  
        /// </summary>
        private static MessageEventType ClassifyFaultMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 401:
                    return MessageEventType.SystemFault;      // "PLC failure detected"
                case 404:
                    return MessageEventType.SystemFault;      // "Spindle drive fault detected" 
                case 405:
                    return MessageEventType.SystemFault;      // "Lubricant level low"
                case 406:
                    return MessageEventType.SystemFault;      // "Emergency Stop detected"
                case 407:
                    return MessageEventType.LimitError;       // "limit (#) tripped"
                default:
                    // Various axis faults (409-447)
                    if (errorCode >= 409 && errorCode <= 447)
                    {
                        return MessageEventType.AxisFault;
                    }
                    // Communication errors (452-453)
                    if (errorCode >= 452 && errorCode <= 453)
                    {
                        return MessageEventType.CommunicationError;
                    }
                    // Various system faults (449-460)
                    if (errorCode >= 449 && errorCode <= 460)
                    {
                        return MessageEventType.SystemFault;
                    }
                    return MessageEventType.SystemFault;
            }
        }

        /// <summary>
        /// Fallback classification based on message content when no error code is found
        /// </summary>
        private static MessageEventType ClassifyByContent(string message)
        {
            var lower = message.ToLower();
            
            // Job-related keywords
            if (lower.Contains("job") && (lower.Contains("start") || lower.Contains("begin")))
                return MessageEventType.JobStarted;
            if (lower.Contains("job") && (lower.Contains("finish") || lower.Contains("complete") || lower.Contains("done")))
                return MessageEventType.JobCompleted;  
            if (lower.Contains("job") && (lower.Contains("cancel") || lower.Contains("abort")))
                return MessageEventType.JobCancelled;
                
            // Error keywords
            if (lower.Contains("limit"))
                return MessageEventType.LimitError;
            if (lower.Contains("probe"))
                return MessageEventType.ProbeError;
            if (lower.Contains("axis") && lower.Contains("fault"))
                return MessageEventType.AxisFault;
            if (lower.Contains("syntax") || lower.Contains("invalid"))
                return MessageEventType.SyntaxError;
            if (lower.Contains("parameter") && lower.Contains("error"))
                return MessageEventType.ParameterError;
            if (lower.Contains("compensation"))
                return MessageEventType.CutterCompensationError;
                
            // Generic classifications
            if (lower.Contains("error") || lower.Contains("fault"))
                return MessageEventType.SystemFault;
            if (lower.Contains("config") || lower.Contains("modified"))
                return MessageEventType.ConfigurationChange;
                
            return MessageEventType.StatusMessage; // Default for unclassified messages
        }

        /// <summary>
        /// Safely get a property value from the CommPacket using reflection
        /// </summary>
        private static T GetPacketProperty<T>(CNCPipe.InboundComm.CommPacket packet, string propertyName, T defaultValue)
        {
            try
            {
                var packetType = packet.GetType();
                var property = packetType.GetProperty(propertyName);
                
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(packet);
                    if (value is T typedValue)
                        return typedValue;
                    
                    // Try to convert the value if it's not the exact type
                    if (value != null)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            // Conversion failed, return default
                        }
                    }
                }
                
                // Also try fields if property not found
                var field = packetType.GetField(propertyName);
                if (field != null)
                {
                    var value = field.GetValue(packet);
                    if (value is T typedValue)
                        return typedValue;
                        
                    if (value != null)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            // Conversion failed, return default
                        }
                    }
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                LogDebug($"Error getting packet property '{propertyName}': {ex.Message}", "MessageEvent");
                return defaultValue;
            }
        }
    }
}