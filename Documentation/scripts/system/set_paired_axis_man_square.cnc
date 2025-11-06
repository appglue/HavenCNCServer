;------------------------------------------------------------------------------
; Filename: set_paired_axis_man_square.cnc
; Description: Helps Operator to align the paired axis gantry.
; Notes:
; Requires: CNC12 V5.40.00+
; Revision Date: 19 Aug 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
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

;Setup Defines
G65 "#400\system\setup_defines.cnc"
G65 "#400\system\setup_key_defines.cnc"

;Display "Welcome" Message
M222 Q1 <MESSAGE_USER_INPUT> "Manual Square Paired Axis Utility\n\nThis Utility will help Guide you on squaring your Paired Axis.\nPress Any Key to Begin" 

;Initialize Variables
#102 = 0
#103 = 0
#104 = 0
#110 = 0
#111 = 0
#112 = 0
#113 = 0
#114 = 0
#115 = 0
#116 = 0
#117 = 0
#118 = 0

N1 ;Determine Number of Paired Axes
#102 = #102 + 1
IF #102 > 8 THEN GOTO 10
IF #[9550 + #102] == 0 THEN GOTO 1

IF ![#104 and [2^[#[9550 + #102]]]] THEN #104 = #104 + [2^[#[9550 + #102]]] ELSE GOTO 1

#103 = #103 + 1

IF #118 == 0 && #117 != 0 THEN #118 = #[20100 + #[9550 + #102]]
IF #117 == 0 && #116 != 0 THEN #117 = #[20100 + #[9550 + #102]]
IF #116 == 0 && #115 != 0 THEN #116 = #[20100 + #[9550 + #102]]
IF #115 == 0 && #114 != 0 THEN #115 = #[20100 + #[9550 + #102]]
IF #114 == 0 && #113 != 0 THEN #114 = #[20100 + #[9550 + #102]]
IF #113 == 0 && #112 != 0 THEN #113 = #[20100 + #[9550 + #102]]
IF #112 == 0 && #111 != 0 THEN #112 = #[20100 + #[9550 + #102]]
IF #111 == 0 THEN #111 = #[20100 + #[9550 + #102]]

GOTO 1

N10 ;Determine which Message Format to use based on # of valid Axes
;Note: User should input the Label for the axis as displayed.
;      If user inputs 1, 2 or 3 it will trick macro into thinking
;      an A, B, or C was typed.
IF #50001
IF #103 == 0 THEN M200 "No Paired Axis Found\nConfigure a Paired Axis to use this Utility\nPress Cycle Cancel to Abort."
#102 = 0
GOTO [10 + #103]

N11
#110 = #111
GOTO 20

N12
M224 #110 "Input Axis Letter to Square\n%c\n%c" #111 #112
GOTO 20

N13                             
M224 #110 "Input Axis Letter to Square\n%c\n%c\n%c" #111 #112 #113
GOTO 20

N14                             
M224 #110 "Input Axis Letter to Square\n%c\n%c\n%c\n%c" #111 #112 #113 #114
GOTO 20

N15                             
M224 #110 "Input Axis Letter to Square\n%c\n%c\n%c\n%c\n%c" #111 #112 #113 #114 #115
GOTO 20

N16                             
M224 #110 "Input Axis Letter to Square\n%c\n%c\n%c\n%c\n%c\n%c" #111 #112 #113 #114 #115 #116
GOTO 20

N17                             
M224 #110 "Input Axis Letter to Square\n%c\n%c\n%c\n%c\n%c\n%c\n%c" #111 #112 #113 #114 #115 #116 #117
GOTO 20

N18                             
M224 #110 "Input Axis Letter to Square\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n%c" #111 #112 #113 #114 #115 #116 #117 #118
GOTO 20

N20 ;Check to Ensure Selection is Valid
IF #110 < 65 THEN #110 = #110 + 64

N100 ;Start Manual Square Process

M201 "Jog the %c axis to safe position.\nPress Cycle Start to un-pair the axis" #110

M294 /$#110

M201 "%c axis is un-paired!\n\nJog the Master Motor to Square Position\nUse Incremental or WMPG for fine adjustments\nPress Cycle Start to re-pair axis" #110

M295 /$#110

M201 "%c axis is now paired\nPress Cycle Start to Complete Manual Axis Squaring". #110

N1000                             ;End of Macro