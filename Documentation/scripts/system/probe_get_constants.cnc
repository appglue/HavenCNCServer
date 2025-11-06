; Filename:   probe_get_constants.cnc
; Purpose:    set up some constant values before calling probing routines
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_get_constants.cnc"  
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


; #34000 fast probing rate
; #34017 slow probing rate
; #34002 sync variable
; #34005 probe input and state
; #34009 probe diameter
if [#9155 != 0] && [#9155 != 1] && [#9155 != 2] then ERROR ; Invalid probing type, check Machine Parameter 155

#34000 = #9014  ; fast probing rate
#34001 = #34000 ; set the clearing feedrate

;set the probe slow measuring feedrate
#34017 = #9015
if [#9155 == 2] then #34017 = #9394 ; use the dp7 feedrate

; find the probe input number and tripped state
if [#25018 == 11] #34002 = 50001 ; sync variable
if [#25018 == 10] #34002 = 6001  ; sync variable
; PLC probe input variable number 
#34003= [#34002-1] + abs[#9011] 
#34004 = 0;  Plc probe tripped state
if [#9011 < 0] then #34004 = 1 

; get the probe input and state
#34005 = #9011  
if [#25018 == 10] then #34005 = -#9011 ; m115 input number

; probe diameter  diameter_offset[tool_D_number[probe_tool_number]]
#34009 =  #[11000+#[13000+[#9012]]] 

#34011 = 0.00001 ;comparison tolerance

#34018 = #9395 ;  Probing traverse rate;
#34019 = #9396 ;  Probing plunge rate


;if [#25018 == 11] then #300 = "c:\cncm"
if [#25018 == 10] then #300 = "c:\cnc10"


