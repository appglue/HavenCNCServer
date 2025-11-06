;------------------------------------------------------------------------------
; Filename: check_home_status.cnc
; Description: Check to ensure axes are homed
; Notes:
; Requires: CNC12 V5.06+
; Revision Date: 28 JUL 2023
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\check_home_status.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #20101-20108 : Label for Axes 1-8
; #23701-23708 : Axis home set for axes 1-8
;
; PLC Variables:
;
; User Variables:
; #101 : Current axis being checked
; #102 : Current axis label
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

#101 = 0                         ;initialize #101

;Loop and check all axes starting at axis 1
N100
#101 = #101 + 1                  ;Increment Axis
IF #101 > 8 THEN GOTO 1000       ;If we passed 8 axes, end macro

;Check to see if Axis is labeled N, if so then move on to next axis.
#102 = #[20100 + #101]
IF #102 == 78 THEN GOTO 100

;Check if axis is homed, if not then display message.
IF #[23700 + #101] THEN GOTO 100

N200
M225 #100 "%c axis home is not set.\nPlease home your machine before attempting commanded moves\nPress Cycle Cancel to exit." #102
IF #[23700 + #101] THEN GOTO 100 ELSE GOTO 200

N1000