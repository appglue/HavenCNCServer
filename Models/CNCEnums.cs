namespace HavenCNCServer.Models
{
    /// <summary>
    /// Jog increment speed multiplier
    /// </summary>
    public enum JogIncrementSpeed
    {
        /// <summary>
        /// X1 increment speed
        /// </summary>
        X1 = 1,

        /// <summary>
        /// X10 increment speed
        /// </summary>
        X10 = 10,

        /// <summary>
        /// X100 increment speed
        /// </summary>
        X100 = 100
    }

    /// <summary>
    /// IO events that can be monitored or triggered
    /// </summary>
    public enum IOEvent
    {
        /// <summary>
        /// X-axis limit switch
        /// </summary>
        XLimit,

        /// <summary>
        /// Y-axis limit switch
        /// </summary>
        YLimit,

        /// <summary>
        /// Z-axis limit switch
        /// </summary>
        ZLimit,

        /// <summary>
        /// Tool touch pad sensor
        /// </summary>
        ToolTouchPad,

        /// <summary>
        /// Material touch pad sensor
        /// </summary>
        MaterialTouchPad
    }

    /// <summary>
    /// Directions for machine movement
    /// </summary>
    public enum MoveDirection
    {
        /// <summary>
        /// Move in positive X direction
        /// </summary>
        XPositive,

        /// <summary>
        /// Move in negative X direction
        /// </summary>
        XNegative,

        /// <summary>
        /// Move in positive Y direction
        /// </summary>
        YPositive,

        /// <summary>
        /// Move in negative Y direction
        /// </summary>
        YNegative,

        /// <summary>
        /// Move in positive Z direction
        /// </summary>
        ZPositive,

        /// <summary>
        /// Move in negative Z direction
        /// </summary>
        ZNegative
    }

    /// <summary>
    /// Types of movement commands
    /// </summary>
    public enum MoveType
    {
        /// <summary>
        /// Relative movement from current position
        /// </summary>
        Relative,

        /// <summary>
        /// Absolute movement to specific coordinates
        /// </summary>
        Absolute
    }

    /// <summary>
    /// Movement strategies for multi-axis moves
    /// </summary>
    public enum MoveStrategy
    {
        /// <summary>
        /// Move all axes simultaneously in a straight line
        /// </summary>
        Direct,

        /// <summary>
        /// Move Z-axis first, then XY axes
        /// </summary>
        ZSeparate
    }

    /// <summary>
    /// Coordinate system types for movement commands
    /// </summary>
    public enum APICoordinateType
    {
        /// <summary>
        /// Machine coordinates (G53) - absolute machine position, ignores work offsets
        /// </summary>
        Machine,

        /// <summary>
        /// Work zero coordinates (G54-G59) - uses active work coordinate system with offsets
        /// </summary>
        WorkZero
    }

    /// <summary>
    /// Actions that can be performed on skin events
    /// </summary>
    public enum SkinEventAction
    {
        /// <summary>
        /// Press and hold the button/event
        /// </summary>
        Press,

        /// <summary>
        /// Release the button/event
        /// </summary>
        Release,

        /// <summary>
        /// Single click (press and release with 100ms delay)
        /// </summary>
        Click,

        /// <summary>
        /// Double click (two clicks with 100ms between each press/release)
        /// </summary>
        DoubleClick
    }

    /// <summary>
    /// Virtual Control Panel (Skin) Events - corresponds to SV_SKIN_EVENT_1 through SV_SKIN_EVENT_256
    /// </summary>
    public enum SkinEvent
    {
        // Row 1 - Spindle Controls and Aux Buttons
        /// <summary>SV_SKIN_EVENT_1 - Spindle Override Plus (Row 1 Column 1)</summary>
        SpinOverPlus = 1,
        /// <summary>SV_SKIN_EVENT_2 - Spindle Auto/Manual Toggle (Row 1 Column 2)</summary>
        SpinAutoMan = 2,
        /// <summary>SV_SKIN_EVENT_3 - Auxiliary Button 1 (Row 1 Column 3)</summary>
        Aux1 = 3,
        /// <summary>SV_SKIN_EVENT_4 - Auxiliary Button 2 (Row 1 Column 4)</summary>
        Aux2 = 4,
        /// <summary>SV_SKIN_EVENT_5 - Auxiliary Button 3 (Row 1 Column 5)</summary>
        Aux3 = 5,

        // Row 2 - Spindle Range and Direction
        /// <summary>SV_SKIN_EVENT_6 - Spindle 100% Override (Row 2 Column 1)</summary>
        Spin100 = 6,
        /// <summary>SV_SKIN_EVENT_7 - Spindle Clockwise (Row 2 Column 2)</summary>
        SpinCW = 7,
        /// <summary>SV_SKIN_EVENT_8 - Auxiliary Button 4 (Row 1 Column 6)</summary>
        Aux4 = 8,
        /// <summary>SV_SKIN_EVENT_9 - Auxiliary Button 5 (Row 2 Column 3)</summary>
        Aux5 = 9,
        /// <summary>SV_SKIN_EVENT_10 - Auxiliary Button 6 (Row 2 Column 4)</summary>
        Aux6 = 10,

        // Row 3 - Spindle Controls Continued
        /// <summary>SV_SKIN_EVENT_11 - Spindle Override Minus (Row 3 Column 1)</summary>
        SpinOverMinus = 11,
        /// <summary>SV_SKIN_EVENT_12 - Spindle Counter-Clockwise (Row 3 Column 2)</summary>
        SpinCCW = 12,
        /// <summary>SV_SKIN_EVENT_13 - Auxiliary Button 7 (Row 2 Column 5)</summary>
        Aux7 = 13,
        /// <summary>SV_SKIN_EVENT_14 - Auxiliary Button 8 (Row 2 Column 6)</summary>
        Aux8 = 14,
        /// <summary>SV_SKIN_EVENT_15 - Auxiliary Button 9 (Row 3 Column 3)</summary>
        Aux9 = 15,

        // Row 4 - Spindle Start/Stop
        /// <summary>SV_SKIN_EVENT_16 - Spindle Stop (Row 4 Column 1)</summary>
        SpinStop = 16,
        /// <summary>SV_SKIN_EVENT_17 - Spindle Start (Row 4 Column 2)</summary>
        SpinStart = 17,
        /// <summary>SV_SKIN_EVENT_18 - Auxiliary Button 10 (Row 3 Column 4)</summary>
        Aux10 = 18,
        /// <summary>SV_SKIN_EVENT_19 - Auxiliary Button 11 (Row 3 Column 5)</summary>
        Aux11 = 19,
        /// <summary>SV_SKIN_EVENT_20 - Auxiliary Button 12 (Row 3 Column 6)</summary>
        Aux12 = 20,

        // Row 5 - Coolant Controls
        /// <summary>SV_SKIN_EVENT_21 - Coolant Auto/Manual (Row 5 Column 1)</summary>
        CoolAutoMan = 21,
        /// <summary>SV_SKIN_EVENT_22 - Coolant Flood (Row 5 Column 2)</summary>
        CoolFlood = 22,
        /// <summary>SV_SKIN_EVENT_23 - Coolant Mist (Row 5 Column 3)</summary>
        CoolMist = 23,
        /// <summary>SV_SKIN_EVENT_24 - Auxiliary Button 13 (Row 5 Column 4)</summary>
        Aux13 = 24,
        /// <summary>SV_SKIN_EVENT_25 - Auxiliary Button 14 (Row 5 Column 5)</summary>
        Aux14 = 25,

        // Row 6 - Jog Increments
        /// <summary>SV_SKIN_EVENT_26 - Incremental/Continuous Jog (Row 6 Column 1)</summary>
        IncCont = 26,
        /// <summary>SV_SKIN_EVENT_27 - X1 Jog Increment (Row 6 Column 2)</summary>
        X1 = 27,
        /// <summary>SV_SKIN_EVENT_28 - X10 Jog Increment (Row 6 Column 3)</summary>
        X10 = 28,
        /// <summary>SV_SKIN_EVENT_29 - X100 Jog Increment (Row 6 Column 4)</summary>
        X100 = 29,
        /// <summary>SV_SKIN_EVENT_30 - MPG Mode (Row 6 Column 5)</summary>
        MPG = 30,

        // Rows 7-9 - Axis Jogging
        /// <summary>SV_SKIN_EVENT_31 - Jog Axis 4 Plus (Row 7 Column 1)</summary>
        JogAx4Plus = 31,
        /// <summary>SV_SKIN_EVENT_32 - Row 7 Column 2 (Generic)</summary>
        R7C2 = 32,
        /// <summary>SV_SKIN_EVENT_33 - Jog Axis 2 Plus (Row 7 Column 3)</summary>
        JogAx2Plus = 33,
        /// <summary>SV_SKIN_EVENT_34 - Row 7 Column 4 (Generic)</summary>
        R7C4 = 34,
        /// <summary>SV_SKIN_EVENT_35 - Jog Axis 3 Plus (Row 7 Column 5)</summary>
        JogAx3Plus = 35,
        /// <summary>SV_SKIN_EVENT_36 - Row 8 Column 1 (Generic)</summary>
        R8C1 = 36,
        /// <summary>SV_SKIN_EVENT_37 - Jog Axis 1 Minus (Row 8 Column 2)</summary>
        JogAx1Minus = 37,
        /// <summary>SV_SKIN_EVENT_38 - Fast/Slow Jog Toggle (Row 8 Column 3)</summary>
        FastSlowJog = 38,
        /// <summary>SV_SKIN_EVENT_39 - Jog Axis 1 Plus (Row 8 Column 4)</summary>
        JogAx1Plus = 39,
        /// <summary>SV_SKIN_EVENT_40 - Row 8 Column 5 (Generic)</summary>
        R8C5 = 40,
        /// <summary>SV_SKIN_EVENT_41 - Jog Axis 4 Minus (Row 9 Column 1)</summary>
        JogAx4Minus = 41,
        /// <summary>SV_SKIN_EVENT_42 - Row 9 Column 2 (Generic)</summary>
        R9C2 = 42,
        /// <summary>SV_SKIN_EVENT_43 - Jog Axis 2 Minus (Row 9 Column 3)</summary>
        JogAx2Minus = 43,
        /// <summary>SV_SKIN_EVENT_44 - Row 9 Column 4 (Generic)</summary>
        R9C4 = 44,
        /// <summary>SV_SKIN_EVENT_45 - Jog Axis 3 Minus (Row 9 Column 5)</summary>
        JogAx3Minus = 45,

        // Row 10 - Cycle Controls
        /// <summary>SV_SKIN_EVENT_46 - Cycle Cancel (Row 10 Column 1)</summary>
        CycleCancel = 46,
        /// <summary>SV_SKIN_EVENT_47 - Single Block Mode (Row 10 Column 2)</summary>
        SingleBlock = 47,
        /// <summary>SV_SKIN_EVENT_48 - Tool Check (Row 10 Column 3)</summary>
        ToolCheck = 48,
        /// <summary>SV_SKIN_EVENT_49 - Feed Hold (Row 10 Column 4)</summary>
        FeedHold = 49,
        /// <summary>SV_SKIN_EVENT_50 - Cycle Start</summary>
        CycleStart = 50,

        // Additional Control Events (51-69)
        /// <summary>SV_SKIN_EVENT_51 - Reset OK</summary>
        ResetOk = 51,
        /// <summary>SV_SKIN_EVENT_52 - Feed Override Minus</summary>
        FeedOverMinus = 52,
        /// <summary>SV_SKIN_EVENT_53 - Feed Override 100%</summary>
        FeedOver100 = 53,
        /// <summary>SV_SKIN_EVENT_54 - Feed Override Plus</summary>
        FeedOverPlus = 54,
        /// <summary>SV_SKIN_EVENT_55 - Keep Alive Signal</summary>
        KeepAlive = 55,
        /// <summary>SV_SKIN_EVENT_56 - Reset Button Pressed</summary>
        ResetButtonPressed = 56,
        /// <summary>SV_SKIN_EVENT_57 - Jog Axis 5 Plus</summary>
        JogAx5Plus = 57,
        /// <summary>SV_SKIN_EVENT_58 - Jog Axis 5 Minus</summary>
        JogAx5Minus = 58,
        /// <summary>SV_SKIN_EVENT_59 - Jog Axis 6 Plus</summary>
        JogAx6Plus = 59,
        /// <summary>SV_SKIN_EVENT_60 - Jog Axis 6 Minus</summary>
        JogAx6Minus = 60,
        /// <summary>SV_SKIN_EVENT_61 - Jog Axis 7 Plus</summary>
        JogAx7Plus = 61,
        /// <summary>SV_SKIN_EVENT_62 - Jog Axis 7 Minus</summary>
        JogAx7Minus = 62,
        /// <summary>SV_SKIN_EVENT_63 - Jog Axis 8 Plus</summary>
        JogAx8Plus = 63,
        /// <summary>SV_SKIN_EVENT_64 - Jog Axis 8 Minus</summary>
        JogAx8Minus = 64,
        // 65 is unused
        /// <summary>SV_SKIN_EVENT_66 - Vacuum On</summary>
        VacOn = 66,
        /// <summary>SV_SKIN_EVENT_67 - Limit Defeat</summary>
        LimitDefeat = 67,
        /// <summary>SV_SKIN_EVENT_68 - Auxiliary Button 15</summary>
        Aux15 = 68,
        /// <summary>SV_SKIN_EVENT_69 - Auxiliary Button 16</summary>
        Aux16 = 69,

        // Machine Controls (70-79)
        /// <summary>SV_SKIN_EVENT_70 - Tail Stock In/Out</summary>
        TailStock = 70,
        /// <summary>SV_SKIN_EVENT_71 - Open Chuck</summary>
        OpenChuck = 71,
        /// <summary>SV_SKIN_EVENT_72 - Close Chuck</summary>
        CloseChuck = 72,
        /// <summary>SV_SKIN_EVENT_73 - Work Light Toggle</summary>
        Worklight = 73,
        /// <summary>SV_SKIN_EVENT_74 - Collet Open/Close</summary>
        ColletOpenClose = 74,
        /// <summary>SV_SKIN_EVENT_75 - Air Blow Nozzle</summary>
        AirBlowNozzle = 75,
        /// <summary>SV_SKIN_EVENT_76 - Spindle High Range</summary>
        SpindleHigh = 76,
        /// <summary>SV_SKIN_EVENT_77 - Spindle Medium Range</summary>
        SpindleMed = 77,
        /// <summary>SV_SKIN_EVENT_78 - Spindle Low Range</summary>
        SpindleLow = 78,
        /// <summary>SV_SKIN_EVENT_79 - Spindle Brake</summary>
        SpindleBrake = 79,

        // System Status and Controls (80-120)
        /// <summary>SV_SKIN_EVENT_80 - CNC and PLC Active</summary>
        CncAndPlcActive = 80,
        /// <summary>SV_SKIN_EVENT_81 - Dry Run Mode</summary>
        DryRun = 81,
        /// <summary>SV_SKIN_EVENT_82 - Rapid Override</summary>
        RapidOver = 82,
        /// <summary>SV_SKIN_EVENT_83 - ATC Index Plus</summary>
        ATCIndexPlus = 83,
        /// <summary>SV_SKIN_EVENT_84 - ATC Index Minus</summary>
        ATCIndexMinus = 84,
        /// <summary>SV_SKIN_EVENT_85 - Turret Index</summary>
        TurretIndex = 85,
        /// <summary>SV_SKIN_EVENT_86 - Torch On</summary>
        TorchOn = 86,
        /// <summary>SV_SKIN_EVENT_87 - Torch Off</summary>
        TorchOff = 87,
        /// <summary>SV_SKIN_EVENT_88 - THC Auto</summary>
        THCAuto = 88,
        /// <summary>SV_SKIN_EVENT_89 - Torch Test</summary>
        TorchTest = 89,
        /// <summary>SV_SKIN_EVENT_90 - Torch Restart Mode</summary>
        TorchRestartMode = 90,
        /// <summary>SV_SKIN_EVENT_91 - Torch Restart Forward</summary>
        TorchRestartForward = 91,
        /// <summary>SV_SKIN_EVENT_92 - Torch Restart Backward</summary>
        TorchRestartBackward = 92,
        /// <summary>SV_SKIN_EVENT_93 - E-Stop Pressed</summary>
        EStopPressed = 93,
        /// <summary>SV_SKIN_EVENT_94 - Using Keyboard Override</summary>
        UsingKbOverride = 94,
        /// <summary>SV_SKIN_EVENT_99 - Spindle Brake Mode</summary>
        SpinBrakeMode = 99,

        // Debug and Restart Features (100-120)
        /// <summary>SV_SKIN_EVENT_100 - Debug Test</summary>
        DebugTest = 100,
        /// <summary>SV_SKIN_EVENT_101 - Restart Cycle Start</summary>
        RestartCycleStart = 101,
        /// <summary>SV_SKIN_EVENT_102 - Restart Cycle Cancel</summary>
        RestartCycleCancel = 102,
        /// <summary>SV_SKIN_EVENT_103 - Torch Check</summary>
        TorchCheck = 103,
        /// <summary>SV_SKIN_EVENT_104 - Restart Mode Cancel</summary>
        RestartModeCancel = 104,
        /// <summary>SV_SKIN_EVENT_105 - Reset Torch Check</summary>
        ResetTorchCheck = 105,
        /// <summary>SV_SKIN_EVENT_106 - Breakaway Disable</summary>
        BreakawayDisable = 106,
        /// <summary>SV_SKIN_EVENT_107 - Jog Axis1 Minus + Axis2 Plus</summary>
        JogAx1MinusAx2Plus = 107,
        /// <summary>SV_SKIN_EVENT_108 - Jog Axis1 + Axis2 Plus</summary>
        JogAx1Ax2Plus = 108,
        /// <summary>SV_SKIN_EVENT_109 - Jog Axis1 + Axis2 Minus</summary>
        JogAx1Ax2Minus = 109,
        /// <summary>SV_SKIN_EVENT_110 - Jog Axis1 Plus + Axis2 Minus</summary>
        JogAx1PlusAx2Minus = 110,
        /// <summary>SV_SKIN_EVENT_111 - Feed Override 75%</summary>
        FeedOver75 = 111,
        /// <summary>SV_SKIN_EVENT_112 - Feed Override 50%</summary>
        FeedOver50 = 112,
        /// <summary>SV_SKIN_EVENT_113 - Feed Override 25%</summary>
        FeedOver25 = 113,
        /// <summary>SV_SKIN_EVENT_114 - Rapid Override 100%</summary>
        RapidOver100 = 114,
        /// <summary>SV_SKIN_EVENT_115 - Rapid Override 75%</summary>
        RapidOver75 = 115,
        /// <summary>SV_SKIN_EVENT_116 - Rapid Override 50%</summary>
        RapidOver50 = 116,
        /// <summary>SV_SKIN_EVENT_117 - Rapid Override 25%</summary>
        RapidOver25 = 117,
        /// <summary>SV_SKIN_EVENT_118 - Feed/Rapid Link</summary>
        FeedRapidLink = 118,
        /// <summary>SV_SKIN_EVENT_119 - Rapid Override Minus</summary>
        RapidOverMinus = 119,
        /// <summary>SV_SKIN_EVENT_120 - Rapid Override Plus</summary>
        RapidOverPlus = 120,

        // Custom Machine Functions (200-211)
        /// <summary>SV_SKIN_EVENT_200 - Manual Lube</summary>
        ManualLube = 200,
        /// <summary>SV_SKIN_EVENT_201 - Clamp On</summary>
        ClampOn = 201,
        /// <summary>SV_SKIN_EVENT_202 - Dust Collection</summary>
        DustCollection = 202,
        /// <summary>SV_SKIN_EVENT_203 - Cutoff</summary>
        Cutoff = 203,
        /// <summary>SV_SKIN_EVENT_204 - Part Chute</summary>
        PartChute = 204,
        /// <summary>SV_SKIN_EVENT_205 - Tool Unclamp</summary>
        ToolUnclamp = 205,
        /// <summary>SV_SKIN_EVENT_206 - Dust Foot</summary>
        DustFoot = 206,
        /// <summary>SV_SKIN_EVENT_207 - Laser Align</summary>
        LaserAlign = 207,
        /// <summary>SV_SKIN_EVENT_208 - Pop Up Pins</summary>
        PopUpPins = 208,
        /// <summary>SV_SKIN_EVENT_209 - Spindle Cooling</summary>
        SpindleCooling = 209,
        /// <summary>SV_SKIN_EVENT_210 - Tool Release</summary>
        ToolRelease = 210,
        /// <summary>SV_SKIN_EVENT_211 - Laser Deploy</summary>
        LaserDeploy = 211,

        // Generic Output Toggles (225-232)
        /// <summary>SV_SKIN_EVENT_225 - Output 1 Toggle</summary>
        Output1Toggle = 225,
        /// <summary>SV_SKIN_EVENT_226 - Output 2 Toggle</summary>
        Output2Toggle = 226,
        /// <summary>SV_SKIN_EVENT_227 - Output 3 Toggle</summary>
        Output3Toggle = 227,
        /// <summary>SV_SKIN_EVENT_228 - Output 4 Toggle</summary>
        Output4Toggle = 228,
        /// <summary>SV_SKIN_EVENT_229 - Output 5 Toggle</summary>
        Output5Toggle = 229,
        /// <summary>SV_SKIN_EVENT_230 - Output 6 Toggle</summary>
        Output6Toggle = 230,
        /// <summary>SV_SKIN_EVENT_231 - Output 7 Toggle</summary>
        Output7Toggle = 231,
        /// <summary>SV_SKIN_EVENT_232 - Output 8 Toggle</summary>
        Output8Toggle = 232
        // Events 233-256 are reserved for future expansion
    }
}
