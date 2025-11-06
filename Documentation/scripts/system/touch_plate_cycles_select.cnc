;------------------------------------------------------------------------------
; Filename: touch_plate_cycles_select.cnc
; Description: Select Touch Plate Cycle to Call.
; Notes: Called by CNC12 for Touch Plate Cycles.
; Requires: CNC12 V5.18+
; Revision Date: 3 Apr 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\touch_plate_cycles_select.cnc"
;
; Diagram:
;   0---------1---------2
;   |         |         |
;   |    8----9----10   |
;   |    |         |    |
;   7   15         11   3
;   |    |         |    |
;   |   14----13---12   |
;   |         |         |
;   6---------5---------4
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
;
; <TOOL_DIAMETER_VALUE>                 : Tool Diameter
; <TOUCH_PLATE_SET_Z_HEIGHT_SETTING>    : Set Material Height (Touch Z)
; <TOUCH_PLATE_FLUTE_REMINDER_SETTING>  : Flute Reminder
; <TOUCH_PLATE_MAGNET_REMINDER_SETTING> : Magnet Reminder
; <TOUCH_PLATE_Z_CLEARANCE_VALUE>       : Z-Clearance Amount
;
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
IF #4201 || #4202 THEN GOTO 10000 ;Skip macro if graphing or searching

N1 ;Check Configuration
;Get Variable Values and Defines
G65 "#400\system\touch_plate_get_variables.cnc"

;Set Message Timer so that messages display indefinitely.
<M225_MESSAGE_TIMER> = 0

;Check Z-Clearance Height
IF <TOUCH_PLATE_Z_CLEARANCE_VALUE> <= 0 THEN M225 <M225_MESSAGE_TIMER> "Z-Clearance Height not Set\nSet Z-Clearance Height and Try Again.\nPress Cycle Cancel to Abort."
IF <TOUCH_PLATE_Z_CLEARANCE_VALUE> <= 0 THEN GOTO 1

;Check to Ensure Valid Selection made for Touchplate Type.
; We check to ensure Touch Plate is not Surface Plate for XY movement Cycles.
IF <TOUCH_PLATE_CYCLE_SELECT> != 16 && <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_SURFACE_ONLY> THEN M225 <M225_MESSAGE_TIMER> "Selected cycle is incompatible with configured surface plate.\n\nSelect Z-Only cycle\nOR\nConfigure touch plate in Plate Setup(F8).\n\nPress Cycle Cancel to abort."
IF <TOUCH_PLATE_CYCLE_SELECT> != 16 && <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_SURFACE_ONLY> THEN GOTO 1

; We check to ensure Touch Plate has Bore for Bore Cycle.
IF <TOUCH_PLATE_CYCLE_SELECT> == 17 && ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_HAS_BORE>] THEN M225 <M225_MESSAGE_TIMER> "Selected cycle is incompatible with configured touch plate.\n\nSelect compatible cycle\nOR\nConfigure touch plate in Plate Setup(F8) with bore.\n\nPress Cycle Cancel to abort."
IF <TOUCH_PLATE_CYCLE_SELECT> == 17 && ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_HAS_BORE>] THEN GOTO 1

;Check Tool Diameter
;We allow Diameter to be 0 on Z-Only move.
IF <TOUCH_PLATE_CYCLE_SELECT> != 16 && <TOOL_DIAMETER_VALUE> <= 0 THEN M225 <M225_MESSAGE_TIMER> "Tool Diameter not set\nSet Tool Diameter and Try Again.\nPress Cycle Cancel to Abort."
IF <TOUCH_PLATE_CYCLE_SELECT> != 16 && <TOOL_DIAMETER_VALUE> <= 0 THEN GOTO 1

N10 ;Program Start
IF <TOUCH_PLATE_CYCLE_SELECT> != 17 THEN M201 "Jog Tool into the Center of Plate\nthen press Cycle Start."
IF <TOUCH_PLATE_CYCLE_SELECT> == 17 THEN M201 "Jog Tool into the Center of Bore\nthen press Cycle Start."

;Perform Magnet Reminder Message if Needed
IF <TOUCH_PLATE_MAGNET_REMINDER_SETTING> == 1 THEN M200 "Ensure Tool Magnet is attached before Proceeding\nPress Cycle Start to Continue."

IF #50001
;Save Initial Starting Location
<SAVED_INITIAL_AXIS_1_POSITION> = <SV_AXIS_1_MACHINE_POSITION>
<SAVED_INITIAL_AXIS_2_POSITION> = <SV_AXIS_2_MACHINE_POSITION>
<SAVED_INITIAL_AXIS_3_POSITION> = <SV_AXIS_3_MACHINE_POSITION>

;Pick Selected Cycle
IF <TOUCH_PLATE_CYCLE_SELECT> >= 0 && <TOUCH_PLATE_CYCLE_SELECT> < 8  THEN GOTO 100 ;Outside Cycles
IF <TOUCH_PLATE_CYCLE_SELECT> >= 8 && <TOUCH_PLATE_CYCLE_SELECT> < 16 THEN GOTO 200 ;Inside Cycles
IF <TOUCH_PLATE_CYCLE_SELECT> == 16 THEN GOTO 300 ;Z-Only
IF <TOUCH_PLATE_CYCLE_SELECT> == 17 THEN GOTO 400 ;Bore Cycle

GOTO 10000 ;End Macro, Selection was invalid

;------------------------------------------------------------------------------
;   Outside Cycles
;------------------------------------------------------------------------------
N100
;Determine if Corner or Side Selection
IF <TOUCH_PLATE_CYCLE_SELECT> % 2 != 0 THEN GOTO 150

N110 ;Corner Cycles

G65 "#400\system\touch_plate_cycles_outside_corner.cnc"

GOTO 10000

N150 ;Side Cycles

G65 "#400\system\touch_plate_cycles_outside_side.cnc"

GOTO 10000

;------------------------------------------------------------------------------
;   Inside Cycles
;------------------------------------------------------------------------------
N200
;Determine if Corner or Side Selection
IF <TOUCH_PLATE_CYCLE_SELECT> % 2 != 0 THEN GOTO 250

N210 ;Corner Cycles

G65 "#400\system\touch_plate_cycles_inside_corner.cnc"

GOTO 10000

N250 ;Side Cycles

G65 "#400\system\touch_plate_cycles_inside_side.cnc"

GOTO 10000

;------------------------------------------------------------------------------
;   Z-Only Cycle
;------------------------------------------------------------------------------
N300

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Point 3
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>
<TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Retract Z
G65 "#400\system\move_primitive.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<TOUCH_PLATE_PROBED_POINT_3_POSITION> + <TOUCH_PLATE_Z_CLEARANCE_VALUE>]

;Set Z Zero
G92 Z[<SV_AXIS_3_MACHINE_POSITION> - <TOUCH_PLATE_PROBED_POINT_3_MACHINE_POSITION> + <PARAM_TOUCH_PLATE_HEIGHT>]

;Move Z to Home if Needed
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

GOTO 10000

;------------------------------------------------------------------------------
;   Bore Cycle
;------------------------------------------------------------------------------
N400

;--X Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_1_LABEL>

;Begin X Negative Move
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;Begin X Positive Move
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>

;Calculate X Center
<TOUCH_PLATE_BORE_AXIS_1_CENTER_CALC> = [<TOUCH_PLATE_PROBED_POINT_1_POSITION> + <TOUCH_PLATE_PROBED_POINT_2_POSITION>] / 2

;Calculate X Width
<TOUCH_PLATE_BORE_AXIS_1_WIDTH_CALC> = abs[<TOUCH_PLATE_PROBED_POINT_1_POSITION> - <TOUCH_PLATE_PROBED_POINT_2_POSITION>] + <TOOL_DIAMETER_VALUE>

M125 /X[<TOUCH_PLATE_BORE_AXIS_1_CENTER_CALC>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;--Y Axis
;Flute Message if Needed
IF <TOUCH_PLATE_FLUTE_REMINDER_SETTING> == 1 THEN M200 "Turn flutes along the %c axis\nThen Press Cycle Start" <SV_AXIS_2_LABEL>

;Begin Y Negative Move
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Begin Y Positive Move
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>

;Calculate Y Center
<TOUCH_PLATE_BORE_AXIS_2_CENTER_CALC> = [<TOUCH_PLATE_PROBED_POINT_1_POSITION> + <TOUCH_PLATE_PROBED_POINT_2_POSITION>] / 2

;Calculate Y Width
<TOUCH_PLATE_BORE_AXIS_2_WIDTH_CALC> = abs[<TOUCH_PLATE_PROBED_POINT_1_POSITION> - <TOUCH_PLATE_PROBED_POINT_2_POSITION>] + <TOOL_DIAMETER_VALUE>

M125 /Y[<TOUCH_PLATE_BORE_AXIS_2_CENTER_CALC>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Set X0Y0 at Center
G92 X0Y0

;Return Z to Home if Set
IF <TOUCH_PLATE_Z_RETURN_HOME_SETTING> == 1 THEN G53 Z0

N10000                            ;End of Macro