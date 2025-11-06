; Filename:   probe_clearance_traverse
; Purpose:    Maneuver over an object (like a boss or web)
; Programmer: John Popovich
; Date:       Nov 9, 2010                                                                           
; Usage:      G65 "/cncm/system/probe_clearance_traverse.cnc"  X[clearance_distance] Z[clearance_height] E[axis_index] D[initial_direction] A[additional_distance]   F[traverse_speed] B[plunge_speed]
;       
; Where:      clearance distance = the x or y clearing distance
;             clearance height   = the z clearance height
;             axis_index = 1 through max axis
;             initial_direction = the direction to move first
;             additional_distance = additional distance to move if the first distance wasn't enough
;             traverse_speed = feedrate during the boss traversing moves (moving up and over to the other side of the boss)
;             plunge_speed   = feedrate for the plunge move (after traversing across to the other side) it moves down before probing.  You might want this slower if you are unsure of the boss width.
; Uses: #34800
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; store the z starting point
#34800 = #5043  * #30021
F#f ; set the traverse feedrate
; protected move Z up to the clearance position
G65 "#400/system/probe_protected_move.cnc"  X[#20103] A[[[#5043 * #30021]+[#[z]]]]

G65 "#400/system/probe_clearance_traverse_across_and_down.cnc"  X[#x] Z[#34800] E[#e] D[#d] A[#a] F[#f] B[#b]

if [#32000 != 1] then M99 ; surface not found

