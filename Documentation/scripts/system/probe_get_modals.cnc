; Filename:   probe_get_modals.cnc
; Purpose:    set up some modal values before calling probing routines
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_get_modals.cnc"  
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841

if [#25018 == 11] then g65 "#400/system/probe_get_constants.cnc"
if [#25018 == 10] then g65 "/cnc10/system/probe_get_constants.cnc"


; inch conversion variable
#30020 = 1 ; converts from inches to current units , used to convert constants in eb macros
if [#31300 == 1] then #30020 = 25.4
#30021  = 1
;#4006 ; units of measure
;#25001  default_units_of_measure
; if the current units are not the same as the default_units_of_measure
IF [#4006 == #25001] THEN GOTO 9999

; convert the parameters into the current units
IF [#25001 == 21] GOTO 50
; inches to mm
#30021 = 25.4
GOTO 100
N50
; mm to inches
#30021 = 1/25.4
N100

#34000 = #34000 * #30021 ; fast probing rate
#34001 = #34001 * #30021 ; clearing feedrate

;set the probe slow measuring feedrate
#34017 = #34017 * #30021

  
#34009 = #34009 * #30021 ; probe diameter

#34018 = #34018 * #30021 ;  Probing traverse rate;
#34019 = #34019 * #30021 ;  Probing plunge rate

N9999




g1 g90
F[#34000]  ;  default to the fast probing rate parameter

