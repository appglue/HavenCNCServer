;------------------------------------------------------------------------------
; Filename: perform_park.cnc
; Description: Park Machine.
; Notes: Will call park.mac macro based on parameter 413.
; Requires: CNC12 V5.29.00+
; Revision Date: 2 Oct 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\perform_park.cnc"
;------------------------------------------------------------------------------
; Parameters:
; #9413 : Park Macro
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

IF #9413 == 0 THEN GOTO 100 ELSE GOTO 200

N100
IF #25014 != 2 THEN G90
IF #25014 != 2 THEN G53 L100 X0Y0Z0
IF #25014 == 2 THEN G53 L100 X0Z0
GOTO 1000

N200

G65 "#400\system\park.mac"

GOTO 1000

N1000                             ;End of Macro