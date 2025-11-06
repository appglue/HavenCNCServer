; Filename:   probe_move_to_intersection
; Purpose:    find the intersection and move the probe there
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_move_to_intersection.cnc" A[line1x1] B[line1y1] C[line1x2] D[line1y2]
;                                                  W[line2x1] X[line2y1] Y[line2x2] Z[line2y2]
;                                                  F[z_clearance] I[prevent_error]
; Where:      line1x1 - line2x2 the 2 lines
;             z_clearance = the z clearing distance
;             set i = 1 if you want to continue running the job when the probe trips
; RETURN:     sets #34850 to 1 if the probe tripped, sets #34850 to zero otherwise
; Notes:      the probe will be at the intersection point at the end of the macro
; uses #120-#126
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; find the intersection point

#120 = [#[d]] - [#[b]] ; change in y for the first line  ; A1
#121 = [#[a]] - [#[c]] ; change in x for the first line  ; B1
#122 = [#[z]] - [#[x]] ; change in y for the second line ; A2
#123 = [#[w]] - [#[y]] ; change in x for the second line ; B2

; find the denominator for the intersection formula
; A1*B2-A2*B1
#124 = [[#120 * #123] - [#122 * #121]]
if [#124 == 0 ] then ERROR lines are parallel


; find C1 = A1 * x1.1 + B1 * y1.1
#125 = [#120 * #[a]]  + [#121 * #[b]]; C1
; find C2 = A2 * x2.1 + B2 * y2.1
#126 = [#122 * #[w]]  + [#123 * #[x]]; C2


; find x of intersection
; x = (B2*C1 - B1*C2)/denomintor
#121 = [[#123 * #125] - [#121 * #126]] / #124
; find y of intersection
; y = (A1*C2 - A2*C1)/denomintor
#122 = [[#120 * #126] - [#122 * #125]] / #124



; now that we have the intersection, move there

; protected move Z up by the clearance amount
m125 /$#20103 [[#5043]+[#[f]]] p[#34005]

; protected move x y to the intersection
G65 "#400/system/probe_protected_move.cnc"  X[#20101] Y[#20102] A[#121] B[#122] D[#i]

