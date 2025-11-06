;------------------------------------------------------------------------------
; Filename: setup_defines.cnc
; Description: Setup Variable Defines for Macros
; Notes:
; Requires: CNC12 V5.39.00+
; Revision Date: 5 Sep 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\setup_defines.cnc"
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;PREFIX/SUFFIX Types
;PARAM_ : Parameter Value, Read Only.
;SV_    : CNC12 System Variable

;------------------------------------------------------------------------------
;                                 Parameters
;------------------------------------------------------------------------------
DEFINE <PARAM_TOOL_HEIGHT_CONTROL>                    #9003
  DEFINE <TOOL_HEIGHT_CONTROL_RESET>                  1
  DEFINE <TOOL_HEIGHT_CONTROL_ZREF_HOME>              2
  DEFINE <TOOL_HEIGHT_CONTROL_RETENTION>              4
  DEFINE <TOOL_HEIGHT_CONTROL_PROBE_TOOL_DETECTION>   16
DEFINE <PARAM_ATC_INSTALLED>                          #9006
DEFINE <PARAM_PROBE_INPUT>                            #9011
DEFINE <PARAM_PROBE_TOOL_NUMBER>                      #9012
DEFINE <PARAM_PROBE_FAST_RATE>                        #9014
DEFINE <PARAM_PROBE_SLOW_RATE>                        #9015
DEFINE <PARAM_PROBE_DETECT_INPUT>                     #9018
DEFINE <PARAM_TOOLTOUCH_INPUT>                        #9044
DEFINE <PARAM_TOOLTOUCH_HEIGHT>                       #9071
DEFINE <PARAM_PROBE_TYPE>                             #9155
  DEFINE <PROBE_TYPE_IS_STANDARD>                     0
  DEFINE <PROBE_TYPE_IS_DSP>                          1
  DEFINE <PROBE_TYPE_IS_DP7>                          2
DEFINE <PARAM_ENHANCED_ATC>                           #9160
  DEFINE <ENHANCED_ATC_IS_NONRANDOM>                  1
  DEFINE <ENHANCED_ATC_IS_RANDOM>                     2
DEFINE <PARAM_MAX_ATC_BINS>                           #9161
DEFINE <PARAM_TOOLTOUCH_DETECT_INPUT>                 #9257
DEFINE <PARAM_PROBE_TRAVERSE_RATE>                    #9395
DEFINE <PARAM_ATC_TYPE>                               #9830
  DEFINE <ATC_TYPE_IS_CAROUSEL>                       1
  DEFINE <ATC_TYPE_IS_RACKMOUNT>                      7

;Axis Properties
DEFINE <PARAM_AXIS_1_PROPERTIES>                      #9091
DEFINE <PARAM_AXIS_2_PROPERTIES>                      #9092
DEFINE <PARAM_AXIS_3_PROPERTIES>                      #9093
DEFINE <PARAM_AXIS_4_PROPERTIES>                      #9094
DEFINE <PARAM_AXIS_5_PROPERTIES>                      #9166
DEFINE <PARAM_AXIS_6_PROPERTIES>                      #9167
DEFINE <PARAM_AXIS_7_PROPERTIES>                      #9168
DEFINE <PARAM_AXIS_8_PROPERTIES>                      #9169
  DEFINE <AXIS_IS_ROTARY>                             1
  DEFINE <AXIS_IS_PARALLEL_TO_X>                      2048
  DEFINE <AXIS_IS_PARALLEL_TO_Y>                      4096
  
;Rotary Parallel X or Y Coordinates
DEFINE <PARAM_ROTARY_X_PARALLEL_Y_POSITION>           #9116
DEFINE <PARAM_ROTARY_X_PARALLEL_Z_POSITION>           #9117
DEFINE <PARAM_ROTARY_Y_PARALLEL_X_POSITION>           #9118
DEFINE <PARAM_ROTARY_Y_PARALLEL_Z_POSITION>           #9119

DEFINE <PARAM_PROBE_INPUT_TYPE>                       #9406
  DEFINE <PROBE_INPUT_IS_NO_TYPE>                     0
  DEFINE <PROBE_INPUT_IS_NC_TYPE>                     1
DEFINE <PARAM_TOOLTOUCH_INPUT_TYPE>                   #9407
  DEFINE <TOOLTOUCH_INPUT_IS_NO_TYPE>                 0
  DEFINE <TOOLTOUCH_INPUT_IS_NC_TYPE>                 1
DEFINE <PARAM_PROBE_WARNING_MSG>                      #9410

;Wizard Paired Axis Parameters 502-508
DEFINE <PARAM_AUTO_SQUARE_DISTANCE>                   #9502
DEFINE <PARAM_AUTO_SQUARE_SLAVE_HOME_INPUT>           #9505
DEFINE <PARAM_AUTO_SQUARE_MASTER_HOME_INPUT>          #9506
DEFINE <PARAM_AUTO_SQUARE_MASTER_AXIS>                #9507
DEFINE <PARAM_AUTO_SQUARE_SLAVE_AXIS>                 #9508

;Plasma/Height Control Parameters 509-539
DEFINE <PARAM_PLASMA_OPTIONS>                         #9523
  DEFINE <PLASMA_OPTIONS_IS_DISABLE_HC>               32

;Axis Pairing 551-559
DEFINE <PARAM_AXIS_1_MASTER>                          #9551
DEFINE <PARAM_AXIS_2_MASTER>                          #9552
DEFINE <PARAM_AXIS_3_MASTER>                          #9553
DEFINE <PARAM_AXIS_4_MASTER>                          #9554
DEFINE <PARAM_AXIS_5_MASTER>                          #9555
DEFINE <PARAM_AXIS_6_MASTER>                          #9556
DEFINE <PARAM_AXIS_7_MASTER>                          #9557
DEFINE <PARAM_AXIS_8_MASTER>                          #9558

;Touch Plate Parameters 540-550
DEFINE <PARAM_TOUCH_PLATE_INPUT>                      #9540
DEFINE <PARAM_TOUCH_PLATE_DETECT_INPUT>               #9541
DEFINE <PARAM_TOUCH_PLATE_INPUT_TYPE>                 #9542
  DEFINE <TOUCH_PLATE_INPUT_IS_NO_TYPE>               0
  DEFINE <TOUCH_PLATE_INPUT_IS_NC_TYPE>               1
DEFINE <PARAM_TOUCH_PLATE_HEIGHT>                     #9543
DEFINE <PARAM_TOUCH_PLATE_WALL_THICKNESS>             #9544
DEFINE <PARAM_TOUCH_PLATE_DIAMETER>                   #9545
DEFINE <PARAM_TOUCH_PLATE_MAX_PROBE_DISTANCE>         #9546
DEFINE <PARAM_TOUCH_PLATE_RETRACT_DISTANCE>           #9547
DEFINE <PARAM_TOUCH_PLATE_FAST_PROBE_RATE>            #9548
DEFINE <PARAM_TOUCH_PLATE_SLOW_PROBE_RATE>            #9549
DEFINE <PARAM_TOUCH_PLATE_ATTRIBUTES>                 #9550
  DEFINE <TOUCH_PLATE_IS_INTERNAL>                    1
  DEFINE <TOUCH_PLATE_HAS_BORE>                       2
  DEFINE <TOUCH_PLATE_IS_SURFACE_ONLY>                4

;------------------------------------------------------------------------------
;                              System Variables
;------------------------------------------------------------------------------
DEFINE <SV_INSTALL_DIRECTORY>               #400    ;String for install directory of CNC12

DEFINE <SV_CSR_ACTIVE_WCS_VALUE>            #2400
DEFINE <SV_AXIS_1_ACTIVE_WCS_VALUE>         #2500
DEFINE <SV_AXIS_2_ACTIVE_WCS_VALUE>         #2600
DEFINE <SV_AXIS_3_ACTIVE_WCS_VALUE>         #2700
DEFINE <SV_POSITIONING_MODE>                #4003
  DEFINE <POSITIONING_IS_ABSOLUTE>          90
  DEFINE <POSITIONING_IS_INCREMENTAL>       91
DEFINE <SV_UNITS_OF_MEASURE>                #4006
  DEFINE <UNITS_IS_INCHES>                  20
  DEFINE <UNITS_IS_METRIC>                  21
DEFINE <SV_TOOL_NUMBER>                     #4120

;Position
DEFINE <SV_AXIS_1_MACHINE_POSITION>         #5021
DEFINE <SV_AXIS_2_MACHINE_POSITION>         #5022
DEFINE <SV_AXIS_3_MACHINE_POSITION>         #5023
DEFINE <SV_AXIS_4_MACHINE_POSITION>         #5024
DEFINE <SV_AXIS_5_MACHINE_POSITION>         #5025
DEFINE <SV_AXIS_6_MACHINE_POSITION>         #5026
DEFINE <SV_AXIS_7_MACHINE_POSITION>         #5027
DEFINE <SV_AXIS_8_MACHINE_POSITION>         #5028
DEFINE <SV_AXIS_1_WCS_POSITION>             #5041
DEFINE <SV_AXIS_2_WCS_POSITION>             #5042
DEFINE <SV_AXIS_3_WCS_POSITION>             #5043
DEFINE <SV_AXIS_4_WCS_POSITION>             #5044
DEFINE <SV_AXIS_5_WCS_POSITION>             #5045
DEFINE <SV_AXIS_6_WCS_POSITION>             #5046
DEFINE <SV_AXIS_7_WCS_POSITION>             #5047
DEFINE <SV_AXIS_8_WCS_POSITION>             #5048

;Tool Information
DEFINE <SV_ACTIVE_HEIGHT_OFFSET>            #10000 ;Currently Active H Value

DEFINE <SV_ACTIVE_DIAMETER_OFFSET>          #11000 ;Currently Active D Value

;Axis Labels
DEFINE <SV_AXIS_1_LABEL>                    #20101
DEFINE <SV_AXIS_2_LABEL>                    #20102
DEFINE <SV_AXIS_3_LABEL>                    #20103
DEFINE <SV_AXIS_4_LABEL>                    #20104
DEFINE <SV_AXIS_5_LABEL>                    #20105
DEFINE <SV_AXIS_6_LABEL>                    #20106
DEFINE <SV_AXIS_7_LABEL>                    #20107
DEFINE <SV_AXIS_8_LABEL>                    #20108
  DEFINE <AXIS_LABEL_IS_@>                  64
  DEFINE <AXIS_LABEL_IS_A>                  65
  DEFINE <AXIS_LABEL_IS_B>                  66
  DEFINE <AXIS_LABEL_IS_C>                  67
  DEFINE <AXIS_LABEL_IS_M>                  77
  DEFINE <AXIS_LABEL_IS_N>                  78
  DEFINE <AXIS_LABEL_IS_U>                  85
  DEFINE <AXIS_LABEL_IS_V>                  86
  DEFINE <AXIS_LABEL_IS_W>                  87
  DEFINE <AXIS_LABEL_IS_X>                  88
  DEFINE <AXIS_LABEL_IS_Y>                  89
  DEFINE <AXIS_LABEL_IS_Z>                  90

;Axis Home Minus Inputs
DEFINE <SV_AXIS_1_HOME_MINUS>               #21301
DEFINE <SV_AXIS_2_HOME_MINUS>               #21302
DEFINE <SV_AXIS_3_HOME_MINUS>               #21303
DEFINE <SV_AXIS_4_HOME_MINUS>               #21304
DEFINE <SV_AXIS_5_HOME_MINUS>               #21305
DEFINE <SV_AXIS_6_HOME_MINUS>               #21306
DEFINE <SV_AXIS_7_HOME_MINUS>               #21307
DEFINE <SV_AXIS_8_HOME_MINUS>               #21308

;Axis Home Plus Inputs
DEFINE <SV_AXIS_1_HOME_PLUS>                #21401
DEFINE <SV_AXIS_2_HOME_PLUS>                #21402
DEFINE <SV_AXIS_3_HOME_PLUS>                #21403
DEFINE <SV_AXIS_4_HOME_PLUS>                #21404
DEFINE <SV_AXIS_5_HOME_PLUS>                #21405
DEFINE <SV_AXIS_6_HOME_PLUS>                #21406
DEFINE <SV_AXIS_7_HOME_PLUS>                #21407
DEFINE <SV_AXIS_8_HOME_PLUS>                #21408

;Control Configuration
DEFINE <SV_DRO_DISPLAY_UNITS>               #25000
DEFINE <SV_MACHINE_UNITS>                   #25001
DEFINE <SV_PLC_TYPE>                        #25002
DEFINE <SV_JOG_PANEL_TYPE>                  #25003
DEFINE <SV_JOG_PANEL_OPTIONAL>              #25004
DEFINE <SV_SPINDLE_SPEED_MIN>               #25005
DEFINE <SV_SPINDLE_SPEED_MAX>               #25006
DEFINE <SV_HOMING_TYPE_AT_STARTUP>          #25007
  DEFINE <HOMING_TYPE_IS_JOG>               0
  DEFINE <HOMING_TYPE_IS_HS>                1
  DEFINE <HOMING_TYPE_IS_REF>               2
  DEFINE <HOMING_TYPE_IS_HOMEINPLACE>       3
  DEFINE <HOMING_TYPE_IS_ABSOLUTE>          4

DEFINE <SV_SOFTWARE_TYPE>                   #25014
  DEFINE <SOFTWARE_TYPE_IS_MILL>            1
  DEFINE <SOFTWARE_TYPE_IS_LATHE>           2
  DEFINE <SOFTWARE_TYPE_IS_ROUTER>          3
  DEFINE <SOFTWARE_TYPE_IS_PLASMA>          4
  DEFINE <SOFTWARE_TYPE_IS_LASER>           5

DEFINE <SV_OS_TYPE>                         #25017
  DEFINE <OS_TYPE_IS_OTHER>                 1
  DEFINE <OS_TYPE_IS_WIN_LINUX>             2
  
;Software Version
DEFINE <SV_CNC_SERIES_NUMBER>               #25018 ;Only Values are 10 or 11 for CNC10 or CNC11+ respectively.
DEFINE <SV_SOFTWARE_VERSION_NUMBER>         #25019
DEFINE <SV_SOFTWARE_REVISION_NUMBER>        #25020

;PLC Device ID
; Shows the Device running PLC, Typically the main board but not always.
DEFINE <SV_PLC_DEVICE_ID>                   #25032
  DEFINE <PLC_DEVICE_ID_IS_ACORN>           19
  DEFINE <PLC_DEVICE_ID_IS_HICKORY>         27
  DEFINE <PLC_DEVICE_ID_IS_ACORNSIX>        29

;Touch Plate Probing Limits
; Determined by max probe distance (P546) or software travel limits
; which ever is closer.
DEFINE <SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>   #24617
DEFINE <SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>   #24618
DEFINE <SV_AXIS_3_TOUCH_PLATE_PLUS_LIMIT>   #24619
DEFINE <SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>  #24717
DEFINE <SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>  #24718
DEFINE <SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>  #24719

;Return Points 1-4 (G28, G30, etc.)
DEFINE <SV_AXIS_1_RETURN_POINT_1>           #26401
DEFINE <SV_AXIS_1_RETURN_POINT_2>           #26402
DEFINE <SV_AXIS_1_RETURN_POINT_3>           #26403
DEFINE <SV_AXIS_1_RETURN_POINT_4>           #26404
DEFINE <SV_AXIS_2_RETURN_POINT_1>           #26501
DEFINE <SV_AXIS_2_RETURN_POINT_2>           #26502
DEFINE <SV_AXIS_2_RETURN_POINT_3>           #26503
DEFINE <SV_AXIS_2_RETURN_POINT_4>           #26504
DEFINE <SV_AXIS_3_RETURN_POINT_1>           #26601
DEFINE <SV_AXIS_3_RETURN_POINT_2>           #26602
DEFINE <SV_AXIS_3_RETURN_POINT_3>           #26603
DEFINE <SV_AXIS_3_RETURN_POINT_4>           #26604
DEFINE <SV_AXIS_4_RETURN_POINT_1>           #26701
DEFINE <SV_AXIS_4_RETURN_POINT_2>           #26702
DEFINE <SV_AXIS_4_RETURN_POINT_3>           #26703
DEFINE <SV_AXIS_4_RETURN_POINT_4>           #26704
DEFINE <SV_AXIS_5_RETURN_POINT_1>           #26801
DEFINE <SV_AXIS_5_RETURN_POINT_2>           #26802
DEFINE <SV_AXIS_5_RETURN_POINT_3>           #26803
DEFINE <SV_AXIS_5_RETURN_POINT_4>           #26804
DEFINE <SV_AXIS_6_RETURN_POINT_1>           #26901
DEFINE <SV_AXIS_6_RETURN_POINT_2>           #26902
DEFINE <SV_AXIS_6_RETURN_POINT_3>           #26903
DEFINE <SV_AXIS_6_RETURN_POINT_4>           #26904
DEFINE <SV_AXIS_7_RETURN_POINT_1>           #27001
DEFINE <SV_AXIS_7_RETURN_POINT_2>           #27002
DEFINE <SV_AXIS_7_RETURN_POINT_3>           #27003
DEFINE <SV_AXIS_7_RETURN_POINT_4>           #27004
DEFINE <SV_AXIS_8_RETURN_POINT_1>           #27101
DEFINE <SV_AXIS_8_RETURN_POINT_2>           #27102
DEFINE <SV_AXIS_8_RETURN_POINT_3>           #27103
DEFINE <SV_AXIS_8_RETURN_POINT_4>           #27104

;Axis Homing Feedrates
DEFINE <SV_AXIS_1_HOME_FEEDRATE>            #27901
DEFINE <SV_AXIS_2_HOME_FEEDRATE>            #27902
DEFINE <SV_AXIS_3_HOME_FEEDRATE>            #27903
DEFINE <SV_AXIS_4_HOME_FEEDRATE>            #27904
DEFINE <SV_AXIS_5_HOME_FEEDRATE>            #27905
DEFINE <SV_AXIS_6_HOME_FEEDRATE>            #27906
DEFINE <SV_AXIS_7_HOME_FEEDRATE>            #27907
DEFINE <SV_AXIS_8_HOME_FEEDRATE>            #27908

;------------------------------------------------------------------------------
;                               User Variables
;------------------------------------------------------------------------------
;CNC12 Writes/Read User Variables
;A Define with "(W)" commented means CNC12 Writes to the Variable
;A Define with "(R)" commented means CNC12 Reads the Variable
;A Define with "(W/R)" commented means CNC12 Writes and Reads the Variable

;Probing Error Flag set by 1 by CNC12, and should be set to 0 at end of macro
;to indicate there was not an error during macro operations.
DEFINE <PROBING_ERROR_FLAG>                           #32000 ;(W/R)
DEFINE <PROBE_PROTECTION_DISABLE>                     #32001 ;(R)

;-Used in Macros
DEFINE <M225_MESSAGE_TIMER>                           #32002
DEFINE <MESSAGE_USER_INPUT>                           #32003
DEFINE <MESSAGE_TIMER>                                #32004
DEFINE <SAVED_POSITIONING_MODE>                       #32005

;Generic Variables for Various Use.
DEFINE <VAR_1>                                        #32011
DEFINE <VAR_2>                                        #32012
DEFINE <VAR_3>                                        #32013
DEFINE <VAR_4>                                        #32014
DEFINE <VAR_5>                                        #32015
DEFINE <VAR_6>                                        #32016
DEFINE <VAR_7>                                        #32017
DEFINE <VAR_8>                                        #32018
DEFINE <VAR_9>                                        #32019

;router_rotary_calibration.cnc
DEFINE <SELECTED_AXIS_PROPERTIES>                     #32900
DEFINE <SAVED_AXIS_1_POINT_1_MACHINE_POSITION>        #32901
DEFINE <SAVED_AXIS_2_POINT_1_MACHINE_POSITION>        #32902
DEFINE <SAVED_AXIS_3_POINT_1_MACHINE_POSITION>        #32903
DEFINE <SAVED_AXIS_1_POINT_2_MACHINE_POSITION>        #32904
DEFINE <SAVED_AXIS_2_POINT_2_MACHINE_POSITION>        #32905
DEFINE <SAVED_AXIS_3_POINT_2_MACHINE_POSITION>        #32906
DEFINE <CALCULATED_AXIS_1_VECTOR>                     #32907
DEFINE <CALCULATED_AXIS_2_VECTOR>                     #32908
DEFINE <CALCULATED_AXIS_3_VECTOR>                     #32909
DEFINE <XY_PLANE_ANGLE>                               #32910
DEFINE <Z_PLANE_ANGLE>                                #32911
DEFINE <AXIS_1_AVERAGE_MACHINE_POSITION>              #32912
DEFINE <AXIS_2_AVERAGE_MACHINE_POSITION>              #32913
DEFINE <AXIS_3_AVERAGE_MACHINE_POSITION>              #32914
DEFINE <ROTARY_PARALLEL_AXIS_LABEL>                   #32915

;router_rotary_unwind.cnc
DEFINE <ROTARY_AXIS_LABEL>                            #32920

;--Touch Plate Menu Macros
;#33000 - #33099 Reserved
;#33000 - #33009 Reserved for CNC12 to Write
DEFINE <TOUCH_PLATE_CYCLE_SELECT>                     #33001 ;(W)
DEFINE <TOOL_DIAMETER_VALUE>                          #33002 ;(W)
DEFINE <TOUCH_PLATE_SET_Z_HEIGHT_SETTING>             #33003 ;(W)
DEFINE <TOUCH_PLATE_FLUTE_REMINDER_SETTING>           #33004 ;(W)
DEFINE <TOUCH_PLATE_MAGNET_REMINDER_SETTING>          #33005 ;(W)
DEFINE <TOUCH_PLATE_Z_CLEARANCE_VALUE>                #33006 ;(W)
DEFINE <TOUCH_PLATE_Z_RETURN_HOME_SETTING>            #33007 ;(W)

;-Used in Macros
;#33010 - #33029 are commmon use
DEFINE <TOUCH_PLATE_INPUT>                            #33010
DEFINE <TOUCH_PLATE_PROBED_POINT_1_POSITION>          #33011
DEFINE <TOUCH_PLATE_PROBED_POINT_2_POSITION>          #33012
DEFINE <TOUCH_PLATE_PROBED_POINT_3_POSITION>          #33013
DEFINE <SAVED_INITIAL_AXIS_1_POSITION>                #33014
DEFINE <SAVED_INITIAL_AXIS_2_POSITION>                #33015
DEFINE <SAVED_INITIAL_AXIS_3_POSITION>                #33016
DEFINE <TOUCH_PLATE_BORE_AXIS_1_CENTER_CALC>          #33017
DEFINE <TOUCH_PLATE_BORE_AXIS_2_CENTER_CALC>          #33018
DEFINE <TOUCH_PLATE_BORE_AXIS_1_WIDTH_CALC>           #33019
DEFINE <TOUCH_PLATE_BORE_AXIS_2_WIDTH_CALC>           #33020
DEFINE <TOUCH_PLATE_DETECT_INPUT>                     #33021
DEFINE <TOUCH_PLATE_USER_INPUT>                       #33022
DEFINE <TOUCH_PLATE_PROBED_POINT_1_MACHINE_POSITION>  #33023
DEFINE <TOUCH_PLATE_PROBED_POINT_2_MACHINE_POSITION>  #33024
DEFINE <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION>  #33025

;#33030-33049 are for touch_plate_move.cnc
DEFINE <TOUCH_PLATE_PROBED_AXIS_1_MACHINE_POSITION>   #33031
DEFINE <TOUCH_PLATE_PROBED_AXIS_2_MACHINE_POSITION>   #33032
DEFINE <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>   #33033
DEFINE <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>       #33034
DEFINE <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>       #33035
DEFINE <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>       #33036
DEFINE <TOUCH_PLATE_MOVE_AXIS_1_INIT_POSITION>        #33037
DEFINE <TOUCH_PLATE_MOVE_AXIS_2_INIT_POSITION>        #33038
DEFINE <TOUCH_PLATE_MOVE_AXIS_3_INIT_POSITION>        #33039

DEFINE <TOUCH_PLATE_PROBED_VECTOR_DISTANCE>           #33040
DEFINE <TOUCH_PLATE_RETRACT_RATIO>                    #33041
DEFINE <TOUCH_PLATE_PROBED_AXIS_1_DISTANCE>           #33042
DEFINE <TOUCH_PLATE_PROBED_AXIS_2_DISTANCE>           #33043
DEFINE <TOUCH_PLATE_PROBED_AXIS_3_DISTANCE>           #33044
DEFINE <TOUCH_PLATE_RETRACT_AXIS_1_POSITION>          #33045
DEFINE <TOUCH_PLATE_RETRACT_AXIS_2_POSITION>          #33046
DEFINE <TOUCH_PLATE_RETRACT_AXIS_3_POSITION>          #33047

;--Touch Plate CSR Macros
;#33100-33119 Reserved
  ;touch_plate_auto_set_csr.cnc
DEFINE <TOUCH_PLATE_CYCLE_CSR_SELECT>                 #33101 ;(W)
DEFINE <TOUCH_PLATE_CSR_RETRACT_VALUE>                #33102 ;(W)
DEFINE <TOUCH_PLATE_CSR_X_PROBED_POSITION>            #33111 ;(R)
DEFINE <TOUCH_PLATE_CSR_Y_PROBED_POSITION>            #33112 ;(R)
DEFINE <TOUCH_PLATE_CSR_Z_PROBED_POSITION>            #33113 ;(R)

;--Tool Offset Menu
DEFINE <SELECTED_TOOL>                                #33200 ;(W)

;--Part Setup Menu
DEFINE <SELECTED_AXIS>                                #33201 ;(W)
DEFINE <SAVED_ROTARY_XY_ANGLE>                        #33202 ;(W/R)
DEFINE <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION>     #33203 ;(W/R)

;--Laser Profile Variables
;#33300-33399
DEFINE <CLASER_PIERCE_DELAY>                          #33303
DEFINE <CLASER_CUT_HEIGHT>                            #33304
DEFINE <CLASER_PIERCE_HEIGHT>                         #33305
DEFINE <CLASER_CUT_FOCUS_POS>                         #33306
DEFINE <CLASER_PIERCE_FOCUS_POS>                      #33307
DEFINE <CLASER_SKIP_PIERCE_HEIGHT>                    #33310
DEFINE <CLASER_PIERCE_TO_CUT_RATE>                    #33311
DEFINE <CLASER_SMOOTHING_ENABLE>                      #33312
DEFINE <CLASER_CUT_POWER>                             #33313
DEFINE <CLASER_PIERCE_POWER>                          #33314
DEFINE <CLASER_PERFORM_TOUCH_OFF>                     #33315
DEFINE <CLASER_FEEDRATE>                              #33316
DEFINE <CLASER_THC_MODE>                              #33317
DEFINE <CLASER_CUTTING_GAS_SELECT>                    #33318
DEFINE <CLASER_PIERCE_FREQUENCY>                      #33319
DEFINE <CLASER_CUT_FREQUENCY>                         #33320
DEFINE <CLASER_SAFE_HEIGHT>                           #33321

;--Plasma Profile Variables
;#33400-33499
DEFINE <THC_TARGET_VOLTAGE>                           #33401
DEFINE <AUTOSENSE>                                    #33402
DEFINE <SMOOTHING_ENABLED>                            #33403
DEFINE <PLASMA_DIVIDER_RATIO>                         #33404 ;CAN BE REMOVED?
DEFINE <THC_DIVE_PERCENT>                             #33405 ;CAN BE REMOVED?
DEFINE <THC_DIVE_HYSTERISIS>                          #33406 ;CAN BE REMOVED?
DEFINE <THC_DIVE_VELOCITY>                            #33407 ;CAN BE REMOVED?
DEFINE <THC_MAX_PLUS_TRAVEL>                          #33408 ;CAN BE REMOVED?
DEFINE <THC_MAX_MINUS_TRAVEL>                         #33409 ;CAN BE REMOVED?
DEFINE <THC_VELOCITY>                                 #33410 ;CAN BE REMOVED?
DEFINE <FEEDRATE>                                     #33411
DEFINE <END_DELAY>                                    #33412
DEFINE <SAFE_HEIGHT>                                  #33413
DEFINE <PIERCE_HEIGHT>                                #33414
DEFINE <PIERCE_DELAY>                                 #33415
DEFINE <CUT_HEIGHT>                                   #33416
DEFINE <CUT_HEIGHT_ENABLED>                           #33417
DEFINE <THC_ACTIVE_DELAY>                             #33418
DEFINE <ARCOK_HIGH_PERCENT>                           #33419 ;Can possibly be removed?
DEFINE <ARCOK_LOW_PERCENT>                            #33420 ;Can possibly be removed?

;traverse.exe
DEFINE <RESTART_MODE_VARIABLE>                        #33500

;--Probing Macros
;#33900-#34999 Reserved
  ;probe_stylus_calibration.cnc
DEFINE <RING_GAUGE_DIAMETER_VALUE>                    #33900
DEFINE <PROBE_STYLUS_DIAMETER_VALUE>                  #33901
DEFINE <BORE_CYCLES_COMPLETED>                        #33902
DEFINE <PROBE_AVERAGE_X_DIAMETER>                     #33903
DEFINE <PROBE_AVERAGE_Y_DIAMETER>                     #33904
DEFINE <PROBE_X_STYLUS_DIAMETER>                      #33905
DEFINE <PROBE_Y_STYLUS_DIAMETER>                      #33906
DEFINE <PROBE_AVERAGE_STYLUS_DIAMETER>                #33907
DEFINE <PROBE_PRETRAVEL_VALUE>                        #33908

  ;probe_check_configuration.cnc
DEFINE <PROBE_INPUT>                                  #33910
DEFINE <PROBE_DETECT_INPUT>                           #33911
DEFINE <PROBE_CONFIG_ERROR>                           #33912

 ;TT
DEFINE <TOOLTOUCH_INPUT>                              #33920
DEFINE <TOOLTOUCH_DETECT_INPUT>                       #33921
DEFINE <TOOLTOUCH_CONFIG_ERROR>                       #33922

 ;Generic
DEFINE <PROBE_PROBED_X_POINT>                         #34006 ;(R)
DEFINE <PROBE_PROBED_Y_POINT>                         #34007 ;(R)
DEFINE <PROBE_PROBED_Z_POINT>                         #34008 ;(R)
DEFINE <PROBEING_DIRECTION>                           #34505 ;(W)
DEFINE <PROBEING_AXIS_LABEL>                          #34506 ;(W)

  ;probe_cycles_select.cnc
DEFINE <PROBE_CYCLE_SELECT>                           #34570 ;(W)
DEFINE <PROBE_CYCLE_ORIENTATION>                      #34571 ;(W)
DEFINE <PROBE_CYCLE_DISTANCE>                         #34572 ;(W)
DEFINE <PROBE_CYCLE_CLEARANCE>                        #34573 ;(W)
DEFINE <PROBE_CURRENT_X_POSITION>                     #34574 ;(R)
DEFINE <PROBE_CURRENT_Y_POSITION>                     #34575 ;(R)
DEFINE <PROBE_CURRENT_Z_POSITION>                     #34576 ;(R)
DEFINE <PROBE_PROBED_X_LENGTH>                        #34578 ;(R)
DEFINE <PROBE_PROBED_Y_LENGTH>                        #34579 ;(R)

  ;Generic
DEFINE <PART_POSITION_VALUE>                          #34610 ;(W)

N1000                            ;End of Macro