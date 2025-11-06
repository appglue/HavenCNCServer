;------------------------------------------------------------------------------
; Filename: probe_stylus_calibration.cnc
; Description: Performs Bore cycle to determine stylus size.
; Notes: Macro called by CNC12 and/or utility macro.
; Requires: CNC12 V5.29.00+
; Revision Date: 23 Jul 2025
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
; Copyright SMC 2025
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\probe_stylus_calibration.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
; #11001-#11200 : Diameter Offset Amount D001-D200
;
; PLC Variables:
;
; User Variables:
; #103 : Touch Probe Detect Input
; #34578 : X-Diameter measurement from bore cycle macro.
; #34579 : Y-Diameter measurement from bore cycle macro
;
;------------------------------------------------------------------------------
IF #50001                         ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000  ;Skip macro if graphing or searching

;Setup Defines
G65 "#400\system\setup_defines.cnc"
G65 "#400\system\setup_key_defines.cnc"

;Setup Variables
<M225_MESSAGE_TIMER> = 0
<BORE_CYCLES_COMPLETED> = 0
<PROBE_AVERAGE_X_DIAMETER> = 0
<PROBE_AVERAGE_Y_DIAMETER> = 0

;Inches or mm
IF <SV_UNITS_OF_MEASURE> == <UNITS_IS_INCHES> THEN #300 = "inches" ELSE #300 = "millimeters"

N1 ;Ask for Ring Gauge Diameter
M224 <RING_GAUGE_DIAMETER_VALUE> "Calibrate Probe Stylus PreTravel Diameter (Tool %.0f)\n\n#)Input Ring Gauge Diameter (%s) and press Cycle Start.\n" <PARAM_PROBE_TOOL_NUMBER> #300

N2 ;Ask for Stylus Diameter
M224 <PROBE_STYLUS_DIAMETER_VALUE> "#)Input Probe Stylus Diameter (%s) and press Cycle Start.\n" #300

N3 ;Start Probe Cycle
M201 "Jog Probe Tip into the Ring Gauge Bore and press Cycle Start to Probe the Bore"

N4 ;Perform Checks
G65 "#400\system\probe_check_configuration.cnc"
IF <PROBE_CONFIG_ERROR> == 1 THEN GOTO 1000 ;End Program if error found

;Set Probe Tool Diameter to 0
#[11000 + <PARAM_PROBE_TOOL_NUMBER>] = 0

;------------------------------------------------------------------------------
;   Perform Calibration Bore Cycle
;------------------------------------------------------------------------------
N100 ;Perform initial Bore cycle
#34570 = 0  ;Set Variable for probing_cycles_select.cnc for bore cycle
G65 "#400\system\probe_cycles_select.cnc"
<BORE_CYCLES_COMPLETED> = <BORE_CYCLES_COMPLETED> + 1
IF <BORE_CYCLES_COMPLETED> <= 1 THEN GOTO 100 ;Perform Bore Cycle again after initial Cycle

N200 ;Accumulate X & Y Diameter Measurement
<PROBE_AVERAGE_X_DIAMETER> = [<PROBE_AVERAGE_X_DIAMETER> + #34578] ;Accumulated Average X Diameter
<PROBE_AVERAGE_Y_DIAMETER> = [<PROBE_AVERAGE_Y_DIAMETER> + #34579] ;Accumulated Average Y Diameter
IF <BORE_CYCLES_COMPLETED> <= 1 THEN GOTO 100 ;Perform Bore Cycle again to obtain average

;Average X & Y Diameter Measurements
<PROBE_AVERAGE_X_DIAMETER> = <PROBE_AVERAGE_X_DIAMETER> / [<BORE_CYCLES_COMPLETED> - 1]
<PROBE_AVERAGE_Y_DIAMETER> = <PROBE_AVERAGE_Y_DIAMETER> / [<BORE_CYCLES_COMPLETED> - 1]

N300 ;Calculate Average Stylus Radius
<PROBE_X_STYLUS_DIAMETER> = <RING_GAUGE_DIAMETER_VALUE> - <PROBE_AVERAGE_X_DIAMETER>          ;Determine X Stylus Radius
<PROBE_Y_STYLUS_DIAMETER> = <RING_GAUGE_DIAMETER_VALUE> - <PROBE_AVERAGE_Y_DIAMETER>          ;Determine Y Stylus Radius
<PROBE_AVERAGE_STYLUS_DIAMETER> = [<PROBE_X_STYLUS_DIAMETER> + <PROBE_Y_STYLUS_DIAMETER>] / 2 ;Average Stylus Radius

;Calculate Pre-Travel Amount
<PROBE_PRETRAVEL_VALUE> = <PROBE_STYLUS_DIAMETER_VALUE> - <PROBE_AVERAGE_STYLUS_DIAMETER>

;Set Probe Tool Diameter
#[11000 + <PARAM_PROBE_TOOL_NUMBER>] = <PROBE_AVERAGE_STYLUS_DIAMETER>

M225 <M225_MESSAGE_TIMER> "Probe Stylus Pretravel amount is: %f %s\nProbe Tool Diameter #%.0f has been set to %f %s\n\nPress Cycle Start to Run Bore Diameter Measurement Cycle." <PROBE_PRETRAVEL_VALUE> #300 <PARAM_PROBE_TOOL_NUMBER> <PROBE_AVERAGE_STYLUS_DIAMETER> #300

;------------------------------------------------------------------------------
;   Perform Measurement Bore Cycle
;------------------------------------------------------------------------------
N400
#34570 = 0  ;Set Variable for probing_cycles_select.cnc for bore cycle
G65 "#400\system\probe_cycles_select.cnc"

N410
M222 Q1 <MESSAGE_USER_INPUT> "Measured Bore Diameter = %f %s\n\nPress 1 to run Bore Measurement again.\nPress 2 to Save and Exit Stylus Calibration." #34579 #300
IF <MESSAGE_USER_INPUT> == <KB_1> THEN GOTO 400 ;Run Bore Cycle Again
IF <MESSAGE_USER_INPUT> == <KB_2> THEN GOTO 900 ;End Program
GOTO 410  ;Invalid Key Pressed, return to message.

N900 ;Clear Error Flag for CNC12 if this macro was ran from probe menu
<PROBING_ERROR_FLAG> = 0

N1000                             ;End of Macro