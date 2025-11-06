;/////////////////////////////////////////////////////////////////
;// Filename:        rotate_vector_b.cnc
;// Author:           Lee Johnston
;// Date:             February 15th 2007
;// Last Modified:    February 15th 2007
;// Description:      Rotate a vector by an angle given, around the B-Axis
;// Usage:            G65 "/cncm/system/rotate_vector_b.cnc"  A[angle]  X[x_vector] Y[y_vector] Z[z_vector]
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

;// Program Flow:
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


;// # define section:
 ;#29651 = [[[cos([#A])]*[[#X]]] � [[sin([#A])]*[[#Z]]]]
 #29651 = [[[cos([#[A])]*[[#[X]]]]]+[[sin(#[A])]*[[#[Z]]]]]
 #29652 = [[#Y]]
 #29653 = [[[cos([#A])]*[[#Z]]]-[[sin([#A])]*[[#X]]]]


M99
