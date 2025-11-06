; Filename:         vertical_wall_follow.cnc
; Author:           Lee Johnston, John Popovich
; Description:      Vertical wall following digitizing
; Usage:            G65 "/cncm/system/vertical_wall_follow.cnc" A[tool_number] B[z_max_depth] D[start_direction] E[x_max] F[y_max] X[x_step] Y[y_step]
; Inputs:           A[tool_number] B[z_max_depth] D[start_direction] E[x_max] F[y_max] X[x_step] Y[y_step] #300[filename]
;                   GRID_DIG_VAR_TOOL_NUMBER     #33504   tool number to use from the tool libray
; Outputs:          
; Notes:     Caller must open a file for recording before calling this macro
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


; get global probe settings
G65 "#400/system/probe_get_modals.cnc"

; change tool
G43 H#[10000+#[12000+#33504]] T#33504 D#[11000+#[13000+#33504]]  


;;;; initialize variables ;;;;;;;;
#34916 = 0 ; reset loop counter
#34911 = #5041  
#34912 = #5042
#34913 = #24703  ; set z to the minus search limit

; TODO this must be passed in from cnc10
#34921 = .01 ; setup step over
#34922 = 0 ; setup step angle

N100 ; main loop start

; probe point and retract
; do not use the C1 compensation since we may be using m227 stylus compensation
; m227 compensation will be added to the return values of probe_move.cnc
G65 "#400/system/probe_move.cnc"  X#34911 Y#34912 Z#34913  W1 C0 H[#34916==0]
; the W1 tells probe_move to pull back by the pull back parameter

; fake the last point after the first move
if [#34916 == 0] then #34906 = #34006
if [#34916 == 0] then #34907 = #34007
if [#34916 == 0] then #34908 = #34008

; record point to file
G65 "#400/system/probe_write_point_to_file.cnc" X#34006 Y#34007 Z#34008 B#34012


; predict step over and next probed point

#34917 = #34006 - #34906 ; dx = x2-x1
#34918 = #34007 - #34907 ; dy = y2-y1
#34919 = #34008 - #34908 ; dz = z2-z1
 
; L1 = sqrt(dx^2 + dy^2 + dz^2)  this is the length between points
#34920 = sqrt[[#34917*#34917] + [#34918*#34918] + [#34919*#34919] ]

;  we want a parallel line scaled to the step over amount
;  compute new deltas which we can add to the starting point to give us 
;  the step over and we can add them to the last probed point for our
;  predicted probe point
if [#34920 == 0] then GOTO 200 ; goto prevent zero length vectors
#34917 = #34921 * [#34917 / #34920] ; dx2 = step_over  * dx1/vector_length
#34918 = #34921 * [#34918 / #34920] ; dy2 = step_over  * dy1/vector_length
#34919 = #34921 * [#34919 / #34920] ; dz2 = step_over  * dz1/vector_length
GOTO 300
N200

; if the vector length is zero, this must be the first move
; or we probed the same point twice 
; use the step angle to determine the new step position

#34917 = #34921 sin [#34922] ; dx = hyp sin theta
#34918 = #34921 cos [#34922] ; dy = hyp cos theta
#34919 = 0 ; no change in z
N300



; step over
G65 "#400/system/probe_step_over.cnc" X[#5041 + #34917] Y[#5042 + #34918] Z[#5043 + #34919]


; store our last point for surface prediction
#34906 = #34006
#34907 = #34007
#34908 = #34008

 


#34916 = #34916 +1 ; increment loop counter

GOTO 100 ; goto main loop start

