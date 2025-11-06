;------------------------------------------------------------------------------
; Filename: set_tool_number.cnc
; Description: allows operator to easily set the Tool Number when manually changing tools. 
; Sets the Tool Number and the matching Tool Height Offset (G43) of that Tool Number
; Notes: For Non-ATC Systems
; Requires: CNC12 V5.09+
; Revision Date: 07 Feb 2024
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
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

N100                             ;Insert your code between N100 and N1000

M224 #101 " Enter Tool Number Currently in Spindle "

T#101 G43 H#101

N1000                            ;End of Macro
