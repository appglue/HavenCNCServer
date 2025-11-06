
; Filename:   probe_outside_corner.cnc
; Purpose:    Find the an outside corner
; Programmer: John Popovich
; Date:        Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_outside_corner.cnc" A[intitial_axis] x[initial_axis_direction] y[other_axis_direction] Z[clearance_height]  e[estimated_distance]
;                                                 c[comp_direction]
;            comp_direction = 1 for left -1 for right
; Where:     inititial_axis          = the axis to move first
;                 initial_axis_direction = the initial direction for the inititial_axis +1 or -1
;                 other_axis_direction = the initial direction for the other axis +1 or -1
;                 clearance height   = the z clearance height
;                 estimated_distance = the estimated distance to the corner
; Notes:      the probe will be at the intersection point at the end of the macro
; Uses:  #34525 - #34536, #34816, #34817, #34818
; Output: #34540 - #34547 measured line points 
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; validate parameters
if [#[a] != 1 ] && [#[a] != 2 ] then ERROR; invalid initial axis 
if [#[x] != 1] && [#[x] != -1] then ERROR; invalid direction
if [#[y] != 1] && [#[y] != -1] then ERROR; invalid direction

;find the second axis index
if [#[a] == 1 ] then #34525 =2 
if [#[a] == 2 ] then #34525 =1

if [#[#34002]] then  ;  force synchronization with the motion control card

#34536 = #c

;store the starting point
#34526 = #5041  ; x axis starting point
#34527 = #5042  ; y axis starting point

; probe the first wall
G65 "#400/system/probe_two_points_on_wall.cnc" A[#[a]] d[#[e]/3.0] X[#[x]] Y[#[y]] C[#34536]

; save return values
#34540 = #34550
#34541 = #34551
#34542 = #34552
#34543 = #34553

; move around corner

;first calculate the estimated corner based on the two measured points

; find the distance between the start point and the first measured point
; which we will use to find the distance between the two lines
#34533 =  abs [ #[34525+#a] - #[34539+#a] ]

#34528 = [#34542 - #34540] ; dx
#34529 = [#34543 - #34541] ; dy

; if dx or dy are both zero then we are in trouble
if [[#34528 == 0] && [#34529 == 0]] then Error

; call atan2
G65 "#400/system/atan2.cnc" x[#34528] y[#34529]

#34532 = #34010 ; copy the angle from atan2's return value

; now the we have the angle find the estimated corner point
#34534 = [[#[e] * 1.1] + [#34009/2] ] ; distance
#34530 = [ #34534 *  [cos [#34532]]] ; dx
#34531 = [ #34534 *  [sin [#34532]]] ; dy
#34528 = #34526 + #34530 ; start point x + dx
#34529 = #34527 + #34531 ; start point y + dy

; find the distance between the two lines 
;#34816 =  abs [#34533 *  sin[#34532]]
#34816 = #34533
;;;; Calculate the search limit ;;;;;;;;;;;;;;
#34817 = #9016 * #y ; get the sign based on the direction
#34817 = [#[5040+#34525] + #34817]
G65 "#400/system/probe_limit_position.cnc"  X[#34817] E[#34525] D[#y]
#34819 = #34900 ; this is the search limit

;M225 #100 "Search position %f" #34819

; protected move to estimated corner point
G65 "#400/system/probe_protected_move.cnc"  X[#20101] Y[#20102] Z[#20103] A[#34528] B[#34529] C[#5043]


GOTO 400 ; skip pull back 
N300 ; Loop Start

; pull back move to the primary axis location that was stored 
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#[a]]] A[#34532]

N400 ;move the secondary axis by the distance between the two lines
#34817 = #34816 * #y ; get the sign based on the direction
#34817 = [#[5040+#34525] + #34817]

; limit the target position to the limit position
if [ [#y ==  1] && [#34817 > #34819] ] then #34817 = #34819
if [ [#y == -1] && [#34817 < #34819] ] then #34817 = #34819


;m225 #100"target %f" #34817

; if currentposition - target < comparison tolerance
if [[ abs[ #[5040+#34525] - #34817 ] ] <  #34011] then #32000 = 2 ; surface not found
if [#32000 != 1] then M99 ; surface not found

G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#34525]] A[#34817]


; find the intersection between the measured line and a line at the current position and the starting axis
#34528 = 0
#34529 = 0
; set the correct offset based on the starting axis
#[34527+#[a]] = #[x]

; store the current primary axis location
#34532 = #[5040+#[a]]

G65 "#400/system/probe_move_to_intersection.cnc" A[#34540] B[#34541] C[#34542] D[#34543] W[#5041] X[#5042] Y[#5041+#34528] Z[#5042+#34529] F0 I1


; if the probe tripped go for another try around the corner
if [#34850] GOTO 300

; once we get to this point we made it to the intersection of the corner and the probed wall points!

; now move the estimated distance on the primary axis
;  m115 /first_label  current_position + (estimated distance * direction)
#34530 = [#[5040+#[a]]+[#[e] * #[x]]]
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#[a]]] A[#34530] D[1] 

; if we stopped early because of a probe trip, pull back in the secondary axis direction
if [#34850] then $#[20100+ #34525]   [ #[5040+#34525] + [#[y] *  [#9013] ]  ]  

;flip comp direction
#34536 = -#34536
; probe the first wall
G65 "#400/system/probe_two_points_on_wall.cnc" A[#34525] d[#[e]/3.0] X[-#[y]] Y[-#[x]] C[#34536]

; save return values
#34544 = #34552
#34545 = #34553
#34546 = #34550
#34547 = #34551


; protected move z up and then back to the corner
G65 "#400/system/probe_move_to_intersection.cnc" A[#34540] B[#34541] C[#34542] D[#34543] W[#34544] X[#34545] Y[#34546] Z[#34547] F[#[z]]
