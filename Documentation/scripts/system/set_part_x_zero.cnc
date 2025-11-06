;------------------------------------------------------------------------------
; Filename: set_part_x_zero.cnc
; Description: Set Work Coordinate X0 at Current Location.
; Notes:
; Requires: CNC12 Mill/Router/Plasma
; Revision Date: 30 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\set_part_x_zero.cnc"
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

N100

G92 X0                            ;Set X to WCS 0

N1000                             ;End of Macro