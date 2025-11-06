; Filename:   probing_cycles_select.cnc
; Purpose:    select the probing macro to call l
; Programmer: John Popovich
; Date:       Nov 23, 2010 
; Usage:      G65 "/cncm/system/probing_cycles_select.cnc" 
; Where       
;
;  Input: 
;  #34570 probing cycle type
;    0 = bore
;    1 = boss
;    2 = slot
;    3 = web
;    4 = in corner
;    5 = out corner
;    6 = axis
;    7 = fixture rotation
;    8 = manual fixture rotation
;  #34571 probing cycle orientation
;  #34572 probing cycle distance
;  #34573 probing cycle clearance
;  Output:
;  #34574  output x position
;  #34575  output y position
;  #34576  output z position
;  #34578  output x length
;  #34579  output y length
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; set probing macro modals
G65 "#400/system/probe_get_modals.cnc"  

;first pick the correct cycle
if [#34570 == 0] then GOTO 100 ;  goto probe bore
if [#34570 == 1] then GOTO 200 ;  goto probe boss
if [#34570 == 2] then GOTO 300 ;  goto probe slot
if [#34570 == 3] then GOTO 400 ;  goto probe web
if [#34570 == 4] then GOTO 500 ;  goto in corner
if [#34570 == 5] then GOTO 600 ;  goto out corner
if [#34570 == 6] then GOTO 700 ;  goto single axis
if [#34570 == 7] then GOTO 800 ;  goto fixture rotation
if [#34570 == 8] then GOTO 900 ;  goto manual fixture rotation

; if we get here, there must be an invalid cycle type
M99;  end program


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe bore ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N100 ; probe bore
; call the bore macro
G65 "#400/system/probe_bore.cnc"  
; set the return values
#34574  =#5041   ; current x position
#34575  =#5042   ; current y position
#34576  =#5043   ; current z position
#34578  =#34560  ; width 1 from bore macro
#34579  =#34561  ; width 2 from bore macro

GOTO 1000 ; end program


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe boss ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N200 ; probe boss
; Orient  #34571
;   0 = X+
;   1 = Y+
;   2 = X-
;   3 = Y-
if [#34571 == 0] then GOTO 210 ; goto x+
if [#34571 == 1] then GOTO 220 ; goto y+
if [#34571 == 2] then GOTO 230 ; goto x-
if [#34571 == 3] then GOTO 240 ; goto y-
; if we get here there is an invalid orientation
M99 ; end program
N210 ; x+
#34570 = 1 ; intitial axis
#34571 = 1 ; intitial direction
GOTO 250
N220 ; y+
#34570 = 2 ; intitial axis
#34571 = 1 ; intitial direction
GOTO 250
N230 ; x-
#34570 = 1 ; intitial axis
#34571 = -1 ; intitial direction
GOTO 250
N240 ; y-
#34570 = 2 ; intitial axis
#34571 = -1 ; intitial direction
GOTO 250

N250 ; probe boss

; check for setup mode
if [#34583 != 1] GOTO 255 ; goto not setup mode
; setup mode is on
G65 "#400/system/probe_boss.cnc" X[#34572] Z[#34573] E[#34570] D[#34571] F[#9395] B[#9396] 

GOTO 260 ; goto get return values
N255 ; not setup mode
G65 "#400/system/probe_boss.cnc" X[#34572] Z[#34573] E[#34570] D[#34571] F[#34000] B[#34000]

N260 ; get return values
; set the return values
#34574  =#5041   ; current x position
#34575  =#5042   ; current y position
#34576  =#5043   ; current z position
#34578  =#34562  ; width 1 from boss macro
#34579  =#34563  ; width 2 from boss macro

GOTO 1000 ; end program
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe slot ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N300 ; probe slot
; Orient  #34571
;   0 = X axis
;   1 = Y axis
;   2 = both

if [#34571 == 0] then GOTO 310 
if [#34571 == 1] then GOTO 320 
if [#34571 == 2] then GOTO 330 

; if we get here there is an invalid orientation
M99 ; end program
N310 ; y axis
#34571 = 1
GOTO 340 ; goto call slot
N320 ; x axis
#34571 = 2
GOTO 340 ; goto call slot
N330 ; both
GOTO 100; goto probe bore
N340 ; call slot
G65 "#400/system/probe_center_inside.cnc"  E[#34571]
; setup return values
#34574  =#5041   ; current x position
#34575  =#5042   ; current y position
#34576  =#5043   ; current z position
#34578  =#34506  ; width from macro
#34579  =#34506  ; width from macro

GOTO 1000 ; end program

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe web ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N400 ; probe web
; Orient  #34571
;   0 = X+
;   1 = Y+
;   2 = X-
;   3 = Y-
if [#34571 == 0] then GOTO 410 ; goto x+
if [#34571 == 1] then GOTO 420 ; goto y+
if [#34571 == 2] then GOTO 430 ; goto x-
if [#34571 == 3] then GOTO 440 ; goto y-
; if we get here there is an invalid orientation
M99 ; end program
N410 ; x+
#34570 = 1 ; intitial axis
#34571 = 1 ; intitial direction
GOTO 450
N420 ; y+
#34570 = 2 ; intitial axis
#34571 = 1 ; intitial direction
GOTO 450
N430 ; x-
#34570 = 1 ; intitial axis
#34571 = -1 ; intitial direction
GOTO 450
N440 ; y-
#34570 = 2 ; intitial axis
#34571 = -1 ; intitial direction
GOTO 450

N450
G65 "#400/system/probe_center_outside.cnc"  X[#34572] Z[#34573] E[#34570] D[#34571] F[#34000] B[#34000]
; setup return values
#34574  =#5041   ; current x position
#34575  =#5042   ; current y position
#34576  =#5043   ; current z position
#34578  =#34509  ; width from macro
#34579  =#34509  ; width from macro

GOTO 1000 ; end program

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe in corner ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N500 ; probe in corner
; Orient  #34571
;  0 = x+ y+
;  1 = x- y+
;  2 = x- y-
;  3 = x+ y-

if [#34571 == 0] then GOTO 510 ; goto x+y+
if [#34571 == 1] then GOTO 520 ; goto x-y+
if [#34571 == 2] then GOTO 530 ; goto x-y-
if [#34571 == 3] then GOTO 540 ; goto x+y-
; if we get here there is an invalid orientation
M99 ; end program
N510 ; x+y+
#34570 = 1 ; intitial x direction
#34571 = 1 ; intitial y direction
GOTO 550
N520 ; x-y+
#34570 = -1 ; intitial x direction
#34571 =  1 ; intitial y direction
GOTO 550
N530 ; x-y-
#34570 = -1 ; intitial x direction
#34571 = -1 ; intitial y direction
GOTO 550
N540 ;x+y-
#34570 =  1 ; intitial x direction
#34571 = -1 ; intitial y direction
GOTO 550

N550

G65 "#400/system/probe_inside_corner.cnc" x[#34570] y[#34571] Z[#34573]   

; setup return values
#34574  =#5041   ; current x position
#34575  =#5042   ; current y position
#34576  =#5043   ; current z position
#34578  =0  ; invalid
#34579  =0  ; invalid


GOTO 1000 ; end program

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe out corner ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N600 ; probe out corner
; Orient  #34571
;  0 = x+ y+
;  1 = x- y+
;  2 = x- y-
;  3 = x+ y-
if [#34580 != 0] && [#34580 != 1] then M99 ; invalid side 
if [#34571 == 0] then GOTO 610 ; goto x+y+
if [#34571 == 1] then GOTO 620 ; goto x-y+
if [#34571 == 2] then GOTO 630 ; goto x-y-
if [#34571 == 3] then GOTO 640 ; goto x+y-
; if we get here there is an invalid orientation
M99 ; end program
N610 ; x+y+
#34571 =  1 ; initial axis direction
#34581 = -1 ; other axis direction
#34582 = 1 ; initial comp direction
if [#34580 == 0] then #34570 =  1 ; initial axis
if [#34580 == 1] then #34570 =  2 ; initial axis

GOTO 650
N620 ; x-y+
#34582 = -1 ; initial comp direction
if [#34580 == 0] then #34571 = -1 ; initial axis direction
if [#34580 == 0] then #34581 = -1 ; other axis direction
if [#34580 == 0] then #34570 =  1 ; initial axis
if [#34580 == 1] then #34571 =  1 ; initial axis direction
if [#34580 == 1] then #34581 =  1 ; other axis direction
if [#34580 == 1] then #34570 =  2 ; initial axis
GOTO 650
N630 ; x-y-
#34582 = 1 ; initial comp direction

#34581 =  1 ; other axis direction
#34571 = -1 ; initial axis direction
if [#34580 == 0] then #34570 =  1 ; initial axis
if [#34580 == 1] then #34570 =  2 ; initial axis
GOTO 650
N640 ;x+y-
#34582 = -1 ; initial comp direction
if [#34580 == 0] then #34571 =  1 ; initial axis direction
if [#34580 == 0] then #34581 =  1 ; other axis direction
if [#34580 == 0] then #34570 =  1 ; initial axis
if [#34580 == 1] then #34571 = -1 ; initial axis direction
if [#34580 == 1] then #34581 = -1 ; other axis direction
if [#34580 == 1] then #34570 =  2 ; initial axis
GOTO 650

N650
if [#34580 == 1] then #34582 = -#34582 ; flip the initial comp direction for side 2
G65 "#400/system/probe_outside_corner.cnc" A[#34570] x[#34571] y[#34581] Z[#34573]  e[#34572] c[#34582]
#34574  =#5041   ; current x position
#34575  =#5042   ; current y position
#34576  =#5043   ; current z position
#34578  =0  ; invalid
#34579  =0  ; invalid

GOTO 1000 ; end program


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe single axis ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N700 ; probe single axis
; Orient  #34571
;   0 = X+
;   1 = Y+
;   2 = X-
;   3 = Y-
;   4 = z-
if [#34571 == 0] then GOTO 710 ; goto x+
if [#34571 == 1] then GOTO 720 ; goto y+
if [#34571 == 2] then GOTO 730 ; goto x-
if [#34571 == 3] then GOTO 740 ; goto y-
if [#34571 == 4] then GOTO 750 ; goto z-
; if we get here there is an invalid orientation
M99 ; end program
N710 ; x+
#34570 = 1 ; axis
#34571 = 1 ; direction
GOTO 760
N720 ; y+
#34570 = 2 ; axis
#34571 = 1 ; direction
GOTO 760
N730 ; x-
#34570 = 1 ; axis
#34571 = -1 ; direction
GOTO 760
N740 ; y-
#34570 = 2 ;  axis
#34571 = -1 ; direction
GOTO 760
N750 ; z-
#34570 = 3 ; axis
#34571 = -1 ; direction
GOTO 760
N760 ; call single axis
G65 "#400/system/probe_move.cnc"  D[#34571] X[#[20100+#34570]] Y78 Z78 W1 C1
; setup return values
#34574  =#34006  ; latched x position
#34575  =#34007  ; latched y position
#34576  =#34008  ; latched z position
#34578  = 0 ; width is invalid
#34579  = 0 ; width is invalid

GOTO 1000 ; end program

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe fixture rotation ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N800 ; fixture rotation
GOTO 1000 ; end program

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  probe manual fixture rotation ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
N900 ; manual fixture rotation
GOTO 1000 ; end program


N1000 ; end macro
; clear the general error variable if it hasn't changed from 1
if [#32000 == 1] then #32000 = 0 ;

