;------------------------------------------------------------------------------
; Filename: touch_plate_cycles_outside_corner.cnc
; Description: Outside Corner Touch Plate Cycles
; Notes:
; Requires: CNC12 V5.18+
; Revision Date: 8 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\touch_plate_cycles_outside_corner.cnc"
;
; Input:
; <TOUCH_PLATE_CYCLE_SELECT> : Touch Plate Cycle Select
;                                0  = Outside Top Left
;                                1  = Outside Top
;                                2  = Outside Top Right
;                                3  = Outside Right
;                                4  = Outside Bottom Right
;                                5  = Outside Bottom
;                                6  = Outside Bottom Left
;                                7  = Outside Left
;                                8  = Inside Top Left
;                                9  = Inside Top
;                               10  = Inside Top Right
;                               11  = Inside Right
;                               12  = Inside Bottom Right
;                               13  = Inside Bottom
;                               14  = Inside Bottom Left
;                               15  = Inside Left
;                               16  = Z-Only
;                               17  = Bore
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
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Determine Cycle Select
IF <TOUCH_PLATE_CYCLE_SELECT> == 0 THEN GOTO 100
IF <TOUCH_PLATE_CYCLE_SELECT> == 2 THEN GOTO 200
IF <TOUCH_PLATE_CYCLE_SELECT> == 4 THEN GOTO 300
IF <TOUCH_PLATE_CYCLE_SELECT> == 6 THEN GOTO 400

GOTO 1000 ;End Macro, Selection Invalid

;------------------------------------------------------------------------------
;   Outside Top Left Cycle
;------------------------------------------------------------------------------
N100
;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Point 3
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>
<TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Check if External/Internal Touch Plate
IF <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL> THEN GOTO 110

;External Touch Plate
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

;--Transition to Next Position
M125 /X[<SAVED_INITIAL_AXIS_1_POSITION> - <SV_AXIS_1_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * -1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;--Transition to Next Position
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[<PARAM_TOUCH_PLATE_DIAMETER> + <TOOL_DIAMETER_VALUE>] / 2] * 1] /Y[<SAVED_INITIAL_AXIS_2_POSITION> - <SV_AXIS_2_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * 1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move X & Y

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * 1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

N110 ;Internal Touch Plate
;Retract Z
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <PARAM_TOUCH_PLATE_RETRACT_DISTANCE>]

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;Move to Center of Plate
M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[[<PARAM_TOUCH_PLATE_DIAMETER> - <TOOL_DIAMETER_VALUE>] / 2] * -1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * -1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * 1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

;------------------------------------------------------------------------------
;   Outside Top Right Cycle
;------------------------------------------------------------------------------
N200
;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Point 3
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>
<TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Check if External/Internal Touch Plate
IF <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL> THEN GOTO 210

;External Touch Plate
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

;--Transition to Next Position
M125 /X[<SAVED_INITIAL_AXIS_1_POSITION> - <SV_AXIS_1_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * 1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;--Transition to Next Position
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[<PARAM_TOUCH_PLATE_DIAMETER> + <TOOL_DIAMETER_VALUE>] / 2] * -1] /Y[<SAVED_INITIAL_AXIS_2_POSITION> - <SV_AXIS_2_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * 1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move X & Y

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * -1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

N210 ;Internal Touch Plate
;Retract Z
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <PARAM_TOUCH_PLATE_RETRACT_DISTANCE>]

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;Move to Center of Plate
M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[[<PARAM_TOUCH_PLATE_DIAMETER> - <TOOL_DIAMETER_VALUE>] / 2] * 1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * 1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * 1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

;------------------------------------------------------------------------------
;   Outside Bottom Right Cycle
;------------------------------------------------------------------------------
N300
;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Point 3
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>
<TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Check if External/Internal Touch Plate
IF <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL> THEN GOTO 310

;External Touch Plate
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

;--Transition to Next Position
M125 /X[<SAVED_INITIAL_AXIS_1_POSITION> - <SV_AXIS_1_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * 1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;--Transition to Next Position
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[<PARAM_TOUCH_PLATE_DIAMETER> + <TOOL_DIAMETER_VALUE>] / 2] * -1] /Y[<SAVED_INITIAL_AXIS_2_POSITION> - <SV_AXIS_2_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * -1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move X & Y

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * -1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * 1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

N310 ;Internal Touch Plate
;Retract Z
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <PARAM_TOUCH_PLATE_RETRACT_DISTANCE>]

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;Move to Center of Plate
M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[[<PARAM_TOUCH_PLATE_DIAMETER> - <TOOL_DIAMETER_VALUE>] / 2] * 1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * 1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

;------------------------------------------------------------------------------
;   Outside Bottom Left Cycle
;------------------------------------------------------------------------------
N400
;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Point 3
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>
<TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Check if External/Internal Touch Plate
IF <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL> THEN GOTO 410

;External Touch Plate
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

;--Transition to Next Position
M125 /X[<SAVED_INITIAL_AXIS_1_POSITION> - <SV_AXIS_1_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * -1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;--Transition to Next Position
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[<PARAM_TOUCH_PLATE_DIAMETER> + <TOOL_DIAMETER_VALUE>] / 2] * 1] /Y[<SAVED_INITIAL_AXIS_2_POSITION> - <SV_AXIS_2_ACTIVE_WCS_VALUE> + [<PARAM_TOUCH_PLATE_DIAMETER> * 0.75 * -1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move X & Y

M125 /Z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + [<PARAM_TOUCH_PLATE_HEIGHT> / 2] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>] ;Move Z Down

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * 1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_WALL_THICKNESS> + [<TOOL_DIAMETER_VALUE> / 2]] * 1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 1000

N410 ;Internal Touch Plate
;Retract Z
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <PARAM_TOUCH_PLATE_RETRACT_DISTANCE>]

;--Touch Off Plate in X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 1
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;Move to Center of Plate
M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [[[<PARAM_TOUCH_PLATE_DIAMETER> - <TOOL_DIAMETER_VALUE>] / 2] * -1]] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;--Touch Off Plate in Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Probeing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Point 2
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Transition to Corner
;Retract Z to Clearance
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

M125 /X[<TOUCH_PLATE_PROBED_POINT_1_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * -1] /Y[<TOUCH_PLATE_PROBED_POINT_2_POSITION> + [<PARAM_TOUCH_PLATE_DIAMETER> - [<TOOL_DIAMETER_VALUE> / 2]] * -1] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set XY0
G92 X0Y0
;Set Z Zero if Needed
IF <TOUCH_PLATE_SET_Z_HEIGHT_SETTING> == 1 THEN G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]
;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

N1000                            ;End of Macro