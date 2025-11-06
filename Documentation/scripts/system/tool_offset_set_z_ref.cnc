;------------------------------------------------------------------------------
; Filename: tool_offset_set_z_ref.cnc
; Description: CNC12 Tool Library Set Z-Ref Macro
; Notes:
; Requires: CNC12 V5.18+
; Revision Date: 25 Aug 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\tool_offset_set_z_ref.cnc"
;
; Output:
; #32000 : Probing Error Flag
; #34574 : Probed or Current X WCS Position
; #34575 : Probed or Current Y WCS Position
; #34576 : Probed or Current Z WCS Position
;------------------------------------------------------------------------------
; Parameters:
; #9011 : Touch Probe Input
; #9012 : Touch Probe Tool Number
; #9018 : Touch Probe Detect Input
; #9044 : Tool Touch Input
; #9257 : Tool Touch Detect Input
; #9406 : Probe Input Type
;           0 = NO Probe (Closes when Tripped)
;           1 = NC Probe (Opens when Tripped)
; #9407 : Tool Touch Type
;           0 = NO Probe (Closes when Tripped)
;           1 = NC Probe (Opens when Tripped)
; #9410 : Probe Warning
;           0 = Disabled
;           1 = Enabled
; #9540 : Touch Plate Input
;
; System Variables:
; #4120 : Tool Number (T)
;
; PLC Variables:
;
; User Variables:
; #101 : Input Device Configured
;         Bit 0 : Touch Probe Configured
;         Bit 1 : Tool Touch Configured
;         Bit 2 : Touch Plate Configured
;
; #103 : Touch Probe Detect Input
; #104 : Tool Touch Detect Input
; #105 : Touch Probe Input
; #106 : Tool Touch Input
;
; #32003 : User Input
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Setup Define Variables for Keys
G65 "#400\system\setup_key_defines.cnc"

N1;Determine Touch Devices Configured
; Assume if Trip or Detect input is defined that the device exists.
IF #9011 != 0 || #9018 != 0 THEN #101 = #101 + 1  ;Touch Probe
IF #9044 != 0 || #9257 != 0 THEN #101 = #101 + 2  ;Tool Touch
IF #9540 != 0 || #9541 != 0 THEN #101 = #101 + 4  ;Touch Plate

N2;Display Selection Message Based on Touch Devices Connected
IF ![#101 and 1] && ![#101 and 2] && ![#101 and 4] THEN M222 Q1 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    4 = Set Z Reference Here"
IF  [#101 and 1] && ![#101 and 2] && ![#101 and 4] THEN M222 Q2 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    3 = Seek to Touch Probe\n    4 = Set Z Reference Here"
IF ![#101 and 1] &&  [#101 and 2] && ![#101 and 4] THEN M222 Q3 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    1 = Seek to Tool Touch\n    4 = Set Z Reference Here"
IF ![#101 and 1] && ![#101 and 2] &&  [#101 and 4] THEN M222 Q4 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    2 = Seek to Touch Plate\n    4 = Set Z Reference Here"
IF  [#101 and 1] &&  [#101 and 2] && ![#101 and 4] THEN M222 Q5 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    1 = Seek to Tool Touch\n    3 = Seek to Touch Probe\n    4 = Set Z Reference Here"
IF ![#101 and 1] &&  [#101 and 2] &&  [#101 and 4] THEN M222 Q6 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    1 = Seek to Tool Touch\n    2 = Seek to Touch Plate\n    4 = Set Z Reference Here"
IF  [#101 and 1] && ![#101 and 2] &&  [#101 and 4] THEN M222 Q7 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    2 = Seek to Touch Plate\n    3 = Seek to Touch Probe\n    4 = Set Z Reference Here"
IF  [#101 and 1] &&  [#101 and 2] &&  [#101 and 4] THEN M222 Q8 #32003 "#)  Establish the Z Reference\n- Load the Reference Tool\n- Jog to the Reference Position\n- Select Following Options\n    1 = Seek to Tool Touch\n    2 = Seek to Touch Plate\n    3 = Seek to Touch Probe\n    4 = Set Z Reference Here"

;--Determine Selection
;Prevent Invalid Selections of Devices not Found
IF ![#101 and 1] && #32003 == <KB_3> THEN GOTO 2 ;Invalid Selection if Probe Selected and None Detected
IF ![#101 and 2] && #32003 == <KB_1> THEN GOTO 2 ;Invalid Selection if TT Selected and None Detected
IF ![#101 and 4] && #32003 == <KB_2> THEN GOTO 2 ;Invalid Selection if Touch Plate Selected and None Detected

;Goto Valid Selection otherwise, go back to selection message.
IF #32003 == <KB_3> THEN GOTO 100 ;Probe Selected
IF #32003 == <KB_1> THEN GOTO 200 ;TT Selected
IF #32003 == <KB_2> THEN GOTO 300 ;Touch Plate Selected
IF #32003 == <KB_4> THEN GOTO 400 ;Set Reference Here
IF #32003 != <KB_1> && #32003 != <KB_2> && #32003 != <KB_3> && #32003 != <KB_4> THEN GOTO 2 ;Invalid Selection go back to Message

;------------------------------------------------------------------------------
;   Seek with Touch Probe Device
;------------------------------------------------------------------------------
N100
;Check Probe Configuration
G65 "#400\system\probe_check_configuration.cnc"
IF <PROBE_CONFIG_ERROR> == 1 THEN GOTO 1000 ;End Program if error found

M201 "Press CYCLE START to begin Auto Probing Z-Ref Location"

#34589 = 3 ;Set Variable for Axis 3
#34590 = -1 ;Set Axis Move Direction
#34592 = 0  ;Set Probe is Used

;Perform Probe Move to Surface
G65 "#400\system\probe_move_to_surface.cnc"

#32001=1 ; disable probe protection
M25 ;Return to Z Home
#32001=0 ; re-enable probe protection

GOTO 1000

;------------------------------------------------------------------------------
;   Seek with Tool Touch Device
;------------------------------------------------------------------------------
N200
;Check Tool Touch Input Configuration
; For Tool Touch if P44 is not defined, then use P11.
IF #9044 == 0 THEN IF #9011 == 0 THEN M222 #32003 "Tool Touch Input Not Configured\n Ensure Tool Touch is Properly Configured!\nPress any key to abort."

;Check Tool Touch Detect Input Configuration
IF abs[#9257] < 50000 THEN #104 = abs[#9257] + 50000 ELSE #104 = abs[#9257]
IF #9257 != 0 THEN IF !#[#104] THEN M222 Q1 #32003 "Tool Touch was not found!\nProbing Cycle canceled. Please check Tool Touch.\nPress any key to continue"
IF #9257 != 0 THEN IF !#[#104] THEN GOTO 1000

;--Check to Ensure Tool Touch Not Tripped
; For Tool Touch if P44 is not defined, then use P11.
IF #9044 != 0 && #9044 < 50000 THEN #106 = abs[#9044] + 50000 ELSE #106 = abs[#9044]
IF #9044 == 0 && #9011 < 50000 THEN #106 = abs[#9011] + 50000
IF #9044 == 0 && #9011 > 50000 THEN #106 = abs[#9011]

;Message for NO Tool Touches or NC Tool Touches with Detect Configured if Tripped.
IF [#[#106] && [#9407 == 0]] || [#[#106] && [#9407 == 1] && [#9257 != 0]] THEN M222 Q1 #32003 "Tool Touch is tripped!\nProbing Cycle canceled. Please check Tool Touch.\nPress any key to abort."
IF [#[#106] && [#9407 == 0]] || [#[#106] && [#9407 == 1] && [#9257 != 0]] THEN GOTO 1000

;Message for NC Probes if Input is tripped and No Detect Configured.
IF #[#106] && [#9407 == 1] && [#9257 == 0] THEN M222 Q1 #32003 "Tool Touch is tripped or not plugged in!\nProbing Cycle Canceled. Please check Tool Touch.\nPress any key to abort."
IF #[#106] && [#9407 == 1] && [#9257 == 0] THEN GOTO 1000

;Verify Message if no Detect
IF #9257 == 0 && #9410 == 1 THEN M222 Q1 #32003 "Verify Tool Touch is functioning properly.\nPress any key to continue."

M201 "Press CYCLE START to begin Auto Tool Touch Z-Ref Location"

;Activate AirBlowNozzle for TT, Un-Comment line to add function.
;M94 /36

#34589 = 3 ;Set Variable for Axis 3
#34590 = -1 ;Set Axis Move Direction
#34592 = 1  ;Set TT is Used

;Perform Probe Move to Surface
G65 "#400\system\probe_move_to_surface.cnc"

;De-Activate AirBlowNozzle for TT, Un-Comment line to add function.
;M95 /36

#32001=1 ; disable probe protection
M25 ;Return to Z Home
#32001=0 ; re-enable probe protection

GOTO 1000

;------------------------------------------------------------------------------
;   Seek with Touch Plate Device
;------------------------------------------------------------------------------
N300 ;Seek with Touch Plate Device
;Get Variable Values and Defines and Error Checking
G65 "#400\system\touch_plate_get_variables.cnc"

;Verify Message if no Detect
IF <PARAM_TOUCH_PLATE_DETECT_INPUT> == 0 && #9410 == 1 THEN M222 Q1 #32003 "Verify Touch Plate is functioning properly.\nPress any key to continue."

M201 "Press CYCLE START to begin Auto Touch Plate Z-Ref Location"

;--Touch Off Plate in Z
IF <SV_TOOL_NUMBER> <= 200 THEN G43 H<SV_TOOL_NUMBER>  ;Set Tool Height

;Begin Z Probing Moves
G65 "#400\system\touch_plate_move.cnc" x[<SV_AXIS_1_WCS_POSITION>] y[<SV_AXIS_2_WCS_POSITION>] z[<SV_AXIS_3_TOUCH_PLATE_MINUS_LIMIT>]

;Save Points
<TOUCH_PLATE_PROBED_POINT_1_POSITION> = <TOUCH_PLATE_PROBED_AXIS_1_MACHINE_POSITION>
<TOUCH_PLATE_PROBED_POINT_2_POSITION> = <TOUCH_PLATE_PROBED_AXIS_2_MACHINE_POSITION>
<TOUCH_PLATE_PROBED_POINT_3_POSITION> = <TOUCH_PLATE_PROBED_AXIS_3_MACHINE_POSITION>

;Save Points to Variables for CNC12
#34574 = <TOUCH_PLATE_PROBED_AXIS_1_WCS_POSITION>
#34575 = <TOUCH_PLATE_PROBED_AXIS_2_WCS_POSITION>
#34576 = <TOUCH_PLATE_PROBED_AXIS_3_WCS_POSITION>

#32000 = 0 ;Clear Probe Error Flag

#32001=1 ; disable probe protection
M25 ;Return to Z Home
#32001=0 ; re-enable probe protection

GOTO 1000

;------------------------------------------------------------------------------
;   Set Z-Ref Here
;------------------------------------------------------------------------------
N400
#34574 = #5041 ;Current X Axis WCS Position
#34575 = #5042 ;Current Y Axis WCS Position
#34576 = #5043 ;Current Z Axis WCS Position

#32000 = 0 ;Clear Probe Error Flag

GOTO 1000

N1000                            ;End of Macro