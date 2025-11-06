;------------------------------------------------------------------------------
; Filename: goto_part_xy_zero.cnc
; Description: Goto Work Coordinate XY0
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\goto_part_xy_zero.cnc"
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

;Check Home Status
G65 "#400\system\check_home_status.cnc"

N100

M25                               ;Raise Z to G28 Position
G0 X0Y0                           ;Move to XY0

N1000                             ;End of Macro