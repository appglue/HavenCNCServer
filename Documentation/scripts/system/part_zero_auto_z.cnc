;------------------------------------------------------------------------------
; Filename: part_zero_auto_z.cnc
; Description: CNC12 Part Zero Auto Measure for 3rd Axis
; Notes:
; Requires: CNC12 V5.24+
; Revision Date: 19 Sep 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\part_zero_auto_z.cnc"
;
; Input:
; <PROBEING_AXIS_LABEL> : Axis performing the Probing
; <PART_POSITION_VALUE> : User input from the Part Zero Screen
;
; Output:
; <PROBE_CURRENT_Z_POSITION>  : Probed Z Axis Position
;                               Note: Macro Performs Height Calculations,
;                                     CNC12 simply updates DRO with Provided Number.
; <PROBING_ERROR_FLAG>        : Probing Error Flag
;
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
; <VAR_1> : Configured Devices
;             Bit 0 : Touch Probe
;             Bit 1 : Tool Touch Off
;             Bit 2 : Touch Plate
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Setup Define Variables
G65 "#400\system\setup_defines.cnc"
G65 "#400\system\setup_key_defines.cnc"

;Initalize Variables
<VAR_1> = 0

N1;Determine Touch Devices Configured
; Assume if Trip or Detect input is defined that the device exists.
IF <PARAM_PROBE_INPUT> != 0 || <PARAM_PROBE_DETECT_INPUT> != 0 THEN <VAR_1> = <VAR_1> + 1  ;Touch Probe
IF <PARAM_TOOLTOUCH_INPUT> != 0 || <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 THEN <VAR_1> = <VAR_1> + 2 ;Tool Touch
IF <PARAM_TOUCH_PLATE_INPUT> != 0 || <PARAM_TOUCH_PLATE_DETECT_INPUT> != 0 THEN <VAR_1> = <VAR_1> + 4 ;Touch Plate

;Display Error if no Devices Found
IF ![<VAR_1> and 1] && ![<VAR_1> and 2] && ![<VAR_1> and 4] THEN M222 Q1 <MESSAGE_USER_INPUT> "No Touch Off Device Configured\nEnsure Touch Off Device is Properly Configured!\nPress any key to abort."
IF ![<VAR_1> and 1] && ![<VAR_1> and 2] && ![<VAR_1> and 4] THEN GOTO 1000

N2;Display Selection Message Based on Touch Devices Connected
;Un-comment out a line of the following code to always use a particular device
;GOTO 100 ;Probe
;GOTO 200 ;TT
;GOTO 300 ;Touch Plate
<MESSAGE_USER_INPUT> = 0 ;Reset User Input Value
IF  [<VAR_1> and 1] && ![<VAR_1> and 2] && ![<VAR_1> and 4] THEN <MESSAGE_USER_INPUT> = <KB_4> ;Seek the Touch Probe
IF ![<VAR_1> and 1] &&  [<VAR_1> and 2] && ![<VAR_1> and 4] THEN <MESSAGE_USER_INPUT> = <KB_1> ;Seek the Tool Touch
IF ![<VAR_1> and 1] && ![<VAR_1> and 2] &&  [<VAR_1> and 4] THEN <MESSAGE_USER_INPUT> = <KB_8> ;Seek the Touch Plate

IF  [<VAR_1> and 1] &&  [<VAR_1> and 2] && ![<VAR_1> and 4] THEN M222 Q1 <MESSAGE_USER_INPUT> "#)      ----------------      Auto  Z  Zero     ----------------      \n\n      ***  Choose which Touch Device to use!  ***\n\n        Press 1 = Seek the TOOL TOUCH OFF \n\n                                         - or -   \n\n        Press 4 = Seek surface w/ TOUCH PROBE  \n "
IF ![<VAR_1> and 1] &&  [<VAR_1> and 2] &&  [<VAR_1> and 4] THEN M222 Q2 <MESSAGE_USER_INPUT> "#)      ----------------      Auto  Z  Zero     ----------------      \n\n      ***  Choose which Touch Device to use!  ***\n\n        Press 1 = Seek the TOOL TOUCH OFF \n\n                                         - or -   \n\n        Press 8 = Seek the TOUCH PLATE \n "
IF  [<VAR_1> and 1] && ![<VAR_1> and 2] &&  [<VAR_1> and 4] THEN M222 Q3 <MESSAGE_USER_INPUT> "#)      ----------------      Auto  Z  Zero     ----------------      \n\n      ***  Choose which Touch Device to use!  ***\n\n        Press 4 = Seek the TOUCH PROBE \n\n                                         - or -   \n\n        Press 8 = Seek the TOUCH PLATE \n "
IF  [<VAR_1> and 1] &&  [<VAR_1> and 2] &&  [<VAR_1> and 4] THEN M222 Q4 <MESSAGE_USER_INPUT> "#)      ----------------      Auto  Z  Zero     ----------------      \n\n      ***  Choose which Touch Device to use!  ***\n\n        Press 1 = Seek the TOOL TOUCH OFF \n\n                                         - or -   \n\n        Press 4 = Seek the TOUCH PROBE \n\n                                         - or -   \n\n        Press 8 = Seek the TOUCH PLATE \n "

;--Determine Selection
;Prevent Invalid Selections of Devices not Found
IF ![<VAR_1> and 1] && <MESSAGE_USER_INPUT> == <KB_4> THEN GOTO 2 ;Invalid Selection if Probe Selected and None Detected
IF ![<VAR_1> and 2] && <MESSAGE_USER_INPUT> == <KB_1> THEN GOTO 2 ;Invalid Selection if TT Selected and None Detected
IF ![<VAR_1> and 4] && <MESSAGE_USER_INPUT> == <KB_8> THEN GOTO 2 ;Invalid Selection if Touch Plate Selected and None Detected

;Goto Valid Selection otherwise, go back to selection message.
IF <MESSAGE_USER_INPUT> == <KB_4> THEN GOTO 100 ;Probe Selected
IF <MESSAGE_USER_INPUT> == <KB_1> THEN GOTO 200 ;TT Selected
IF <MESSAGE_USER_INPUT> == <KB_8> THEN GOTO 300 ;Touch Plate Selected
IF <MESSAGE_USER_INPUT> != <KB_1> && <MESSAGE_USER_INPUT> != <KB_4> && <MESSAGE_USER_INPUT> != <KB_8> THEN GOTO 2 ;Invalid Selection go back to Message

;------------------------------------------------------------------------------
;   Seek with Touch Probe Device
;------------------------------------------------------------------------------
N100
;Check Probe Configuration
G65 "#400\system\probe_check_configuration.cnc" A1
IF <PROBE_CONFIG_ERROR> == 1 THEN GOTO 1000 ;End Program if error found

M201 " Press CYCLE START to seek with TOUCH PROBE and Set Z "

#34589 = 3 ;Set Variable for Axis 3
#34590 = -1 ;Set Axis Move Direction
#34592 = 0  ;Set Probe is Used

;Perform Probe Move to Surface
G65 "#400\system\probe_move_to_surface_p0.cnc"

;Retract if Probe is Tripped.
IF #[<PROBE_INPUT>] THEN G65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#34503] X[#34006] Y[#34007] Z[#34008] F[#f]

;Save Point to Variable for CNC12
<PROBE_CURRENT_Z_POSITION> = <PART_POSITION_VALUE> - [#34576 - #5043]
<PROBING_ERROR_FLAG> = 0

GOTO 1000

;------------------------------------------------------------------------------
;   Seek with Tool Touch Device
;------------------------------------------------------------------------------
N200
;Check Tool Touch Input Configuration
; For Tool Touch if P44 is not defined, then use P11.
IF <PARAM_TOOLTOUCH_INPUT> == 0 && <PARAM_PROBE_INPUT> == 0 THEN M222 <MESSAGE_USER_INPUT> "Tool Touch Input Not Configured\n Ensure Tool Touch is Properly Configured!\nPress any key to abort."

;Assign Inputs
; Convert Input to 50000 variable if it is not already.
; For Tool Touch if P44 is not defined, then use P11.
IF <PARAM_TOOLTOUCH_INPUT> != 0 && abs[<PARAM_TOOLTOUCH_INPUT>] < 50000 THEN <TOOLTOUCH_INPUT> = abs[<PARAM_PROBE_INPUT>] + 50000 ELSE <TOOLTOUCH_INPUT> = abs[<PARAM_TOOLTOUCH_INPUT>]
IF <PARAM_TOOLTOUCH_INPUT> == 0 && abs[<PARAM_PROBE_INPUT>] < 50000 THEN <TOOLTOUCH_INPUT> = abs[<PARAM_PROBE_INPUT>] + 50000 ELSE IF <PARAM_TOOLTOUCH_INPUT> == 0 THEN <TOOLTOUCH_INPUT> = abs[<PARAM_PROBE_INPUT>]

;Check Tool Touch Detect Input Configuration
IF <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 && abs[<PARAM_TOOLTOUCH_DETECT_INPUT>] < 50000 THEN <TOOLTOUCH_DETECT_INPUT> = abs[<PARAM_TOOLTOUCH_DETECT_INPUT>] + 50000 ELSE <TOOLTOUCH_DETECT_INPUT> = abs[<PARAM_TOOLTOUCH_DETECT_INPUT>]
IF <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 THEN IF !#[<TOOLTOUCH_DETECT_INPUT>] THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch was not found!\nEnsure Tool Touch is Properly Connected!\nPress any key to continue"
IF <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 THEN IF !#[<TOOLTOUCH_DETECT_INPUT>] THEN GOTO 1000

;--Check to Ensure Tool Touch Not Tripped
;Message for NO Tool Touches or NC Tool Touches with Detect Configured if Tripped.
IF [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == <TOOLTOUCH_INPUT_IS_NO_TYPE>]] || [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> != 0]] THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch is tripped!\nProbing Cycle canceled. Please check Tool Touch.\nPress any key to abort."
IF [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == <TOOLTOUCH_INPUT_IS_NO_TYPE>]] || [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> != 0]] THEN GOTO 1000

;Message for NC Probes if Input is tripped and No Detect Configured.
IF #[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> == 0] THEN M222 Q1 #32003 "Tool Touch is tripped or not plugged in!\nProbing Cycle Canceled. Please check Tool Touch.\nPress any key to abort."
IF #[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> == 0] THEN GOTO 1000

M201 " Press CYCLE START to seek TOOL TOUCH OFF and Set Z "

#34589 = 3 ;Set Variable for Axis 3
#34590 = -1 ;Set Axis Move Direction
#34592 = 1  ;Set TT is Used

;Perform Probe Move to Surface
G65 "#400\system\probe_move_to_surface_p0.cnc"

;Retract if TT is Tripped
M0
IF #[<TOOLTOUCH_INPUT>] THEN G65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#34503] X[#34006] Y[#34007] Z[#34008] F[#f]
M0

;Save Points to Variables for CNC12
<PROBE_CURRENT_Z_POSITION> = <PART_POSITION_VALUE> - [#34576 - #5043] - <PARAM_TOOLTOUCH_HEIGHT>
<PROBING_ERROR_FLAG> = 0

GOTO 1000

;------------------------------------------------------------------------------
;   Seek with Touch Plate Device
;------------------------------------------------------------------------------
N300 ;Seek with Touch Plate Device
;Get Variable Values and Defines and Error Checking
G65 "#400\system\touch_plate_get_variables.cnc"

M201 " Press CYCLE START to seek TOUCH PLATE and Set Z "

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move_p0.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Points
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>

;Retract Z
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

;Save Points to Variables for CNC12
<PROBE_CURRENT_Z_POSITION> = <PART_POSITION_VALUE> + <PARAM_TOUCH_PLATE_HEIGHT> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>
<PROBING_ERROR_FLAG> = 0

N1000                            ;End of Macro