;------------------------------------------------------------------------------
; Filename: set_laser_xy.cnc
; Description: Set XY zero locaiton using Laser Alignment cross hairs
; Notes: Uses Parameters 560 and 561 for X & Y Offset Values.
; Requires: CNC12 V5.18+
; Revision Date:  02 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Parameters:
; #9560 = X-Axis Laser Offset
; #9561 = Y-Axis Laser Offset
;
; System Variables:
;
; PLC Variables:
; LaserAlignActivate_SV IS SV_M94_M95_29
;
; User Variables:
; #100 = Permanent M225 Message
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

#100 = 0

N100

M94 /29             ;Turn on LaserAlignActivate

M201 "Jog to Position with Crosshair Laser\nThen Press Cycle Start to Set XY Zero"

G92 X[#9560 * -1] Y[#9561 * -1]

;M25                ;Uncomment if you wish for Z to return to Home

M95 /29             ;turn off LaserAlignActivate

;M225 #100 "XY Location Set\nPRESS CYCLE START/ESC TO EXIT"

N1000                            ;End of Macro