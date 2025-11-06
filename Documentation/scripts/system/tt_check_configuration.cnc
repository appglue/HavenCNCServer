;------------------------------------------------------------------------------
; Filename: tt_check_configuration.cnc
; Description: Check to ensure TT is correctly configured before use
; Notes: For Use during M6 Tool Change Macro
; Requires: CNC12 V5.40.00+
; Revision Date: 22 Aug 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Parameters:
; #9011 : Probe Input
; #9014 : Fast Probing Rate
; #9015 : Slow Probing Rate
; #9043 : TT1 Options
;         +1 = Parameter 71 will be subtracted from height
; #9044 : TT Input
; #9071 : TT Height
;
; System Variables:
; #25032 : PLC Device ID
;          19 = Acorn
;
; PLC Variables:
;
; User Variables:
; #33001 : TT Input
; #33002 : Fast Probing Rate
; #33003 : Slow Probing Rate
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

N100
;Check for PLC input
;If check if either parameter 11 or parameter 44 are > 0
IF !#9011 && !#9044 THEN M225 #110 "No PLC input has been configured for the Tool Touch Off device.\nPlease enter the PLC input for the Tool Touch Off device in parameter 11\nPress Cycle Cancel to end job now."
IF !#9011 && !#9044 THEN GOTO 100

;Assign Inputs
; For Tool Touch if P44 is not defined, then use P11.
; Convert Input to 50000 variable if it is not already.
IF #9044 != 0 THEN #33001 = abs[#9044] ELSE #33001 = abs[#9011]

IF #33001 < 50000 THEN #33001 = #33001 + 50000

;ACORN only, Check to ensure Input is 1-8.
IF #25032 == 19 && [#33001 < 50001 || #33001 > 50008] THEN M225 #110 "Tool Touch input configured is invalid.\nEnsure Tool Touch input is on inputs between 1 and 8\nPress Cycle Cancel to end job now."
IF #25032 == 19 && [#33001 < 50001 || #33001 > 50008] THEN GOTO 100

;Check that touch off device height has been set
IF #9071 == 0 && [#9043 and 1] THEN M225 #110 "The height for the tool touch off device has not been set in parameter 71.\nPlease measure the height of your tool touch off device and enter it parameter 71\nPress Cycle Cancel to end job now."
IF #50001                         			;Force lookahead to stop processing
IF #9071 == 0 && [#9043 and 1] THEN GOTO 100

;Check/set probing rates
IF [#9014 == 0] && [#4006 == 20] THEN #33002 = 10             ;If fast probing rate = 0, set to default 10"/min
IF #50001                         			;Force lookahead to stop processing
IF [#9014 != 0] && [#4006 == 20] THEN #33002 = #9014          ;If fast probing rate != 0, set to value in #9014
IF #50001                         			;Force lookahead to stop processing

IF [#9014 == 0] && [#4006 == 21] THEN #33002 = 254      		;If fast probing rate = 0, set to default 254mm/min
IF #50001                         			;Force lookahead to stop processing
IF [#9014 != 0] && [#4006 == 21] THEN #33002 = #9014    		;If fast probing rate != 0, set to value in #9014
IF #50001                         			;Force lookahead to stop processing

IF [#9015 == 0] && [#4006 == 20] THEN #33003 = 1        		;If slow probing rate = 0, set to default 1"/min
IF #50001 
IF [#9015 != 0] && [#4006 == 20] THEN #33003 = #9015			;If slow probing rate != 0, set to value in #9015
IF #50001									;Force lookahead to stop processing	
IF [#9015 == 0] && [#4006 == 21] THEN #33003 = 25.4        	;If slow probing rate = 0, set to default 25.4 mm/min
IF #50001                         			;Force lookahead to stop processing
IF [#9015 != 0] && [#4006 == 21] THEN #33003 = #9015        	;If slow probing rate != 0, set to value in #9015
IF #50001                         			;Force lookahead to stop processing

N1000                            ;End of Macro