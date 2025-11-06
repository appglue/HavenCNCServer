;------------------------------------------------------------------------------
; Filename: Axis_Calibration.cnc
; Description: Calcuates the "Overall Turns Ratio" for Each Axis.
; Notes:
; Overall Turns Ratio in inches: is the number of turns of the Axis motor to produce 1" of machine travel.
; Overall Turns Ratio in mm: is the mm amount the machine moves for one revolution of the axis motor.
; The "Steps per revolution" (number of steps per 1 revolution of the axis motor) 
;   must be set in Wizard to match the Drive steps per revolution for this macro to work.
; See this thread for more information on Steps/Counts per Revolution.
; https://centroidcncforum.com/viewtopic.php?f=63&t=1801
; Requires: CNC12 V5.39.00+
; Revision Date: 11 Jul 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Parameters:
; #9064 : Axis Pairing
;
; System Variables:
; #4006 : Units (Imperial or Metric)
;
; PLC Variables:
;
; User Variables:
; #100 : Infinite Message Timer
; #101 : Timed Message
; #102 : Axis Loop Variable
; #103 : Number of Valid Axes
; #110 : Selected Axis to Measure
; #111 : 1st Axis Selection Option
; #112 : 2nd Axis Selection Option
; #113 : 3rd Axis Selection Option
; #114 : 4th Axis Selection Option
; #115 : 5th Axis Selection Option
; #116 : 6th Axis Selection Option
; #117 : 7th Axis Selection Option
; #118 : 8th Axis Selection Option
; #120 : User Measurement Input
; #121 : Calculated Axis Pitch Multiplier
; #122 : Axis Label
; #123 : Axis Pitch Value
; #124 : Axis DRO Value
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

;--Definitions
DEFINE <UNITS> #4006
DEFINE <SLAVED_AXIS_VALUE> #9064

DEFINE <AXIS_1_DRO>   #5021
DEFINE <AXIS_2_DRO>   #5022
DEFINE <AXIS_3_DRO>   #5023
DEFINE <AXIS_4_DRO>   #5024
DEFINE <AXIS_5_DRO>   #5025
DEFINE <AXIS_6_DRO>   #5026
DEFINE <AXIS_7_DRO>   #5027
DEFINE <AXIS_8_DRO>   #5028

DEFINE <AXIS_1_LABEL> #20101
DEFINE <AXIS_2_LABEL> #20102
DEFINE <AXIS_3_LABEL> #20103
DEFINE <AXIS_4_LABEL> #20104
DEFINE <AXIS_5_LABEL> #20105
DEFINE <AXIS_6_LABEL> #20106
DEFINE <AXIS_7_LABEL> #20107
DEFINE <AXIS_8_LABEL> #20108

DEFINE <AXIS_1_PITCH> #20401
DEFINE <AXIS_2_PITCH> #20402
DEFINE <AXIS_3_PITCH> #20403
DEFINE <AXIS_4_PITCH> #20404
DEFINE <AXIS_5_PITCH> #20405
DEFINE <AXIS_6_PITCH> #20406
DEFINE <AXIS_7_PITCH> #20407
DEFINE <AXIS_8_PITCH> #20408

DEFINE <MEASUREMENT>  #120
DEFINE <MULTIPLIER>   #121
DEFINE <AXIS_LABEL>   #122
DEFINE <AXIS_PITCH>   #123
DEFINE <AXIS_DRO>     #124

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

N100

M225 #100 "Axis Pitch (Overall Turns Ratio) Calibration\nNote: Steps per Revolution must be set properly for Axis Calibration.\n(More Information can be found in Tech Bulletin 36)\n\nPress Cycle Start to Continue"

N150 ;Determine Number of axes
#102 = #102 + 1
IF #102 > 8 THEN GOTO 160
IF #[20100 + #102] == 78 THEN GOTO 150
IF #[9550 + #102] != 0 THEN GOTO 150 ;Skip axis if slaved

#103 = #103 + 1

IF #118 == 0 && #117 != 0 THEN #118 = #[20100 + #102]
IF #117 == 0 && #116 != 0 THEN #117 = #[20100 + #102]
IF #116 == 0 && #115 != 0 THEN #116 = #[20100 + #102]
IF #115 == 0 && #114 != 0 THEN #115 = #[20100 + #102]
IF #114 == 0 && #113 != 0 THEN #114 = #[20100 + #102]
IF #113 == 0 && #112 != 0 THEN #113 = #[20100 + #102]
IF #112 == 0 && #111 != 0 THEN #112 = #[20100 + #102]
IF #111 == 0 THEN #111 = #[20100 + #102]

GOTO 150

N160 ;Determine which Message Format to use based on # of valid Axes
;Note: User should input the Label for the axis as displayed.
;      If user inputs 1, 2 or 3 it will trick macro into thinking
;      an A, B, or C was typed.
IF #50001
#102 = 0
GOTO [160 + #103]

N161
#110 = #111
GOTO 170

N162
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n0: Calibrate All" #111 #112
GOTO 170

N163                             
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n%c\n0: Calibrate All" #111 #112 #113
GOTO 170

N164                             
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n%c\n%c\n0: Calibrate All" #111 #112 #113 #114
GOTO 170

N165                             
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n%c\n%c\n%c\n0: Calibrate All" #111 #112 #113 #114 #115
GOTO 170

N166                             
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n%c\n%c\n%c\n%c\n0: Calibrate All" #111 #112 #113 #114 #115 #116
GOTO 170

N167                             
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n0: Calibrate All" #111 #112 #113 #114 #115 #116 #117
GOTO 170

N168                             
M224 #110 "Input Axis Letter to Calibrate or 0\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n%c\n0: Calibrate All" #111 #112 #113 #114 #115 #116 #117 #118
GOTO 170

N170  ;Check to Ensure Selection is Valid
IF #110 == 0 THEN GOTO 500 ;Skip to Calibrate All Section

;Convert Value to Ascii Value
IF #110 < 65 THEN #110 = #110 + 64

#102 = 0
N171
#102 = #102 + 1
IF #102 > 8 THEN GOTO 160 ;Invalid Selection, loop back to Input Message
IF #110 == #[20100 + #102] THEN GOTO 200

GOTO 171 ;Continue Search

N200 ;Determine Axis Label from Selection
IF #50001

<AXIS_LABEL> = #110

N210 ;Determine Axis Pitch
<AXIS_PITCH> = #[20400 + #102]

N220 ;Move Axis to Home
M225 #100 "Machine will now move the %c Axis to Home Position\nPress Cycle Start to Begin Movement" <AXIS_LABEL>

G53 $<AXIS_LABEL> 0

N300 ;Course Adjustment

M201 "1. Physically Locate/Mark Current Location\n2. Jog Axis %c away from this location\nNote: The more distance the better\n\nPress Cycle Start once Complete" <AXIS_LABEL>

N310
M224 <MEASUREMENT> "Physically Measure Distance Traveled as accurate as possible and input your\n#)measurement Value\nNote: DO NOT USE THE CNC12 DRO POSITION FOR MEASUREMENT"
IF <MEASUREMENT> == 0 THEN GOTO 310

;Calculate New Pitch for Axis
IF #50001
<AXIS_DRO> = #[5020 + #102]

;Ensure Axis DRO is not Zero
IF abs[<AXIS_DRO>] <= 0.0001 THEN M201 "Axis Movement is zero!\nAxis needs to be moved to accurately measure axis pitch\n\nPress Cycle Start to try again\nPress Cycle Cancel to Abort."
IF abs[<AXIS_DRO>] <= 0.0001 THEN GOTO 220

IF <UNITS> == 20 THEN <MULTIPLIER> = ABS[ <AXIS_DRO> / <MEASUREMENT>] ELSE <MULTIPLIER> = ABS[ <MEASUREMENT> / <AXIS_DRO>]

;Set New Pitch Value
#[20400 + #102] = <MULTIPLIER> * <AXIS_PITCH>

IF #9551 == #102 THEN #20401 = #[20400 + #102] ;Set 1st Axis Pair Pitch to Master Axis
IF #9552 == #102 THEN #20402 = #[20400 + #102] ;Set 2nd Axis Pair Pitch to Master Axis
IF #9553 == #102 THEN #20403 = #[20400 + #102] ;Set 3rd Axis Pair Pitch to Master Axis
IF #9554 == #102 THEN #20404 = #[20400 + #102] ;Set 4th Axis Pair Pitch to Master Axis
IF #9555 == #102 THEN #20405 = #[20400 + #102] ;Set 5th Axis Pair Pitch to Master Axis
IF #9556 == #102 THEN #20406 = #[20400 + #102] ;Set 6th Axis Pair Pitch to Master Axis
IF #9557 == #102 THEN #20407 = #[20400 + #102] ;Set 7th Axis Pair Pitch to Master Axis
IF #9558 == #102 THEN #20408 = #[20400 + #102] ;Set 8th Axis Pair Pitch to Master Axis

M225 #101 "Axis %c Pitch Adjusted Successfully" <AXIS_LABEL>

GOTO 1000 ;End Macro

N500 ;Calibrate All Axes
M225 #100 "Warning! Machine will move to the Machine Home Position\n Press Cycle Start to Begin Movement"

;Move all Axes except ones labeled as N or are Slaved Pairs
IF <AXIS_1_LABEL> != 78 && #9551 == 0 THEN G53 $<AXIS_1_LABEL> 0
IF <AXIS_2_LABEL> != 78 && #9552 == 0 THEN G53 $<AXIS_2_LABEL> 0
IF <AXIS_3_LABEL> != 78 && #9553 == 0 THEN G53 $<AXIS_3_LABEL> 0
IF <AXIS_4_LABEL> != 78 && #9554 == 0 THEN G53 $<AXIS_4_LABEL> 0
IF <AXIS_5_LABEL> != 78 && #9555 == 0 THEN G53 $<AXIS_5_LABEL> 0
IF <AXIS_6_LABEL> != 78 && #9556 == 0 THEN G53 $<AXIS_6_LABEL> 0
IF <AXIS_7_LABEL> != 78 && #9557 == 0 THEN G53 $<AXIS_7_LABEL> 0
IF <AXIS_8_LABEL> != 78 && #9558 == 0 THEN G53 $<AXIS_8_LABEL> 0

IF #50001
#102 = 0
N600 ;Course Adjustment
;Loop Through Labeled Axes
#102 = #102 + 1
IF #102 > 8 THEN GOTO 1000
IF #[20100 + #102] == 78 THEN GOTO 600
IF #[9550 + #102] != 0 THEN GOTO 600 ;Skip axis if slaved

;Define Current Iteration Label and Pitch
<AXIS_LABEL> = #[20100 + #102]
<AXIS_PITCH> = #[20400 + #102]

N610
M201 "1. Physically Locate/Mark Current Location\n2. Jog Axis %c away from this location\nNote: The more distance the better\n\nPress Cycle Start once Complete" <AXIS_LABEL>

N620
M224 <MEASUREMENT> "Physically Measure Distance Traveled as accurate as possible and input your\n#)measurement Value\nNote: DO NOT USE THE CNC12 DRO POSITION FOR MEASUREMENT"
IF <MEASUREMENT> == 0 THEN GOTO 620

;Calculate New Pitch for Axis
IF #50001
<AXIS_DRO> = #[5020 + #102]

;Ensure Axis DRO is not Zero
IF abs[<AXIS_DRO>] <= 0.0001 THEN M201 "Axis Movement is zero!\nAxis needs to be moved to accurately measure axis pitch\n\nPress Cycle Start to try again\nPress Cycle Cancel to Abort."
IF abs[<AXIS_DRO>] <= 0.0001 THEN GOTO 610

IF <UNITS> == 20 THEN <MULTIPLIER> = ABS[ <AXIS_DRO> / <MEASUREMENT>] ELSE <MULTIPLIER> = ABS[ <MEASUREMENT> / <AXIS_DRO>]

;Set New Pitch Value
#[20400 + #102] = <MULTIPLIER> * <AXIS_PITCH>

IF #9551 == #102 THEN #20401 = #[20400 + #102] ;Set 1st Axis Pair Pitch to Master Axis
IF #9552 == #102 THEN #20402 = #[20400 + #102] ;Set 2nd Axis Pair Pitch to Master Axis
IF #9553 == #102 THEN #20403 = #[20400 + #102] ;Set 3rd Axis Pair Pitch to Master Axis
IF #9554 == #102 THEN #20404 = #[20400 + #102] ;Set 4th Axis Pair Pitch to Master Axis
IF #9555 == #102 THEN #20405 = #[20400 + #102] ;Set 5th Axis Pair Pitch to Master Axis
IF #9556 == #102 THEN #20406 = #[20400 + #102] ;Set 6th Axis Pair Pitch to Master Axis
IF #9557 == #102 THEN #20407 = #[20400 + #102] ;Set 7th Axis Pair Pitch to Master Axis
IF #9558 == #102 THEN #20408 = #[20400 + #102] ;Set 8th Axis Pair Pitch to Master Axis

GOTO 600 ;Loop back to Next Axis

N1000                            ;End of Macro
