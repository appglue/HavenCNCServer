;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Filename:        correct_position.cnc
; Author:           Lee Johnston, John Popvich
; Date:             February 15th 2007
; Last Modified:    July 14 2007
; Description:      Convert a 5 axis vector into 3d space for Articulated Head
; Usage:            G65 "/cncm/correct_position.cnc"  A[angle] B[angle] X[x_position] Y[y_position] Z[z_position]
; Inputs:           
;                   X[x_position] 
;                   Y[y_position] 
;                   Z[z_position]
;                   A[a_position]
;                   B[b_position]
;
; Outputs:          corrected_position_x           #136
;                   corrected_position_y           #137
;                   corrected_position_y           #138
;
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

; Calculate current tool height offset to center of rotation for the B-Axis
 #134 = #9119-#10000
 
; rotate tool offset around b-axis
 #131 = #134 * sin[#b] ; dx
 #135 = #134 * cos[#b] ; dz
 #135 = #135 - #134

 ; apply rotated vector to position
 #29651 = #X + #131
 #29652 = #Y
 #29653 = #Z + #135

 ;Rotate around the A axis
 #136 = [#29651]; // X coordinate remains the same for A rotation
 #137 = [[[#29652]*[cos[#[A]]]]-[[#29653]*[sin[#[A]]]]];
 #138 = [[[#29653]*[cos[#[A]]]]+[[#29652]*[sin[#[A]]]]];

M99
