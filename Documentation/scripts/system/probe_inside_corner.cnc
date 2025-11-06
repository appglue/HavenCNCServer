; Filename:   probe_inside_corner.cnc
; Purpose:    Find an inside corner
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_inside_corner.cnc" x[initial_x_direction] y[initial_y_direction] Z[clearance_height]   

; Where:      initial_x_direction = the initial direction for the first  axis +1 or -1
;             initial_y_direction = the initial direction for the second axis +1 or -1
;             clearance height   = the z clearance height
; Notes:      the probe will be at the intersection point at the end of the macro
; uses #34517 -#34524
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


#34521 = #5041 ; store the intial x axis position
#34522 = #5042 ; store the intial y axis position

#34558 = -1 ; comp direction
if [#x == #y] then #34558 = -#34558 ; flip the initial comp direction


; probe point #1 for the x axis wall
G65 "#400/system/probe_move.cnc"  D[#[x]] X[#20101] Y78 Z78 W1

; store the probed point for the x axis
#34517 = #34006
#34518 = #34007

; move x back 2/3 of the way to the initial position
G65 "#400/system/probe_protected_move.cnc"  X[#20101] A[#34521 + [[#34517-#34521]/3]]


; probe point #2 for the y axis wall
G65 "#400/system/probe_move.cnc"  D[#[y]] X[#20102] Y78 Z78 W1

; store the probed point for the y axis
#34523 = #34006  
#34524 = #34007

; move y back 2/3 of the way to the initial position
G65 "#400/system/probe_protected_move.cnc"  X[#20102] A[#34522 + [[#34524-#34522]/3]]

; probe point #2 for the x axis wall
G65 "#400/system/probe_move.cnc"  D[#[x]] X[#20101] Y78 Z78 W1

; store the probed point for the x axis
#34519 = #34006  
#34520 = #34007

; move x back to the initial position
G65 "#400/system/probe_protected_move.cnc"  X[#20101] A[#34521]

; probe point #1 for the y axis wall
G65 "#400/system/probe_move.cnc"  D[#[y]] X78 Y[#20102] Z78 W1

; store the probed point for the y axis
#34521 = #34006  
#34522 = #34007

; compensate for the probe radius
G65 "#400/system/probe_comp_two_points_on_a_line.cnc" c[#34517] d[#34518] x[#34519] y[#34520] a[#34558]
#34517 = #34585
#34518 = #34586
#34519 = #34587
#34520 = #34588

#34558 = -#34558 ; flip the comp direction

; compensate for the probe radius
G65 "#400/system/probe_comp_two_points_on_a_line.cnc" c[#34521] d[#34522] x[#34523] y[#34524] a[#34558]
#34521 = #34585
#34522 = #34586
#34523 = #34587
#34524 = #34588

; find the intersection of the two lines
G65 "#400/system/probe_move_to_intersection.cnc" A[#34517] B[#34518] C[#34519] D[#34520] W[#34521] X[#34522] Y[#34523] Z[#34524] F[#[z]]



 
