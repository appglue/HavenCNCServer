;------------------------------------------------------------------------------
; Filename: router_rotary_calibration.cnc
; Description: Calibrate Location of Level of Rotary
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\router_rotary_calibration.cnc"
;
; Input:
; <SELECTED_AXIS> : Selected Axis on CNC12 Menu
;
; Output:
; <SAVED_ROTARY_XY_ANGLE>                     : CSR Angle of Rotary
; <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION>  : Zero Position of Parallel Axis
;
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Setup Defines
G65 "#400\system\setup_key_defines.cnc"
G65 "#400\system\setup_defines.cnc"

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;Set Message Timer to indefinite.
<M225_MESSAGE_TIMER> = 0

N1;Get Axis Properties of Selected Axis
IF <SELECTED_AXIS> > 4 THEN <SELECTED_AXIS_PROPERTIES> = #[9165 + <SELECTED_AXIS> - 4] ELSE <SELECTED_AXIS_PROPERTIES> = #[9090 + <SELECTED_AXIS>]

IF ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X>] && ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_Y>] THEN M225 <M225_MESSAGE_TIMER> "Rotary Configuration Error!\nUse the Wizard Rotary Configuration Menu to Setup Rotary Axis.\nRotary must be parallel to X or Y.\nPress Cycle Cancel to Abort"
IF ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X>] && ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_Y>] THEN GOTO 1

N2;Get Parallel Axis Label
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <ROTARY_PARALLEL_AXIS_LABEL> = <SV_AXIS_1_LABEL> ELSE <ROTARY_PARALLEL_AXIS_LABEL> = <SV_AXIS_2_LABEL>

M222 Q1 <MESSAGE_USER_INPUT> "Rotary Table Spacial Position Calibration Procedure\n\nThis will guide you through positioning the tip of the tool to the centerline of the chuck and tailstock\nUse a tool that has been measured and has a tool height offset.\n\nPress any key to continue."

IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

N100

M201 "Rotary Table Spacial Position Calibration Procedure\nStep 1: Set the Chuck Center Line Position\n\nJog the tip of tool so that the tool is lined up with the centerline of chuck.\nPress Cycle Start to set the chuck center line position."

IF #50001
<SAVED_AXIS_1_POINT_1_MACHINE_POSITION> = <SV_AXIS_1_MACHINE_POSITION>
<SAVED_AXIS_2_POINT_1_MACHINE_POSITION> = <SV_AXIS_2_MACHINE_POSITION>
<SAVED_AXIS_3_POINT_1_MACHINE_POSITION> = <SV_AXIS_3_MACHINE_POSITION> - <SV_ACTIVE_HEIGHT_OFFSET>

M201 "Rotary Table Spacial Position Calibration Procedure\nStep 2: Set the TailStock Center Line Position\n\nJog the tip of tool so that the tool is lined up with the centerline of the tailstock.\nPress Cycle Start to set the tailstock center line position."

IF #50001
<SAVED_AXIS_1_POINT_2_MACHINE_POSITION> = <SV_AXIS_1_MACHINE_POSITION>
<SAVED_AXIS_2_POINT_2_MACHINE_POSITION> = <SV_AXIS_2_MACHINE_POSITION>
<SAVED_AXIS_3_POINT_2_MACHINE_POSITION> = <SV_AXIS_3_MACHINE_POSITION> - <SV_ACTIVE_HEIGHT_OFFSET>

N200 ;Angle Calculations
;Calculate Component Vectors
<CALCULATED_AXIS_1_VECTOR> = abs[<SAVED_AXIS_1_POINT_2_MACHINE_POSITION> - <SAVED_AXIS_1_POINT_1_MACHINE_POSITION>]
<CALCULATED_AXIS_2_VECTOR> = abs[<SAVED_AXIS_2_POINT_2_MACHINE_POSITION> - <SAVED_AXIS_2_POINT_1_MACHINE_POSITION>]
<CALCULATED_AXIS_3_VECTOR> = abs[<SAVED_AXIS_3_POINT_2_MACHINE_POSITION> - <SAVED_AXIS_3_POINT_1_MACHINE_POSITION>]

;Calculate Component Average Positions
<AXIS_1_AVERAGE_MACHINE_POSITION> = [<SAVED_AXIS_1_POINT_1_MACHINE_POSITION> + <SAVED_AXIS_1_POINT_2_MACHINE_POSITION>] / 2
<AXIS_2_AVERAGE_MACHINE_POSITION> = [<SAVED_AXIS_2_POINT_1_MACHINE_POSITION> + <SAVED_AXIS_2_POINT_2_MACHINE_POSITION>] / 2
<AXIS_3_AVERAGE_MACHINE_POSITION> = [<SAVED_AXIS_3_POINT_1_MACHINE_POSITION> + <SAVED_AXIS_3_POINT_2_MACHINE_POSITION>] / 2

;Determine Angle
;Calculate XY Angle
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN G65 "#400\system\atan2.cnc" x[<CALCULATED_AXIS_1_VECTOR>] y[<CALCULATED_AXIS_2_VECTOR>]
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_Y> THEN G65 "#400\system\atan2.cnc" x[<CALCULATED_AXIS_2_VECTOR>] y[<CALCULATED_AXIS_1_VECTOR>]
<XY_PLANE_ANGLE> = #34010 ;Get angle from atan2.cnc

;Calculate XZ OR YZ Angle (Depending on Parallel with X or Y)
; If Parallel to X, use XZ Plane
; IF Parallel to Y, use YZ Plane
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN G65 "#400\system\atan2.cnc" x[<CALCULATED_AXIS_1_VECTOR>] y[<CALCULATED_AXIS_3_VECTOR>]
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_Y> THEN G65 "#400\system\atan2.cnc" x[<CALCULATED_AXIS_2_VECTOR>] y[<CALCULATED_AXIS_3_VECTOR>]

<Z_PLANE_ANGLE> = #34010

N300
IF <Z_PLANE_ANGLE> > 0 THEN M225 <M225_MESSAGE_TIMER> "Warning!\nRotary Z Angle is off by %.1f°\nThe Z Angle will not be compensated for!\nPhysical adjustment of Rotary Alignment may be needed!\nRun Rotary Calibration again after adjusting Rotary Alignment.\n\nPress Cycle Start to Accept Angle\nPress Cycle Cancel to Abort" <Z_PLANE_ANGLE>

; Set CSR to XY Angle
IF <XY_PLANE_ANGLE> == 0 THEN GOTO 310
IF <XY_PLANE_ANGLE> != 0 THEN M222 Q1 <MESSAGE_USER_INPUT> "XY Angle is %.1f°!\nWould you like to set CSR?\n(Y)es/(N)o" <XY_PLANE_ANGLE>
IF <MESSAGE_USER_INPUT> == <KB_N> || <MESSAGE_USER_INPUT> == <KB_SHIFT_N> THEN GOTO 310
IF <MESSAGE_USER_INPUT> == <KB_Y> || <MESSAGE_USER_INPUT> == <KB_SHIFT_Y> THEN GOTO 320

GOTO 300 ;If we got this far, an invalid key was entered. Go back to message.

N310 ;Reset Saved CSR Value
<SAVED_ROTARY_XY_ANGLE> = 0

GOTO 400

N320 ;Save CSR Value
<SAVED_ROTARY_XY_ANGLE> = <XY_PLANE_ANGLE>

N400
M201 "Rotary Table Spacial Position Calibration Procedure\nStep 3: Set the Default %c axis part zero\n\nJog tool to a typical %c axis zero position.\nPress Cycle Start to set the position." <ROTARY_PARALLEL_AXIS_LABEL> <ROTARY_PARALLEL_AXIS_LABEL>

;Set Parallel Axis Position for CNC12 to Save.
IF #50001
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION> = <SV_AXIS_1_MACHINE_POSITION> ELSE <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION> = <SV_AXIS_2_MACHINE_POSITION>

N500 ;Set Parameters
;Set Z Position, we use the average position between the two points from before.
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN G10 P117 R[<AXIS_3_AVERAGE_MACHINE_POSITION>] ELSE G10 P119 R[<AXIS_3_AVERAGE_MACHINE_POSITION>]

;Set Perpendicular Axis
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN G10 P116 R[<AXIS_2_AVERAGE_MACHINE_POSITION>] ELSE G10 P118 R[<AXIS_1_AVERAGE_MACHINE_POSITION>]

N600 ;Set WCS Values
;Set Parallel Axis
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <SV_AXIS_1_ACTIVE_WCS_VALUE> = <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION> ELSE <SV_AXIS_2_ACTIVE_WCS_VALUE> = <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION>
;Set Perpendicular Axis
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <SV_AXIS_2_ACTIVE_WCS_VALUE> = <AXIS_2_AVERAGE_MACHINE_POSITION> ELSE <SV_AXIS_1_ACTIVE_WCS_VALUE> = <AXIS_1_AVERAGE_MACHINE_POSITION>
;Set Z Axis
<SV_AXIS_3_ACTIVE_WCS_VALUE> = <AXIS_3_AVERAGE_MACHINE_POSITION>
;Set CSR
<SV_CSR_ACTIVE_WCS_VALUE> = <SAVED_ROTARY_XY_ANGLE>

<M225_MESSAGE_TIMER> = 3

M225 <M225_MESSAGE_TIMER> "Rotary Calibration Completed Successfully!"

<PROBING_ERROR_FLAG> = 0  ;clear probing error flag

N1000                             ;End of Macro