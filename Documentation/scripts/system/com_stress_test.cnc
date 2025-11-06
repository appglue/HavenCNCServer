; Message timer varible
#100 = 0

; Variable that saves the amount of times we looped the test
#101 = 0

; Save the amount of communication errors before running the stress test
#102 = #25024 ; System Varible that contains the amount of packets that has been resent
#103 = #25025 ; System Varible that contains the amount of generic communication errors
#104 = #25026 ; System Varible that contains the amount of packets out of ordered errors
#105 = #25027 ; System Varible that contains the amount of NAcks (negative acknowledgement packets) that were sent
#106 = #25028 ; System Varible that contains the amount of NAcks (negative acknowledgement packets) that were received

#100 = 0
M221 #100 "Communications Stress Test in progress, Please Wait for results"
; Loops the stess test m code 5000 times
N10
IF #101 == 5000 THEN GOTO 20
M100/50512
#101 = #101 + 1
GOTO 10

; Check the previous amount of communtation error plus the amount we will accept during job to the acutal ammount 
N20
IF #102 + 5 <= #25024 || #103 + 5 <= #25025 || #104 + 5 <= #25026 || #105 + 5 <= #25027 || #106 + 5 <= #25028 THEN GOTO 40 ELSE GOTO 30

; If the amount of error is under the amount we accpet then the test passed
N30
#100 = 0
IF #25024 - #102 > 0 THEN #102 = #25024 - #102 ELSE #102 = 0
IF #25025 - #103 > 0 THEN #103 = #25025 - #103 ELSE #103 = 0
IF #25026 - #104 > 0 THEN #104 = #25026 - #104 ELSE #104 = 0
IF #25027 - #105 > 0 THEN #105 = #25027 - #105 ELSE #105 = 0
IF #25028 - #106 > 0 THEN #106 = #25028 - #106 ELSE #106 = 0
M225 #100 "Communications Stress Test \nPASSED\nmax. errors acceptable = 5\nResults:\nPackets Resent: %.f\n Generic Communication Errors: %.f\nPackets Out of Order: %.f\nNAcks Packets Sent: %.f \nNAcks Packets Recieved: %.f" #102 #103 #104 #105 #106
GOTO 100

; If the amount of error is over the amount we accpet then the test passed
N40
#100 = 0
IF #25024 - #102 > 0 THEN #102 = #25024 - #102 ELSE #102 = 0
IF #25025 - #103 > 0 THEN #103 = #25025 - #103 ELSE #103 = 0
IF #25026 - #104 > 0 THEN #104 = #25026 - #104 ELSE #104 = 0
IF #25027 - #105 > 0 THEN #105 = #25027 - #105 ELSE #105 = 0
IF #25028 - #106 > 0 THEN #106 = #25028 - #106 ELSE #106 = 0
M225 #100 "Communications Stress Test Failed, See Tech Support Forum and Bulletin 270 for more information\nPackets Resent: %.f Generic Communication Errors: %.f\nPackets Out of Order: %.f\nNAcks Packets Sent: %.f NAcks Packets Recieved: %.f" #102 #103 #104 #105 #106

N100