The CPULoadTester estimates the value of Pi, using either a single thread or multiple threads
This can be used to simulate varying levels of load on a computer

Program syntax:
CPULoadTester.exe
 [/Mode:{1,2,3,4}] [/RunTime:Seconds] [/Threads:ThreadCount] [/UseTiered]

/Mode:1 is serial calculation (single thread)
/Mode:2 uses a Parallel.For loop
/Mode:3 uses the Task Parallel Library (TPL) framework, initializing with factories
/Mode:4 uses the Task Parallel Library (TPL) framework, but without factories

Specify the runtime, in seconds, using /RunTime

Specify the number of threads to use with /Threads
If not specified, all cores will be used

Use /UseTiered with modes 2 through 4 to indicate that different threads should run for 
tiered runtimes (each thread will run for 80% of the length of the previous thread)

-------------------------------------------------------------------------------
Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015

E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov
-------------------------------------------------------------------------------

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0

Notice: This computer software was prepared by Battelle Memorial Institute, 
hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
Department of Energy (DOE).  All rights in the computer software are reserved 
by DOE on behalf of the United States Government and the Contractor as 
provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
SOFTWARE.  This notice including this sentence must appear on any copies of 
this computer software.
