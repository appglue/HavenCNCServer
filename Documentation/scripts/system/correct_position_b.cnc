;/////////////////////////////////////////////////////////////////
;// Filename:        correct_position_b.cnc
;// Author:           Lee Johnston
;// Date:             February 15th 2007
;// Last Modified:    February 15th 2007
;// Description:      Compensates for tool center point offset from B-Axis center of rotation.
;// Usage:            /cncm/correct_position_b.cnc"  B[angle]  X[x_position] Y[y_position] Z[z_position]
;// Inputs:           B[angle]
;//                        X[x_position] 
;//                        Y[y_position] 
;//                        Z[z_position]
;//
;// Outputs:          corrected_position_x           #136
;//                   corrected_position_y           #137
;//                   corrected_position_y           #138
;//
;/////////////////////////////////////////////////////////////////
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841
;// # define section:
;// Calculate current tool height offset to center ot rotation for the B-Axis
 #134 = [[#9119]-[#10000]]

;// rotate tool ofest around b-axis
 #131 = [[0]+[[sin[#[B]]]*[#134]]]
 #133 = [[cos[#[B]]]*[#134]]-[0]
 #135 = [[#133]-[#9119]]

;// apply rotated vector to position
 #29651 = [[#[X]]+[#131]]
 #29652 = [#[Y]]
 #29653 = [[#[Z]]+[#135]]

M99
