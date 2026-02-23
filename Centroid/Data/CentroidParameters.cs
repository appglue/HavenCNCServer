namespace HavenCNCServer.Centroid.Data
{
    /// <summary>
    /// Comprehensive Centroid CNC parameter enumeration
    /// Contains all parameters used across the system for type safety and clarity
    /// </summary>
    public enum CentroidParameters
    {
        // Basic System Parameters
        /// <summary>Emergency stop input parameter</summary>
        ESTOP_INPUT_PARM = 0,
        /// <summary>X axis orientation parameter</summary>
        X_ORIENTATION_PARM = 1,
        /// <summary>CNC compatibility parameter - bit field for various compatibility flags</summary>
        CNC_COMPATIBILITY_PARM = 2,
        /// <summary>Tool changer installed parameter</summary>
        TOOL_CHANGER_INSTALLED = 6,

        // Jog Parameters
        /// <summary>Basic linear jog increment parameter (inches)</summary>
        BASIC_JOG_INCREMENT_PARM = 40,
        /// <summary>Rotary axis jog increment parameter (degrees)</summary>
        ROTARY_JOG_INCREMENT_PARM = 41,

        // Spindle Parameters
        /// <summary>Spindle encoder counts per revolution parameter</summary>
        SPINDLE_COUNTS_REV_PARM = 34,
        /// <summary>Spindle axis assignment parameter</summary>
        SPINDLE_AXIS_PARM = 35,
        /// <summary>Rigid tapping enable parameter</summary>
        RIGID_TAPPING_PARM = 36,
        /// <summary>Spindle deceleration time parameter</summary>
        SPINDLE_DECEL_TIME_PARM = 37,

        // Gear Ratio Parameters
        /// <summary>Low gear ratio parameter</summary>
        LOW_GEAR_RATIO_PARM = 65,
        /// <summary>Medium-low gear ratio parameter</summary>
        MED_LOW_GEAR_RATIO_PARM = 66,
        /// <summary>High gear ratio parameter</summary>
        HIGH_GEAR_RATIO_PARM = 67,

        // Rigid Tapping Parameters
        /// <summary>Rigid tapping slow spindle speed parameter</summary>
        RT_SLOW_SPINDLE_SPEED_PARM = 68,
        /// <summary>Rigid tapping slow spindle time parameter</summary>
        RT_SLOW_SPINDLE_TIME_PARM = 69,
        /// <summary>Spindle control parameter</summary>
        SPINDLE_PARM = 78,
        /// <summary>Rigid tapping spindle cutoff drift parameter</summary>
        RT_SPINDLE_CUTOFF_DRIFT_PARM = 82,

        // Axis Property Parameters (Bit Fields)
        /// <summary>Axis 1 properties parameter</summary>
        AXIS_1_PROPERTIES = 91,
        /// <summary>Axis 2 properties parameter</summary>
        AXIS_2_PROPERTIES = 92,
        /// <summary>Axis 3 properties parameter</summary>
        AXIS_3_PROPERTIES = 93,
        /// <summary>Axis 4 properties parameter</summary>
        AXIS_4_PROPERTIES = 94,

        // ATC Parameters
        /// <summary>ATC maximum bins parameter</summary>
        ATC_MAX_BINS = 161,

        // Additional Axis Properties
        /// <summary>Axis 5 properties parameter</summary>
        AXIS_5_PROPERTIES = 166,
        /// <summary>Axis 6 properties parameter</summary>
        AXIS_6_PROPERTIES = 167,
        /// <summary>Axis 7 properties parameter</summary>
        AXIS_7_PROPERTIES = 168,
        /// <summary>Axis 8 properties parameter</summary>
        AXIS_8_PROPERTIES = 169,

        // Threading and Tapping
        /// <summary>Threading and tapping acceleration/deceleration distance parameter</summary>
        THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM = 240,
        /// <summary>Threading and tapping acceleration/deceleration rotation degree step amount parameter</summary>
        THREADING_AND_TAPPING_ACCEL_DECEL_ROT_DEG_STEP_AMT_PARM = 241,

        // Encoder Parameter
        /// <summary>Encoder port assignment parameter</summary>
        ENCODER_PORT_ASSIGNMENT = 315,

        // Probe Parameters
        /// <summary>Probe tool number parameter</summary>
        PROBE_TOOL_NUMBER_PARM = 12,
        /// <summary>Probe recovery distance parameter</summary>
        PROBING_RECOVERY_DISTANCE_PARM = 13,
        /// <summary>Fast probing rate parameter</summary>
        FAST_PROBING_RATE_PARM = 14,
        /// <summary>Slow probing rate parameter</summary>
        SLOW_PROBING_RATE_PARM = 15,
        /// <summary>Probe protection parameter</summary>
        PROBE_PROTECTION_PARM = 153,
        /// <summary>Probe input parameter</summary>
        PROBE_INPUT_PARM = 405,
        /// <summary>Probe input type parameter</summary>
        PROBE_INPUT_TYPE = 406,
        /// <summary>Touch plate input number parameter</summary>
        TOUCH_PLATE_INPUT_NUMBER = 407,
        /// <summary>Touch plate input type parameter</summary>
        TOUCH_PLATE_INPUT_TYPE = 408,
        /// <summary>Probe type configuration parameter</summary>
        PROBE_TYPE = 409,
        /// <summary>Display probe warning parameter</summary>
        DISPLAY_PROBE_WARNING_PARAM = 410,
        /// <summary>Probe protection/inhibit settings parameter</summary>
        PROBE_INHIBIT_PARM = 416,

        // Tool Touch Off Parameters
        /// <summary>Modal tool parameter (used for height calculation method bit 1)</summary>
        MODAL_TOOL_PARM = 3,
        /// <summary>Fixed location mode parameter</summary>
        FIXED_LOCATION_MODE_PARM = 17,
        /// <summary>Tool measure properties bit field parameter (Mill only)</summary>
        TOOL_MEASURE_PROPERTIES_PARM = 43,
        /// <summary>Touch off tool PLC input parameter (Mill)</summary>
        TOUCH_OFF_TOOL_PLC_INPUT_MILL = 44,
        /// <summary>Tool touch off height parameter</summary>
        TOOL_TOUCH_OFF_HEIGHT_PARM = 71,
        /// <summary>Touch off tool PLC input parameter (Lathe)</summary>
        TOUCH_OFF_TOOL_PLC_INPUT_LATHE = 244,
        /// <summary>Tool touch off detect input parameter</summary>
        TOOL_TOUCH_OFF_DETECT_INPUT_PARM = 257,
        /// <summary>Tool touch off type parameter</summary>
        TOOL_TOUCH_OFF_TYPE_PARM = 405,
        /// <summary>Tool touch off input type parameter</summary>
        TOOL_TOUCH_OFF_INPUT_TYPE_PARM = 407,

        // PLC Parameters
        /// <summary>PLC analog parameter</summary>
        PLC_ANALOG_PARM = 420,
        /// <summary>RTG display parameter</summary>
        RTG_DISPLAY_PARM = 430,
        /// <summary>ATC holding configuration parameter</summary>
        ATC_HOLDING_CONFIGURATION = 431,
        /// <summary>ATC tool length method parameter</summary>
        ATC_TOOL_LENGTH_METHOD = 432,

        // Second Spindle Parameters
        /// <summary>Second spindle enable parameter</summary>
        SECOND_SPINDLE_ENABLE = 459,
        /// <summary>Second spindle maximum speed parameter</summary>
        SECOND_SPINDLE_MAX_SPEED = 460,
        /// <summary>Second spindle minimum speed parameter</summary>
        SECOND_SPINDLE_MIN_SPEED = 461,
        /// <summary>Second spindle encoder counts parameter</summary>
        SECOND_SPINDLE_ENCODER_COUNTS = 462,

        // Touch Plate Configuration Parameters
        /// <summary>Touch plate input assignment parameter</summary>
        TOUCH_PLATE_INPUT = 540,
        /// <summary>Touch plate detection input parameter</summary>
        TOUCH_PLATE_DETECT = 541,
        /// <summary>Touch plate input type parameter</summary>
        TOUCH_PLATE_INPUT_TYPE_PARM = 542,
        /// <summary>Touch plate wall height parameter</summary>
        TOUCH_PLATE_WALL_HEIGHT_PARM = 543,
        /// <summary>Touch plate wall thickness parameter</summary>
        TOUCH_PLATE_WALL_THICKNESS_PARM = 544,
        /// <summary>Touch plate internal diameter parameter</summary>
        TOUCH_PLATE_INTERNAL_DIAMETER_PARM = 545,
        /// <summary>Touch plate maximum distance parameter</summary>
        TOUCH_PLATE_MAX_DISTANCE_PARM = 546,
        /// <summary>Touch plate retract distance parameter</summary>
        TOUCH_PLATE_RETRACT_DISTANCE_PARM = 547,
        /// <summary>Touch plate fast rate parameter</summary>
        TOUCH_PLATE_FAST_RATE_PARM = 548,
        /// <summary>Touch plate slow rate parameter</summary>
        TOUCH_PLATE_SLOW_RATE_PARM = 549,
        /// <summary>Touch plate attributes parameter</summary>
        TOUCH_PLATE_ATTRIBUTES_PARM = 550,

        // Axis Pairing Parameters
        /// <summary>Axis 4 pairing parameter</summary>
        AXIS_4_PAIRING = 554,
        /// <summary>Axis 5 pairing parameter</summary>
        AXIS_5_PAIRING = 555,

        // Global System Parameters
        /// <summary>Charge pump frequency divider parameter</summary>
        CHARGE_PUMP_PARM = 960,
        /// <summary>Global axis signal inversion parameter</summary>
        ACORN_OUTPUT_INVERSION_PARM = 961,
        /// <summary>Global drive fault delay parameter</summary>
        PLC_CLEARPATH_OR_G540 = 991,

        // Low Resolution Mode Parameter
        /// <summary>Plasma low resolution mode parameter</summary>
        AD2_LOW_RESOLUTION_PARM = 225,

        // PWM Parameters (Output 1)
        /// <summary>Acorn PWM frequency parameter</summary>
        ACORN_PWM_FREQUENCY_PARM = 814,
        /// <summary>Acorn PWM options parameter</summary>
        ACORN_PWM_OPTIONS_PARM = 815,
        /// <summary>Acorn PWM velocity parameter</summary>
        ACORN_PWM_VELOCITY_PARM = 816,
        /// <summary>Acorn PWM floor parameter</summary>
        ACORN_PWM_FLOOR_PARM = 817,

        // PWM Parameters (Output 2)
        /// <summary>Acorn PWM frequency parameter (output 2)</summary>
        ACORN_PWM_FREQUENCY_PARM_2 = 824,
        /// <summary>Acorn PWM options parameter (output 2)</summary>
        ACORN_PWM_OPTIONS_PARM_2 = 825,
        /// <summary>Acorn PWM velocity parameter (output 2)</summary>
        ACORN_PWM_VELOCITY_PARM_2 = 826,
        /// <summary>Acorn PWM floor parameter (output 2)</summary>
        ACORN_PWM_FLOOR_PARM_2 = 827,

        // PWM Parameters (Output 3)
        /// <summary>Acorn PWM frequency parameter (output 3)</summary>
        ACORN_PWM_FREQUENCY_PARM_3 = 834,
        /// <summary>Acorn PWM options parameter (output 3)</summary>
        ACORN_PWM_OPTIONS_PARM_3 = 835,
        /// <summary>Acorn PWM velocity parameter (output 3)</summary>
        ACORN_PWM_VELOCITY_PARM_3 = 836,
        /// <summary>Acorn PWM floor parameter (output 3)</summary>
        ACORN_PWM_FLOOR_PARM_3 = 837,

        // ATC Type and Configuration
        /// <summary>ATC type parameter</summary>
        ATC_TYPE = 830,
        /// <summary>ATC time to reverse parameter</summary>
        ATC_TIME_TO_REVERSE = 848,
        /// <summary>ATC time to fault parameter</summary>
        ATC_TIME_TO_FAULT = 849,
        /// <summary>ATC time delay to start parameter</summary>
        ATC_TIME_DELAY_TO_START = 850,
        /// <summary>ATC time delay to start alternate parameter</summary>
        ATC_TIME_DELAY_TO_START_ALT = 851,
        /// <summary>ATC skip first count on reversal parameter</summary>
        ATC_SKIP_FIRST_COUNT_ON_REVERSAL = 852,
        /// <summary>ATC travel past distance parameter</summary>
        ATC_TRAVEL_PAST_DISTANCE = 853,
        /// <summary>ATC travel behind distance parameter</summary>
        ATC_TRAVEL_BEHIND_DISTANCE = 854,

        // Input Inversion Parameters
        /// <summary>Input inversion parameter for inputs 1-16</summary>
        INPUT_INVERSION_1_16 = 911,
        /// <summary>Input inversion parameter for inputs 17-32</summary>
        INPUT_INVERSION_17_32 = 912,
        /// <summary>Input inversion parameter for inputs 33-48</summary>
        INPUT_INVERSION_33_48 = 913,
        /// <summary>Input inversion parameter for inputs 49-64</summary>
        INPUT_INVERSION_49_64 = 914,
        /// <summary>Input inversion parameter for inputs 65-80</summary>
        INPUT_INVERSION_65_80 = 915,

        // Turn Ratio Parameter (renamed to Step Frequency Parameter)
        /// <summary>Global step frequency parameter (was turn ratio)</summary>
        ACORN_STEPPER_PULSE_RATE_PARM = 968,

        // ATC Time Parameters
        /// <summary>ATC time per tool position parameter</summary>
        ATC_TIME_PER_TOOL_POSITION = 975,

        // SSV/FRV Parameters
        /// <summary>SSV cycle time parameter</summary>
        SSV_CYCLE_TIME = 982,
        /// <summary>SSV amount parameter</summary>
        SSV_AMOUNT = 983,
        /// <summary>FRV cycle time parameter</summary>
        FRV_CYCLE_TIME = 984,

        // Delay Timers
        /// <summary>Spindle OK delay parameter</summary>
        SPINDLE_OK_DELAY_PARM = 996,
        /// <summary>Spindle cooling fan delay timer parameter</summary>
        SPINDLE_COOLING_FAN_DELAY_TIMER = 997,
        /// <summary>Laser cooling fan delay timer parameter</summary>
        LASER_COOLING_FAN_DELAY_TIMER = 998,

        // Enhanced ATC and Additional Parameters
        /// <summary>Enhanced ATC parameter</summary>
        ENHANCED_ATC_PARM = 163,
        /// <summary>Gang tool enable parameter (bit 0)</summary>
        GANG_TOOL_ENABLE = 163,
        /// <summary>Rack mount holding configuration parameter</summary>
        RTC_RACK_MOUNT_HOLDING_CONFIG = 431,
        /// <summary>Rack mount tool length method parameter</summary>
        RTC_RACK_MOUNT_TOOL_LENGTH_METHOD = 432,
        /// <summary>Axis driven turret travel past distance parameter</summary>
        AXIS_DRIVEN_TURRET_TRAVEL_PAST_DISTANCE = 853,
        /// <summary>Axis driven turret travel behind distance parameter</summary>
        AXIS_DRIVEN_TURRET_TRAVEL_BEHIND_DISTANCE = 854,
        /// <summary>Turret settle time parameter</summary>
        TURRET_SETTLE_TIME = 847,
        /// <summary>Time to reverse parameter</summary>
        TIME_TO_REVERSE = 848,
        /// <summary>Time to fault parameter</summary>
        TIME_TO_FAULT = 849,
        /// <summary>Time delay to start parameter</summary>
        TIME_DELAY_TO_START = 850,
        /// <summary>Time delay before reverse parameter</summary>
        TIME_DELAY_BEFORE_REVERSE = 851,

        // Wireless MPG Parameters
        /// <summary>Wireless MPG device type (0=CWP-4, 1=WMPG-4, 2=WMPG-6, 3=WMPG-4 Plasma)</summary>
        MPG_TYPE_PARM = 411,
        /// <summary>USB MPG active axes bitmask (Axis1=1, Axis2=2, Axis3=4, Axis4=8, Axis5=16, Axis6=32)</summary>
        USB_MPG_OPTIONS_PARM = 218,
        /// <summary>MPG jog performance mode (0=Smooth, 1=Balanced, 2=Quick)</summary>
        MPG_PERFORMANCE_MODE_PARAM = 855
    }
}
