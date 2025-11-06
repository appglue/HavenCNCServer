;------------------------------------------------------------------------------
; Filename: touch_plate_get_variables.cnc
; Description: Setup Variables for Touch Plate Macros
; Notes: Setup Defines, Variables, and Error Checks Configuration.
; Requires: CNC12 V5.18+
; Revision Date: 2 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\touch_plate_get_variables.cnc"
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

;Setup Macro Defines
G65 "#400\system\setup_defines.cnc"

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;Set Message Timer
<M225_MESSAGE_TIMER> = 0

;--Check Touch Plate Configuration
;Check Input Configuration
N1
IF <PARAM_TOUCH_PLATE_INPUT> == 0 THEN M225 <M225_MESSAGE_TIMER> "Touch Plate Input Not Configured\nEnsure Touch Plate is Properly Configured in the Wizard!\nPress Cycle Cancel to Abort."
IF <PARAM_TOUCH_PLATE_INPUT> == 0 THEN GOTO 1

;Assign Touch Plate Input
IF abs[<PARAM_TOUCH_PLATE_INPUT>] < 50000 THEN <TOUCH_PLATE_INPUT> = abs[<PARAM_TOUCH_PLATE_INPUT>] + 50000 ELSE <TOUCH_PLATE_INPUT> = abs[<PARAM_TOUCH_PLATE_INPUT>]

;Check Detect Input
N2
IF abs[<PARAM_TOUCH_PLATE_DETECT_INPUT>] < 50000 THEN <TOUCH_PLATE_DETECT_INPUT> = abs[<PARAM_TOUCH_PLATE_DETECT_INPUT>] + 50000 ELSE <TOUCH_PLATE_DETECT_INPUT> = abs[<PARAM_TOUCH_PLATE_DETECT_INPUT>]
IF <PARAM_TOUCH_PLATE_DETECT_INPUT> != 0 THEN IF !#[<TOUCH_PLATE_DETECT_INPUT>] THEN M225 <M225_MESSAGE_TIMER> "Touch Plate Detect Not Found\nEnsure Touch Plate is Properly Connected!\nPress Cycle Start to Retry or Cycle Cancel to Abort."
IF <PARAM_TOUCH_PLATE_DETECT_INPUT> != 0 THEN IF !#[<TOUCH_PLATE_DETECT_INPUT>] THEN GOTO 2

;Check Touch Plate Input Tripped
; Performed After Detect in case of NC Type.
N3
;Check NO Type or NC Type with Detect for Tripped Input.
IF #[<TOUCH_PLATE_INPUT>] THEN IF [<PARAM_TOUCH_PLATE_INPUT_TYPE> == <TOUCH_PLATE_INPUT_IS_NO_TYPE>] || [<PARAM_TOUCH_PLATE_INPUT_TYPE> == <TOUCH_PLATE_INPUT_IS_NC_TYPE> && <PARAM_TOUCH_PLATE_DETECT_INPUT> != 0] THEN M225 <M225_MESSAGE_TIMER> "Touch Plate is Tripped!\nCheck Touch Plate.\nPress Cycle Start to Retry or Cycle Cancel to Abort."
IF #[<TOUCH_PLATE_INPUT>] THEN IF [<PARAM_TOUCH_PLATE_INPUT_TYPE> == <TOUCH_PLATE_INPUT_IS_NO_TYPE>] || [<PARAM_TOUCH_PLATE_INPUT_TYPE> == <TOUCH_PLATE_INPUT_IS_NC_TYPE> && <PARAM_TOUCH_PLATE_DETECT_INPUT> != 0] THEN GOTO 3

;Check NC Type without detect for Tripped Input.
IF #[<TOUCH_PLATE_INPUT>] && <PARAM_TOUCH_PLATE_INPUT_TYPE> == <TOUCH_PLATE_INPUT_IS_NC_TYPE> && <PARAM_TOUCH_PLATE_DETECT_INPUT> == 0 THEN M225 <M225_MESSAGE_TIMER> "Touch Plate is tripped or not plugged in!\nCheck Touch Plate.\nPress Cycle Start to Retry or Cycle Cancel to Abort."
IF #[<TOUCH_PLATE_INPUT>] && <PARAM_TOUCH_PLATE_INPUT_TYPE> == <TOUCH_PLATE_INPUT_IS_NC_TYPE> && <PARAM_TOUCH_PLATE_DETECT_INPUT> == 0 THEN GOTO 3

N1000                            ;End of Macro