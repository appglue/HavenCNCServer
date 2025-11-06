;------------------------------------------------------------------------------
; Filename: router_rotary_recall.cnc
; Description: Recall Location and CSR of Rotary
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\router_rotary_recall.cnc"
;
; Input:
; <SELECTED_AXIS> : Selected Axis on CNC12 Menu
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
G65 "#400\system\setup_defines.cnc"

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;Set Message Timer to indefinite.
<M225_MESSAGE_TIMER> = 0

N1;Get Axis Properties of Selected Axis
IF <SELECTED_AXIS> > 4 THEN <SELECTED_AXIS_PROPERTIES> = #[9165 + <SELECTED_AXIS> - 4] ELSE <SELECTED_AXIS_PROPERTIES> = #[9090 + <SELECTED_AXIS>]

IF ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X>] && ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_Y>] THEN M225 <M225_MESSAGE_TIMER> "Rotary Configuration Error!\nUse the Wizard Rotary Configuration Menu to Setup Rotary Axis.\nRotary must be parallel to X or Y.\nPress Cycle Cancel to Abort"
IF ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X>] && ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_Y>] THEN GOTO 1

N100
M225 <M225_MESSAGE_TIMER> "Rotary Recall Calibration\n\nRecall Calibration will restore last known rotary calibration position\nNote: -Recall Calibration requires 'Spacial Calibrate' (F7) to have been successfully completed.\n-Be sure to Reset Z part zero after Recall for your current tool.\n\nPress Cycle Start to Recall Calibration.\nPress Cycle Cancel to Abort."

;Set Parallel Axis
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <SV_AXIS_1_ACTIVE_WCS_VALUE> = <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION> ELSE <SV_AXIS_2_ACTIVE_WCS_VALUE> = <SAVED_ROTARY_PARALLEL_AXIS_ZERO_POSITION>

;Set Perpendicular Axis
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <SV_AXIS_2_ACTIVE_WCS_VALUE> = <PARAM_ROTARY_X_PARALLEL_Y_POSITION> ELSE <SV_AXIS_1_ACTIVE_WCS_VALUE> = <PARAM_ROTARY_Y_PARALLEL_X_POSITION>

;Set Z Axis
IF <SELECTED_AXIS_PROPERTIES> and <AXIS_IS_PARALLEL_TO_X> THEN <SV_AXIS_3_ACTIVE_WCS_VALUE> = <PARAM_ROTARY_X_PARALLEL_Z_POSITION> ELSE <SV_AXIS_3_ACTIVE_WCS_VALUE> = <PARAM_ROTARY_Y_PARALLEL_Z_POSITION>

;Set CSR
<SV_CSR_ACTIVE_WCS_VALUE> = <SAVED_ROTARY_XY_ANGLE>

<M225_MESSAGE_TIMER> = 3

M225 <M225_MESSAGE_TIMER> "Rotary Recall Completed Successfully!"

N1000                             ;End of Macro