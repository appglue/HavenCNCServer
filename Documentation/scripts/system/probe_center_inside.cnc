
; Filename:   probe_center_inside
; Purpose:    Find the center in the specified axis inside of a hole, bore, or slot
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_center_inside.cnc"  E[axis_index]
; Where:      axis_index = 1 through max axis
; Notes:      the probe will be at the center point at the end of the macro
; Output  #34506 = found width + probe diameter
;         #34507 = first measured point
;         #34508 = second measured point
; 
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; store the start point
#34506 = #[5040+[#[e]]]  

 ; probe negative
G65 "#400/system/probe_move.cnc"  D-1 X[#[20100+#[e]]] Y78 Z78 W1

; store the probed point for the specified axis

#34507 = [#[34005 + [#[e]]]]  

; probe positive
G65 "#400/system/probe_move.cnc"  D1 X[#[20100+#[e]]] Y78 Z78 W1

; store the probed point for the specified axis
#34508 = [#[34005 + [#[e]]]]  

; calculate the center
; center = point1 + (point2-point1)/2 
#34506 = #34507 + [[#34508 - #34507]/2]  

;m225 #100 "moving to center %f" #34506
; protected move to center
G65 "#400/system/probe_protected_move.cnc"  X[#[20100+#[e]]] A[#34506]

; calculate the width
#34506 = abs[#34508 - #34507] + #34009
