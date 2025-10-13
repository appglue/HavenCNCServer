using CentroidAPI;
using System;

namespace HavenCNCServer.Test
{
    /// <summary>
    /// Test class to check CentroidAPI structure and available methods
    /// </summary>
    public static class CentroidAPITest
    {
        /// <summary>
        /// Tests the CentroidAPI structure and available methods for development purposes
        /// </summary>
        public static void TestAPIStructure()
        {
            // This is just for development - checking what's available
            try
            {
                var pipe = new CNCPipe();
                if (pipe.IsConstructed())
                {
                    // Check inbound communications
                    var inbound = pipe.inbound_communications;
                    
                    // Try to find available methods
                    Console.WriteLine("InboundComm type: " + inbound.GetType().FullName);
                    
                    // Test the actual enum values and see what works
                    // Check available communication types
                    var allEnumValues = Enum.GetValues(typeof(CNCPipe.InboundComm.CommunicationTypes));
                    foreach (var value in allEnumValues)
                    {
                        Console.WriteLine("Available CommunicationType: " + value);
                    }
                    
                    // Test packet structure using reflection
                    var packetType = typeof(CNCPipe.InboundComm.CommPacket);
                    Console.WriteLine("CommPacket properties:");
                    foreach (var prop in packetType.GetProperties())
                    {
                        Console.WriteLine($"  Property: {prop.Name} ({prop.PropertyType.Name})");
                    }
                    
                    Console.WriteLine("CommPacket fields:");
                    foreach (var field in packetType.GetFields())
                    {
                        Console.WriteLine($"  Field: {field.Name} ({field.FieldType.Name})");
                    }
                }
                else
                {
                    Console.WriteLine("CNCPipe not constructed - CNC12 may not be running");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error testing API: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
            }
        }
    }
}