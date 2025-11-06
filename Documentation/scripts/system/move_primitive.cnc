; Filename:   move_primitive.cnc
; Purpose:    Move and handle cases where some axes are disabled
; Programmer: John Popovich
; Date:       OCT 7, 2011 
; Usage:      G65 "/cncm/system/move_primitive.cnc" x[x_position] y[y_position] z[z_position]
;
; Where:      
;             x_position = the first axis position to move to
;             y_position = the second axis position to move to
;             z_position = the third axis position to move to
; Notes:      WARNING do not add an N0 to this macro, it will fail when either x,y,or z is disabled
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


;Check for N labels
;  the parser will evaluate them as being Block numbers
;  this is harmless unless there is a GOTO in the same file
if [#20101 == 78] THEN #[x] = 0
if [#20102 == 78] THEN #[y] = 0
if [#20103 == 78] THEN #[z] = 0

; make the move
$#20101 #x $#20102 #y $#20103 #z 

