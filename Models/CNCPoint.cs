using System;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Represents a point in CNC machine coordinates with X, Y, Z, and A axis values
    /// </summary>
    public class CNCPoint
    {
        /// <summary>
        /// X-axis coordinate
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y-axis coordinate
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Z-axis coordinate
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// A-axis coordinate (rotational axis)
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public CNCPoint()
        {
        }

        /// <summary>
        /// Constructor with X, Y, Z coordinates
        /// </summary>
        /// <param name="x">X-axis coordinate</param>
        /// <param name="y">Y-axis coordinate</param>
        /// <param name="z">Z-axis coordinate</param>
        public CNCPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            A = 0;
        }

        /// <summary>
        /// Constructor with X, Y, Z, A coordinates
        /// </summary>
        /// <param name="x">X-axis coordinate</param>
        /// <param name="y">Y-axis coordinate</param>
        /// <param name="z">Z-axis coordinate</param>
        /// <param name="a">A-axis coordinate</param>
        public CNCPoint(double x, double y, double z, double a)
        {
            X = x;
            Y = y;
            Z = z;
            A = a;
        }

        /// <summary>
        /// Returns a string representation of the CNC point
        /// </summary>
        /// <returns>String in format "X:{X}, Y:{Y}, Z:{Z}, A:{A}"</returns>
        public override string ToString()
        {
            return $"X:{X}, Y:{Y}, Z:{Z}, A:{A}";
        }
    }
}
