; Filename:   probe_protected_move.cnc
; Purpose:    make a protected probe move
; Programmer: John Popovich
; Date:       Dec 1, 2010 
; Usage:      G65 "/cncm/system/probe_protected_move.cnc"  X[first_axis_label] Y[second_axis_label] Z[third_axis_label] A[x_position] B[y_position] C[z_position] D[no_error_on_trip]
; Where:      first_axis_label   0 for don't care (axis isn't changing local position)
;             second_axis_label  0 for don't care (axis isn't changing local position)
;             third_axis_label   0 for don't care (axis isn't changing local position)
;              x_position = first axis destination
;              y_position = second destination
;              z_position = third destination
;              no_error_on_trip = 0 = error on trip (default) 1 = don't error on trip 
; Notes:      
; Input:      
; Output:       #34850 1 if no_error_on_trip and the probe tripped, 0 otherwise
;               #34012 , latched boundary crossed variable
; #34953 = tripped point x (if the probed tripped) 
; #34954 = tripped point y
; #34955 = tripped point z

; Uses 34811-#34813 , #34850 
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

#34811 = #5041 * #30021
#34812 = #5042 * #30021
#34813 = #5043 * #30021
#34850 = 0

if [#x == #20101] then #34811 = #a
if [#y == #20101] then #34811 = #b
if [#z == #20101] then #34811 = #c

if [#x == #20102] then #34812 = #a
if [#y == #20102] then #34812 = #b
if [#z == #20102] then #34812 = #c

if [#x == #20103] then #34813 = #a
if [#y == #20103] then #34813 = #b
if [#z == #20103] then #34813 = #c

;m225 #100 "Protected Probe Move %c %f %c %f %c %f P%f" #20101 #34811 #20102 #34812 #20103 #34813 #34005
; perform the protected move
m125 /$#20101 #34811 /$#20102 #34812 /$#20103 #34813 p[#34005] L[#d]

; latch the boundary crossed variable
#34012 =  #25021

if [#d==0] GOTO 400 ; goto end of macro

; determine if the m115 stopped early because of a probe hit

#34850 = #25022

N400 ; end of macro

; save the tripped point
#34953 = #24501
#34954 = #24502
#34955 = #24503

;m225 #100 "after protected move"

