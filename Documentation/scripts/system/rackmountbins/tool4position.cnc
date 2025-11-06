IF #A == 1 THEN GOTO 100 ;X Position of Tool
IF #A == 2 THEN GOTO 200 ;Y Position of Tool
IF #A == 3 THEN GOTO 300 ;XY Position of Tool
IF #A == 4 THEN GOTO 400 ;Z Position of Tool
IF #A == 5 THEN GOTO 500 ;Finger Clearance Position
IF #A == 6 THEN GOTO 600 ;Engage Fingers
IF #A == 7 THEN GOTO 700 ;Disengage Fingers
IF #A == 8 THEN GOTO 800 ;Z Position of Tool with L

N100 ;X Position of Tool
G53 X53.4863
GOTO 1000

N200 ;Y Position of Tool
G53 Y-0.2486
GOTO 1000

N300 ;XY Position of Tool
G53 X53.4863Y-0.2486
GOTO 1000

N400 ;Z Height 
G53 Z-7.86
GOTO 1000

N500 ;XY Position with Clearance Added
G53 X53.4863Y-4.2486
GOTO 1000

N600 ;Position of Tool (Based on Clearance Axis)
G53 Y-0.2486 L50
GOTO 1000

N700 ;Cleared Position of Tool (Based on Clearance Axis)
G53 Y-4.2486 L50
GOTO 1000

N800 ;Z Height with L (For Hole Type Rack)
G53 Z-7.86 L50
GOTO 1000
N1000