; Filename:   probe_angle.cnc
; Purpose:    probe for an angle
; Programmer: John Popovich
; Date:       Dec 1, 2010 
; Usage:      G65 "/cncm/system/probe_angle.cnc"  
; Where:      
; Notes:      this is the Auto method only
; Input:      #34592 ; probing_axis
;             #34593 ; probing_direction
;             #34594 ; surface_axis
;             #34595 ; surface_direction
;             #34596 ; distance
;             #34597 ; z clearance
; Output:     #34601-3 ; first point
;             #34701-3 ; second point
; uses #34598 #34810 #34818
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; set probing macro modals
G65 "#400/system/probe_get_modals.cnc"  

; store the start point
#34598  = #[5040+#34592]
; store the z starting point
#34810  = #5043

; probe first point
G65 "#400/system/probe_move.cnc"  D[#34593] X[#[20100+#34592]] Y78 Z78 W1 C0

; store the probed point for the specified axis
#34601 = #34006
#34602 = #34007
#34603 = #34008 
;move back to the start point
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#34592]] A[#34598]

if [#34592 != 3] then GOTO 50 ; goto first clearance traverse

; protected move Z up to the clearance position
G65 "#400/system/probe_protected_move.cnc"  X[#20103] A[#5043+#34597]

; protected move the specified axis by the clearance distance
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#34594]] A[[ [#[5040+#34594]]  + [#34596 * #34595] ]]

; protected move Z down by the clearance amount
G65 "#400/system/probe_protected_move.cnc"  X[#20103] A[#5043-#34597]


GOTO 100

N50 ; first clearance traverse
; clearance traverse without moving further in the surface axis than the specified distance
G65 "#400/system/probe_clearance_traverse.cnc"  X[#34596] Z[#34597] E[#34594] D[#34595] A[0] F[#34000] B[#34000]

; if the z height isn't correct, the clearance traverse failed
;  move out in the primary axis until the z height is reached
#34818 = abs[#34810 - #5043]

;this has been changed to used the latched probe tripped flag
;if [#34818 < #34011] then GOTO 100 ; clearance traverse success, skip second clearance
if [![#25022]] then GOTO 100 ; clearance traverse success, skip second clearance

; if the probing axis is z then skip the  second clearance
if [#34592 == 3] then GOTO 100

; protected move Z up to the clearance position
G65 "#400/system/probe_protected_move.cnc"  X[#20103] A[#34810+#34597]

; second clearance traverse in primary axis 10 % of the surface distance
G65 "#400/system/probe_clearance_traverse_across_and_down.cnc"  X[abs[#34596] * .1] Z[#34810] E[#34592] D[-#34593] A[abs[#34596] * .1] F[#34000] B[#34000]

; check the return status of clearance traverse
if [#32000 != 1] then M99

; store the start point
#34598  = #[5040+#34592]

N100 ; skip second clearance traverse

; probe second point
G65 "#400/system/probe_move.cnc"  D[#34593] X[#[20100+#34592]] Y78 Z78 W1 C0

; store the probed point for the specified axis
#34701 = #34006
#34702 = #34007
#34703 = #34008 

;move back to the start point
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#34592]] A[#34598]

; clear the general error variable
#32000 = 0;

