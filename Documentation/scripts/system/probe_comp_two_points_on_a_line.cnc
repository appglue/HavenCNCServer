; Filename:   probe_comp_two_points_on_a_line.cnc
; Purpose:    compensate by the probe radius for two points on a line
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_comp_two_points_on_a_line.cnc" a[comp_direction] c[point1_x] d[point1_y] x[point2_x] y[point2_y] 

; Where:      
;             comp_direction = 1 for left -1 for right
;             point1_x = the x value for the measured point 1
;             point1_y = the y value for the measured point 1
;             point2_x = the x value for the measured point 2
;             point2_y = the y value for the measured point 2
; Input:      #34009 = probe diameter
; Output:     
;             #34585 ; return value compensated point 1 x
;             #34586 ; return value compensated point 1 y
;             #34587 ; return value compensated point 2 x
;             #34588 ; return value compensated point 2 y
; Notes:      
; uses #34597  #34599
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


; calculate the angle
G65 "#400/system/atan2.cnc" x[#c - #x] y[#d - #y]

#34586 = #34010 + [#a * 90]

#34585 = [[#34009/2] * cos [#34586] ] ; dx
#34586 = [[#34009/2] * sin [#34586] ] ; dy
#34587 = #34585
#34588 = #34586

#34585 = #c + #34585 ; add dx to original x
#34586 = #d + #34586 ; add dy to original y
#34587 = #x + #34587 ; add dx to original x
#34588 = #y + #34588 ; add dy to original y

