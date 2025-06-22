@echo off
SET CCNetLabel=0.2.%date:~8%%date:~3,2%.%date:~0,2%
nant.bat -D:CCNetLabel=%CCNetLabel% dev-build
