;------------------------------------------------------------------------------
; Filename: select_laser_macros.cnc
; Description: Select Laser Macro Operation
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
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
; #101 : Timed Message
; #110 : Restore Parameter 10 Flag
; #111 : User Selection

;File: teach_laser_offset.cnc
; #102 : Saved X Machine Position
; #103 : Saved Y Machine Position

;File: set_laser_csr.cnc
; #104 : First X Axis Point
; #105 : First Y Axis Point
; #106 : Change in X Axis dX
; #107 : Change in Y Axis dY
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Setup Key Defines
G65 "#400\system\setup_key_defines.cnc"

N100
;Determine if P10 has bit 0 set
;if set then disable bit 0 to hide G&M Codes
IF #9010 and 1 THEN #110 = 1
IF #110 == 1 THEN G10 P10 R[#9010 - 1]

M94 /29   ;Laser On

M222 Q1 #111 "#)Choose a Crosshair LASER Function to run:\n1: Set the Part XY0 Location with the LASER\n2: Check the current XY0 Location with the LASER\n3: Set Coordinate System Rotation(CSR) with the LASER\n4: Teach-in the LASER Offset Distance from the Spindle Centerline"

IF #111 == <KB_1> THEN GOTO 101
IF #111 == <KB_2> THEN GOTO 102
IF #111 == <KB_3> THEN GOTO 103
IF #111 == <KB_4> THEN GOTO 104

GOTO 100

N101
G65 "#400\system\set_laser_xy.cnc"
GOTO 1000

N102
G65 "#400\system\check_laser_xy0.cnc"
GOTO 1000

N103
G65 "#400\system\set_laser_csr.cnc"
GOTO 1000

N104
G65 "#400\system\teach_laser_offset.cnc"
GOTO 1000

N1000                            ;End of Macro

;Restore G&M Codes if parameter 10 was changed
IF #50001
IF #110 == 1 THEN G10 P10 R[#9010 + 1]

M95 /29   ;Laser Off