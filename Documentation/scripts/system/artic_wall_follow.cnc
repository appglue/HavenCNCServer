;///////////////////////////////////////////////////////////////////
;//// Filename:         artic_wall_follow.cnc
;//// Author:           Lee Johnston
;//// Date:             February 13th 2008
;//// Last Modified:    July 1st 2008
;                       Dec 12 2011 : requires CNC11 305r37 and up. or CNC10 207r52 and up
;//// Description:      Radial Wall Following digitizing for articulating head machines
;//// Usage:            G65 "/cncm/artic_wall_follow.cnc" A[tool_number] B[z_patch_depth] C[z_step_down] D[start_direction] E[outer_stepover] I[inside/outside] J[replay_feedrate] K[cw/ccw]
;//// Inputs:           A[tool_number] B[z_patch_depth] C[z_step_down] D[start_direction] E[outer_stepover] I[inside/outside] J[replay_feedrate] K[cw/ccw] #301[filename]
;////
;//// Outputs:          
;//// 
;///////////////////////////////////////////////////////////////////

;//// Program Flow:

;//// # define section:
;//// #120 - #125 reserved for scale_vector.cnc



;//// #126 - #130 reserved for rotate_vector.cnc
;//// #131 - #135 reserved for rotate_b_position.cnc

M120 "#301.txt"    ; over write prev files
M120 "#301.dig5a"


;//// #136 - #140 reserved for correct_b_position
;//// User Variables Range: #29000 - 29999
;// de bug setting goes here:
 #146 = [0] ;// Set debug state
 #119 = [0]
 #149 = [0]
IF [#9166 and 1025] THEN #119 = [1] ;// set machine type
IF [#9094 and 1] THEN #149 = [1]

;// Init some variables
 #147 = [59]
 #106 = [1]
 #107 = [0]
 #29654 = [0]
 #108 = 0
 #109 = 0
 #29611 = 0
 #29612 = 0
 #29613 = 0

N[100] ;//// Check to make sure that probe parameters are properly configured
  if #9011 == [0] THEN GOTO 9990 ;//// Probe input parameter not set
  if [#9012] == [0] THEN GOTO 9991 ;//// Probe tool number parameter not set 
  if [#9013] == [0] THEN GOTO 9992 ;// // Recover distance parameter not set 
  if [#9015] == [0] THEN GOTO 9993 ;//// Slow probing feedrate parameter not set
  if [#9014] == [0] THEN GOTO 9994 ;//// Fast probing feedrate parameter not set
  if [#9016] == [0] THEN GOTO 9995 ;//// Probe search distance parameter not set
  #29656 = 0
  if [#9018 < 0] then #29656 = 1
  if [#[abs[#9018]] == #29656] THEN GOTO 9997 ; //// Probe not detected
  ;//// End param_check

N[500]
  #100 = 0 ;// Set message timer to 0
  ;//G54
  ;M25
  ;M225 #100 "Load Probe"
  #115 = #[A] ;M224 #115 "Enter Probe Tool Number:\n"
  ;#29665 = 12000+#115
  ;#29667 = ##29665
  ;m225 #100 "H %f %f" #29665 #29667
  G43 H#[12000+#115]

  ;M224 #105 "Enter desired B-Axis Angle for Digitizing:\n"
  ;M225 #100 "B-Axis will be positioned to B%.5f\nPress Cycle Start to position B-Axis" #105
  ;G1 B[#105] F500
  ;M224 #301 "Enter filename for digitizing output:\n"
  ;M200 "Jog probe to Start Position\nPress Cycle Start when ready"
  #29681 = [#5041] ;// record center point
  #29682 = [#5042] ;// record center point
  #29683 = [#5043] ;// record center point
  #29684 = [#5044] ;// record center point
  #29685 = [#5045] ;// record center point
  #101 = [#5041] ;// record starting x
  #102 = [#5042] ;// record starting y
  #103 = [#5043] ;// record starting z
  #104 = [#5044] ;// record starting a
  #105 = [#5045] ;// record starting b
  #110 = #[E] ;M224 #110 "Enter Outer Stepover Distance:\n"
  #111 = #[C] ;M224 #111 "Enter Z Stepdown Distance:\n"
  #112 = #[B] ;M224 #112 "Enter Max Z Depth:\n"
  ;M224 #113 "Enter Containment Radius:\n"
  #117 = [#9013] ;#M224 #117 "Enter Retract Clearance Distance:\n"
  #114 = #[D] ;M224 #114 "Enter Start direction:\n"
  M121 "#301.dig5a"
  ;// output Header info to file below:
  M223 " %c dig_type 5 Axis Wall Following Digitizing\n" #147
  M223 " %c Digitize Diameter %.5f\n" #147 #11000
  M223 " %c Current WCS( %.0f ) offsets\n" #147 [[#4014]-[53]]
  M223 " %c X:    %.5f Y:    %.5f Z:    %.5f A:     %.5f B:     %.5f\n" #147 #2500 #2600 #2700 #2800 #2900
  M223 " %c CSR Angle: %.5f\n" #147 #2400
  M223 " %c Center of Rotation A axis\n" #147
  M223 " %c Y:  %.5f Z:  %.5f\n" #147 #9116 #9117
  M223 " %c Center of Rotation B axis\n" #147
  M223 " %c X:  %.5f Z:  %.5f\n" #147 #9118 #9119
  M223 " %c ************************************************************************* \n" #147
  M223 "\n"
  M223 "M25\n"
  M223 "G0 X%.5f Y%.5f Z%.5f A%.5f B%.5f %c no data\n" #101 #102 #103 #104 #105 #147
  M223 "G1 F%.4f %c no data\n" #[J] #147

  #113 = #9016
  ;IF [abs[#105] < .001] THEN #119 = [0]
  ;m225 #100 "119 = %f 105=%f" #119 #105
  IF #112 > [0] THEN #29746 = [-1]
  IF #112 < [0] THEN #29746 = [1]
  IF #112 == [0] THEN #29746 = [0]
  IF #29746 == [1] THEN #29747 = [0-[#111]]
  IF #29746 == [1] THEN #111 = [#29747]

;//----------------------------------------------------------------------------------------
;// ******************************** Main Loop *******************************************
;//----------------------------------------------------------------------------------------  
N[1000]
  #29611 = 0
  #29612 = 0
  #29613 = 0
  ;// First point in spline? If so then go to first_point_block_ section
  IF [#106] == [1] THEN GOTO 2100 ;// jump to first point block
 N1100
  ;// Second point in spline? If so then go to second_point_block_ 
  IF [#106] == [0] THEN IF #108 == 1 THEN GOTO 2200 ;// jump to second point block
 N[1200]

  ;// Determine next contact with surface, go to predict_contact_:
  GOTO 2400
 N[1400]

  ;// Calculate retract move vector, this move will be 90 degrees / perpendicular to the previous surface vector (_n_minus_1_vector):      
  GOTO 2500
 N[1500]
  ;// Retract Move:

  ;// Add retract vector to probed position (position probed in _probe_point_block_):
  #29701 = [[#29511]+[#29641]]
  #29702 = [[#29512]+[#29642]]
  #29703 = [[#29513]+[#29643]]
  #29703 = [#29693]
  ;IF #119 == [1] THEN #29703 = [[#29513]+[#29533]]
  IF #119 == [1] THEN #29703 = [#29723]

  ;// If machine type is Articulated Head, then rotate position found above to the B-Axis Angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]
  IF #119 == [0] THEN #29703 = [#29693]

;  m223 "%c retract to %f %f %f\n" #147 #29701 #29702 #29703; *** DEBUG ***

  ;// Perform Retract move away from probed point on surface:
  G1 X[#29701] Y[#29702] Z[#29703] F[#9014]

  ;// if debug then Record retract point to the output file:
  if [#146] THEN M223 "G1 X%.5f Y%.5f Z%.5f %cA%.5f B%.5f\n" #29701 #29702 #29703 #147 #29734 #29735

  ;// Record present position for use in stepover move:
  #141 = [#5041] ;// record x
  #142 = [#5042] ;// record y
  #143 = [#5043] ;// record z

  #29701 = [#141]
  #29702 = [#142]
  #29703 = [#143]

  ;// If machine type is Articulated Head, then rotate present position, found above, onto the XY plane:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29723]

  IF #119 == [1] THEN #29701 = [[#29701]+[#29551]]
  IF #119 == [1] THEN #29702 = [[#29702]+[#29552]]
  IF #119 == [1] THEN #29703 = [#29723]

  ;// Caculate stepover position, by adding the predicted surface vector to the present, unrotated, position:
  #29711 = [[#29701]+[#29551]]
  #29712 = [[#29702]+[#29552]]
  #29713 = [[#29703]+[#29553]]

  IF #119 == [1] THEN #29711 = [#29701]
  IF #119 == [1] THEN #29712 = [#29702]
  IF #119 == [1] THEN #29713 = [#29703]
  IF #119 == [0] THEN #29713 = [#29693]

  ;// If machine type is Articulated Head, then  rotate stepover position to the B-Axis angle, prependicular to the B-Axis:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29711] Y[#29712] Z[#29713]
  IF #119 == [1] THEN #29711 = [#29651]
  IF #119 == [1] THEN #29712 = [#29652]
  IF #119 == [1] THEN #29713 = [#29653]

;  m223 "%c Stepping to %f %f %f\n" #147 #29711 #29712 #29713; *** DEBUG ***

  ;// Perform protected move into stepover position:
  M115 /X[#29711] /Y[#29712] /Z[#29713] P#9011 F[#9014] L1 ;//Probe point
  ;//IF !#[probe_input] THEN M0                    ;// Contact during stepover...

  ;// if debug then Record stepover point to the output file:
  if [#146] THEN M223 "G1 X%.5f Y%.5f Z%.5f %cA%.5f B%.5f\n" #29711 #29712 #29713 #147 #29734 #29735

  ;// Calculate and make probe move:
  GOTO 2600
 N[1600]

  ;// Open dig file for output:
  M121 "#301.dig5a"

  ;// Record raw point to the output file, uncorrected for all:
  M223 "G1 X%.5f Y%.5f Z%.5f A%.5f B%.5f\n" #29731 #29732 #29733 #29734 #29735

  ;// Open text file for mastercam output:
  M121 "#301.txt"

  ;// Record raw point to the output file, uncorrected for non artic head:
  IF #119 == [0] THEN IF #149 == [0] THEN M223 "X%.5f Y%.5f Z%.5f A%.5f B%.5f\n" #29731 #29732 #29733 #29734 #29735
  IF #119 == [0] THEN IF #149 == [1] THEN G65 "#400/system/correct_position_a.cnc" A[#29734] X[#29731] Y[#29732] Z[#29733]
  IF #119 == [0] THEN IF #149 == [1] THEN M223 "%.5f %.5f %.5f\n" #136 #137 #138
  IF #119 == [1] THEN G65 "#400/system/correct_position.cnc" A[#29734] B[#29735] X[#29731] Y[#29732] Z[#29733]
  If #119 == [1] THEN M223 "%.5f %.5f %.5f\n" #136 #137 #138

  M121 "#301.dig5a"

  ;// if more than 3 points in spline have been probed, then check for the end of spline:
  IF #108 > [3] THEN GOTO 2700
 N[1700]

  ;// Update points probed:
  #108 = [[#108]+1]
  #109 = [[#109]+1]

  ;// If the end of spline condition has been met, then goto the _step_down_block to perform step down move:
  IF [#29654] == [1] THEN GOTO 2800 ;// jump to perform step down
 N[1800]
  ;// If the end of spline is true, then after the stepdown move, go to the _first_point_block_ to move towards surface and start new spline:
  IF [#29654] == [1] THEN GOTO 2100 ;// jump to first point block

  GOTO 1000
  ;//// End main_loop  
  GOTO 9999

N[2100] ;// First Point Block, this block is used for the initial move towards the surface
;  m223 "%c First point block\n" #147 ;*** Debug ***
  ;// record starting position:
  #29691 = [#5041]
  #29692 = [#5042]
  #29693 = [#5043]

  ;// new spline, set end spline to false
  #29654 = [0]

  ;// Record present spline start positions:
  #29701 = [#29691]
  #29702 = [#29692]
  #29703 = [#29693]
  #29704 = [#5044]
  #29705 = [#5045]

  ;// If machine type is Articulated Head, then rotate start position flat onto the XY Plane:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]
  IF #119 == [1] THEN #29721 = [#29651]
  IF #119 == [1] THEN #29722 = [#29652]
  IF #119 == [1] THEN #29723 = [#29653]

  ;// REcord the spline starting height, for use later int he end spline function:
  IF #119 == [0] THEN #144 = [#5043] ;// record z height
  IF #119 == [1] THEN #144 = [#29703] ;// record z height

  ;// Calculate the starting move vector for the first probing move:
  IF #114 == 12 THEN #29702 =[[#29692]+[#113]]
  IF #114 == 3 THEN #29701 =[[#29691]+[#113]]
  IF #114 == 6 THEN #29702 =[[#29692]-[#113]]
  IF #114 == 9 THEN #29701 =[[#29691]-[#113]]

  IF #119 == [1] THEN IF #114 == 12 THEN #29702 =[[#29722]+[#113]]
  IF #119 == [1] THEN IF #114 == 3 THEN #29701 =[[#29721]+[#113]]
  IF #119 == [1] THEN IF #114 == 6 THEN #29702 =[[#29722]-[#113]]
  IF #119 == [1] THEN IF #114 == 9 THEN #29701 =[[#29721]-[#113]]

  ;// If Articulated Head then rotate probing vector to the B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]

  ;// perform protected probing move towards surface:
;  m223 "%c 1st point Probing to %f %f %f\n" #147 #29701 #29702 #29703 ;*** Debug ***
  M115 /X[#29701] /Y[#29702] /Z[#29703] P#9011 F[#9015] L1 ;//Probe point
  ;//IF [probe_input] THEN GOTO probe_no_contact
  ;// record positions found by probe:
  #29561 = [[#24301]-[#2500]] ;// record x
  #29562 = [[#24302]-[#2600]] ;// record y
  #29563 = [[#24303]-[#2700]-#10000] ;// record z
  #29564 = [[#24304]-[#2800]] ;// record a
  #29565 = [[#24305]-[#2900]] ;// record b
  
  ;m225 #100 "z %f" #29563
;  m223 "%c First point %f %f %f\n" #147 #29561 #29562 #29563 ; *** DEBUG ***
  
  if [#146] THEN M225 #100 "Recorded Position:\nX: %.5f Y: %.5f Z: %.5f A: %.5f B: %.5f" #29561 #29562 #29563 #29564 #29565

  ;// Record position as point_n position:
  #29511 = #29561 ;// record x
  #29512 = #29562 ;// record y
  #29513 = #29563 ;// record z
  #29514 = #29564 ;// record a
  #29515 = #29565 ;// record b

  #29741 = #29561 ; // record raw first point for closing spline
  #29742 = #29562 ; // record raw first point for closing spline
  #29743 = #29563 ; // record raw first point for closing spline
  #29744 = #29564 ; // record raw first point for closing spline
  #29745 = #29565 ; // record raw first point for closing spline

  ;// record raw position for later writing to file:
  #29731 = #29561 ;// store raw point for later saving to file
  #29732 = #29562 ;// store raw point for later saving to file
  #29733 = #29563 ;// store raw point for later saving to file
  #29734 = #29564 ;// store raw point for later saving to file
  #29735 = #29565 ;// store raw point for later saving to file

  ;// Open dig file for output:
  M121 "#301.dig5a"

  ;// Record raw point to the output file, uncorrected for all
  M223 "G1 X%.5f Y%.5f Z%.5f A%.5f B%.5f\n" #29731 #29732 #29733 #29734 #29735

  ;// Open text file for mastercam output:
  M121 "#301.txt"

  ;// Record raw point to the output file, uncorrected for non artic head:
  IF #119 == [0] THEN IF #149 == [0] THEN M223 "X%.5f Y%.5f Z%.5f A%.5f B%.5f\n" #29731 #29732 #29733 #29734 #29735
  IF #119 == [0] THEN IF #149 == [1] THEN G65 "#400/system/correct_position_a.cnc" A[#29734] X[#29731] Y[#29732] Z[#29733]
  IF #119 == [0] THEN IF #149 == [1] THEN M223 "%.5f %.5f %.5f\n" #136 #137 #138
  IF #119 == [1] THEN G65 "#400/system/correct_position.cnc" A[#29734] B[#29735] X[#29731] Y[#29732] Z[#29733]
  If #119 == [1] THEN M223 "%.5f %.5f %.5f\n" #136 #137 #138

  M121 "#301.dig5a"

  ;// If Articulated Head machien then rotate position flat onto XY Plane:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#29511] Y[#29512] Z[#29513]
  IF #119 == [1] THEN #29711 = [#29651]
  IF #119 == [1] THEN #29712 = [#29652]
  IF #119 == [1] THEN #29713 = [#29653]
  IF #119 == [1] THEN #29511 = [#29651]
  IF #119 == [1] THEN #29512 = [#29652]
  IF #119 == [1] THEN #29513 = [#29653]

;  m223 "%c First point rotated %f %f %f\n" #147 #29711 #29712 #29713;*** Debug ***

  IF #119 == [1] THEN #29561 = [#29651]
  IF #119 == [1] THEN #29562 = [#29652]
  IF #119 == [1] THEN #29563 = [#29653]

  ;// Calculate n_minus_1 vector for Articulated Head machine
  IF #119 == [1] THEN #29601 = [[#29711]-[#29721]]
  IF #119 == [1] THEN #29602 = [[#29712]-[#29722]]
  IF #119 == [1] THEN #29603 = [0]

  ;// Calculate n_minus_1 vector for standard head machines:
  IF #119 == [0] THEN #29601 = [[#29511]-[#29691]]
  IF #119 == [0] THEN #29602 = [[#29512]-[#29692]]
  IF #119 == [0] THEN #29603 = [0]
  M121 "#301.dig5a"

  if [#146] THEN M225 #100 "Recorded n minus 1 Vector:\nX: %.5f Y: %.5f Z: %.5f" #29601 #29602 #29603

  ;// Calculate retract from surface:
  ;// scale vector n_minus_1, to account for the required retract distance, returns as scaled_vector_x, scaled_vector_y, scaled_vector_z
  G65 "#400/system/scale_vector.cnc" R[#117] X[#29601] Y[#29602] Z[#29603]
  if [#146] THEN M225 #100 "Scaled Vectors:\n Requested Length: %.5f\n Actual Length: %.5f" #117 #122
  if [#146] THEN M225 #100 "Scaled Vector:\nX: %.5f Y: %.5f Z: %.5f" #29631 #29632 #29633

  #118 = 180 ;// rotate vector 180 degrees to retract from surface
  ;// rotate vector, returns as rotated_vector_x , rotated_vector_y, rotated_vector_z
  G65 "#400/system/rotate_vector.cnc" A[#118] X[#29631] Y[#29632] Z[#29633]
  if [#146] THEN M225 #100 "Rotated Vector:\nX: %.5f Y: %.5f Z: %.5f" #29641 #29642 #29643

  ;// store returned rotated vector:
  #29531 = [#29641] ;// Record scaled and rotated retract vector

  #29532 = [#29642] ;// Record scaled and rotated retract vector
  #29533 = [#29643] ;// Record scaled and rotated retract vector
  ;// If articulated head, then we must invert the rotated z axis position, this ensures the retract takes the same path as the aproach:
  IF #119 == [1] THEN #29533 = [-1*[#29643]] ;// Record scaled and rotated retract vector

  ;// calculate the position to retract to:
  #29701 = [[#29511]+[#29531]]
  #29702 = [[#29512]+[#29532]]
  #29703 = [#29693]
  ;IF #119 == [1] THEN #29703 = [[#29513]+[#29533]]
  IF #119 == [1] THEN #29703 = [#29723]

  ;// if machine type is articulated head, then the retract position must be rotated to the B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]

  if [#146] THEN M225 #100 "Retract to position:\nX: %.5f Y: %.5f Z: %.5f" #29701 #29702 #29703
  ;// perform retract move to retract positon found above:
  G1 X[#29701] Y[#29702] Z[#29703] F[#9014]

  ;// if debug then Record retract point to the output file:
  if [#146] THEN M223 "G1 X%.5f Y%.5f Z%.5f %cA%.5f B%.5f\n" #29701 #29702 #29703 #147 #29734 #29735

  ;// update point status:
  #106 = [0]
  #108 = [1]
  #109 = [[#109]+1]

  ;// Calculate stepover perpendicular to pullback just used:
  ;// scale vector, returns as scaled_vector_x, scaled_vector_y, scaled_vector_z
  G65 "#400/system/scale_vector.cnc" R[#110] X[[#29641]] Y[[#29642]] Z[[#29643]]
  if [#146] THEN M225 #100 "Scaled Vectors:\n Requested Length: %.5f\n Actual Length: %.5f" #110 #122
  if [#146] THEN M225 #100 "Scaled Vector:\nX: %.5f Y: %.5f Z: %.5f" #29631 #29632 #29633

  IF #[I] == [1] THEN IF #[K] == [1] THEN #118 = 90 ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [1] THEN IF #[K] == [-1] THEN #118 = -90 ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [-1] THEN IF #[K] == [1] THEN #118 = -90 ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [-1] THEN IF #[K] == [-1] THEN #118 = 90 ;// rotate vector 90 degrees for stepover move:
  ;// rotate vector, returns as rotated_vector_x , rotated_vector_y, rotated_vector_z
  G65 "#400/system/rotate_vector.cnc" A[#118] X[#29631] Y[#29632] Z[#29633]
  if [#146] THEN M225 #100 "Rotated Vector:\nX: %.5f Y: %.5f Z: %.5f" #29641 #29642 #29643

  ;// Record starting position:
  #141 = [#5041] ;// record x
  #142 = [#5042] ;// record y
  #143 = [#5043] ;// record z

  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#141] Y[#142] Z[#143]
  IF #119 == [1] THEN #29711 = [#29651]
  IF #119 == [1] THEN #29712 = [#29652]
  IF #119 == [1] THEN #29713 = [#29723]

  IF #119 == [1] THEN #29703 = [#29713]
  IF #119 == [1] THEN #29701 = [[#29711]+[#29641]]
  IF #119 == [1] THEN #29702 = [[#29712]+[#29642]]
  ;IF #119 == [1] THEN #29703 = [[#143]+[#29703]]
  ;// if articulated head machine, then rotate stepover vector to B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]

  ;// calculate articulated head ending stepover position:
  ;IF #119 == [1] THEN #29701 = [[#141]+[#29701]]
  ;IF #119 == [1] THEN #29702 = [[#142]+[#29702]]
  ;IF #119 == [1] THEN #29703 = [[#143]+[#29703]]

  ;// calculate standard machine stepover position:
  #29711 = [#141]+[#29641]
  #29712 = [#142]+[#29642]
  #29713 = [#29693]
  IF #119 == [1] THEN #29711 = [#29701]
  IF #119 == [1] THEN #29712 = [#29702]
  IF #119 == [1] THEN #29713 = [#29703]


;  m223 "%c 1st point stepping %f %f %f\n" #147 #29711 #29712 #29713 ;*** Debug ***
  ;// Perform protected probing stepover move:
  M115 /X[#29711] /Y[#29712] /Z[#29713] P#9011 F[#9014] L1 ;//Probe point
  ;//IF !#[probe_input] THEN M0                    ;// Contact during stepover...

  ;// if debug then Record stepover point to the output file:
  if [#146] THEN M223 "G1 X%.5f Y%.5f Z%.5f %cA%.5f B%.5f\n" #29711 #29712 #29713 #147 #29734 #29735

;  m223 "%c End First point block\n" #147 ; *** DEBUG ***   
  ;// Return:
  GOTO 1100 ;// return to main loop.
  ;//// End first_point_block

N[2200] ;// second point block
;  m223 "%c Second point block\n" #147 ; *** DEBUG ***
  ;// snapshot of position:
  #141 = [#5041] ;// record x
  #142 = [#5042] ;// record y
  #143 = [#5043] ;// record z

  ;// shift point_n to point_n_minus_1:
  #29501 = [#29511]
  #29502 = [#29512]
  #29503 = [#29513]
  #29504 = [#29514]
  #29505 = [#29515]

  #29701 = [#141]
  #29702 = [#142]
  #29703 = [#143]

  ;// if articulated head machine, rotate starting position flat onto XY Plane:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29723]

  ;// calculate starting move / vector:
  IF #114 == 12 THEN #29702 =[[#29702]+[#113]]
  IF #114 == 3 THEN #29701 =[[#29701]+[#113]]
  IF #114 == 6 THEN #29702 =[[#29702]-[#113]]
  IF #114 == 9 THEN #29701 =[[#29701]-[#113]]

  ;// if articulated head machine, then rotate starting move to B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29701] Y[#29702] Z[#29703]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]
  IF #119 == [0] THEN #29703 = [#29693]

;  m223 "%c 2nd point Probing to %f %f %f\n" #147 #29701 #29702 #29703 ;*** Debug ***

  ;//perform protected probing move towards surface::
  M115 /X[#29701] /Y[#29702] /Z[#29703] P#9011 F[#9015] L1 ;//Probe point
  ;//IF [probe_input] THEN GOTO probe_no_contact
  ;// record positions:
  #29511 = [[#24301]-[#2500]] ;// record x
  #29512 = [[#24302]-[#2600]] ;// record y
  #29513 = [[#24303]-[#2700]-#10000] ;// record z
  #29514 = [[#24304]-[#2800]] ;// record a
  #29515 = [[#24305]-[#2900]] ;// record b

;  m223 "%c Second point %f %f %f\n" #147 #29511 #29512 #29513; *** DEBUG ***

  #29731 = [#29511] ;// store raw point for later saving to file
  #29732 = [#29512] ;// store raw point for later saving to file
  #29733 = [#29513] ;// store raw point for later saving to file
  #29734 = [#29514] ;// store raw point for later saving to file
  #29735 = [#29515] ;// store raw point for later saving to file

  if [#146] THEN M225 #100 "Recorded Position:\nX: %.5f Y: %.5f Z: %.5f" #29511 #29512 #29513

  ;// if articulated head machine, then rotate positions flat onto XY Plane:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#29511] Y[#29512] Z[#29513]
  IF #119 == [1] THEN #29711 = [#29651]
  IF #119 == [1] THEN #29712 = [#29652]
  IF #119 == [1] THEN #29713 = [#29653]
  IF #119 == [1] THEN #29511 = [#29651]
  IF #119 == [1] THEN #29512 = [#29652]
  IF #119 == [1] THEN #29513 = [#29653]

  ;// calculate the n_minus_1_vector, this is the vector from the first point to the second point:
  #29601 = [[#29511]-[#29501]]
  #29602 = [[#29512]-[#29502]]
  #29603 = [0]
  if [#146] THEN M225 #100 "Recorded n minus 1 Vector:\nX: %.5f Y: %.5f Z: %.5f" #29601 #29602 #29603

  ;// Open dig file for output:
  M121 "#301.dig5a"

  ;// Record raw point to the output file, uncorrected for all:
  M223 "G1 X%.5f Y%.5f Z%.5f A%.5f B%.5f\n" #29731 #29732 #29733 #29734 #29735

  ;// Open text file for mastercam output:
  M121 "#301.txt"

  ;// Record raw point to the output file, uncorrected for non artic head:
  IF #119 == [0] THEN IF #149 == [0] THEN M223 "X%.5f Y%.5f Z%.5f A%.5f B%.5f\n" #29731 #29732 #29733 #29734 #29735
  IF #119 == [0] THEN IF #149 == [1] THEN G65 "#400/system/correct_position_a.cnc" A[#29734] X[#29731] Y[#29732] Z[#29733]
  IF #119 == [0] THEN IF #149 == [1] THEN M223 "%.5f %.5f %.5f\n" #136 #137 #138
  IF #119 == [1] THEN G65 "#400/system/correct_position.cnc" A[#29734] B[#29735] X[#29731] Y[#29732] Z[#29733]
  If #119 == [1] THEN M223 "%.5f %.5f %.5f\n" #136 #137 #138
  M121 "#301.dig5a"

  #108 = [2]
  #109 = [[#109]+1]

  ;// Return:
  GOTO 1200 ;// return to main loop
  ;//// End second point block

N[2600] ;// probe point block
;  m223 "%c probe point block\n" #147 ;*** Debug ***
  ;// snapshot of starting position:
  #141 = [#5041] ;// record x
  #142 = [#5042] ;// record y
  #143 = [#5043] ;// record z

  ;// if articulated head machine, then rotate position flat onto XY Plane
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#141] Y[#142] Z[#143]
  IF #119 == [1] THEN #141 = [#29651]
  IF #119 == [1] THEN #142 = [#29652]
  IF #119 == [1] THEN #143 = [#29723]

;  m223 "%c z %f %f\n" #147 #5043 #143 ;*** Debug ***
;  m223 "%c probe point block last point %f %f\n" #147 #29511 #29512;*** Debug ***

  ;// shift last point to point n minus 1
  #29501 = [#29511]
  #29502 = [#29512]
  #29503 = [#29513]
  #29504 = [#29514]
  #29505 = [#29515]

  ;// shift last vector to n minus 2 vector
  #29671 = [#29601]
  #29672 = [#29602]
  #29673 = [#29603]

  ;// save predicted vector 
  #29551 = [[#29521]-[#141]]
  #29552 = [[#29522]-[#142]]
  #29553 = [[#29523]-[#143]]

;  m223 "%c Original probing vector %f %f %f\n" #147 #29551 #29552 #29553 ;*** Debug ***

  ;// calculate position to probe to
  ;// Scale predicted vector to account for probe search distance:
  G65 "#400/system/scale_vector.cnc" R[#9016] X[#29551] Y[#29552] Z[#29553]
;  m223 "%c Scaled probing vector %f %f %f\n" #147 #29631 #29632 #29633 ;*** Debug ***

    ;// calculaate position to probe to:
  #29711 = [[#141]+[#29631]]
  #29712 = [[#142]+[#29632]]
  #29713 = [[#143]+[#29633]]
  IF #119 == [0] THEN #29713 = [#29693]
  IF #119 == [1] THEN #29713 = [#29723]

;  m223 "%c Probing to Before rotate %f %f %f\n" #147 #29711 #29712 #29713 ;*** Debug ***

  ;// if articulated head machine, then rotate position to probe to to the B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[#29711] Y[#29712] Z[#29713]
  IF #119 == [1] THEN #29711 = [#29651]
  IF #119 == [1] THEN #29712 = [#29652]
  IF #119 == [1] THEN #29713 = [#29653]

;  m223 "%c Probing to %f %f %f\n" #147 #29711 #29712 #29713 ;*** Debug ***

  ;// Perform protected probing move towards the surface:
  M115 /X[#29711] /Y[#29712] /Z[#29713] P#9011 F[#9014] L1 ;//Probe point

  ;// record positions:
  #29511 = [[#24301]-[#2500]] ;// record x
  #29512 = [[#24302]-[#2600]] ;// record y
  #29513 = [[#24303]-[#2700]-#10000] ;// record z
  #29514 = [[#24304]-[#2800]] ;// record a
  #29515 = [[#24305]-[#2900]] ;// record b

  #29731 = [#29511] ;// store raw point for later saving to file
  #29732 = [#29512] ;// store raw point for later saving to file
  #29733 = [#29513] ;// store raw point for later saving to file
  #29734 = [#29514] ;// store raw point for later saving to file
  #29735 = [#29515] ;// store raw point for later saving to file

;  m223 "%c Probed point %f %f %f\n" #147 #29511 #29512 #29513 ;*** Debug ***
;  m225 #100 "after probe point %f %f" #29511 #29512

  ;// if articulated head machine then rotate position found flat onto the XY Plane:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[[#105]*[-1]] X[#29511] Y[#29512] Z[#29513]
  IF #119 == [1] THEN #29511 = [#29651]
  IF #119 == [1] THEN #29512 = [#29652]
  IF #119 == [1] THEN #29513 = [#29653]

  ;// calculate n_minus_1 vector:
  #29601 = [[#29511]-[#29501]]
  #29602 = [[#29512]-[#29502]]
  #29603 = [0]

  ;// Return:
  GOTO 1600
  ;//// End probe point block  

N[2400]
  ;m225 #100 "predict contact vector"
  ;//M0 ;// predict contact vector:
  ;// if num points less than 2, then not possible to use last vector to predict next one...
  IF #[I] == [1] THEN IF #[K] == [1] THEN if #108 < [2] THEN #118 = [-90] ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [1] THEN IF #[K] == [-1] THEN if #108 < [2] THEN #118 = [90] ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [-1] THEN IF #[K] == [1] THEN if #108 < [2] THEN #118 = [90] ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [-1] THEN IF #[K] == [-1] THEN if #108 < [2] THEN #118 = [-90] ;// rotate vector 90 degrees for stepover move:
  ;if #108 < [2] THEN #118 = [-90]
  ;//if num_points_probed < [2] THEN G65 "#400/system/rotate_vector.cnc"  A[angle]  X[n_minus_1_vector_x] Y[n_minus_1_vector_y] Z[n_minus_1_vector_z]
  if #108 >= [2] THEN G65 "#400/system/scale_vector.cnc" R[#110] X[[#29611]+[#29601]] Y[[#29612]+[#29602]] Z[[#29613]+[#29603]]

  ;//if num_points_probed < [2] THEN predicted_vector_x = [rotated_vector_x]
  ;//if num_points_probed < [2] THEN predicted_vector_y = [rotated_vector_y]
  ;//if num_points_probed < [2] THEN predicted_vector_z = [rotated_vector_z]

  if #108 < [2] THEN #29551 = [#29601]
  if #108 < [2] THEN #29552 = [#29602]
  if #108 < [2] THEN #29553 = [#29603]

  if #108 >= [2] THEN #29551 = [#29631]
  if #108 >= [2] THEN #29552 = [#29632]
  if #108 >= [2] THEN #29553 = [#29633]

  if [#146] THEN M225 #100 "Predicted Vector:\nX: %.5f Y: %.5f Z: %.5f" #29551 #29552 #29553

  #29521 = [[#29551]+[#29511]]
  #29522 = [[#29552]+[#29512]]
  #29523 = [[#29553]+[#29513]]
;  m223 "%c predict contact %f %f z %f\n" #147 #29553 #29513 #5043; *** Debug ***
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  GOTO 1400
  ;//// End predict contact point  

N[2500]
  ;//M0 ;// calculate retract vector:
  ;// scale vector for retract, returns as scaled_vector_x, scaled_vector_y, scaled_vector_z
  G65 "#400/system/scale_vector.cnc" R[#117] X[#29551] Y[#29552] Z[#29553]

  IF #[I] == [1] THEN IF #[K] == [1] THEN #118 = [-90] ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [1] THEN IF #[K] == [-1] THEN #118 = [90] ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [-1] THEN IF #[K] == [1] THEN #118 = [90] ;// rotate vector 90 degrees for stepover move:
  IF #[I] == [-1] THEN IF #[K] == [-1] THEN #118 = [-90] ;// rotate vector 90 degrees for stepover move:
  ;#118 = -90 ;// rotate vector -90 degrees to create retract vector perpendicular to the surface probed
  ;// rotate vector, returns as rotated_vector_x , rotated_vector_y, rotated_vector_z
  G65 "#400/system/rotate_vector.cnc" A[#118] X[#29631] Y[#29632] Z[#29633]

  #29531 = [#29641] ;// Record scaled and rotated retract vector
  #29532 = [#29642] ;// Record scaled and rotated retract vector
  #29533 = [#29643] ;// Record scaled and rotated retract vector

  if [#146] THEN M225 #100 "Retract Vector:\nX: %.5f Y: %.5f Z: %.5f" #29531 #29532 #29533

  ;// Return:
  GOTO 1500
  ;//// End calculate retract

N[2700] ;// Check for _end of spline block
  ;// check distance to first point in spline.
  #29566 = [[#29511]-[#29561]]
  #29567 = [[#29512]-[#29562]]
  #29568 = [[#29513]-[#29563]]

  ;// calculate distance
  #29569 = [SQRT[[[#29566]*[#29566]]+[[#29567]*[#29567]]+[[#29568]*[#29568]]]]

  ;// Check to see if distance to first point is less than stepover, if so then end of spline found
  IF #29569 <= [#110] THEN #29654 = [1]

  GOTO 1700
  ;//// End end spline check block  

N[2800] ;// perform step down to next z when required:

  ;// Open dig file for output:
  M121 "#301.dig5a"

  ;// Record raw point to the output file, uncorrected for all
  M223 "G1 X%.5f Y%.5f Z%.5f A%.5f B%.5f\n\n" #29741 #29742 #29743 #29744 #29745

  ;// Open text file for mastercam output:
  M121 "#301.txt"

  ;// Record raw point to the output file, uncorrected for non artic head:
  IF #119 == [0] THEN IF #149 == [0] THEN M223 "X%.5f Y%.5f Z%.5f A%.5f B%.5f\n\n" #29741 #29742 #29743 #147 #29744 #29745
  IF #119 == [0] THEN IF #149 == [1] THEN G65 "#400/system/correct_position_a.cnc" A[#29744] X[#29741] Y[#29742] Z[#29743]
  IF #119 == [0] THEN IF #149 == [1] THEN M223 "%.5f %.5f %.5f\n\n" #136 #137 #138
  IF #119 == [1] THEN G65 "#400/system/correct_position.cnc" A[#29744] B[#29745] X[#29741] Y[#29742] Z[#29743]
  If #119 == [1] THEN M223 "%.5f %.5f %.5f\n\n" #136 #137 #138

  ;// if articulated head machine, then rotate the dig depth vector to the B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[0] Y[0] Z[-[#112]]
  IF #119 == [1] THEN #29701 = [#29651]
  IF #119 == [1] THEN #29702 = [#29652]
  IF #119 == [1] THEN #29703 = [#29653]

  ;// if articulated ead machine then rotate dig stepdown vector to the B-Axis angle:
  IF #119 == [1] THEN G65 "#400/system/rotate_vector_b.cnc" A[#105] X[0] Y[0] Z[-[#111]]
  IF #119 == [1] THEN #29711 = [#29651]
  IF #119 == [1] THEN #29712 = [#29652]
  IF #119 == [1] THEN #29713 = [#29653]

  ;// move to start position before stepping down:
  G1 X[#29691] Y[#29692] Z[#29693] F[#9014] ;//perform move

  ;// perform checks to see what to do with the Z-Axis stepdown:
  IF #29746 == [-1] THEN IF #119 == [0] THEN IF [#29693] <= [[#103]-[#112]] THEN GOTO 9999 ;// _end if we have reached max z depth already
  IF #29746 == [-1] THEN IF #119 == [1] THEN IF [#29693] <= [[#103]+[#29703]] THEN GOTO 9999 ;// _end if we have reached max z depth already
  IF #29746 == [1] THEN IF #119 == [0] THEN IF [#29693] >= [[#103]-[#112]] THEN GOTO 9999 ;// _end if we have reached max z depth already
  IF #29746 == [1] THEN IF #119 == [1] THEN IF [#29693] >= [[#103]+[#29703]] THEN GOTO 9999 ;// _end if we have reached max z depth already

  IF #119 == [0] THEN #145 = [[#29693]-[#111]]
  IF #119 == [1] THEN #145 = [[#29693]+[#29713]]
  IF #119 == [1] THEN #148 = [[#29691]+[#29711]]
  IF #29746 == [-1] THEN IF #119 == [0] THEN IF [#145] < [[#103]-[#112]] THEN #145 = [[#103]-[#112]] ;// if step would go below max z then max z is set to step z
  IF #29746 == [-1] THEN IF #119 == [1] THEN IF [#145] < [[#103]+[#29703]] THEN #145 = [[#103]+[#29713]] ;// if step would go below max z then max z is set to step z
  IF #29746 == [-1] THEN IF #119 == [1] THEN IF [#145] < [[#103]+[#29703]] THEN #148 = [[#101]+[#29711]]
  IF #29746 == [1] THEN IF #119 == [0] THEN IF [#145] > [[#103]-[#112]] THEN #145 = [[#103]-[#112]] ;// if step would go below max z then max z is set to step z
  IF #29746 == [1] THEN IF #119 == [1] THEN IF [#145] > [[#103]+[#29703]] THEN #145 = [[#103]+[#29713]] ;// if step would go below max z then max z is set to step z
  IF #29746 == [1] THEN IF #119 == [1] THEN IF [#145] > [[#103]+[#29703]] THEN #148 = [[#101]+[#29711]]

  ;// move to start position before stepping down:
  G1 X[#29691] Y[#29692] Z[#29693] F[#9014] ;//perform move

  ;// perform the step down move:
  IF #119 == [0] THEN M115 /X[#29691] /Y[#29692] /Z[#145] P#9011 F[#9014] L1 ;//perform move
  IF #119 == [1] THEN M115 /X[#148] /Y[#29692] /Z[#145] P#9011 F[#9014] L1 ;//perform move

  ;// check for contact during the step down:
  ;IF #[#[[#9011]+6000]] THEN M200 "Contact during stepdown\nPlease check probe"

  ;// Return:
  GOTO 1800
  ;//// end step down block

N[9990]
  #100 = 0
  M225 #100 "Probe Input set to 0./n Please correct error"
  GOTO bad_end
  ;//// End bad_probe_message

N[9991]
  #100 = 0
  M225 #100 "Probe Tool Number set to 0./n Please correct error"
  GOTO bad_end
  ;//// End bad_probe_tool_num

N[9992]
  #100 = 0
  M225 #100 "Probe Recovery set to 0./n Please correct error"
  GOTO bad_end
  ;//// End bad_recvery_dist

N[9993]
  #100 = 0
  M225 #100 "Slow Probe Rate set to 0./n Please correct error"
  GOTO bad_end
  ;//// End bad_slow_probe

N[9994]
  #100 = 0
  M225 #100 "Fast Probe Rate set to 0./n Please correct error"
  GOTO bad_end
  ;//// End bad_fast_probe

N[9995]
  #100 = 0
  M225 #100 "Probe Search Distance set to 0./n Please correct error"
  GOTO bad_end
  ;//// End bad_search_dist

N[9996]
  #100 = 0
  M225 #100 "Unable to detect surface"
  GOTO bad_end
  ;//// End probe_no_contact

N[9997]
  #100 = 0
  M225 #100 "Unable to detect Probe"
  GOTO bad_end
  ;//// End bad_probe_detect  


N[9999]
  #100 = 3
  M225 #100 "Digitizing Complete"
  #32000 = 0 ; Set #32000 to OK at 9999
  M99

N[bad_end]
  #32000 = 1 ; Set #32000 to BAD at 9999
  M99
