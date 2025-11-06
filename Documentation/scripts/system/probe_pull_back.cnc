; Filename:   probe_pull_back.cnc
; Purpose:    pull back after a probing move
; Programmer: John Popovich, Keith Dennison
; Date:       Nov 9, 2010 
;             30-Jul-13 KSD Removed a debugging assignment to #120 that would cause a problem if P392 <= P13.
; Usage:      G65 "/cncm/system/probe_pull_back.cnc" A[start_x] B[start_y] C[start_z] X[probed_x] Y[probed_z] Z[probed_z] F[force_pullback_distance]
;
; Where:      start_x = the original starting point before the probing move
;             start_y = the original starting point before the probing move
;             start_z = the original starting point before the probing move
;             probed_x = the probed location
;             probed_y = the probed location
;             probed_z = the probed location
;             force_pullback_distance = set to one to force the pullback distance, even if it is passed the starting point
; Notes: 
; Uses variables 34512 - 34516
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

;m225 #100 "Pull back Force %f" #105
;m225 #100 "start pull back"
; skip this pull back if using a dp7 and the dp7's pull back is greater than the normal pullback
if [ [#9155 == 2 ] &&  [#9013 <= #9392] && [![#f]]] then GOTO 300 ; goto end of macro

#34957= #9013

if [[#9155 == 2 ] &&  [#9013 <= #9392]] then #34957 = #9392 ; use the greater of the two pullback if forcing is on

#34512 = #[a] - #[x]
#34513 = #[b] - #[y]
#34514 = #[c] - #[z]


; find the vector length
#34515 = sqrt[ [ [#34512] * [#34512] ] + [ [#34513] * [#34513] ] + [ [#34514] * [#34514] ] ] ; compute the 3 dimensional vector length (Pythagoras)

; find the ratio between the pull back distance and the vector length
if [#34515 != 0] #34516 = [#34957]/[#34515]
;m225 #100 "ratio %f" #34516

; The probe was at the surface at the the beginning of the move, use a zero ratio
if [#34515 == 0] then #34516=0 

if [#f == 1] then goto 50 ; if pullback forcing is on then skip the next check, (the check for the start point)

; if the move distance is less than the pullback distance , then just move back to the start point
; else move pull back the specified amount
if [[#34515] < [#34957]] goto 100
N50

;  the distance to the start point is greater than the pullback, compute the pullback position
#34512 = [#[x] + [[#34512] * #34516]] 
#34513 = [#[y] + [[#34513] * #34516]] 
#34514 = [#[z] + [[#34514] * #34516]] 

GOTO 200
N100
#34512 = #[a]
#34513 = #[b]
#34514 = #[c]

N200
#32001=1 ; disable probe protection before pulling back
F[#34001]
g65 "#400/system/move_primitive.cnc" X[#34512] Y[#34513] Z[#34514]
F[#34000]
#32001=0 ; re-enable probe protection (if it is set) after pulling back

N300 ; end of macro

;m225 #100 "end pull back"
