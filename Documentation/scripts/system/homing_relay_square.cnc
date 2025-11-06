;------------------------------------------------------------------------------
; Filename: homing_relay_square.cnc
; Description: Homing Macro for Hard Wired Paired Axis with Disable Relay.
; Notes: Relay Disables Slave Motor
; Requires: CNC12 V5.18+
; Revision Date: 23 May 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\homing_relay_square.cnc" A[Axis]
; Arguments:  Axis = Axis # to Home (1-4)
;------------------------------------------------------------------------------
; Parameters:
; #9502 : Square Distance
; #9504 : Switch Deadband Distance
;
; System Variables:
; #20101-#20108 : Labels for Axes 1-8
; #21301-#21308 : Minus Home Axes 1-8
; #21401-#21408 : Plus Home Axes 1-8
; #27901-#27908 : Homing Rates for Axes 1-8
;
; PLC Variables:
;
; User Variables:
; #100 : M225 Message Timer (Infinite Timer)
; #101 : M225 Message Timer
; #106 : AutoSquare Master Axis
; #108 : AutoSquare Master Input
; #109 : AutoSquare Slave Input
; #110 : AutoSquare Attempts Count
; #112 : Master Axis Label Value
; #113 : Slave Axis has been Homed.
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;--Define Setup
DEFINE <MASTER_AXIS>          #106
DEFINE <MASTER_INPUT>         #108
DEFINE <SLAVE_INPUT>          #109
DEFINE <AUTOSQUARE_ATTEMPTS>  #110
DEFINE <MASTER_AXIS_LABEL>    #112

DEFINE <AXIS_1_HOME_INPUT>    #121
DEFINE <AXIS_2_HOME_INPUT>    #122
DEFINE <AXIS_3_HOME_INPUT>    #123
DEFINE <AXIS_4_HOME_INPUT>    #124
DEFINE <AXIS_5_HOME_INPUT>    #125
DEFINE <AXIS_6_HOME_INPUT>    #126
DEFINE <AXIS_7_HOME_INPUT>    #127
DEFINE <AXIS_8_HOME_INPUT>    #128

DEFINE <AXIS_SQUARE_DISTANCE> #9502

DEFINE <AXIS_1_LABEL>         #20101
DEFINE <AXIS_2_LABEL>         #20102
DEFINE <AXIS_3_LABEL>         #20103
DEFINE <AXIS_4_LABEL>         #20104
DEFINE <AXIS_5_LABEL>         #20105
DEFINE <AXIS_6_LABEL>         #20106
DEFINE <AXIS_7_LABEL>         #20107
DEFINE <AXIS_8_LABEL>         #20108
DEFINE <AXIS_1_HOME_MINUS>    #21301
DEFINE <AXIS_2_HOME_MINUS>    #21302
DEFINE <AXIS_3_HOME_MINUS>    #21303
DEFINE <AXIS_4_HOME_MINUS>    #21304
DEFINE <AXIS_5_HOME_MINUS>    #21305
DEFINE <AXIS_6_HOME_MINUS>    #21306
DEFINE <AXIS_7_HOME_MINUS>    #21307
DEFINE <AXIS_8_HOME_MINUS>    #21308
DEFINE <AXIS_1_HOME_PLUS>     #21401
DEFINE <AXIS_2_HOME_PLUS>     #21402
DEFINE <AXIS_3_HOME_PLUS>     #21403
DEFINE <AXIS_4_HOME_PLUS>     #21404
DEFINE <AXIS_5_HOME_PLUS>     #21405
DEFINE <AXIS_6_HOME_PLUS>     #21406
DEFINE <AXIS_7_HOME_PLUS>     #21407
DEFINE <AXIS_8_HOME_PLUS>     #21408

DEFINE <AXIS_1_HOME_FEED>     #27901
DEFINE <AXIS_2_HOME_FEED>     #27902
DEFINE <AXIS_3_HOME_FEED>     #27903
DEFINE <AXIS_4_HOME_FEED>     #27904
DEFINE <AXIS_5_HOME_FEED>     #27905
DEFINE <AXIS_6_HOME_FEED>     #27906
DEFINE <AXIS_7_HOME_FEED>     #27907
DEFINE <AXIS_8_HOME_FEED>     #27908

;--User Variable Setup
#100 = 0  ;Message Timer for "Permanent" Messages
#101 = 2  ;Message Timer for Timed Messages in Seconds
#113 = 0  ;Slave has been Homed Flag
<AUTOSQUARE_ATTEMPTS> = 0 ;Auto Square Attempts Count

;-----------------------------------------------------------------------------------------
;										  Setup
;-----------------------------------------------------------------------------------------
N10
;Determine the Auto Square Axis
IF #A <= 0 THEN #A = 1
<MASTER_AXIS> = #A

;Determine Master Axis Label
<MASTER_AXIS_LABEL> = #[20100 + <MASTER_AXIS>]

;Determine Axes Home Inputs
IF <AXIS_1_HOME_PLUS>  != 0 && <AXIS_1_HOME_PLUS>  <= 50000 THEN <AXIS_1_HOME_INPUT> = <AXIS_1_HOME_PLUS> + 50000
IF <AXIS_1_HOME_PLUS>  != 0 && <AXIS_1_HOME_PLUS>  >  50000 THEN <AXIS_1_HOME_INPUT> = <AXIS_1_HOME_PLUS>
IF <AXIS_1_HOME_MINUS> != 0 && <AXIS_1_HOME_MINUS> <= 50000 THEN <AXIS_1_HOME_INPUT> = <AXIS_1_HOME_MINUS> + 50000
IF <AXIS_1_HOME_MINUS> != 0 && <AXIS_1_HOME_MINUS> >  50000 THEN <AXIS_1_HOME_INPUT> = <AXIS_1_HOME_MINUS>

IF <AXIS_2_HOME_PLUS>  != 0 && <AXIS_2_HOME_PLUS>  <= 50000 THEN <AXIS_2_HOME_INPUT> = <AXIS_2_HOME_PLUS> + 50000
IF <AXIS_2_HOME_PLUS>  != 0 && <AXIS_2_HOME_PLUS>  >  50000 THEN <AXIS_2_HOME_INPUT> = <AXIS_2_HOME_PLUS>
IF <AXIS_2_HOME_MINUS> != 0 && <AXIS_2_HOME_MINUS> <= 50000 THEN <AXIS_2_HOME_INPUT> = <AXIS_2_HOME_MINUS> + 50000
IF <AXIS_2_HOME_MINUS> != 0 && <AXIS_2_HOME_MINUS> >  50000 THEN <AXIS_2_HOME_INPUT> = <AXIS_2_HOME_MINUS>

IF <AXIS_3_HOME_PLUS>  != 0 && <AXIS_3_HOME_PLUS>  <= 50000 THEN <AXIS_3_HOME_INPUT> = <AXIS_3_HOME_PLUS> + 50000
IF <AXIS_3_HOME_PLUS>  != 0 && <AXIS_3_HOME_PLUS>  >  50000 THEN <AXIS_3_HOME_INPUT> = <AXIS_3_HOME_PLUS>
IF <AXIS_3_HOME_MINUS> != 0 && <AXIS_3_HOME_MINUS> <= 50000 THEN <AXIS_3_HOME_INPUT> = <AXIS_3_HOME_MINUS> + 50000
IF <AXIS_3_HOME_MINUS> != 0 && <AXIS_3_HOME_MINUS> >  50000 THEN <AXIS_3_HOME_INPUT> = <AXIS_3_HOME_MINUS>

IF <AXIS_4_HOME_PLUS>  != 0 && <AXIS_4_HOME_PLUS>  <= 50000 THEN <AXIS_4_HOME_INPUT> = <AXIS_4_HOME_PLUS> + 50000
IF <AXIS_4_HOME_PLUS>  != 0 && <AXIS_4_HOME_PLUS>  >  50000 THEN <AXIS_4_HOME_INPUT> = <AXIS_4_HOME_PLUS>
IF <AXIS_4_HOME_MINUS> != 0 && <AXIS_4_HOME_MINUS> <= 50000 THEN <AXIS_4_HOME_INPUT> = <AXIS_4_HOME_MINUS> + 50000
IF <AXIS_4_HOME_MINUS> != 0 && <AXIS_4_HOME_MINUS> >  50000 THEN <AXIS_4_HOME_INPUT> = <AXIS_4_HOME_MINUS>

IF <AXIS_5_HOME_PLUS>  != 0 && <AXIS_5_HOME_PLUS>  <= 50000 THEN <AXIS_5_HOME_INPUT> = <AXIS_5_HOME_PLUS> + 50000
IF <AXIS_5_HOME_PLUS>  != 0 && <AXIS_5_HOME_PLUS>  >  50000 THEN <AXIS_5_HOME_INPUT> = <AXIS_5_HOME_PLUS>
IF <AXIS_5_HOME_MINUS> != 0 && <AXIS_5_HOME_MINUS> <= 50000 THEN <AXIS_5_HOME_INPUT> = <AXIS_5_HOME_MINUS> + 50000
IF <AXIS_5_HOME_MINUS> != 0 && <AXIS_5_HOME_MINUS> >  50000 THEN <AXIS_5_HOME_INPUT> = <AXIS_5_HOME_MINUS>

IF <AXIS_6_HOME_PLUS>  != 0 && <AXIS_6_HOME_PLUS>  <= 50000 THEN <AXIS_6_HOME_INPUT> = <AXIS_6_HOME_PLUS> + 50000
IF <AXIS_6_HOME_PLUS>  != 0 && <AXIS_6_HOME_PLUS>  >  50000 THEN <AXIS_6_HOME_INPUT> = <AXIS_6_HOME_PLUS>
IF <AXIS_6_HOME_MINUS> != 0 && <AXIS_6_HOME_MINUS> <= 50000 THEN <AXIS_6_HOME_INPUT> = <AXIS_6_HOME_MINUS> + 50000
IF <AXIS_6_HOME_MINUS> != 0 && <AXIS_6_HOME_MINUS> >  50000 THEN <AXIS_6_HOME_INPUT> = <AXIS_6_HOME_MINUS>

IF <AXIS_7_HOME_PLUS>  != 0 && <AXIS_7_HOME_PLUS>  <= 50000 THEN <AXIS_7_HOME_INPUT> = <AXIS_7_HOME_PLUS> + 50000
IF <AXIS_7_HOME_PLUS>  != 0 && <AXIS_7_HOME_PLUS>  >  50000 THEN <AXIS_7_HOME_INPUT> = <AXIS_7_HOME_PLUS>
IF <AXIS_7_HOME_MINUS> != 0 && <AXIS_7_HOME_MINUS> <= 50000 THEN <AXIS_7_HOME_INPUT> = <AXIS_7_HOME_MINUS> + 50000
IF <AXIS_7_HOME_MINUS> != 0 && <AXIS_7_HOME_MINUS> >  50000 THEN <AXIS_7_HOME_INPUT> = <AXIS_7_HOME_MINUS>

IF <AXIS_8_HOME_PLUS>  != 0 && <AXIS_8_HOME_PLUS>  <= 50000 THEN <AXIS_8_HOME_INPUT> = <AXIS_8_HOME_PLUS> + 50000
IF <AXIS_8_HOME_PLUS>  != 0 && <AXIS_8_HOME_PLUS>  >  50000 THEN <AXIS_8_HOME_INPUT> = <AXIS_8_HOME_PLUS>
IF <AXIS_8_HOME_MINUS> != 0 && <AXIS_8_HOME_MINUS> <= 50000 THEN <AXIS_8_HOME_INPUT> = <AXIS_8_HOME_MINUS> + 50000
IF <AXIS_8_HOME_MINUS> != 0 && <AXIS_8_HOME_MINUS> <  50000 THEN <AXIS_8_HOME_INPUT> = <AXIS_8_HOME_MINUS>

;Set Master and Slave Axis Inputs
<MASTER_INPUT> = #[120 + <MASTER_AXIS>]
<SLAVE_INPUT> = 70091

GOTO 110

;-----------------------------------------------------------------------------------------
;								Axes Home Tripped Checking
;-----------------------------------------------------------------------------------------
;Check to ensure Home switches are not tripped and only one home input is assigned.
N100

M225 #100 "Configuration errors were detected\nPress Cycle Cancel to Abort\nVerify Setup in Wizard"

GOTO 100

N110

;Check to ensure only one home direction assigned
IF #50001
IF <AXIS_1_HOME_PLUS> && <AXIS_1_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_1_LABEL>
IF <AXIS_2_HOME_PLUS> && <AXIS_2_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_2_LABEL>
IF <AXIS_3_HOME_PLUS> && <AXIS_3_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_3_LABEL>
IF <AXIS_4_HOME_PLUS> && <AXIS_4_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_4_LABEL>
IF <AXIS_5_HOME_PLUS> && <AXIS_5_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_5_LABEL>
IF <AXIS_6_HOME_PLUS> && <AXIS_6_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_6_LABEL>
IF <AXIS_7_HOME_PLUS> && <AXIS_7_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_7_LABEL>
IF <AXIS_8_HOME_PLUS> && <AXIS_8_HOME_MINUS> THEN M225 #100 "Home switches set in both directions on %c axis\nPress Cycle Start to continue configuration check.\nPress Cycle Cancel to abort" <AXIS_8_LABEL>

N120;Prevent moving pass this point if the above errors were detected
IF <AXIS_1_HOME_PLUS> && <AXIS_1_HOME_MINUS> THEN GOTO 100
IF <AXIS_2_HOME_PLUS> && <AXIS_2_HOME_MINUS> THEN GOTO 100
IF <AXIS_3_HOME_PLUS> && <AXIS_3_HOME_MINUS> THEN GOTO 100
IF <AXIS_4_HOME_PLUS> && <AXIS_4_HOME_MINUS> THEN GOTO 100
IF <AXIS_5_HOME_PLUS> && <AXIS_5_HOME_MINUS> THEN GOTO 100
IF <AXIS_6_HOME_PLUS> && <AXIS_6_HOME_MINUS> THEN GOTO 100
IF <AXIS_7_HOME_PLUS> && <AXIS_7_HOME_MINUS> THEN GOTO 100
IF <AXIS_8_HOME_PLUS> && <AXIS_8_HOME_MINUS> THEN GOTO 100

;-----------------------------------------------------------------------------------------
;							  Auto Square Paired Axis Homing
;-----------------------------------------------------------------------------------------
N500
IF #50001
IF <AUTOSQUARE_ATTEMPTS> >= 10 THEN M225 #100 "Master Switch is tripped first!\nPlease re-configure machine to trip the slave first and try again.\n\nPress ESC or Cycle Cancel to abort."
IF <AUTOSQUARE_ATTEMPTS> >= 10 THEN GOTO 500
M221 #100 "Moving %c Paired Axes until slave switch is tripped" <MASTER_AXIS_LABEL>

;Move Until either Master or Slave input is Open
IF <AXIS_1_HOME_PLUS>  && <MASTER_AXIS> == 1 THEN M106 /$<AXIS_1_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_1_HOME_MINUS> && <MASTER_AXIS> == 1 THEN M105 /$<AXIS_1_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_2_HOME_PLUS>  && <MASTER_AXIS> == 2 THEN M106 /$<AXIS_2_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_2_HOME_MINUS> && <MASTER_AXIS> == 2 THEN M105 /$<AXIS_2_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_3_HOME_PLUS>  && <MASTER_AXIS> == 3 THEN M106 /$<AXIS_3_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_3_HOME_MINUS> && <MASTER_AXIS> == 3 THEN M105 /$<AXIS_3_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_4_HOME_PLUS>  && <MASTER_AXIS> == 4 THEN M106 /$<AXIS_4_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_4_HOME_FEED>
IF <AXIS_4_HOME_MINUS> && <MASTER_AXIS> == 4 THEN M105 /$<AXIS_4_LABEL> P<MASTER_INPUT> Q<SLAVE_INPUT> F<AXIS_4_HOME_FEED>

N510 ;Determine which Switch Tripped
;When Master is tripped and not slave, we will begin a routine to try to "nudge" gantry to hit slave first.
; this process will fail after 10 tries.
;When Slave is tripped, we continue normally.
IF !#<SLAVE_INPUT> THEN GOTO 520 ELSE GOTO 530

N520 ;Move off Slave Switch
M221 #100 "Moving %c Paired Axes until slave switch is un-tripped" <MASTER_AXIS_LABEL>
IF <AXIS_1_HOME_PLUS>  && <MASTER_AXIS> == 1 THEN M105 /$<AXIS_1_LABEL> P-<SLAVE_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_1_HOME_MINUS> && <MASTER_AXIS> == 1 THEN M106 /$<AXIS_1_LABEL> P-<SLAVE_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_2_HOME_PLUS>  && <MASTER_AXIS> == 2 THEN M105 /$<AXIS_2_LABEL> P-<SLAVE_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_2_HOME_MINUS> && <MASTER_AXIS> == 2 THEN M106 /$<AXIS_2_LABEL> P-<SLAVE_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_3_HOME_PLUS>  && <MASTER_AXIS> == 3 THEN M105 /$<AXIS_3_LABEL> P-<SLAVE_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_3_HOME_MINUS> && <MASTER_AXIS> == 3 THEN M106 /$<AXIS_3_LABEL> P-<SLAVE_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_4_HOME_PLUS>  && <MASTER_AXIS> == 4 THEN M105 /$<AXIS_4_LABEL> P-<SLAVE_INPUT> F<AXIS_4_HOME_FEED>
IF <AXIS_4_HOME_MINUS> && <MASTER_AXIS> == 4 THEN M106 /$<AXIS_4_LABEL> P-<SLAVE_INPUT> F<AXIS_4_HOME_FEED>

;Set that Slave has been Homed off Switch
#113 = 1

N530 ;Unpair Axis and Move Master off Switch if Needed
M94 /106
M225 #101 "Un-pairing %c axis" <MASTER_AXIS_LABEL>

;If Master is not tripped and Slave had been tripped and homed,then proceed normally
; with moving master to switch otherwise move off switch for "nudge routine"
IF #<MASTER_INPUT> && #113 == 1 THEN GOTO 540

;Move Master Off Switch
IF <AUTOSQUARE_ATTEMPTS> == 0 THEN M225 #101 "Master Switch Tripped First!\nAttempting Automatic Square Routine until slave trips first!"
M221 #100 "Moving Master %c Axis until master switch is un-tripped" <MASTER_AXIS_LABEL>
IF <AXIS_1_HOME_PLUS>  && <MASTER_AXIS> == 1 THEN M105 /$<AXIS_1_LABEL> P-<MASTER_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_1_HOME_MINUS> && <MASTER_AXIS> == 1 THEN M106 /$<AXIS_1_LABEL> P-<MASTER_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_2_HOME_PLUS>  && <MASTER_AXIS> == 2 THEN M105 /$<AXIS_2_LABEL> P-<MASTER_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_2_HOME_MINUS> && <MASTER_AXIS> == 2 THEN M106 /$<AXIS_2_LABEL> P-<MASTER_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_3_HOME_PLUS>  && <MASTER_AXIS> == 3 THEN M105 /$<AXIS_3_LABEL> P-<MASTER_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_3_HOME_MINUS> && <MASTER_AXIS> == 3 THEN M106 /$<AXIS_3_LABEL> P-<MASTER_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_4_HOME_PLUS>  && <MASTER_AXIS> == 4 THEN M105 /$<AXIS_4_LABEL> P-<MASTER_INPUT> F<AXIS_4_HOME_FEED>
IF <AXIS_4_HOME_MINUS> && <MASTER_AXIS> == 4 THEN M106 /$<AXIS_4_LABEL> P-<MASTER_INPUT> F<AXIS_4_HOME_FEED>

;Re-pair axis to continue AutoSquare
IF #50001
M95 /106
M225 #101 "Re-pairing %c Axis" <MASTER_AXIS_LABEL>

IF #50001
<AUTOSQUARE_ATTEMPTS> = <AUTOSQUARE_ATTEMPTS> + 1

GOTO 500 ;Repeat Cycle Until slave hits first.

N540 ;Move Master until Master input is open
M221 #100 "Moving Master %c Axis until master switch is tripped" <MASTER_AXIS_LABEL>
IF <AXIS_1_HOME_PLUS>  && <MASTER_AXIS> == 1 THEN M106 /$<AXIS_1_LABEL> P<MASTER_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_1_HOME_MINUS> && <MASTER_AXIS> == 1 THEN M105 /$<AXIS_1_LABEL> P<MASTER_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_2_HOME_PLUS>  && <MASTER_AXIS> == 2 THEN M106 /$<AXIS_2_LABEL> P<MASTER_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_2_HOME_MINUS> && <MASTER_AXIS> == 2 THEN M105 /$<AXIS_2_LABEL> P<MASTER_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_3_HOME_PLUS>  && <MASTER_AXIS> == 3 THEN M106 /$<AXIS_3_LABEL> P<MASTER_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_3_HOME_MINUS> && <MASTER_AXIS> == 3 THEN M105 /$<AXIS_3_LABEL> P<MASTER_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_4_HOME_PLUS>  && <MASTER_AXIS> == 4 THEN M106 /$<AXIS_4_LABEL> P<MASTER_INPUT> F<AXIS_4_HOME_FEED>
IF <AXIS_4_HOME_MINUS> && <MASTER_AXIS> == 4 THEN M105 /$<AXIS_4_LABEL> P<MASTER_INPUT> F<AXIS_4_HOME_FEED>

N550 ;Move Master Off Switch
M221 #100 "Moving Master %c Axis until master switch is un-tripped" <MASTER_AXIS_LABEL>
IF <AXIS_1_HOME_PLUS>  && <MASTER_AXIS> == 1 THEN M105 /$<AXIS_1_LABEL> P-<MASTER_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_1_HOME_MINUS> && <MASTER_AXIS> == 1 THEN M106 /$<AXIS_1_LABEL> P-<MASTER_INPUT> F<AXIS_1_HOME_FEED>
IF <AXIS_2_HOME_PLUS>  && <MASTER_AXIS> == 2 THEN M105 /$<AXIS_2_LABEL> P-<MASTER_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_2_HOME_MINUS> && <MASTER_AXIS> == 2 THEN M106 /$<AXIS_2_LABEL> P-<MASTER_INPUT> F<AXIS_2_HOME_FEED>
IF <AXIS_3_HOME_PLUS>  && <MASTER_AXIS> == 3 THEN M105 /$<AXIS_3_LABEL> P-<MASTER_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_3_HOME_MINUS> && <MASTER_AXIS> == 3 THEN M106 /$<AXIS_3_LABEL> P-<MASTER_INPUT> F<AXIS_3_HOME_FEED>
IF <AXIS_4_HOME_PLUS>  && <MASTER_AXIS> == 4 THEN M105 /$<AXIS_4_LABEL> P-<MASTER_INPUT> F<AXIS_4_HOME_FEED>
IF <AXIS_4_HOME_MINUS> && <MASTER_AXIS> == 4 THEN M106 /$<AXIS_4_LABEL> P-<MASTER_INPUT> F<AXIS_4_HOME_FEED>

N560 ;Move Master Axis back to Square Position
IF <AXIS_SQUARE_DISTANCE> == 0 THEN GOTO 570 ;Skip Moving AutoSquare Distance if there is none.

;Home Master
;This allows following AutoSquare G1 moves if it is "behind" the travel limits on a re-home.
M26 /$<MASTER_AXIS_LABEL>

;Move Master back
M221 #100 "Moving Master %c Axis back to square position" <MASTER_AXIS_LABEL>
IF <MASTER_AXIS> == 1 && <AXIS_1_HOME_PLUS>  THEN G91 G1 $<AXIS_1_LABEL> [-<AXIS_SQUARE_DISTANCE>] F<AXIS_1_HOME_FEED>
IF <MASTER_AXIS> == 1 && <AXIS_1_HOME_MINUS> THEN G91 G1 $<AXIS_1_LABEL> [ <AXIS_SQUARE_DISTANCE>] F<AXIS_1_HOME_FEED>
IF <MASTER_AXIS> == 2 && <AXIS_2_HOME_PLUS>  THEN G91 G1 $<AXIS_2_LABEL> [-<AXIS_SQUARE_DISTANCE>] F<AXIS_2_HOME_FEED>
IF <MASTER_AXIS> == 2 && <AXIS_2_HOME_MINUS> THEN G91 G1 $<AXIS_2_LABEL> [ <AXIS_SQUARE_DISTANCE>] F<AXIS_2_HOME_FEED>
IF <MASTER_AXIS> == 3 && <AXIS_3_HOME_PLUS>  THEN G91 G1 $<AXIS_3_LABEL> [-<AXIS_SQUARE_DISTANCE>] F<AXIS_3_HOME_FEED>
IF <MASTER_AXIS> == 3 && <AXIS_3_HOME_MINUS> THEN G91 G1 $<AXIS_3_LABEL> [ <AXIS_SQUARE_DISTANCE>] F<AXIS_3_HOME_FEED>
IF <MASTER_AXIS> == 4 && <AXIS_4_HOME_PLUS>  THEN G91 G1 $<AXIS_4_LABEL> [-<AXIS_SQUARE_DISTANCE>] F<AXIS_4_HOME_FEED>
IF <MASTER_AXIS> == 4 && <AXIS_4_HOME_MINUS> THEN G91 G1 $<AXIS_4_LABEL> [ <AXIS_SQUARE_DISTANCE>] F<AXIS_4_HOME_FEED>

N570 ;Re-pair Axis
M95 /106
M225 #101 "Re-pairing %c Axis" <MASTER_AXIS_LABEL>

M26 /$<MASTER_AXIS_LABEL>

N1000                            ;End of Macro