;------------------------------------------------------------------------------
; Filename: touch_plate_auto_z_zero.cnc
; Description: Set WCS Z0 with Touch Plate
; Notes:
; Requires: CNC12 V5.18+
; Revision Date: 28 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
; #100 : Infinite Message Timer
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

#100 = 0

;Check Configuration and get Variable Values.
G65 "#400\system\touch_plate_get_variables.cnc"

M5                                ;Spindle Off

N100

M201 "Remove dust shroud,\nJog Tool into the Center of Plate\nthen Press Cycle Start"

;Reminder Message for Magnet for Touch Plate
M200 "Ensure Tool Magnet is attached before Proceeding\nPress Cycle Start to begin touchoff"

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Point 3
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Set Z Zero
G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]

M25                               ;Retract Z to Home

M225 #100 "Replace dust shroud\nPress Cycle Start to continue"

N1000                             ;End of Macro