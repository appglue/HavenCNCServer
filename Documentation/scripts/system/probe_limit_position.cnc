; Filename:   probe_limit_position.cnc
; Purpose:    limit a destination to the travel limit or to the probe search distance
; Programmer: John Popovich
; Date:       Nov 9, 2010                                                                           
; Usage:      G65 "/cncm/system/probe_limit_position.cnc"  X[target_position] E[axis_index] D[direction] H[ingore_search_distance]
;       
; Where:      target_position = the original target position
;             axis_index = 1 through max axis
;             direction = the direction to move first
;            ingore_search_distance = 1 to only limit the positon by the travel limits (not the search distance)
;             
; Uses: #34901
; Output: #34900 ; return value limit position
;         #32000 = 2 if we are already at the limit position
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

; get the limit position from the system variable

; compute the variable to use
#34901 = 24600 ; positive limit position variable
if [#d == -1] then #34901 = #34901+100   ; use negative limit position variable 
if [#h == 1]  then #34901 = #34901 + 1600 ; use local travel limit variable

#34901 = #[#e + #34901] ; get the value of the limit position variable

#34900 = #x
if [ [#d ==  1] && [ #34900 > #34901 ] ] || [ [#d == -1] && [ #34900 < #34901 ] ]  then #34900 = #34901

; if we are already at the limit position then error
#34901 = abs[[#[5040+#e]] - #34900]
if [#34901 < #34011] then #32000 = 2 ; Surface not found

