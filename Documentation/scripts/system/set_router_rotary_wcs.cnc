;------------------------------------------------------------------------------
; Filename: set_router_rotary_wcs.cnc
; Description: Guide User to setup proper WCS Coordinates for Rotary Axis
; Notes: Parallel to X or Y is required for this macro.
; Requires: CNC12 V5.10+
; Revision Date: 16 Feb 2024
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
; #100 : Infinite Message Timer
; #102 : User Input Tool Number
; #103 : User Input Z Diameter
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;--Definitions
DEFINE <ROTARY_AXIS_LABEL> #120
DEFINE <ROTARY_AXIS_PARALLEL_X> #121
DEFINE <ROTARY_AXIS_PARALLEL_Y> #122
DEFINE <ROTARY_CENTERLINE> #123
DEFINE <LINEAR_AXIS_LABEL> #124
DEFINE <PARALLEL_AXIS_LABEL> #125

N100 ;Determine Valid Rotary Axis setup as Parallel X or Y
IF #9091 and 1 && [[#9091 and 2048] || [#9091 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20101
IF <ROTARY_AXIS_LABEL> == #20101 && [#9091 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20101 && [#9091 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9092 and 1 && [[#9092 and 2048] || [#9092 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20102
IF <ROTARY_AXIS_LABEL> == #20102 && [#9092 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20102 && [#9092 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9093 and 1 && [[#9093 and 2048] || [#9093 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20103
IF <ROTARY_AXIS_LABEL> == #20103 && [#9093 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20103 && [#9093 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9094 and 1 && [[#9094 and 2048] || [#9094 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20104
IF <ROTARY_AXIS_LABEL> == #20104 && [#9094 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20104 && [#9094 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9166 and 1 && [[#9166 and 2048] || [#9166 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20105
IF <ROTARY_AXIS_LABEL> == #20105 && [#9166 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20105 && [#9166 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9167 and 1 && [[#9167 and 2048] || [#9167 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20106
IF <ROTARY_AXIS_LABEL> == #20106 && [#9167 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20106 && [#9167 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9168 and 1 && [[#9168 and 2048] || [#9168 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20107
IF <ROTARY_AXIS_LABEL> == #20107 && [#9168 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20107 && [#9168 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1
IF #9169 and 1 && [[#9169 and 2048] || [#9169 and 4096]] THEN <ROTARY_AXIS_LABEL> = #20108
IF <ROTARY_AXIS_LABEL> == #20108 && [#9169 and 2048] THEN <ROTARY_AXIS_PARALLEL_X> = 1
IF <ROTARY_AXIS_LABEL> == #20108 && [#9169 and 4096] THEN <ROTARY_AXIS_PARALLEL_Y> = 1

IF <ROTARY_AXIS_LABEL> == 78 || <ROTARY_AXIS_LABEL> == 0 THEN GOTO 110 ;Error if Rotary is Labled N or is still 0
IF <ROTARY_AXIS_PARALLEL_X> == 0 && <ROTARY_AXIS_PARALLEL_Y> == 0 THEN GOTO 110 ;Error if Not Parallel to X or Y
IF <ROTARY_AXIS_PARALLEL_X> == 1 && <ROTARY_AXIS_PARALLEL_Y> == 1 THEN GOTO 110 ;Error if Both Parallel to X and Y
IF <ROTARY_AXIS_LABEL> == #20101 || <ROTARY_AXIS_LABEL> == #20102 THEN GOTO 110 ;Error if Rotary is X or Y Axis

;Determine Non-Parallel Axis Label
IF <ROTARY_AXIS_PARALLEL_X> == 1 THEN <LINEAR_AXIS_LABEL> = #20102
IF <ROTARY_AXIS_PARALLEL_Y> == 1 THEN <LINEAR_AXIS_LABEL> = #20101

;Determine Parallel Axis Label
IF <ROTARY_AXIS_PARALLEL_X> == 1 THEN <PARALLEL_AXIS_LABEL> = #20101
IF <ROTARY_AXIS_PARALLEL_Y> == 1 THEN <PARALLEL_AXIS_LABEL> = #20102

;Determine X or Y Centerline Position
IF <ROTARY_AXIS_PARALLEL_X> == 1 THEN <ROTARY_CENTERLINE> = #9116
IF <ROTARY_AXIS_PARALLEL_Y> == 1 THEN <ROTARY_CENTERLINE> = #9118

GOTO 200 ;Continue macro if all seems ok

N110
M225 #100 "Rotary Configuration Error!\nUse the Wizard Rotary Configuration Menu to Setup Rotary Axis.\nRotary must be Parallel to X or Y.\nPress Cycle Cancel to Abort"
GOTO 110

N200 ;Set WCS
M225 #100 "Set Rotary Part WCS\n \n Machine will automatically move to Z Home then to Rotary Centerline\n  \n Press Cycle Start to Begin"

M25 ;Move Z to Home

G53 $<LINEAR_AXIS_LABEL> <ROTARY_CENTERLINE>

M224 #102 " Enter Tool Number Currently in Spindle "

T#102 G43 H#102

M201 "Set Parallel axis Part WCS Location: Jog Tool to desired %c0 location along the center line\n \n Press Cycle Start to Continue" <PARALLEL_AXIS_LABEL>

G92 $<PARALLEL_AXIS_LABEL> 0 ; Set Parallel Axis Part 0

M201 "Set Z Diameter: Jog Tool and touch a known diameter\n\nPress Cycle Start to Continue"

M224 #103 "Set Z Diameter: Enter known diameter value\n\nPress Cycle Start to Continue"

G92 Z[#103/2]

N1000                            ;End of Macro