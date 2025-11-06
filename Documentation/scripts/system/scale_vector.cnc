;/////////////////////////////////////////////////////////////////
;// Filename:        scale_vectorcnc
;// Author:           Lee Johnston
;// Date:             February 15th 2007
;// Last Modified:    February 15th 2007
;// Description:      Scale a vector from an input length to a required length
;// Usage:            G65 "/cncm/system/scale_vector.cnc"  R[requested_length] X[x_vector] Y[y_vector] Z[z_vector]
;// Inputs:           R[requested_length] 
;//                        X[x_vector] 
;//                        Y[y_vector] 
;//                        Z[z_vector]
;//
;// Outputs:          scaled_vector_x           #29631
;//                         scaled_vector_y           #29632
;//                         scaled_vector_y           #29632
;//
;/////////////////////////////////////////////////////////////////
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

;// Program Flow:


;// # define section:
;// Calculate vector length, then calculate the scale factor for the new vector
 #120 = [SQRT[[[[#X]]*[[#X]]]+[[[#Y]]*[[#Y]]]+[[[#Z]]*[[#Z]]]]]
 #121 = [[[#R]]/[#120]]

;// Scale vectors:
 #29631 = [[[#X]]*[#121]]
 #29632 = [[[#Y]]*[#121]]
 #29633 = [[[#Z]]*[#121]]

 #122 = [SQRT[[[#29631]*[#29631]]+[[#29632]*[#29632]]+[[#29633]*[#29633]]]]

M99
