# HavenCNCServer Documentation and API

This directory contains documentation and API wrappers for the HavenCNCServer project.

## Files Overview

### API Classes (/CentriodAPI/)
- CNCUtils_Final.cs - Clean CNC12 API wrapper with no dependencies
  - Replaces GeneralUtils from Centroid Wizard project
  - Provides parameter access, workpiece reference points, and bit manipulation
  - Requires only CentroidAPI reference

### Documentation (/Documentation/)
- CNCUtils_Integration_Guide.md - Setup and usage guide for CNCUtils
- PLC_File_Format_Guide.md - Complete PLC programming guide (updated for CNCUtils)
- PLC_IO_Writing_Documentation.md - PLC I/O configuration guide (updated for CNCUtils)
- GeneralUtils_API_Documentation.md - Original API reference for comparison

## Quick Start

1. Initialize CNCUtils:
   using HavenCNCServer.CentriodAPI;
   CNCUtils.Initialize(yourCentroidApiInstance);

2. Use in PLC code:
   double value = CNCUtils.GetParameterValue(CNC12Parameters.SPINDLE_COUNTS_REV_PARM);
   CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 1, newXPos);

3. Read the guides:
   - Start with CNCUtils_Integration_Guide.md
   - Reference PLC_File_Format_Guide.md for PLC programming
   - Use PLC_IO_Writing_Documentation.md for I/O configuration

## Migration from GeneralUtils

All documentation has been updated to use CNCUtils instead of GeneralUtils:
- 124 method calls updated across documentation
- Zero external dependencies
- Same API surface as GeneralUtils
- Real CNC12 parameter values included

Generated on: 2025-10-04 13:26:37
From: Centroid Wizard project
