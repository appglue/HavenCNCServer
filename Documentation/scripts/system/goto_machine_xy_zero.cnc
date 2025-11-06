;------------------------------------------------------------------------------
; Filename: goto_machine_xy_zero.cnc
; Description: Move to XY Home Position
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\goto_machine_xy_zero.cnc"
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

G53 X0Y0                          ;Goto XY Home Positions

N1000                             ;End of Macro