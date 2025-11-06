;------------------------------------------------------------------------------
; Filename: perform_tool_change.cnc
; Description: Tool Change Request from a CNC12 Menu.
; Notes: Will Move to G28 Position if Manual Tool Changer.
;         Otherwise, will run M6 macro for ATC.
; Requires: CNC12 V5.29.00+
; Revision Date: 25 Oct 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\perform_tool_change.cnc"
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

;Setup Macro Defines
G65 "#400\system\setup_defines.cnc"

<M225_MESSAGE_TIMER> = 0

;Ask Operator what tool to change too.
IF <PARAM_ATC_INSTALLED> == 0 THEN M224 <MESSAGE_USER_INPUT> " Operator Manual Tool Change \n\n#) Enter Tool Number and press Cycle Start to continue. \n\n#)Input desired Tool #:"
IF <PARAM_ATC_INSTALLED> != 0 THEN M224 <MESSAGE_USER_INPUT> " Perform Automatic Tool Change \n\n Enter Tool Number and Press Cycle Start. \n (Machine will Put away the tool currently in the spindle \n and go Get the specified tool number.) \n\n#)Get Tool #: "

;Move to Tool Check Position if Manual Tool Changer
; Skip message if already at G28 Position.
; Comment out following lines to remove G28 Move.
IF <PARAM_ATC_INSTALLED> == 0 && <SV_AXIS_3_MACHINE_POSITION> != <SV_AXIS_3_RETURN_POINT_1> THEN M225 <M225_MESSAGE_TIMER> "Moving to Tool Check Height (G28)\nPress Cycle Start to Begin."
IF <PARAM_ATC_INSTALLED> == 0 THEN G28 G91 Z0

;Display Message for Operator to Insert tool if Manual Tool Changer.
IF <PARAM_ATC_INSTALLED> == 0 THEN M225 <M225_MESSAGE_TIMER> "Insert Tool: %.0f into the Spindle \n\n then press Cycle Start to apply the matching Tool Height Offset \n(there will be no automatic motion)." <MESSAGE_USER_INPUT>

;Perform Tool Change
; Call M6 if Automatic Tool Changer
; Otherwise just call T#
IF <PARAM_ATC_INSTALLED> == 0 THEN T[<MESSAGE_USER_INPUT>]
IF <PARAM_ATC_INSTALLED> != 0 THEN T[<MESSAGE_USER_INPUT>] M6

;Set Tool Height Compensation if Tool Height Retention is Enabled.
IF <PARAM_TOOL_HEIGHT_CONTROL> and <TOOL_HEIGHT_CONTROL_RETENTION> THEN G43 H[<MESSAGE_USER_INPUT>]

N1000                            ;End of Macro