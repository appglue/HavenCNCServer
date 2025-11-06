;------------------------------------------------------------------------------
; Filename: check_laser_xy0.cnc
; Description: Check XY Zero Position
; Notes:
; Requires: CNC12 V5.10
; Revision Date: 26 Feb 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Parameters:
; #9560 : X-Axis Laser Offset
; #9561 : Y-Axis Laser Offset
;
; System Variables:
;
; PLC Variables:
; LaserAlignActivate_SV IS SV_M94_M95_29
;
; User Variables:
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

N100

M94 /29                          ;Turn on LaserAlignActivate

;M25                             ;Move Z Up, Comment out to remove Z Up move.
G53 Z0 L100                      ;Move to Machine Z0 at 100ipm

;G0 G90 X#9560 Y#9561            ;Move to Zero Position at maximum rate
G1 G90 X#9560 Y#9561  F100       ;Move to Zero Position Change F value to preferred speed
G4 P.05
M225 #100 "Press Cycle Start to Complete Check and Turn off Laser"

M95 /29                          ;Turn off LaserAlignActivate

N1000                            ;End of Macro