; Filename:   probe_move_to_surface.cnc
; Purpose:    move the probe to a surface 
; Programmer: John Popovich
; Date:        Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_move_to_surface.cnc"
; Input:
;             #34589 axis index to move
;             #34590 direction to move 1 , -1
;             #34592 ; 0 for probing, 1 for TT1
;
; OUTPUT:     
;             #34574 // output x position
;             #34575 // output y position
;             #34576 // output z position
; USES: #34591
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

G65 "#400/system/probe_get_modals.cnc"  


if [#34592 == 1] then GOTO 100 ; goto use tt1

#34591 = 0 ; tell probe move to not pull back by default
if [#9155 == 2] then #34591 = 1 ; dp7 should move away from surface 
G65 "#400/system/probe_move.cnc"  D[#34590] X[#[20100+#34589]] Y78 Z78 W[#34591] C0 J1  

; if this is a dsp probe, insert a move to the dsp position
if[#9155 != 1 ] then GOTO 50
;$#20101 #34006 $#20102 #34007 $#20103 #34008
G65 "#400/system/move_primitive.cnc" X[#34006] Y[#34007] Z[#34008]
N50

GOTO 200 ; goto copy return values

N100 ; use tt1
G65 "#400/system/probe_tt1_move.cnc"  D[#34590] Z[#[20100+#34589]]

N200 ; copy return values

#34574 = #34006
#34575 = #34007
#34576 = #34008

;m225 #100 " %f %f %f" #34006 #34007 #34008
N1000 ; end macro
; clear the general error variable
#32000 = 0;

