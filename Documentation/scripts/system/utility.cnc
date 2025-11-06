;------------------------------------------------------------------------------
; Filename: utility.cnc
; Description: CNC Control Utility
; Notes: This macro is provided by Centroid as a home to put common CNC control utilities under one button on the VCP.
;        User editable and customizable, users can change, delete, or add their own functions to this macro to suit
;         their applications and personal preferences.
; Requires: CNC12 V5.18+
; Revision Date: 17 Jul 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Parameters:
; #9820 : Machine Type
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
; #111 : User Selection
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Setup Key Defines
G65 "#400\system\setup_key_defines.cnc"

IF #9820 == 0 THEN GOTO 100   ;Mill
IF #9820 == 1 THEN GOTO 200   ;Lathe
IF #9820 == 2 THEN GOTO 300   ;Router
IF #9820 == 3 THEN GOTO 400   ;Plasma
IF #9820 == 4 THEN GOTO 500   ;Laser

;-----------------------------------------------------------------------------
; MILL SECTION
;-----------------------------------------------------------------------------
N100
M222 Q1 #111 "#)Which Utility Do You Want To Run?\n1: Calibrate Axes commanded movement vs actual movement\n2: Run Communications Stress Test\n3: Run Spindle Bench Test\n4: Calibrate Probe Stylus Pre Travel Diameter\n5: Manual Set Paired Axis Square"

IF #111 == <KB_1> THEN GOTO 101
IF #111 == <KB_2> THEN GOTO 102
IF #111 == <KB_3> THEN GOTO 103
IF #111 == <KB_4> THEN GOTO 104
IF #111 == <KB_5> THEN GOTO 105

GOTO 100

N101
G65 "#400\system\Axis_Calibration.cnc"
GOTO 1000

N102
G65 "#400\system\com_stress_test.cnc"
GOTO 1000

N103
G65 "#400\system\spindlebenchtest.cnc"
GOTO 1000

N104
G65 "#400\system\probe_stylus_calibration.cnc"
GOTO 1000

N105
G65 "#400\system\set_paired_axis_man_square.cnc"
GOTO 1000

GOTO 1000

;-----------------------------------------------------------------------------
; LATHE SECTION
;-----------------------------------------------------------------------------
N200
M222 Q1 #111 "#)Which Utility Do You Want To Run?\n1: Calibrate Axes commanded movement vs actual movement\n2: Run Communications Stress Test\n3: Run Spindle Bench Test\n4: Manual Set Paired Axis Square"

IF #111 == <KB_1> THEN GOTO 201
IF #111 == <KB_2> THEN GOTO 202
IF #111 == <KB_3> THEN GOTO 203
IF #111 == <KB_4> THEN GOTO 204

GOTO 200

N201
G65 "#400\system\Axis_Calibration.cnc"
GOTO 1000

N202
G65 "#400\system\com_stress_test.cnc"
GOTO 1000

N203
G65 "#400\system\spindlebenchtest.cnc"
GOTO 1000

N204
G65 "#400\system\set_paired_axis_man_square.cnc"
GOTO 1000

GOTO 1000

;-----------------------------------------------------------------------------
; ROUTER SECTION
;-----------------------------------------------------------------------------
N300
M222 Q1 #111 "#)Which Utility Do You Want To Run?\n1: Calibrate Axes commanded movement vs actual movement\n2: Run Communications Stress Test\n3: Run Spindle Bench Test\n4: Teach-in the LASER Offset Distance from the Spindle Centerline\n5: Calibrate Probe Stylus Pre Travel Diameter\n6: Manual Set Paired Axis Square"

IF #111 == <KB_1> THEN GOTO 301
IF #111 == <KB_2> THEN GOTO 302
IF #111 == <KB_3> THEN GOTO 303
IF #111 == <KB_4> THEN GOTO 304
IF #111 == <KB_5> THEN GOTO 305
IF #111 == <KB_6> THEN GOTO 306

GOTO 300

N301
G65 "#400\system\Axis_Calibration.cnc"
GOTO 1000

N302
G65 "#400\system\com_stress_test.cnc"
GOTO 1000

N303
G65 "#400\system\spindlebenchtest.cnc"
GOTO 1000

N304
G65 "#400\system\teach_laser_offset.cnc"
GOTO 1000

N305
G65 "#400\system\probe_stylus_calibration.cnc"
GOTO 1000

N306
G65 "#400\system\set_paired_axis_man_square.cnc"
GOTO 1000

GOTO 1000

;-----------------------------------------------------------------------------
; PLASMA SECTION
;-----------------------------------------------------------------------------
N400
M222 Q1 #111 "#)Which Utility Do You Want To Run?\n1: Calibrate Axes commanded movement vs actual movement\n2: Run Communications Stress Test\n3: Calibrate Torch\n4: Teach-in the LASER Offset Distance from the Torch Centerline\n5: Set the Part XY0 Location with the LASER\n6: Calibrate Ohmic And Float Offset\n7: Manual Set Paired Axis Square"

IF #111 == <KB_1> THEN GOTO 401
IF #111 == <KB_2> THEN GOTO 402
IF #111 == <KB_3> THEN GOTO 403
IF #111 == <KB_4> THEN GOTO 404
IF #111 == <KB_5> THEN GOTO 405
IF #111 == <KB_6> THEN GOTO 406
IF #111 == <KB_7> THEN GOTO 407

GOTO 400

N401
G65 "#400\system\Axis_Calibration.cnc"
GOTO 1000

N402
G65 "#400\system\com_stress_test.cnc"
GOTO 1000

N403
G65 "#400\system\Torch_Calibration.cnc"
GOTO 1000

N404
G65 "#400\system\teach_laser_offset.cnc"
GOTO 1000

N405
G65 "#400\system\set_laser_xy.cnc"
GOTO 1000

N406
G65 "#400\system\teach_float_offset.cnc"
GOTO 1000

N407
G65 "#400\system\set_paired_axis_man_square.cnc"
GOTO 1000

;-----------------------------------------------------------------------------
; LASER SECTION
;-----------------------------------------------------------------------------
N500
M222 Q1 #111 "#)Which Utility Do You Want To Run?\n1: Calibrate Axes commanded movement vs actual movement\n2: Run Communications Stress Test\n3: Manual Set Paired Axis Square"

IF #111 == <KB_1> THEN GOTO 501
IF #111 == <KB_2> THEN GOTO 502
IF #111 == <KB_3> THEN GOTO 503

GOTO 500

N501
G65 "#400\system\Axis_Calibration.cnc"
GOTO 1000

N502
G65 "#400\system\com_stress_test.cnc"
GOTO 1000

N503
G65 "#400\system\set_paired_axis_man_square.cnc"
GOTO 1000

N1000                            ;End of Macro