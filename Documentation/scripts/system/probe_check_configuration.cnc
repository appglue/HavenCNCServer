;------------------------------------------------------------------------------
; Filename: probe_check_configuration.cnc
; Description: Perform Various Checks to ensure Probe is configured properly.
; Notes:
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\probe_check_configuration.cnc"
;
; Output:
; <PROBE_CONFIG_ERROR> : Configuration Error Found
;                         0 = No Error
;                         1 = Error
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
; #1 : Argument A, Skip Verify Message.
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Setup Defines
G65 "#400\system\setup_defines.cnc"

;Set Error Flag
; Will be cleared later if no error found
<PROBE_CONFIG_ERROR> = 1

N100
;--Check Probe Input Configuration
IF <PARAM_PROBE_INPUT> == 0 THEN M222 Q1 <MESSAGE_USER_INPUT> "Touch Probe Input Not Configured\nEnsure Touch Probe is Properly Configured!\nPress any key to abort."
IF <PARAM_PROBE_INPUT> == 0 THEN GOTO 1000

IF <PARAM_PROBE_INPUT> < 50000 THEN <PROBE_INPUT> = abs[<PARAM_PROBE_INPUT>] + 50000 ELSE <PROBE_INPUT> = <PARAM_PROBE_INPUT>

;--Check Probe Detect Input Configuration
IF abs[<PARAM_PROBE_DETECT_INPUT>] < 50000 THEN <PROBE_DETECT_INPUT> = abs[<PARAM_PROBE_DETECT_INPUT>] + 50000 ELSE <PROBE_DETECT_INPUT> = abs[<PARAM_PROBE_DETECT_INPUT>]
IF <PARAM_PROBE_DETECT_INPUT> != 0 THEN IF !#[<PROBE_DETECT_INPUT>] THEN M222 Q1 <MESSAGE_USER_INPUT> "Touch Probe was not found!\nProbing Cycle canceled. Please check Touch Probe.\nPress any key to abort."
IF <PARAM_PROBE_DETECT_INPUT> != 0 THEN IF !#[<PROBE_DETECT_INPUT>] THEN GOTO 1000

;--Check Probe Tool Parameter Configuration
IF <PARAM_PROBE_TOOL_NUMBER> <= 0 THEN M222 <MESSAGE_USER_INPUT> "Probe Tool Number Parameter is not configured (P12)\nProbe Tool Number must be a positive non-zero integer.\nConfigure Probe Tool Number\nPress any key to abort."
IF <PARAM_PROBE_TOOL_NUMBER> <= 0 THEN GOTO 1000

;--Check NC Probe with No Detect has Probe Tool Loaded.
;Requires Probe Tool Probe Detection
;If Probe Tool Probe Protection is disabled (P3 Bit 3 (0 based) not set) display configuration error. Probe Input will
; not be detected in subsequent probing moves without it resulting in a crash.
;If P3 Bit 3 (0 based) is set, then check to ensure probe tool is loaded (P12).
IF [<PARAM_PROBE_DETECT_INPUT> == 0] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && ![<PARAM_TOOL_HEIGHT_CONTROL> and <TOOL_HEIGHT_CONTROL_PROBE_TOOL_DETECTION>] THEN M222 Q1 <MESSAGE_USER_INPUT> "Probe Protection based on Tool Number Not Configured\nProbe with NC Input without detect requires Tool Number Probe Protection\nPress any key to abort."
IF [<PARAM_PROBE_DETECT_INPUT> == 0] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && [<SV_TOOL_NUMBER> != <PARAM_PROBE_TOOL_NUMBER>] THEN M222 Q1 <MESSAGE_USER_INPUT> "Touch Probe was not found!\nPlease ensure that the touch probe tool is loaded.\nPress any key to abort."
IF [<PARAM_PROBE_DETECT_INPUT> == 0] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && ![<PARAM_TOOL_HEIGHT_CONTROL> and <TOOL_HEIGHT_CONTROL_PROBE_TOOL_DETECTION>] THEN GOTO 1000
IF [<PARAM_PROBE_DETECT_INPUT> == 0] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && [<SV_TOOL_NUMBER> != <PARAM_PROBE_TOOL_NUMBER>] THEN GOTO 1000

;--Check to Ensure Probe Not Tripped
;Message for NO Probes or NC Probes with Detect Configured if Tripped.
IF [#[<PROBE_INPUT>] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NO_TYPE>]] || [#[<PROBE_INPUT>] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && [<PARAM_PROBE_DETECT_INPUT> != 0]] THEN M222 Q1 <MESSAGE_USER_INPUT> "Touch Probe is tripped!\nProbing Cycle canceled. Please check Touch Probe.\nPress any key to abort."
IF [#[<PROBE_INPUT>] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NO_TYPE>]] || [#[<PROBE_INPUT>] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && [<PARAM_PROBE_DETECT_INPUT> != 0]] THEN GOTO 1000

;Message for NC Probes if Input is tripped and No Detect Configured.
IF #[<PROBE_INPUT>] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && [<PARAM_PROBE_DETECT_INPUT> == 0] THEN M222 Q1 <MESSAGE_USER_INPUT> "Touch Probe is tripped or not plugged in!\nProbing Cycle Canceled. Please check Touch Probe.\nPress any key to abort."
IF #[<PROBE_INPUT>] && [<PARAM_PROBE_INPUT_TYPE> == <PROBE_INPUT_IS_NC_TYPE>] && [<PARAM_PROBE_DETECT_INPUT> == 0] THEN GOTO 1000

;Verify Message if no Detect
IF #1 == 0 && [<PARAM_PROBE_DETECT_INPUT> == 0] && [<PARAM_PROBE_WARNING_MSG> == 1] THEN M222 <MESSAGE_USER_INPUT> "Verify Touch Probe is functioning properly.\nPress any key to continue."

<PROBE_CONFIG_ERROR> = 0  ;Clear Error Flag if no error found

N1000                             ;End of Macro