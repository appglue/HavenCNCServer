; Filename:   probe_tt1_move.cnc
; Purpose:    A generic 3 axis probing move which handles the differences between probe types
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage2:     G65 "/cncm/system/probe_tt1_move.cnc"  D[direction] Z[z_axis_label]
; Where       direction = -1 for negative or 1 positive
;             z_axis_label = label of the axis to move
;
; OUTPUT:     #34006 = x probed point
;             #34007 = y probed point
;             #34008 = z probed point
;
; Notes:      In order for this macro to work the dp7 and dsp probe parameter must be disabled
; single axis tt1 move
;
; Uses #34500 - #34504
;
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

;change the clearance feedrate to the fast probing rate for tt1 moves
#34001 = #9014

; check for invalid parameters
if [#d != 1] && [#d != -1] then ERROR;  Invalid direction

; get the tt1 plc input number
#34504 = #9044 ; default to the tt1 input parameter
if [#25018 == 10] then #34504 = -#9044

if [#9044 == 0] then #34504 = #34005 ; if the tt1 parameter is zero then use probe input number (according to the manual)

#34500 = 116 ; default to m116
if [#d==-1] then #34500 = 115 ; use m115 for negative direction

if [#[#34002]] then  ;  force synchronization with the motion control card

; store the current start point
#34501 = #5041
#34502 = #5042
#34503 = #5043

F[#9014]

; do the tt1 move!
M[#34500] /$#[z] p[#34504] F[#9014] ; move the specified axes in the given direction at the fast probing rate

#32001=1 ; disable probe protection for pullback move
G65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#34503] X[#5041] Y[#5042] Z[#5043] F1
#32001=0 ; re-enable probe protection if necessary

; This loop ensures the probe is clear before making the measuring move.
N10
IF abs[#34504] > 50000 THEN #101=#[abs[#34504]] ELSE #101 = #[abs[#34504] + 50000]
IF [#34504 > 0] && [#101==1] THEN GOTO 10 ELSE IF [#34504 < 0] && [#101==0] THEN GOTO 10

; make the measuring move
M[#34500] /$#[z] p[#34504]  F[#9015]; move the specified axes in the given direction at the slow probing rate

; store the probed point
#34006 = #5041 ; expected positions
#34007 = #5042
#34008 = #5043

