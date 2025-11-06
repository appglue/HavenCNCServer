;------------------------------------------------------------------------------
; Filename: hc_calibration_zero.cnc
; Description: CNC12 HC Calibration, Move to Plate
; Notes:
; Requires: CNC12 V5.30.00+
; Revision Date: 31 Jul 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\hc_calibration_zero.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
;
; PLC Variables:
; TorchTipTouchOff_M  IS MEM741 (#70741)
;
; User Variables:
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Setup Define Variables
G65 "#400\system\setup_defines.cnc"

;Check if THC is Installed
IF [<PARAM_PLASMA_OPTIONS> and <PLASMA_OPTIONS_IS_DISABLE_HC>] THEN M222 Q1 <MESSAGE_USER_INPUT> "Height Control Not Enabled/Installed\nHeight Control Board is Required for HC Calibration.\nEnsure that HC is enabled in Profile Manager Configuration Settings.\nPress any key to abort."
IF [<PARAM_PLASMA_OPTIONS> and <PLASMA_OPTIONS_IS_DISABLE_HC>] THEN GOTO 1000

N100 ;Perform Move to Plate
M94 /100    ;Disable Touch Alarm Fault
G4 P0.06    ;Dwell to ensure PLC Bits are set.

M105 /Z P-70741 F#9015

G92 Z0

IF #50001 ;Prevent lookahead from parsing past here
<PROBING_ERROR_FLAG> = 0

N1000                             ;End of Macro