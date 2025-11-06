;------------------------------------------------------------------------------
; Filename: tooltouch_check_configuration.cnc
; Description: Perform Various Checks to ensure TT is configured properly.
; Notes:
; Requires: CNC12 V4.50.00+
; Revision Date: 22 Aug 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\tt_check_configuration.cnc" A[SkipVerify]
; Arguments:  SkipVerify = 1 Skip Verify Message
;
; Output:
; <PROBE_CONFIG_ERROR> : Configuration Error Found
;                         0 = No Error
;                         1 = Error
;------------------------------------------------------------------------------
; Parameters:
; #9011 : Probe Input
; #9014 : Fast Probing Rate
; #9015 : Slow Probing Rate
; #9043 : TT1 Options
;         +1 : Parameter 71 will be subtracted from height
; #9044 : TT Input
; #9071 : TT Height
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
; #104
; #33001 : TT Input
; #33002 : Fast Probing Rate
; #33003 : Slow Probing Rate
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Setup Defines
G65 "#400\system\setup_defines.cnc"

;Set Error Flag
; Will be cleared later if no error found
<TOOLTOUCH_CONFIG_ERROR> = 1

N100
;--Check Tool Touch Input Configuration
; For Tool Touch if P44 is not defined, then use P11.
IF <PARAM_TOOLTOUCH_INPUT> == 0 && <PARAM_PROBE_INPUT> == 0 THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch Input Not Configured\n Ensure Tool Touch is Properly Configured!\nPress any key to abort."
IF <PARAM_TOOLTOUCH_INPUT> == 0 && <PARAM_PROBE_INPUT> == 0 THEN GOTO 1000

;Assign Inputs
; Convert Input to 50000 variable if it is not already.
IF <PARAM_TOOLTOUCH_INPUT> != 0 THEN <TOOLTOUCH_INPUT> = abs[<PARAM_TOOLTOUCH_INPUT>] ELSE <TOOLTOUCH_INPUT> = abs[<PARAM_PROBE_INPUT>]
IF <TOOLTOUCH_INPUT> < 50000 THEN <TOOLTOUCH_INPUT> = <TOOLTOUCH_INPUT> + 50000

;ACORN Only, Check to ensure on Inputs 1-8
IF <SV_PLC_DEVICE_ID> == <PLC_DEVICE_ID_IS_ACORN> && [<TOOL_TOUCH_INPUT> < 50001 || <TOOL_TOUCH_INPUT> > 50008] THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch Input Configured is invalid.\nEnsure Tool Touch Input is on inputs between 1 and 8\nPress any key to abort."
IF <SV_PLC_DEVICE_ID> == <PLC_DEVICE_ID_IS_ACORN> && [<TOOL_TOUCH_INPUT> < 50001 || <TOOL_TOUCH_INPUT> > 50008] THEN GOTO 1000

;Check Tool Touch Detect Input Configuration
IF <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 && abs[<PARAM_TOOLTOUCH_DETECT_INPUT>] < 50000 THEN <TOOLTOUCH_DETECT_INPUT> = abs[<PARAM_TOOLTOUCH_DETECT_INPUT>] + 50000 ELSE <TOOLTOUCH_DETECT_INPUT> = abs[<PARAM_TOOLTOUCH_DETECT_INPUT>]
IF <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 THEN IF !#[<TOOLTOUCH_DETECT_INPUT>] THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch was not found!\nEnsure Tool Touch is Properly Connected!\nPress any key to continue"
IF <PARAM_TOOLTOUCH_DETECT_INPUT> != 0 THEN IF !#[<TOOLTOUCH_DETECT_INPUT>] THEN GOTO 1000

;--Check to Ensure Tool Touch Not Tripped
;Message for NO Tool Touches or NC Tool Touches with Detect Configured if Tripped.
IF [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == <TOOLTOUCH_INPUT_IS_NO_TYPE>]] || [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> != 0]] THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch is tripped!\nProbing Cycle canceled. Please check Tool Touch.\nPress any key to abort."
IF [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == <TOOLTOUCH_INPUT_IS_NO_TYPE>]] || [#[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> != 0]] THEN GOTO 1000

;Message for NC Probes if Input is tripped and No Detect Configured.
IF #[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> == 0] THEN M222 Q1 <MESSAGE_USER_INPUT> "Tool Touch is tripped or not plugged in!\nProbing Cycle Canceled. Please check Tool Touch.\nPress any key to abort."
IF #[<TOOLTOUCH_INPUT>] && [<PARAM_TOOLTOUCH_INPUT_TYPE> == 1] && [<PARAM_TOOLTOUCH_DETECT_INPUT> == 0] THEN GOTO 1000

;Verify Message if no Detect
IF #1 == 0 && [<PARAM_TOOLTOUCH_DETECT_INPUT == 0] && [<PARAM_PROBE_WARNING_MSG> == 1] THEN M222 <MESSAGE_USER_INPUT> "Verify Toll Touch is functioning properly.\nPress any key to continue."

<TOOLTOUCH_CONFIG_ERROR> = 0 ;Clear Error Flag if no error found

N1000                            ;End of Macro