;/////////////////////////////////////////////////////////////////
;// Filename:        correct_position_a.cnc
;// Author:           Lee Johnston
;// Date:             February 15th 2007
;// Last Modified:    February 15th 2007
;// Description:      Correct position on 
;// Usage:            G65 "/cncm/correct_position_a.cnc"  A[angle]  X[x_position] Y[y_position] Z[z_position]
;// Inputs:           A[angle]
;//                        X[x_position] 
;//                        Y[y_position] 
;//                        Z[z_position]
;//
;// Outputs:          corrected_position_x           #136
;//                         corrected_position_y           #137
;//                         corrected_position_y           #138
;//
;/////////////////////////////////////////////////////////////////
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841
;// Program Flow:


;// # define section:
;// Correct for tool height offset:
 #133 = [[#[Z]]-[#10000]]

;// Rotate around the A axis
 #136 = [#[X]]; // X coordinate remains the same for A rotation
 #137 = [[[#[Y]]*[cos[#[A]]]]-[[#133]*[sin[#[A]]]]];
 #138 = [[[#133]*[cos[#[A]]]]+[[#[Y]]*[sin[#[A]]]]];

N[9999]
M99
