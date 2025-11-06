; Filename:   probe_bore
; Purpose:    Find the center of a bore
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_bore.cnc"  
; Where:      No parameters
; Notes:      the probe will be at the center point at the end of the macro
; Output:     #34560 = width 1
;             #34561 = width 2
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; find center of x
G65 "#400/system/probe_center_inside.cnc" E1

; store width 1
#34560 = #34506

; find center of y
G65 "#400/system/probe_center_inside.cnc" E2

; store width 2
#34561 = #34506

