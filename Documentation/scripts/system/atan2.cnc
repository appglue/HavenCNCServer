; Filename:   atan2.cnc
; Purpose:    given dx, dy find the angle (in the correct quadrant
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/atan2.cnc" x[dx] y[dy]
; Where:      dx = change in x
;             dy = change in y
; Output:    #34010 = the angle 
; Notes:     if dx and dy are both zero then 0 will be returned
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841
#34010 = 0;
if [[#[x] == 0] && [#[y] == 0]] then GOTO 500 ; goto end of macro 
if [[#[x] == 0] && [#[y] < 0] ] then #34010 = -90;
if [[#[x] == 0] && [#[y] > 0] ] then #34010 = 90;

if [#[x] != 0] then #34010 = atan[#[y] / #[x]] ; calculate the angle


; find the correct quadrants
; if dx is negative and dy is positive  
if [[#[x] < 0 ] && [#[y] >= 0]] then #34010 = (#34010 + 180)
; if dx is negative and dy is negative
if [[#[x] < 0 ] && [#[y] < 0]] then #34010 = (#34010 - 180)


N500 ; end of macro
