; Filename:   probe_boss
; Purpose:    Find the center of a boss
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Updated:    May 10, 2011
; Usage:      G65 "/cncm/system/probe_boss.cnc" X[clearance_distance] Z[clearance_height] E[initial_axis_index] D[initial_direction] F[traverse_speed] B[plunge_speed] 

; Where:      clearance distance = the x and y clearing distance
;             clearance height   = the z clearance height
;             axis_index = 1 through max axis, the axis to move first
;             initial_direction = the direction to move first
;             traverse_speed = feedrate during the boss traversing moves (moving up and over to the other side of the boss)
;             plunge_speed   = feedrate for the plunge move (after traversing across to the other side) it moves down before probing.  You might want this slower if you are unsure of the boss width.
; Notes:      the probe will be at the center point at the end of the macro
;             the plunge and traverse speed are normally the fast probing rate.  However, some CNC11 setup routines for five axis systems want to move faster to save time.  This is ok since the routines have a really good idea of the location and size of the boss they are probing.
; uses        #34505
; Output:     #34562 width 1
;             #34563 width 2
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


#34505 = [#[e]] ; save the intial axis index

;check parameters
if [#34505 != 1] && [#34505 != 2] then m99 ; invalid starting axis


; find the center of the first axis
G65 "#400/system/probe_center_outside.cnc" X[#x] Z[#[z]] E[#34505] D[#[d]] F[#f] B[#b]

; check the return status of clearance traverse
if [#32000 != 1] then M99

;store width 1
#34562 = #34509

; switch the axis index
if [[#[e]] == 1] then #34505=2
if [[#[e]] == 2] then #34505=1

; move to the start point for the next axis's web measurement
G65 "#400/system/probe_clearance_traverse_across_and_down.cnc"  X[[#x/2] + [#34009/2] + #9013] Z[[#5043 * #30021]-[#[z]]] E[#34505] D[1] A[#x *.1] F[#f] B[#b]

; check the return status of clearance traverse
if [#32000 != 1] then M99

; find center of the second axis
G65 "#400/system/probe_center_outside.cnc" X[#x] Z[#[z]] E[#34505] D-1 F[#f] B[#b]

;store width 2
#34563 = #34509


