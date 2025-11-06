;------------------------------------------------------------------------------
; Filename: set_laser_csr.cnc
; Description: Laser Orient CSR Macro
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\set_laser_csr.cnc"
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

N100

#2400 = 0                         ;Set CSR to 0

M94 /29                           ;Turn on LaserAlignActivate

M201 "Jog to First Point along wall.\nThen Press Cycle Start to Accept Point\nOr Cycle Cancel to Clear Orientation"

#101 = #5041
#102 = #5042

M201 "Jog to Second Point along wall.\nThen Press Cycle Start to Accept Point"

#103 = #5041 - #101
#104 = #5042 - #102

G65 "#400\system\atan2.cnc" X#103 Y#104

;Assume if Y Difference is larger that User measured the Y-Axis Side
; this requires us to subtract 90 degrees from the Angle for correct orientation.
IF abs[#104] > abs[#103] THEN #34010 = #34010 - 90

;Flip Angle when dx was negative
IF #34010 >  90 THEN #34010 = #34010 - 180  ;Flip Angle
IF #34010 < -90 THEN #34010 = #34010 + 180  ;Flip Angle

;Ensure Degrees is between 90 and -90.
#34010 = #34010 % 90

;Set CSR Value
#2400 = #34010

N1000                             ;End of Macro