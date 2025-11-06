
; Filename:         probe_step_over.cnc
; Author:           John Popovich
; Description:      Step over in a digitizing macro
; Usage:            G65 "/cncm/system/probe_step_over.cnc" X[x_step] Y[y_step] Z[z_step] W[pull_back]
; Where:            x_step = the x step over amount
;                   y_step = the y step over amount
;                   z_step = the z step over amount
;                   pull_back = 1 if the probe should pull back on unexpected contact
; Inputs:           
; Outputs:     #29551 x distance from the starting point to the probed point
;              #29552 y distance from the starting point to the probed point
;              #29553 z distance from the starting point to the probed point
;              #34850 will be set to one if the probe tripped
;              #34012 will be set to one if a boundary was crossed
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


; record the starting point
#34950 = #5041 * #30021
#34951 = #5042 * #30021
;#34952 = #5043  * #30021

; attempt the step over move
; returns #34850 = 1 if the probe tripped
G65 "#400/system/probe_protected_move.cnc"  X#20101 Y#20102 Z#20103 A[[#5041 * #30021]+#x] B[[#5042 * #30021]+#y] C[[#5043 * #30021]] D[#w]
if [#w == 0 ]     then goto 400 ; goto end of macro
if [#34850 == 0 ] then goto 400 ; goto end of macro
if [#34012 == 1 ] then goto 400 ; goto end of macro

 ; compute a vector from the starting point to the probed point
#29551 =  #24501 - #34950
#29552 =  #24502 - #34951
;#29553 =  #24503 - #34952

; pull back if the probe tripped
G65 "#400/system/probe_pull_back.cnc" A[#34950] B[#34951] C[#5043 * #30021] X[#24501] Y[#24502] Z[#5043 * #30021] F1

N400
