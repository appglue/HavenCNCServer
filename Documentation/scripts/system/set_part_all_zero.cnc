;------------------------------------------------------------------------------
; Filename: set_part_all_zero.cnc
; Description: Set Work Coordinate of all axes at Current Location.
; Notes:
; Requires: CNC12
; Revision Date: 11 Jul 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\set_part_all_zero.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #25014 : Software Type
;          1 = Mill
;          2 = Lathe
;
; PLC Variables:
;
; User Variables:
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;Setup Definitions
G65 "#400\system\setup_defines.cnc"

N100 ;Set All Axes Part 0
IF #25014 == 2 THEN GOTO 110 ;Lathe Uses G50 not G92

DEFINE <PARAM_AXIS_1_MASTER>                          #9551
DEFINE <PARAM_AXIS_2_MASTER>                          #9552
DEFINE <PARAM_AXIS_3_MASTER>                          #9553
DEFINE <PARAM_AXIS_4_MASTER>                          #9554
DEFINE <PARAM_AXIS_5_MASTER>                          #9555
DEFINE <PARAM_AXIS_6_MASTER>                          #9556
DEFINE <PARAM_AXIS_7_MASTER>                          #9557
DEFINE <PARAM_AXIS_8_MASTER>                          #9558

IF <SV_AXIS_1_LABEL> != 78 && <PARAM_AXIS_1_MASTER> == 0 THEN G92 $<SV_AXIS_1_LABEL> 0
IF <SV_AXIS_2_LABEL> != 78 && <PARAM_AXIS_2_MASTER> == 0 THEN G92 $<SV_AXIS_2_LABEL> 0
IF <SV_AXIS_3_LABEL> != 78 && <PARAM_AXIS_3_MASTER> == 0 THEN G92 $<SV_AXIS_3_LABEL> 0
IF <SV_AXIS_4_LABEL> != 78 && <PARAM_AXIS_4_MASTER> == 0 THEN G92 $<SV_AXIS_4_LABEL> 0
IF <SV_AXIS_5_LABEL> != 78 && <PARAM_AXIS_5_MASTER> == 0 THEN G92 $<SV_AXIS_5_LABEL> 0
IF <SV_AXIS_6_LABEL> != 78 && <PARAM_AXIS_6_MASTER> == 0 THEN G92 $<SV_AXIS_6_LABEL> 0
IF <SV_AXIS_7_LABEL> != 78 && <PARAM_AXIS_7_MASTER> == 0 THEN G92 $<SV_AXIS_7_LABEL> 0
IF <SV_AXIS_8_LABEL> != 78 && <PARAM_AXIS_8_MASTER> == 0 THEN G92 $<SV_AXIS_8_LABEL> 0

GOTO 1000

N110 ;Set All Axes Part 0 on Lathe
IF <SV_AXIS_1_LABEL> != 78 && <PARAM_AXIS_1_MASTER> == 0 THEN G50 $<SV_AXIS_1_LABEL> 0
IF <SV_AXIS_2_LABEL> != 78 && <PARAM_AXIS_2_MASTER> == 0 THEN G50 $<SV_AXIS_2_LABEL> 0
IF <SV_AXIS_3_LABEL> != 78 && <PARAM_AXIS_3_MASTER> == 0 THEN G50 $<SV_AXIS_3_LABEL> 0
IF <SV_AXIS_4_LABEL> != 78 && <PARAM_AXIS_4_MASTER> == 0 THEN G50 $<SV_AXIS_4_LABEL> 0
IF <SV_AXIS_5_LABEL> != 78 && <PARAM_AXIS_5_MASTER> == 0 THEN G50 $<SV_AXIS_5_LABEL> 0
IF <SV_AXIS_6_LABEL> != 78 && <PARAM_AXIS_6_MASTER> == 0 THEN G50 $<SV_AXIS_6_LABEL> 0
IF <SV_AXIS_7_LABEL> != 78 && <PARAM_AXIS_7_MASTER> == 0 THEN G50 $<SV_AXIS_7_LABEL> 0
IF <SV_AXIS_8_LABEL> != 78 && <PARAM_AXIS_8_MASTER> == 0 THEN G50 $<SV_AXIS_8_LABEL> 0

N1000                             ;End of Macro