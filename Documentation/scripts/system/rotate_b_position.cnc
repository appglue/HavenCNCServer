;/////////////////////////////////////////////////////////////////
;// Filename:        rotate_b_position.cnc
;// Author:           Lee Johnston
;// Date:             February 15th 2007
;// Last Modified:    February 15th 2007
;// Description:      Rotate a given position around the b-axis center of rotation, by the angle given
;// Usage:            G65 "/cncm/system/rotate_vector.cnc"  A[angle]  X[x_position] Y[y_position] Z[z_position]
;// Inputs:           A[angle]
;//                        X[x_position] 
;//                        Y[y_position] 
;//                        Z[z_position]
;//
;// Outputs:          rotated_position_x           #29651
;//                         rotated_position_y           #29652
;//                         rotated_position_y           #29652
;//
;/////////////////////////////////////////////////////////////////

;// Program Flow:
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


;// # define section:
;// Calculate current tool height offset to center ot rotation for the B-Axis
 #134 = [[#9119]+[#10000]]

;// rotate tool ofest around b-axis
;#131 = [[cos([#A])]*[0]] � [[sin([#A])]*[#134]]
;#133 = [[cos[[#A]]]*[#134]] + [[sin[[#A]]]*[0]]
#131 = [[0] � [[sin([#A])]*[#134]]]
#133 = [[cos[[#A]]]*[#134]] + [0]

;// apply rotated vector to position
 #29651 = [[[#X]]+[#131]]
 #29652 = [[#Y]]
 #29653 = [[[#Z]]+[#133]]

M99
