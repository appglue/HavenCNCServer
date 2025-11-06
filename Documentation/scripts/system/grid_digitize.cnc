; Filename:         vertical_wall_follow.cnc
; Author:           Lee Johnston, John Popovich
; Description:      Vertical wall following digitizing
; Usage:            G65 "/cncm/system/vertical_wall_follow.cnc"
; Inputs:                                                                            
; GRID_DIG_VAR_TOOL_NUMBER     #33504
; GRID_DIG_VAR_TOOL_DESC       #300
;
; GRID_DIG_VAR_X_STEP_OVER     #33500
; GRID_DIG_VAR_Y_STEP_OVER     #33501
; GRID_DIG_VAR_DIRECTION       #33502
;   GRID_DIG_VAR_DIRECTION_X  0
;   GRID_DIG_VAR_DIRECTION_Y  1
; GRID_DIG_VAR_PATTERN         #33503
;   GRID_DIG_VAR_PATTERN_ZIG_ZAG    0
;   GRID_DIG_VAR_PATTERN_ONE_WAY    1
; GRID_DIG_VAR_TOOL_NUMBER     #33504
; GRID_DIG_VAR_REPLAY_FEEDRATE #33505
; GRID_DIG_VAR_STEP_MODE       #33506
;   GRID_DIG_VAR_STEP_MODE_NORMAL 0
;   GRID_DIG_VAR_STEP_MODE_ANGLE  1
; GRID_DIG_VAR_CENTER_X       #33507
; GRID_DIG_VAR_CENTER_Y       #33508
; GRID_DIG_VAR_MAX_Z_DEPTH    #33509
; GRID_DIG_VAR_RADIUS         #33510
;
;
; Outputs:          
; Notes:     Caller must open a file for recording before calling this macro
;
; USES: 
; #34901  ; x step over
; #34902  ; y step over
; #34903  ; z step over
; #34906  ; last point x
; #34907  ; last point y
; #34908  ; last point z
; #34911  ; predicted point x
; #34912  ; predicted point y
; #34913  ; predicted point z
; #34916 ; loop counter
; #34917 ; dx
; #34918 ; dy
; #34919 ; dz
; #34920 ; vector length
; #34921 ; primary step over length
; #34922 ; step over angle
; #34923 ; secondary step over
; #34924 ; last step boundary crossed
; #34925 ; secondary step angle
; #34926 ; last pass var
; #34927 ; secondary step (1 if this is a secondary step)
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; get global probe settings
G65 "#400/system/probe_get_modals.cnc"

; change tool
G43 H#[12000+#33504] T#33504 D#[13000+#33504]  


;;;; initialize variables ;;;;;;;;
#34916 = 0 ; reset loop counter
#34911 = #5041   ; first predicted probe point x
#34912 = #5042   ; first predicted probe point y
#34913 = #24703  ; first predicted probe point z set to the negative search limit

#34921 = #[33500+#33502] ; get primary step over
#34923 = #[33500+[!#33502]] ; get secondary step over
#34922 = 90*#33502 ; setup step angle
#34925 = 90*[!#33502] ; setup step angle
#34926 = 0 ; last pass variable
#34924 = 0 ; last boundary crossed (primary axis)

N100 ; main loop start

; probe point and retract
; do not use the C1 compensation since we may be using m227 stylus compensation
; m227 compensation will be added to the return values of probe_move.cnc
;m225 #100 "searching to x %f y %f z %f" #34911 #34912 #34913
G65 "#400/system/probe_move.cnc"  X#34911 Y#34912 Z#34913  W1 C0 H[#34916!=0] I1

; the W1 tells probe_move to pull back by the pull back parameter

; fake the last point after the first move
if [#34916 == 0] then #34906 = #34006
if [#34916 == 0] then #34907 = #34007
if [#34916 == 0] then #34908 = #34008


; record point to file, including the stylus compensation
G65 "#400/system/probe_write_point_to_file.cnc" X[#34006+#34013] Y[#34007+#34014] Z[#34008+#34015]

; if a boundary is crossed and we haven't moved, then it's time to do a secondary axis stepover 
if [!#34924] then GOTO 150

  if [#34926] then GOTO 10000 ; if this is the last pass then end the macro
  
  #34917 = #34923 * cos [#34925] ; dx = hyp cos theta
  #34918 = #34923 * sin [#34925] ; dy = hyp sin theta
  #34919 = 0 ; no change in z
  
  ; flip primary axis direction
  #34921 = -#34921
  
  #34927 = 1 ; set the secondary step flag
  goto 300 ; predict point
N150
  #34927 = 0 ; reset the secondary step flag
  
goto 200
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

#34917 = #34921 * cos [#34922] ; dx = hyp cos theta
#34918 = #34921 * sin [#34922] ; dy = hyp sin theta
#34919 = 0 ; no change in z
N300 ; predict point

;;;;;; now that we have deltas, compute our predicted point
#34911 = #34006 + #29631 ; predicted probe point x
#34912 = #34007 + #29632 ; predicted probe point y
#34913 = #34008 + #29633 ; predicted probe point z

G65 "#400/system/scale_vector.cnc" R[#9016] X[#34911-#504] Y[#5042-#34912] Z
m225 #100 "vector %f %f %f Scaled %f %f %f" #34917 #34918 #34919 #29631 #29632 #29633

; store our last point for surface prediction
#34906 = #34006
#34907 = #34007
#34908 = #34008

; step over
G65 "#400/system/probe_step_over.cnc" X[#34917] Y[#34918] Z[#34919]

; if secondary step boundary crossed then we are on the last pass
if #34927 && #34012 then #34926 = 1

#34924 = #34012 ; save last boundary crossed

if [#34927] then #34924 = 0 ; reset the last boundary crossed flag if this is a secondary step to prevent another secondary step   

#34916 = #34916 +1 ; increment loop counter

GOTO 100 ; goto main loop start

N10000 ; end of macro
; clear the general error variable if it hasn't changed from 1
if [#32000 == 1] then #32000 = 0 ;


