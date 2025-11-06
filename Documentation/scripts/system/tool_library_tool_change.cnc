;------------------------------------------------------------------------------
; Filename: tool_library_tool_change.cnc
; Description: Tool Change Request from Tool Library Menu
; Notes:
; Requires: CNC12 V5.18+
; Revision Date: 14 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\tool_library_tool_change.cnc"
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

G65 "#400\system\perform_tool_change.cnc"

N1000                            ;End of Macro