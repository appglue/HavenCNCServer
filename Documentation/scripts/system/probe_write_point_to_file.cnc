; Filename:   probe_write_point_to_file.cnc
; Purpose:    Write a point to the currently open data file
; Programmer: John Popovich
; Date:       Nov 9, 2010 
; Usage:      G65 "/cncm/system/probe_write_point_to_file.cnc"  X[x_position] Y[y_position] Z[z_position]  D[skip_new_line]
; Where:      x_position = x point
;             y_position = y point
;             z_position = z point
;             skip_new_line = 1 to skip the new line
; Notes:   A file must be open for output
; Copyright (C) 2011  Centroid Corporation, Howard, PA 16841


if [#24901] then M223 "%c %f " #20101 #24
if [#24902] then M223 "%c %f " #20102 #25
if [#24903] then M223 "%c %f " #20103 #26
if [#24904] then M223 "%c %f " #20104 #5044
if [#24905] then M223 "%c %f " #20105 #5045
if [#d != 1] then m223 "\n"
