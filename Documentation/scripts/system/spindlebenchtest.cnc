;------------------------------------------------------------------------------
; Filename: spindlebenchtest.cnc
; Description: Tests analog output for the spindle.
; Notes:
; Requires: CNC12
; Revision Date: 6 Jan 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "\cncm\system\spindlebenchtest.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #25005 : Minimum Spindle Speed
; #25006 : Maximum Spindle Speed
;
; PLC Variables:
;
; User Variables:
; #100 : Infinite Message Timer
; #101 : Requested Spindle Speed
; #102 : Requested Voltage
; #103 : Acceptable Voltage Lower Limit
; #104 : Acceptable Voltage Upper Limit
; #105 : Voltage Reading Number
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Initialize Variables
#100 = 0
#101 = 0
#102 = 0
#103 = 0
#104 = 0
#105 = 1

M225 #100 "Welcome to the Bench Testing Utility.\nPlease make sure you have a DVM and a copy of the Installation Manual on hand.\nPress Cycle Start (alt-s) to continue"

N100
#101 = #25006 * [0.25 * #105]
IF #101 < #25005 THEN #101 = #25005 ;Ensure commanded Spindle speed is not less than Min Speed
IF #101 > #25006 THEN #101 = #25006 ;Ensure commanded Spindle Speed is not more than Max Speed

;Set Expected Voltage
#102 = 10 * [#101 / #25006]
#103 = #102 * 0.9 ;Lower Limit Voltage
#104 = #102 * 1.1 ;Upper Limit Voltage

N200 ;Start Measurement Test
M3 S#101  ;Start Analog Voltage
M224 #29001 "Voltage Reading #%.0f - S%.0f\nEnter the voltage (VDC) read between Spindle Analog and Spindle Analog Com\nPress Cycle Start (alt-s) to continue" #105 #101
IF [#29001 < #103] || [#29001 > #104] THEN M225 #100 "Invalid Voltage. Voltage should be within ±10 percent of %.2fVDC\nReview Spindle Bench Test Section of the Installation Manual.\nPress Cycle Start (alt-s) to retry" #102
IF [#29001 < #103] || [#29001 > #104] THEN GOTO 200

#105 = #105 + 1
IF #105 <= 4 THEN GOTO 100

N1000                             ;End of Macro