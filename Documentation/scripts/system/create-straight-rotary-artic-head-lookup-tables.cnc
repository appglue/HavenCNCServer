;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; File:    create-straight-rotary-artic-head-lookup-tables.cnc
; Author:  Keith Dennison
; Date:    03-Jul-2017
;          10-Jan-2020 KSD Support 20 or 22 bit scale encoder
; Purpose: Generate an artic-head-lut.txt file for a B axis with Magnescale scale.
;          This file is used as input for the calibration routines to 
;          generate a tilt.tab.
; Output:  artic-head-lut.txt
; Notes:   Assumes that B has been homed at level position, i.e.,
;          that motor position at level is zero. Moves in one 
;          degree increments.
;         
; Copyright 2017-2020 Centroid Corp. Howard, PA         
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
G20  ; Select inch units

; Open (with overwrite) the file used to record the data
M120 "#400\artic-head-lut.txt"

; #103 is the number of scale counts per revolution
#103 = (#27505 * 360.0  + 0.5) 
#103 = #103 - (#103 % 1.0)
if [#103 == 2^20 || #103 == 2^22] then goto 3

; Error, Scale Counts not 20 or 22 bit
m225 #100 "Error: Scale Encoder Counts/Rev not 20 or 22 bit"

goto 10000


N3 


; #105 is the commanded B axis position
; #27405 is the B scale reading
; #25205 is the B scale count at level position (recorded via Set Level in CNC12)
; #23805 is the B motor position
; #23505 is the B minus travel limit
; #23605 is the B plus travel limit

; Write out the file version. This should be the first line written as  
; it is used to specify the format of the rest of the file. The line
; should start with a number that can be parsed as a double. The remainder
; of the line is not used.
; Note: We place two spaces on the header lines so that the output is
;       compatible with the plot.exe program.
M223 "  1.0  // File version\n" 

; The format for file version 1.0:
; Second line is a string containing date information.
; Third line is the motor count creeping to level from negative side
; Fourth line is the motor count creeping to level from the positive side
; Fifth line is a label line for use with the plot program
; Each line thereafter contains these five fields separated by whitespace:
; Field 1 Commanded B axis motor position in local coordinates (degrees)
; Field 2 The B axis motor encoder count 
; Field 3 The B axis scale encoder count
; Field 4 The B axis scale encoder count converted to degrees
; Field 5 Difference (absolute error) between Field 4 and Field 1
;
; A typical line of output is:
; 57.0000      69935    3912699  57.002478   0.002478
;
; The data is recorded starting at the positive travel limit and proceeds from
; there to the negative travel limit and then back to the positive travel 
; limit. Commanded positions are in whole degrees except at the travel limits.
;

; Record the date 
m123L1; 
M127

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Macro to move to position specified by #105 
; and then record a line of info in the open file.
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
O9990
; Take reading
  G90 G1 B#105 F360
  G4 P1
  if #50001
; write line of data (commanded position, B motor, B scale)
  #101 = 360 * (#27405 - #25205) / #103
  #102 = #101 - #105
  M223 "%9.4f %10.0f %10.0f %10.6f %10.6f\n" #105 #27305 #27405 #101 #102  
m99

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

#100 = -0.01
N5
; Creep to B0 from the negative side
  G90 G1 B-1.00 F30 
  G90 G1 B#100 F30
  G4 P1
  if #50001  
  if #27405 < #25205 then goto 10
  #100 = #100 * 2
  if #100 <= -1.0 then goto 30
  goto 5
  
N10
  G91 G1 M128/B L-1
  if #50001
  if #27405 < #25205 then goto 10
  ; Record motor counts and scale counts
  m223 "  %.0f // Creep to level from negative side\n" #27305 

#100 = 0.01
N15 
; Creep to B0 from the positive side
  G90 G1 B+1.00 F30
  G90 G1 B#100 F30
  G4 P1
  if #50001  
  if #27405 > #25205 then goto 20
  #100 = #100 * 2
  if #100 >= 1.0 then goto 30
  goto 15
  
N20
  G91 G1 M128/B L1
  if #50001
  if #27405 > #25205 then goto 20
  ; Record motor counts and scale counts
  m223 "  %.0f // Creep to level from positive side\n" #27305 
  goto 50
  
N30 ; Error
 #100 = 0
 m225 #100 "Error: tolerance exceeded on creep to zero"
 #error Tolerance exceeded on creep procedure

N50


; Output label line for plot program
m223 "commanded      motor      scale      angle      error\n"  

; Set first position to the positive travel limit  
; then move and record position
  #105 = #23605  ; Plus travel limit
  M98 P9990
  
; Set next position (second position) to the nearest integer less than 
; the positive travel limit  
  #102 = #23605 % 1.0
  if #102 == 0.0 then #102 = 1 
  #105 = #23605 - #102
  
N100  

; Move and record positions until the negative travel limit is reached
  M98 P9990
  #105 = #105 - 1
  if #105 > #23505 then goto 100
   
  #105 = #23505
; Move and record position
  M98 P9990

; Set position to nearest integer greater than the negative travel limit  
  #102 = abs(#23505 % 1.0)
  if #102 == 0.0 then #102 = 1 
  #105 = #23505 + #102
  
N200

; Move and record positions until the positive travel limit is reached
  M98 P9990
  #105 = #105 + 1
  if #105 < #23605 then goto 200

; Set last position to the positive travel limit  
; then move and record position
  #105 = #23605
  M98 P9990
  
; Move to B0  
  G90 G1 B0 F360  
  
N9999 ; the end 
; Communicate to CNC12 no error. CNC12 will set #32000 = 1.0
; before running this job. If some other error (parsing, E-stop, etc.)
; happens before this line, the calling code in CNC12 will know.
  #32000 = 0 
  
N10000
