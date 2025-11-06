;------------------------------------------------------------------------------
; Filename: teach_laser_offset.cnc
; Description:
; Notes:
; Requires: CNC12 V5.20+ (Router)
; Revision Date: 11 Apr 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #5021 : Current X Machine Position
; #5022 : Current Y Machine Position
;
; PLC Variables:
;
; User Variables:
; #100 : Message
; #101 : Timed Message
; #102 : Saved X Machine Position
; #103 : Saved Y Machine Position
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Check to ensure axes are homed
G65 "#400\system\check_home_status.cnc"

#101 = 3

N100                              ;Insert your code between N100 and N1000  

M201 "On the Machine Table, mark the location of the spindle center line (that can be reached by the LASER).\nThen Press Cycle Start"
IF #50001
#102 = #5021
#103 = #5022

M94 /29                           ;Activate Laser

M201 "Now, Jog LASER and line up on the Spindle Center Line Mark just made.\nPress Cycle Start to Teach-in Laser Position"

M95 /29                           ;De-Activate Laser

IF #50001
G10 P560 R[#102 - #5021]          ;Laser X Offset
G10 P561 R[#103 - #5022]          ;Laser Y Offset

M225 #101 "Laser Offset Teach Successful"

N1000                             ;End of Macro