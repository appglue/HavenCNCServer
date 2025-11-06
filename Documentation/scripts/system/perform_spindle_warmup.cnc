;------------------------------------------------------------------------------
; Filename: perform_spindle_warmup.cnc
; Description: Spindle Warmup Macro
; Notes:
; Requires: CNC12
; Revision Date: 20 Jun 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\perform_spindle_warmup.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #25005 : Spin Speed Minimum
; #25006 : Spin Speed Maximum
;
; PLC Variables:
;
; User Variables:
; #100 : Message Timer
; #101 : Ascii Value of % sign
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

M201 "Spindle Warmup Cycle\n\nWarning: Remove Tool From Spindle!\n\nJog Spindle to Safe Height\nThen Press Cycle Start to Begin Spindle Warmup."

;G53 Z0       ; optional move Z to Z home

;G53 X12 Y2   ; optional move X and Y to a machine coordinate position

;--Variable Setup
#100 = 0    ;Message Timer for "Permanent" Messages
#101 = 37   ;Ascii value for % sign for later Messages

N100

M221 #100 "Running Spindle at 25%c Maximum RPM for 3 Minutes" #101
M3 S[#25006 * 0.25]   ;Turn on Spindle at 25%
G4 P180               ;Run for 3 Minutes
M5                    ;Turn off Spindle

M221 #100 "Spindle Cooling for 1 Minute"
G4 P60                ;Let Spindle Cool for 1 minute

M221 #100 "Running Spindle at 50%c Maximum RPM for 4 Minutes" #101
M3 S[#25006 * 0.50]   ;Turn on Spindle at 50%
G4 P240               ;Run for 4 Minutes
M5                    ;Turn off Spindle

M221 #100 "Spindle Cooling for 1 Minute"
G4 P60                ;Let Spindle Cool for 1 minute

M221 #100 "Running Spindle at 75%c Maximum RPM for 4 Minutes" #101
M3 S[#25006 * 0.75]   ;Turn on Spindle At 75%
G4 P240               ;Run for 4 Minutes
M5

M221 #100 "Spindle Cooling for 1 Minute"
G4 P60                ;Let Spindle Cool for 1 minute

N1000                             ;End of Macro