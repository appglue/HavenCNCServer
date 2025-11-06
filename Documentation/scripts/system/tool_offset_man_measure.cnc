;------------------------------------------------------------------------------
; Filename: tool_offset_man_measure.cnc
; Description: CNC12 Tool Library Manual Measure Macro
; Notes:
; Requires: CNC12 V5.18+
; Revision Date: 6 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\tool_offset_man_measure.cnc"
;
; Output:
; #32000 : Probing Error Flag
; #34574 : Probed or Current X WCS Position
; #34575 : Probed or Current Y WCS Position
; #34576 : Probed or Current Z WCS Position
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #5041 : Current X Axis WCS Position
; #5042 : Current Y Axis WCS Position
; #5043 : Current Z Axis WCS POsition
;
; PLC Variables:
;
; User Variables:
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Set Manual Measure to Current Location
#34574 = #5041 ;Current X Axis WCS Position
#34575 = #5042 ;Current Y Axis WCS Position
#34576 = #5043 ;Current Z Axis WCS Position

#32000 = 0 ;Clear Probe Error Flag

N1000                            ;End of Macro