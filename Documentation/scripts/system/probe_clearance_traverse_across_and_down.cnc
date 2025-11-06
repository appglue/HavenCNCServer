; Filename:   probe_clearance_traverse_across_and_down.cnc
; Purpose:    Maneuver over an object (like a boss or web)
; Programmer: John Popovich
; Date:       Nov 9, 2010   
; Updated:    May 10, 2011
; Usage:      G65 "/cncm/system/probe_clearance_traverse_across_and_down.cnc"  X[clearance_distance] Z[z_target_position] E[axis_index] D[initial_direction] A[additional_distance]  F[traverse_speed] B[plunge_speed]
;       
; Where:      clearance distance = the x or y clearing distance
;             z_target_position   = the z target position
;             axis_index = 1 through max axis
;             initial_direction = the direction to move first
;             additional_distance = additional distance to move if the first distance wasn't enough
;             traverse_speed = feedrate during the boss traversing moves (moving up and over to the other side of the boss)
;             plunge_speed   = feedrate for the plunge move (after traversing across to the other side) it moves down before probing.  You might want this slower if you are unsure of the boss width.
; Uses: #34801 #34814 #34815 #34816 #34817
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


; store the z at the starting point
#34801 = #5043 * #30021

; get the clearance axis limit point
#34814 = 0
if [#d == -1] then #34814 = 100
#34814 = #[24600 + #e + #34814]

; protected move the specified axis by the clearance distance
; m125 /axislabel  [current_position + [distance] * direction]


; limit the move to the limit position
#34815 = [#[5040+#[e]]  * #30021]  + [[#[x]] * #[d]]
G65 "#400/system/probe_limit_position.cnc"  X[#34815] E[#e] D[#d] h1
#34815 = #34900


F#f ; set the traverse feedrate

G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#[e]]] A[ #34815 ]

; save the limit position
#34816 = 0
if [#d == -1] then #34816 = 100
#34816 = #[24600 + #e + #34816]

N100

F#b ; set the plunge feedrate
; use m115 L1 so that the job isn't canceled if the probe trips
G65 "#400/system/probe_protected_move.cnc"  X[#20103] A[#z] D[1] 
F#f ; set the traverse feedrate


; if we stopped early because of a probe trip, move by the additional distance and retry
if [#34850] then GOTO 200 ; goto move back up and retry

GOTO 300 ; goto end of macro

N200 ;  move back up and retry
;if no additional distance is given then just finish
if [#a == 0] then GOTO 300

; un protected move Z up to the starting point
;g1 $#20101 #5041 $#20102 #5042 $#20103 #34801
g1
G65 "#400/system/move_primitive.cnc" X[#5041 * #30021] Y[#5042 * #30021] Z[#34801]

; protected move the specified axis by the additional distance
; m125 /axislabel  [current_position + [distance] * direction]

#34815 = [ [#[5040+#[e]]]  + [[#[a]] * #[d]] ]

; limit the move to the limit position

if [ [#d ==  1] && [ #34815 > #34816 ] ] || [ [#d == -1] && [ #34815 < #34816 ] ]  then #34815 = #34816

; if we are already at the limit position then error
#34817 = abs [ [#[5040+#e]] - #34815 ]
if [#34817 < #34011 ] then #32000 = 2 ; Surface not found
;if [#32000 != 1] then m225 #100 "surface not found"
if [#32000 != 1] then M99 ; Surface not found
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#e]] A[ #34815 ] 


GOTO 100 ; goto protected move Z down to the z target position

N300 ; end of macro

