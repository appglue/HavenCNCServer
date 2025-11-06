; Filename:   probe_two_points_on_wall.cnc
; Purpose:    Probe two points along a wall
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_two_points_on_wall.cnc" d[distance] A[initial_axis] X[initial_direction] Y[secondary_direction]
;                                                 c[comp_direction]
;             comp_direction = 1 for left -1 for right

; Where       distance = the distance to move along the wall
;             initial_direction = the direction towards the wall 1 or -1
;             secondary_direction = the direction to move along the wall 1 or -1
; Output      #34550 = first point x
;             #34551 = first point y
;             #34552 = second point x
;             #34553 = second point y
; Uses        #34554 - #34557 
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

;find the secondary axis
if [#[a]==1] then #34554 = 2
if [#[a]==2] then #34554 = 1

;save the start point 
#34555 = #5041
#34556 = #5042

; probed first point on first wall
G65 "#400/system/probe_move.cnc"  D[#[x]] X[#[20100 + #[a] ] ] Y78 Z78 W1 C0

; save the probed point
#34550 = #34006
#34551 = #34007

; protected move back to the start point
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#[a]]] A[#[34554+#[a]]]  


; move along the wall 
; position = [ #[start_pos_base+secondary_axis_index] +  [secondary_direction * distance] 
#34557 = [ #[34554+#34554]  +    [ #[y]  * #[d] ] ]
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#34554]] A[#34557] D1

; if we stopped early because of a probe trip, pull back in the initial axis direction
if [#34850] then $#[20100+ #[a]]   [ #[5040+#[a]] + [ [- #[x] ] *  [#9013]    ]  ]

 
if [#[#34002]] then  ;  force synchronization with the motion control card
 
; compute the distance between the start point and the first measured point
#34557 = #[34554+#[a]] - #[34549+#[a]] 

; probe second point along the wall and pull back
G65 "#400/system/probe_move.cnc"  D[#[x]] X[#[20100 + #[a] ] ] Y78 Z78 W1 C0

; save the probed point
#34552 = #34006
#34553 = #34007

#34557 = #[34551+#[a]] + #34557

; protected move back the distance between the start point and the first measured point
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#[a]]] A[#34557]


; compensate for the probe radius
G65 "#400/system/probe_comp_two_points_on_a_line.cnc" c[#34552] d[#34553] x[#34550] y[#34551] a[#c]
#34552 = #34585
#34553 = #34586
#34550 = #34587
#34551 = #34588




