;------------------------------------------------------------------------------
; Filename: set_part_axis_zero.cnc
; Description: Set Work Coordinate of specified axes at Current Location.
; Notes:
; Requires: CNC12
; Revision Date: 11 Jul 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\set_part_axis_zero.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #25014 : Software Type
;          1 = Mill
;          2 = Lathe
;
; PLC Variables:
;
; User Variables:
; #100 : Infinite Message Timer
; #101 : Timed Message
; #102 : Axis Loop Variable
; #103 : Number of Valid Axes
; #110 : Selected Axis to Set Part Zero
; #111 : 1st Axis Selection Option
; #112 : 2nd Axis Selection Option
; #113 : 3rd Axis Selection Option
; #114 : 4th Axis Selection Option
; #115 : 5th Axis Selection Option
; #116 : 6th Axis Selection Option
; #117 : 7th Axis Selection Option
; #118 : 8th Axis Selection Option
; #122 : Axis Label
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;Setup Definitions
G65 "#400\system\setup_defines.cnc"

;--Definitions
DEFINE <AXIS_LABEL>   #122

;Initialize Variables
IF #50001
#100 = 0
#101 = 3  ;3 Second Message Timer
#102 = 0
#103 = 0
#110 = 0
#111 = 0
#112 = 0
#113 = 0
#114 = 0
#115 = 0
#116 = 0
#117 = 0
#118 = 0

N100 ;Determine Number of axes
#102 = #102 + 1
IF #102 > 8 THEN GOTO 200
IF #[20100 + #102] == 78 THEN GOTO 100
IF #[9550 + #102] != 0 THEN GOTO 100 ;Skip axis if slaved

#103 = #103 + 1

IF #118 == 0 && #117 != 0 THEN #118 = #[20100 + #102]
IF #117 == 0 && #116 != 0 THEN #117 = #[20100 + #102]
IF #116 == 0 && #115 != 0 THEN #116 = #[20100 + #102]
IF #115 == 0 && #114 != 0 THEN #115 = #[20100 + #102]
IF #114 == 0 && #113 != 0 THEN #114 = #[20100 + #102]
IF #113 == 0 && #112 != 0 THEN #113 = #[20100 + #102]
IF #112 == 0 && #111 != 0 THEN #112 = #[20100 + #102]
IF #111 == 0 THEN #111 = #[20100 + #102]

GOTO 100

N200 ;Determine which Message Format to use based on # of valid Axes
;Note: User should input the Label for the axis as displayed.
;      If user inputs 1, 2 or 3 it will trick macro into thinking
;      an A, B, or C was typed.
IF #50001
#102 = 0
GOTO [200 + #103]

N201
#110 = #111
GOTO 250

N202
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n0: Set Part Zero for All" #111 #112
GOTO 250

N203                             
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n%c\n0: Set Part Zero for All" #111 #112 #113
GOTO 250

N204                             
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n%c\n%c\n0: Set Part Zero for All" #111 #112 #113 #114
GOTO 250

N205                             
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n%c\n%c\n%c\n0: Set Part Zero for All" #111 #112 #113 #114 #115
GOTO 250

N206                             
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n%c\n%c\n%c\n%c\n0: Set Part Zero for All" #111 #112 #113 #114 #115 #116
GOTO 250

N207                             
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n0: Set Part Zero for All" #111 #112 #113 #114 #115 #116 #117
GOTO 250

N208                             
M224 #110 "Input Axis Letter to Set Part Zero or 0\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n0: Set Part Zero for All" #111 #112 #113 #114 #115 #116 #117 #118
GOTO 250

N250  ;Check to Ensure Selection is Valid
IF #110 == 0 THEN GOTO 500 ;Skip to Set All Section

;Convert Value to Ascii Value
IF #110 < 65 THEN #110 = #110 + 64

#102 = 0
N251
#102 = #102 + 1
IF #102 > 8 THEN GOTO 200 ;Invalid Selection, loop back to Input Message
IF #[9550 + #102] != 0 THEN GOTO 200 ;Labled Slaved Axis Selected, Invalid
IF #110 == #[20100 + #102] THEN GOTO 300

GOTO 251 ;Continue Search

N300 ;Determine Axis Label from Selection
IF #50001

<AXIS_LABEL> = #110

N400 ;Set selected axis to 0 
IF #25014 != 2 THEN G92 $<AXIS_LABEL> 0 ;Mill/Router/Plasma/Laser
IF #25014 == 2 THEN G50 $<AXIS_LABEL> 0 ;Lathe

GOTO 1000

N500 ;Set All Axes Part 0
G65 "#400\system\set_part_all_zero.cnc"

N1000                             ;End of Macro