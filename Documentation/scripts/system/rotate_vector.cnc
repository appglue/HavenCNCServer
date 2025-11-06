;/////////////////////////////////////////////////////////////////
;// Filename:        rotate_vector.cnc
;// Author:           Lee Johnston
;// Date:             February 15th 2007
;// Last Modified:    February 15th 2007
;// Description:      Rotate a vector by an angle given.
;// Usage:            G65 "/cncm/system/rotate_vector.cnc"  A[angle]  X[x_vector] Y[y_vector] Z[z_vector]
;// Inputs:           A[angle]
;//                        X[x_vector] 
;//                        Y[y_vector] 
;//                        Z[z_vector]
;//
;// Outputs:          rotated_vector_x           #29641
;//                         rotated_vector_y           #29642
;//                         rotated_vector_y           #29642
;//
;/////////////////////////////////////////////////////////////////
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

;// Program Flow:


;// # define section:
#29641 = [cos([#[A]])*[#X]] - [sin([#[A]])*[#Y]]
#29642 = [cos([#A])*[#Y]]+[[sin([#A])]*[[#X]]]
#29643 = [#Z]

M99
