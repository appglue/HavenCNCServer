;------------------------------------------------------------------------------
; Filename: hc_calibration_collect.cnc
; Description: CNC12 HC Calibration, Collect Data Move.
; Notes: Moves Z up to collect Voltage Data.
; Requires: CNC12 V5.30.00+
; Revision Date: 31 Jul 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\hc_calibration_collect.cnc"
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

N100 ;Perform Z Move

G93 G1 Z.5 F16

IF #50001 ;Prevent lookahead from parsing past here
<PROBING_ERROR_FLAG> = 0

M95 /100    ;Enable Touch Alarm Fault

N1000                             ;End of Macro