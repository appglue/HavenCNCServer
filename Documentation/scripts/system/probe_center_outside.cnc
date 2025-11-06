; Filename:   probe_center_outside
; Purpose:    Find the center in the specified axis around a boss or web
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Updated:    May 10 2010 : fix for negative starting direction
; Usage:      G65 "/cncm/system/probe_center_outside.cnc"  X[clearance_distance] Z[clearance_height] E[axis_index] D[initial_direction] F[traverse_speed] B[plunge_speed]
;       
; Where:      clearance distance = the x or y clearing distance
;             clearance height   = the z clearance height
;             axis_index = 1 through max axis
;             initial_direction = the direction to move first
;             traverse_speed = feedrate during the slot traversing moves (moving up and over to the other side of the slot)
;             plunge_speed   = feedrate for the plunge move (after traversing across to the other side) it moves down before probing.  You might want this slower if you are unsure of the boss width.
; Notes:      the probe will be at the center point at the end of the macro
; Output      #34509 = measured width + probe diameter
;             #34510 = point 1
;             #34511 = point 2
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; probe first direction

G65 "#400/system/probe_move.cnc"  D[#[d]] X[#[20100+#[e]]] Y78 Z78 W1 ; probe to surface and pull back

; store the probed point for the specified axis
#34510 = [#[34005 + [#[e]]]]  

; move the estimated distance + the probe diameter + the pull back distance
#34511 = #34510 + [#d * [#x + #34009 + #9013]];

;m225 #100 "probed point %f clearance position %f" #34510 #34511
#34511 = #34511 - #[5040 + #e]  ; compute the distance to the clearance destination from where we are currently sitting
#34511 = abs[#34511]
; clearance traverse 110% of the given width ,use 10% of the given width for the additional distance
G65 "#400/system/probe_clearance_traverse.cnc"  X[#34511] Z[#z] E[#e] D[#d] A[#x * .1] F[#f] B[#b]

; check the return status of clearance traverse
if [#32000 != 1] then M99

; probe second direction
G65 "#400/system/probe_move.cnc"  D[-#[d]] X[#[20100+#[e]]] Y78 Z78 W1 ; probe to surface and pull back

; store the probed point for the specified axis
#34511 = [#[34005 + [#[e]]]]  

F#f ; set the traverse feedrate

; protected move Z up by the clearance amount
m125 /$#20103 [[#5043 * #30021]+[#[z]]] p[#34005]

; calculate the center
; center = point1 + (point2-point1)/2 
#34509 = #34510 + [#34511 - #34510]/2  

; move to center
m125 /$#[20100+#[e]] #34509 p[#34005]

; calculate the width
#34509 = abs[#34511 - #34510] - #34009

