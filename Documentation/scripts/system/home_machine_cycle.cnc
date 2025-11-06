;------------------------------------------------------------------------------
; Filename: home_machine_cycle.cnc
; Description: Runs Homing Routine for Mill/Lathe/Router/Plasma
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 5 Sep 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\home_machine_cycle.cnc"
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

;Setup Defines
G65 "#400\system\setup_defines.cnc"

;Skip Running .hom file if one is not used
; Jog and HomeinPlace do not run .hom file for homing.
IF <SV_HOMING_TYPE_AT_STARTUP> == <HOMING_TYPE_IS_HOMEINPLACE> THEN GOTO 200
IF <SV_HOMING_TYPE_AT_STARTUP> == <HOMING_TYPE_IS_JOG> THEN GOTO 300

N100 ;Run Home File

IF #25014 == 1 THEN G65 "#400\cncm.hom"  ;Mill
IF #25014 == 2 THEN G65 "#400\cnct.hom"  ;Lathe
IF #25014 == 3 THEN G65 "#400\cncm.hom"  ;Router
IF #25014 == 4 THEN G65 "#400\cncm.hom"  ;Plasma
IF #25014 == 5 THEN G65 "#400\cncm.hom"  ;Laser

GOTO 1000

N200 ;HomeinPlace
M200 "Warning!\nReset Home will permanently move your home to the current position\n\nPress Cycle Start to Reset Home.\nPress Cycle Cancel to Abort."

N300 ;Jog Homing
IF <SV_AXIS_1_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_1_LABEL>
IF <SV_AXIS_2_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_2_LABEL>
IF <SV_AXIS_3_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_3_LABEL>
IF <SV_AXIS_4_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_4_LABEL>
IF <SV_AXIS_5_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_5_LABEL>
IF <SV_AXIS_6_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_6_LABEL>
IF <SV_AXIS_7_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_7_LABEL>
IF <SV_AXIS_8_LABEL> != <AXIS_LABEL_IS_N> THEN M26 /$<SV_AXIS_8_LABEL>

N1000                             ;End of Macro