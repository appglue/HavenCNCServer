;------------------------------------------------------------------------------
; Filename: router_rotary_unwind.cnc
; Description: Unwind Rotary Revolutions
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\router_rotary_unwind.cnc"
;
; Input:
; <SELECTED_AXIS> : Selected Axis on CNC12 Menu
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

IF ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_ROTARY>] THEN M225 <M225_MESSAGE_TIMER> "Rotary Configuration Error!\nAxis is not setup as Rotary Axis\n Use the Wizard Rotary Configuration Menu to Setup Rotary Axis.\nPress Cycle Cancel to Abort"
IF ![<SELECTED_AXIS_PROPERTIES> and <AXIS_IS_ROTARY>] THEN GOTO 1

;Determine Axis Label
<ROTARY_AXIS_LABEL> = #[20100 + <SELECTED_AXIS>]

N100 ;Perform Unwind

M151 /$<ROTARY_AXIS_LABEL>

N1000                             ;End of Macro