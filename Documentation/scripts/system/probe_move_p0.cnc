; Filename:   probe_move_p0.cnc
; Purpose:    A generic 3 axis probing move which handles the differences between probe types
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_move.cnc"  X[x_position] Y[y_position] Z[z_position] W[pull_back] C[compensate] B[ingore_z]
; H[skip_fast_move] I[ingore_unable_to_detect_surface] F[force_pullback_distance] 
; Where:      x_position = the first axis' position to move to
;             y_position = the second axis' position to move to
;             z_position = the third axis' position to move to   
;             pull_back = if 1 then the probe will retract by the minimum distance of the pull back distance (parameter 13) and the distance to the initial point
;             compensate = set to 1 to compensate for the probe radius
;             skip_fast_move = 1 to skip the fast move in (for mechanical and dp7 probes)
;             ingore_unable_to_detect_surface = set to 0 for error when a surface wasn't found
;                                               set to 1 for don't error when a surface wasn't found
;                                               set to 2 don't error and skip any pullback when the surface isn't found
;                                                  note that any return values are invalid if the probe doesn't trip (except for the probe tripped variable)
;             force_pullback_distance = set to one to force the pullback distance, even if it is passed the starting point
; Usage2:     G65 "/cncm/system/probe_move.cnc"  D[direction] X[x_axis_label] Y[y_axis_label] Z[y_axis_label] W[pull_back] C[compensate] H[skip_fast_move] I[ingore_unable_to_detect_surface] K[stop_at_travel_limit]
; Where       direction = -1 for negative or 1 positive
;             x_axis_label = label of the first axis, set to N (78) if the axis shouldn't move
;             y_axis_label = label of the first axis, set to N (78) if the axis shouldn't move
;             z_axis_label = label of the first axis, set to N (78) if the axis shouldn't move
;             pull_back = if 1 then the probe will retract by the minimum distance of the pull back distance (parameter 13) and the distance to the initial point
;             compensate = set to 1 to compensate for the probe radius
;             skip_fast_move = 1 to skip the fast move in (for mechanical and dp7 probes)
;             ingore_unable_to_detect_surface = set to 0 for error when a surface wasn't found
;                                               set to 1 for don't error when a surface wasn't found
;                                               set to 2 don't error and skip any pullback when the surface isn't found
;                                                  note that any return values are invalid if the probe doesn't trip (except for the probe tripped variable)
;             force_pullback_distance = set to one to force the pullback distance, even if it is passed the starting point
;             stop_at_travel_limit  = 0 to stop at the search distance or the travel limit, 1 to ignore the search distance and use the travel limit
; OUTPUT:     #34006 = x probed point
;             #34007 = y probed point
;             #34008 = z probed point
;             #34012 = latched boundary crossed variable
;             #34013 = latched stylus compensation value x
;             #34014 = latched stylus compensation value y
;             #34015 = latched stylus compensation value z
;             #34016 ; latched probe tripped value
;
; Notes:      uses variables 
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


; 3 axis probing move
; works for up to 3 axes x, y, z  but you do not have to have 3 axes to use this macro
; if the direction is specified, the probing move will move the search distance or to the travel limit
;
; Uses #34500 #34800
;
#34016 = 0 ; reset probe tripped flag
if [#[#34002]] then  ;  force synchronization with the motion control card


; store x y z 
#34800 = 0
#34801 = #x
#34802 = #y
#34803 = #z

if [#d==0] GOTO 50
if [#d==-1] #34800 = 100

#34801 = #5041 * #30021
#34802 = #5042 * #30021
#34803 = #5043 * #30021

; get the axis limit positions
if [#j==1] then GOTO 30 ; goto stop at the travel limit ignoring the search distance
;stop at the travel limit or search distance
if [#x == #20101 || #y == #20101 || #z == #20101 ] then #34801 = #[24601+#34800] * #30021
if [#x == #20102 || #y == #20102 || #z == #20102 ] then #34802 = #[24602+#34800] * #30021
if [#x == #20103 || #y == #20103 || #z == #20103 ] then #34803 = #[24603+#34800] * #30021
GOTO 50
N30 ;stop at the travel limit ignoring the search distance
if [#x == #20101 || #y == #20101 || #z == #20101 ] then #34801 = #[26201+#34800] * #30021
if [#x == #20102 || #y == #20102 || #z == #20102 ] then #34802 = #[26202+#34800] * #30021
if [#x == #20103 || #y == #20103 || #z == #20103 ] then #34803 = #[26203+#34800] * #30021
N50


; check for invalid parameters
if [#d != 0] && [#d != 1] && [#d != -1] then ERROR;  Invalid direction

#34500 = 116 ; default to m116
if [#d==-1] then #34500 = 115 ; use m115 for negative direction

; store the current start point
#34501 = #5041 * #30021
#34502 = #5042 * #30021
#34503 = #5043 * #30021

F[#34000]

if [#h != 0] && [#9155 != 1 ] then GOTO 75; skip the fast search move if told to
; do the probing move!
;m225 #100 "%f %f %f" #34801 #34802 #34803
if [#i==0] then M115  /$#20101 #34801 /$#20102 #34802 /$#20103 #34803 p0 Q1   ; move to the position specified
if [#i!=0] then M115  /$#20101 #34801 /$#20102 #34802 /$#20103 #34803 p0 L1 Q1 ; move to the position specified
#34016=#25022 ; latch the probe tripped flag
#34012 = #25021 ; latch the boundary crossed variable
; if the probe didn't trip skip the pull back if told to
if [[#i==2] && [#34016 ==0]] then GOTO 400 ; goto end macro

; store the probed point
#34006 = #24501 * #30021 ; dsp locations
#34007 = #24502 * #30021
#34008 = #24503 * #30021

if [#9155 == 0 ] then #34006 = #5041 * #30021
if [#9155 == 0 ] then #34007 = #5042 * #30021
if [#9155 == 0 ] then #34008 = #5043 * #30021

; if this is a mechanical or a dp7 probe, do a pull back and then a slow measuring move
if [#9155 == 1 ] goto 200 ; goto pull back if told to


if [#b != 0] then goto 60 ; goto ignore z
g65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#34503] X[#34006] Y[#34007] Z[#34008] F[#f]
GOTO 65
N60
g65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#5043 * #30021] X[#34006] Y[#34007] Z[#5043 * #30021] F[#f]
N65

N75 ; slow measuring move
; make the measuring move
;m225 #100 "probing z to %f"  #34803
if [#i==0] M115  /$#20101 #34801 /$#20102 #34802 /$#20103 #34803 p0 Q1 F[#34017]  ; move to the position specified at the slow probing rate
if [#i!=0] M115  /$#20101 #34801 /$#20102 #34802 /$#20103 #34803 p0 L1 Q1 F[#34017]  ; move to the position specified at the slow probing rate
#34016=#25022 ; latch the probe tripped flag
#34012 = #25021 ; latch the boundary crossed variable

F[#34000] ; set the feedrate back to the fast probing rate

; if the probe didn't trip skip the pull back if told to
if [[#i==2] && [#34016 ==0]] then GOTO 400 ; goto end macro

; store the probed point
if [#9155 != 0] goto 100 ; goto record dsp positions
#34006 = #5041 * #30021; expected positions
#34007 = #5042 * #30021
#34008 = #5043 * #30021
GOTO 200 ; goto pull back if told to
N100 ; record dsp positions
#34006 = #24501 * #30021; dsp locations
#34007 = #24502 * #30021
#34008 = #24503 * #30021
;m225 #100 "dsp %f %f %f" #34006 #34007 #34008


N200  ; pull back if told to
; save the boundary crossed variable and stylus compensations
#34012 = #25021
;m225 #100 "boundary hit %f" #34012
#34013 = #24801 * #30021
#34014 = #24802 * #30021
#34015 = #24803 * #30021

; be sure to pull back from the recorded probe position, not the current expected position

if [[#[w]]!=1] then GOTO 300 ; goto end of macro

; pull back
if [#b != 0] then GOTO 245 ; goto ignore z
g65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#34503] X[#34006] Y[#34007] Z[#34008] F[#f]
GOTO 250
N245
g65 "#400/system/probe_pull_back.cnc" A[#34501] B[#34502] C[#5043 * #30021] X[#34006] Y[#34007] Z[#5043 * #30021] F[#f]
N250

N300 ; compensate 
if [#[c] != 1] then GOTO 400 ; do not compensate unless told to, some cycles do compensation differently

;compensate x and y by the probe tip radius, the z radius is in the tool height offset
; the diameter is stored in #34009
#34501 = #34006 - #34501 ; dx
#34502 = #34007 - #34502 ; dy
#34503 = sqrt[ #34501 * #34501 + #34502 * #34502 ] ; xy vector length of move
if [#34503 != 0] then #34503 = [#34009/2] / #34503   ; ratio probe_radius/length 
#34006 = #34006 + #34503 * #34501 ;  x = x + dx * ratio
#34007 = #34007 + #34503 * #34502 ;  y = y + dy * ratio

N400

