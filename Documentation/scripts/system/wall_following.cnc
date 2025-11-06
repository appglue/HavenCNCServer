;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Filename:         wall_following.cnc
; Author:           Lee Johnston, John Popovich
; Date:             February 13th 2008
; Last Modified:    Feb 3rd 2011
;                   May 31 2011 : changed the z patch distance so that a negative value means to go down and a positive value goes up.
;                   Dec 12 2011 : requires CNC11 305r37 and up. or CNC10 207r52 and up
; Description:      WAll following / surface following digitizing routine for XY plane
; Usage:            G65 "/cncm/system/wall_following.cnc" A[tool_number] B[z_patch_depth] C[z_step_down] D[start_direction] E[outer_stepover] I[inside/outside] J[replay_feedrate] K[cw/ccw]
; Inputs:           #301 = output file name (without the extension)
;                   tool_number     = the tool number to use (for the probe) during this cycle
;                   z_patch_depth   = the total depth from the starting point (can be negative which means that the cycle will step z up)
;                   z_step_down     = the incremental amount to step in z (should always be positive)
;                   start_direction = the probe starting direction
;                                     WALL_FOLLOWING_GCODE_START_DIRECTION_X_PLUS   3
;                                     WALL_FOLLOWING_GCODE_START_DIRECTION_X_MINUS  9
;                                     WALL_FOLLOWING_GCODE_START_DIRECTION_Y_PLUS   12
;                                     WALL_FOLLOWING_GCODE_START_DIRECTION_Y_MINUS  6
;                   outer_stepover  = the distance to step between points                   
;                   inside_outside  = probing an inside or outside object
;                                     WALL_FOLLOWING_GCODE_START_INSIDE     1
;                                     WALL_FOLLOWING_GCODE_START_OUTSIDE  (-1)
;                   replay_feedrate = the feedrate to post in the .dig output file
;                   cw/ccw          = clockwise or counter clockwize
;                                     WALL_FOLLOWING_GCODE_ROTATION_CLOCKWISE           1
;                                     WALL_FOLLOWING_GCODE_ROTATION_COUNTER_CLOCKWISE (-1)
; Outputs:          
;
; NOTES:
; #120 - #125 reserved for scale_vector.cnc
; #126 - #130 reserved for rotate_vector.cnc
; #131 - #135 reserved for rotate_b_position.cnc
; #136 - #140 reserved for correct_b_position
; USES:
; #29561-3 ; first point in spline
; #29566-8 ; per axis distance to first point in spline
; #29569   ; total distance to first point in spline
; #34901  ; x step over
; #34902  ; y step over
; #34903  ; z step over
; #34906  ; last point x
; #34907  ; last point y
; #34908  ; last point z
; #34911  ; predicted point x
; #34912  ; predicted point y
; #34913  ; predicted point z
; #34917 ; dx
; #34918 ; dy
; #34919 ; dz
; #34921 ; primary step over length
; #34928 ; surface normal angle from step over
; #34929 ; number of points in this spline
; #34930 ; total number of points
; #34931 ; total z step distance so far
; #34932 ; max z depth
; #34933 ; z step amount (given as always positive)
; #34934 ; last pull back distance
; #34935 ; starting step axis
; #34936 ; starting step direction
; #34937 ; number of prediction misses
; #34938 ; probe move ingore unable to detect surface
; #34939 ; probe tripped on measuring move
; #34940 ; surface search distance (not from the parameter, max value between pullback and stepover
; #34941 ; starting probe vector
; #34942 ; starting probe vector
; #34943 ; starting probe vector
; #34944 ; next step
; #34945 ; next step
; #34946 ; next step
; #34947 ; starting point to first point vector x
; #34948 ; starting point to first point vector y
; #34949 ; starting point to first point vector z
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

#150 = 0 ; debug

G65 "#400/system/probe_get_modals.cnc"  ; get and set global and modal values

M232 ; disable boundary checking
;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Tool Change ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

G43 H#[12000+#a] T#a D#[13000+#a]  

; Initialize variables
#29681 = #5041 ; record starting point
#29682 = #5042 ; record starting point
#29683 = #5043 ; record starting point
#29684 = #5044 ; record starting point
#29685 = #5045 ; record starting point
#34933 = #c ; Z Stepdown Distance
#34933 = abs[#34933] ; should be given as a positive value only
#34932 = -#b ; Max Z Depth" Inverted.   This value is now called Z Patch Distance
#151   = 59 ; semi colon
; change the sign of z step based on the z depth sign
if [#34932 < 0] then #34933 = -#34933

; compute the step over based on the outer step over, direction, cc/ccw, and inside/outside
; starting step over axis = start direction % 2
if [#d == 3]  then #34935=2    ; xplus start
if [#d == 9]  then #34935=2    ; xminus start
if [#d == 12] then #34935=1    ; yplus start
if [#d == 6]  then #34935=1    ; yminus start

; assume inside
if [#d == 3]  then #34936 = -#k    ; xplus start
if [#d == 9]  then #34936 = #k    ; xminus start
if [#d == 12] then #34936 = #k    ; yplus start
if [#d == 6]  then #34936 = -#k    ; yminus start
#34936 = #34936 * #i ; opposite start direction for outside

; initial step overs
; #34901 = x probing step over (signed primary axis)
; #34902 = y probing step over (signed primary axis)
; #34903 = z probing step over (signed primary axis)
#34901 = 0
#34902 = 0
#34903 = 0
#[34900+#34935] = #34936 * abs[#e] ; initial step over = direction * outer stepover
;if #150 m225 #100 "step over axis = %f" #34935
; #34928 = x y angle from step over vector to a predicted probe point
; angle = 90 * cc/ccw * inside_outside
#34928 = [90 * #k * #i]

#34921 = abs[#e] ;sqrt[[#34901*#34901] + [#34902*#34902] + [#34903*#34903]] ; compute step over length
#34930 = 0 ; total number of points probed
#34931 = 0 ; total z step distance so far
; init first point to probe
#34911 = #5041 ; init to current position  
#34912 = #5042 ; init to current position
#34913 = #5043 ; init to current position
#34941 = 0 ; starting probe vector 
#34942 = 0 ; starting probe vector
#34943 = 0 ; starting probe vector
#34956 = 0 ; probe tripped on stepover flag
if [#d == 3]  then #34941 = #24601-#34911 ; xplus  limit position
if [#d == 9]  then #34941 = #24701-#34911 ; xminus limit position
if [#d == 12] then #34942 = #24602-#34912 ; yplus  limit position
if [#d == 6]  then #34942 = #24702-#34912 ; yminus limit position

;if #150 m225 #100 "step %f %f %f length %f" #34901 #34902 #34903 #34921

#106 = #j  
  ; open file 
  M120 "#301.dig"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Header ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  M223 " %c dig_type Wall Following Digitizing\n" #151
  M223 " %c Digitize Diameter %.5f\n" #151 #11000
  M223 " %c Current WCS( %.0f ) offsets\n" #151 [[#4014]-[53]]
  M223 " %c X:    %.5f Y:    %.5f Z:    %.5f A:     %.5f B:     %.5f\n" #151 #2500 #2600 #2700 #2800 #2900
  M223 " %c CSR Angle: %.5f\n" #151 #2400
  M223 " %c Center of Rotation A axis\n" #151
  M223 " %c Y:  %.5f Z:  %.5f\n" #151 #9116 #9117
  M223 " %c Center of Rotation B axis\n" #151
  M223 " %c X:  %.5f Z:  %.5f\n" #151 #9118 #9119
  M223 " %c ************************************************************************* \n" #151
  M223 "\n"
  M223 "M25\n"
  M223 "G0"
  G65 "#400/system/probe_write_point_to_file.cnc" X#29681 Y#29682 Z#29683 D1
  M223 " %c no data\n" #151
  M223 "G1 F%.4f" #106
  M223 "\n"



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Main Loop ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  
N1000 ; new spline 
  #34929 = 0 ; number of points probed in this spline
  #34916 = 1 ; set the first point in spline flag
  if #150 m225 #100 "New spline"
  ; compute first vector using the starting probe vector
  #34911 = #34941 + #5041
  #34912 = #34942 + #5042
  #34913 = #34943 + #5043
  M223 "\n" ; insert a blank line at the start of spline
N1100 ; next point
#34937 = 0 ; reset the number of prediction misses
N1150 ; probe or probe again (after a prediction miss)
;if [#34929 > 30] then #150 = 1

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; Probe Point ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; probe point and retract
; do not use the C1 compensation since we may be using m227 stylus compensation
; m227 compensation will not be added to the return values of probe_move.cnc
;m225 #100 "searching to x %f y %f z %f" #34911 #34912 #34913
; the W1 tells probe_move to pull back by the pull back parameter
; the F1 tells probe_move to pull back by the entire pullback even if it is passed the starting point
if #150 m225 #100 "probing to %f %f %f" #34911 #34912 #34913


#34938 = 2 ; ignore surface not found flag
if [#34929==0] then #34938 = 0 ; fail if the surface isn't found on the first point in the spline
;G65 "#400/system/probe_move.cnc"  X#34911 Y#34912 Z#34913  W1 C0 H[#34929!=0] I#34938 F1
G65 "#400/system/probe_move.cnc"  X#34911 Y#34912 Z#5043  W1 C0 H[#34929!=0] I#34938 F1 B1

; if we crossed a boundary, its either the end of the spline or we need to try again 
if #150 then if [#34012 == 1] then m225 #100 "boundary on probe move"
if [#34012 == 0] then GOTO 1155 ; no boundary crossed, goto save positions
if #150 then m225 #100 "boundary crossed checking num points %f" #34929
if [#34929 < 5]  then GOTO 1150 ; boundary crossed but we haven't probed enough points, try again
;here the boundary was crossed and we probed enough points
if #150 then m225 #100 "after probe move going to end of spline"
GOTO 1625 ; goto end of spline found

N1155 ; save positions  
#34939 = #34016 ; save the probe tripped state

if [#34939 == 0] then GOTO 1160 ; skip this when the probe didn't trip
; compute and store the last pullback for the next predicted point
#34917 = #5041 - #34006 ; dx = current position - probed x
#34918 = #5042 - #34007 ; dy = current position - probed y
#34919 = #5043 - #34008 ; dz = current position - probed z
if [#34930 != 0] then GOTO 1160 ; only compute the pullback on the first move
;#34934 = sqrt[[#34917*#34917]+[#34918*#34918]+[#34919*#34919]] ; distance from probed point 
#34934 = sqrt[[#34917*#34917]+[#34918*#34918]] ; distance from probed point 
N1160

if #150 then m225 #100 "boundary %f"  #34012
if [#34012 == 1 ] then GOTO 1625 ; if the boundary was crossed on the measuring move, goto end of spline

;;;;;;;;;;;;;;;;;;;;;;; Handle Probe not tripped ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; #34939 will be set to one if the probe tripped, 0 if it didn't (the software sets it)
if [#34939 != 0] then GOTO 1175
#34937 = #34937 + 1 ; increment prediction misses
if #150 then m225 #100 "Probe not tripped miss number %f" #34937
; if this is the fourth miss then fail
if [#34937 >= 4] then #32000 = 2 ; surface not found
if [#32000 == 2] then M99
if #150 then m225 #100 "probe didn't trip rotating %f degrees %f %f %f" #34928 #34917 #34918 #34919


; the probe didn't trip, rotate the probing vector and try again
GOTO 1700

N1175

;;;;;;;;;;;;;;;;;;;;;;; Handle first point ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
if [#34916 != 1] then GOTO 1200 ; not the first point then goto 1200
; fake the last point after the first move
#34906 = #34006
#34907 = #34007
#34908 = #5043
; record the first point in the spline
#29561 = #34006
#29562 = #34007
#29563 = #5043

#34947 = #29681 - #29561 ; compute starting point to first point vector x
#34948 = #29682 - #29562 ; compute starting point to first point vector y
#34949 = sqrt[[#34947*#34947]+[#34948*#34948]] ; starting vector length
if [#34949 < #34934] then #34949 = #34934
;if the length of the starting vector is less than the pull back scale the vector to the pullback
G65 "#400/system/scale_vector.cnc" R[#34949*1.5] X[#34947] Y[#34948] Z[0]
#34947 = #29631
#34948 = #29632
N1180

M228 ; reset the gcode line list
M229 /x #29561 /y #29562; add start point
M230 /x [#29561+#34947] /y [#29562+#34948]; add end point
M231 ; enable boundary checking

N1200


;;;;;;;;;;;;;;;;;;;;;;  Save Point to File ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; record point to file, including the stylus compensation
;if #150 m225 #100 "Saving point to file" 
;G65 "#400/system/probe_write_point_to_file.cnc" X[#34006+#34013] Y[#34007+#34014] Z[#34008+#34015]
G65 "#400/system/probe_write_point_to_file.cnc" X[#34006+#34013] Y[#34007+#34014] Z[#5043]

; Update points probed:
#34929 = #34929+1
#34930 = #34930+1
N1300
; compute the search distance for when a surface isn't found
#34940 = #34934  ; use the maximum value between the pullback and the step over
if [#34921 > #34934] then #34940 = #34921
#34940 = #34940*2

; compute step over
#34917 = #34006 - #34906 ; dx = x2-x1
#34918 = #34007 - #34907 ; dy = y2-y1
#34919 = #34008 - #34908 ; dz = z2-z1

; store our last point for surface prediction
if [#34956] then GOTO 1350 
#34906 = #34006
#34907 = #34007
#34908 = #34008
N1350

; if this is the first point in the spline just use the initial step over
if [#34916 != 1] then GOTO 1400
#34917 = #34901
#34918 = #34902
#34919 = #34903
if #150 m225 #100 "first point step %f %f %f" #34901 #34902 #34903
GOTO 1600
N1400
;  we want a parallel line scaled to the step over amount
;  compute new deltas which we can add to the starting point to give us 
;  the step over

; L1 = sqrt(dx^2 + dy^2 + dz^2)  this is the length between points
#34920 = sqrt[[#34917*#34917] + [#34918*#34918] + [#34919*#34919] ]

if [#34920 == 0] then GOTO 1500 ; goto prevent zero length vectors
#34917 = #34921 * [#34917 / #34920] ; dx2 = step_over  * dx1/vector_length
#34918 = #34921 * [#34918 / #34920] ; dy2 = step_over  * dy1/vector_length
#34919 = 0 ;#34921 * [#34919 / #34920] ; dz2 = step_over  * dz1/vector_length
GOTO 1600

N1500 ; zero length vector
#32000 = 2 ; surface not found
M99 ; end macro with error

N1600 ; predict point

#34916 = 0 ; reset the first point in spline flag
#34937 = 1 ; reset the probe not tripped flag
;;;;;; now that we have step over deltas, compute our step over
; scale the probing vector to the pullback
G65 "#400/system/scale_vector.cnc" R[#34934] X[#34917] Y[#34918] Z[#34919]
; rotate it 90 degrees
G65 "#400/system/rotate_vector.cnc" A[-#34928] X[#29631] Y[#29632] Z[#29633]
; add it to the predicted probe point and compute a vector to the stepover
#34944 = #34906 + #34917 + #29641 - #5041
#34945 = #34907 + #34918 + #29642 - #5042
#34946 = 0;#34908 + #34919 + #29643 - #5043
if #150 m225 #100 "Step Over %f %f %f" #34944 #34945 #34946

; if the probe tripped on the last step over, skip ahead to predict point
if [#34956] then GOTO 1700

;;;;;;;;;;;;;;;;;;;;;;; step over ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;G65 "#400/system/probe_step_over.cnc" X[#34944] Y[#34945] Z[#34946] W1
G65 "#400/system/probe_step_over.cnc" X[#34944] Y[#34945] Z[0] W1

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; spline check ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
if #150 M225 #100 "boundary crossed %f" #34012
if #150 then if [#34012== 1] then m225 #100 "after step boundary found"
if [#34012== 0] then GOTO 1650 ; if a boundary was crossed on the step over
if #150 then m225 #100 "boundary crossed after step num points %f" #34929
if [#34929 < 5] then GOTO 1650 ; and we probed enough points
                               ; then this is the end of the spline
N1625 ; end of spline found
M232 ; disable boundary checking
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; step down ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; if we get here, the end of spline was found.  Perform the step down

; close the spline  by outputting the first point
G65 "#400/system/probe_write_point_to_file.cnc"  X#29561 Y#29562 Z#29563 

;#34931 = z depth so far
;#34932 = max z depth
;#34933 = signed z step
;#29681-3 = starting point
if #150 m225 #100 "End of Spline" 

; if the total step down is greater or equal to the max, then we are done
;if [[abs[#34931]] >= [abs[#34932]]] then GOTO 9999 ; goto end of cycle
if #150 m225 #100 "checking for finish %.6f %.6f %.6f" #34931 #34932 #34011
if [[abs[abs[#34931]- abs[#34932]]] < #34011] then GOTO 9999


if #150 m225 #100 "moving xy to start point %f %f" #29681 #29682
; move to start xy position before stepping down:
G65 "#400/system/probe_protected_move.cnc"  X#20101 Y#20102 A#29681 B#29682 D0

; calculate the next step down position
#34931 = #5043 + #34933
; limit the step down to the max z depth
if [[abs[#29683 - #34931]] > abs[#34932]] then #34931 = #29683+#34932

if #150 m225 #100 "stepping z down %f" #34931
;// perform the step down move:
G65 "#400/system/probe_protected_move.cnc"  Z#20103 C[#34931] D0

; calculate the z step so far
#34931 = abs[#29683 - #5043] ; starting point minus current position

GOTO 1000 ;start new spline

N1650
;;;;;;;;;;;;;;;;;;;;;;;;;;;;; end spline check ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;


;;;;;;;;;;;;;;;;;;; Probe tripped on step over ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; if the probe tripped during the step over, use the step over vector to make a measuring move
if [#34850 == 0] then GOTO 1700
#34956 = 1 ; set the probe tripped on last step flag
#34850 = 0 ; reset the probe tripped on step flag
#34006 = #34953 ; copy the tripped position from probe_step_over
#34007 = #34954 ; copy the tripped position from probe_step_over
#34008 = #5043; #34955 ; copy the tripped position from probe_step_over
if #150 then m225 #100 "probe tripped on step"
GOTO 1300 ; recompute next point
N1700
#34956 = 0 ; reset the probe tripped on last step flag
; rotate the stepover vector 90 degrees to get the predicted point
G65 "#400/system/rotate_vector.cnc" A[#34928] X[#34917] Y[#34918] Z[#34919]
; store the probing vector
#34917 = #29641
#34918 = #29642
#34919 = 0; #29643

N1800
; now take the predicted point and scale it to twice the maximum between the pullback and the stepover
G65 "#400/system/scale_vector.cnc" R[#34940] X[#34917] Y[#34918] Z[#34919]
; store the probing vector
#34917 = #29631
#34918 = #29632
#34919 = 0; #29633

;compute the predicted point
#34911 = #5041 + #34917 
#34912 = #5042 + #34918 
#34913 = #5043 ;+ #34919 
if #150 then if [#34850 == 1] then m225 #100 "probe tripped on step next probe %f %f %f" #34917 #34918] #34919

; the probe didn't trip try again
;if #150 m225 #100 "tripped flag %f" #34939
if [#34939 == 0] then GOTO 1150 ; goto probe again

if #150 then "end main loop"
GOTO 1100 ; End main_loop
N9999 ; digitizing finished
	
; reset the error variable to finished
#32000 = 0
