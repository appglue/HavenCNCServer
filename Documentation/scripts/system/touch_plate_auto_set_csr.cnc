;------------------------------------------------------------------------------
; Filename: touch_plate_auto_set_csr.cnc
; Description: Select Touch Plate Set CSR Cycle to Call.
; Notes: Called by CNC12 for Touch Plate Auto CSR.
; Requires: CNC12 V5.18+
; Revision Date: 9 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\touch_plate_cycles_select.cnc"
;
; Input:
; <TOUCH_PLATE_CYCLE_CSR_SELECT>  : Touch Plate CSR Select
;                                   0  = X+
;                                   1  = Y+
;                                   2  = X-
;                                   3  = Y-
;
; <TOUCH_PLATE_CSR_RETRACT_VALUE> : Probe Retract Distance
;
; Output:
; <PROBING_ERROR_FLAG>                : Probing Error Flag
; <TOUCH_PLATE_CSR_X_PROBED_POSITION> : X Probed Position
; <TOUCH_PLATE_CSR_Y_PROBED_POSITION> : Y Probed Position
; <TOUCH_PLATE_CSR_Z_PROBED_POSITION> : Z Probed Position
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

;Check Configuration and get Variable Values.
G65 "#400\system\touch_plate_get_variables.cnc"

;Message to remind operator to position tool.
IF <PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL> THEN M201 "Ensure Tool is in Center of Plate\npress Cycle Start to begin probing."
IF ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL>] THEN M201 "Ensure Tool is in Position\npress Cycle Start to begin probing."

N10 ;Program Start
IF #50001
;Save Initial Starting Location
<SAVED_INITIAL_AXIS_1_POSITION> = <SV_AXIS_1_WCS_POSITION>
<SAVED_INITIAL_AXIS_2_POSITION> = <SV_AXIS_2_WCS_POSITION>
<SAVED_INITIAL_AXIS_3_POSITION> = <SV_AXIS_3_WCS_POSITION>

;Pick Selected Cycle
IF <TOUCH_PLATE_CYCLE_CSR_SELECT> == 0 THEN GOTO 100
IF <TOUCH_PLATE_CYCLE_CSR_SELECT> == 1 THEN GOTO 200
IF <TOUCH_PLATE_CYCLE_CSR_SELECT> == 2 THEN GOTO 300
IF <TOUCH_PLATE_CYCLE_CSR_SELECT> == 3 THEN GOTO 400

;------------------------------------------------------------------------------
;   X+ Cycle
;------------------------------------------------------------------------------
N100
;Check if External/Internal Touch Plate
; Skip Z-Move if External
IF ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL>] THEN GOTO 110

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

N110
;--Touch Off Plate in X Axis
;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_PLUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Probed XYZ Positions
<TOUCH_PLATE_CSR_X_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Y_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Z_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Retract X by Specified CSR Retract Distance
M125 /X[<TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION> + [<TOUCH_PLATE_CSR_RETRACT_VALUE> * -1]] /Y[<SV_AXIS_2_WCS_POSITION>] /Z[<SV_AXIS_3_WCS_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Move Z to Initial Height
M125 /Z[<SAVED_INITIAL_AXIS_3_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

<PROBING_ERROR_FLAG> = 0  ;clear probing error flag

GOTO 10000

;------------------------------------------------------------------------------
;   Y+ Cycle
;------------------------------------------------------------------------------
N200
;Check if External/Internal Touch Plate
; Skip Z-Move if External
IF ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL>] THEN GOTO 210

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

N210
;--Touch Off Plate in Y Axis
;Begin Y Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_PLUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Probed XYZ Positions
<TOUCH_PLATE_CSR_X_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Y_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Z_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Retract Y by Specified CSR Retract Distance
M125 /X[<SV_AXIS_1_WCS_POSITION>] /Y[<TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION> + [<TOUCH_PLATE_CSR_RETRACT_VALUE> * -1]] /Z[<SV_AXIS_3_WCS_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Move Z to Initial Height
M125 /Z[<SAVED_INITIAL_AXIS_3_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

<PROBING_ERROR_FLAG> = 0  ;clear probing error flag

GOTO 10000

;------------------------------------------------------------------------------
;   X- Cycle
;------------------------------------------------------------------------------
N300
;Check if External/Internal Touch Plate
; Skip Z-Move if External
IF ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL>] THEN GOTO 310

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

N310
;--Touch Off Plate in X Axis
;Begin X Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_TOUCH_PLATE_MINUS_LIMIT>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Probed XYZ Positions
<TOUCH_PLATE_CSR_X_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Y_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Z_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Retract X by Specified CSR Retract Distance
M125 /X[<TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION> + [<TOUCH_PLATE_CSR_RETRACT_VALUE> * 1]] /Y[<SV_AXIS_2_WCS_POSITION>] /Z[<SV_AXIS_3_WCS_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Move Z to Initial Height
M125 /Z[<SAVED_INITIAL_AXIS_3_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

<PROBING_ERROR_FLAG> = 0  ;clear probing error flag

GOTO 10000

;------------------------------------------------------------------------------
;   Y- Cycle
;------------------------------------------------------------------------------
N400
;Check if External/Internal Touch Plate
; Skip Z-Move if External
IF ![<PARAM_TOUCH_PLATE_ATTRIBUTES> and <TOUCH_PLATE_IS_INTERNAL>] THEN GOTO 410

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

N410
;--Touch Off Plate in Y Axis
;Begin Y Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_TOUCH_PLATE_MINUS_LIMIT>] z[<SV_AXIS_3_WCS_POSITION>]

;Save Probed XYZ Positions
<TOUCH_PLATE_CSR_X_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Y_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_MACHINE_POSITION>
<TOUCH_PLATE_CSR_Z_PROBED_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Retract Y by Specified CSR Retract Distance
M125 /X[<SV_AXIS_1_WCS_POSITION>] /Y[<TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION> + [<TOUCH_PLATE_CSR_RETRACT_VALUE> * 1]] /Z[<SV_AXIS_3_WCS_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

;Move Z to Initial Height
M125 /Z[<SAVED_INITIAL_AXIS_3_POSITION>] P[<TOUCH_PLATE_INPUT>] F[<PARAM_PROBE_TRAVERSE_RATE>]

<PROBING_ERROR_FLAG> = 0  ;clear probing error flag

GOTO 10000

N10000                            ;End of Macro