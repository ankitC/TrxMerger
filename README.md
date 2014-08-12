=========
TrxMerger
=========

A simple utility to merge TRX files from Visual Studio 2012.

  - Merges the following sections:
      - TestDefinitions
      - Results
      - TestEntries
      
  - Updates the following counters:
      - Passed
      - Failed
      - Total
      - Executed
      - Times
      
      

Usage: ./<exename> <path to the folder of all .trx files>

- Output file names FinalResults.trx.
- Output file is placed at the same location as with all the other trx files.

=====
Note: 
=====
  - Currently merges only from the top-level folder. 
  - Please make sure all the files to be merged are in the same folder.
  - YOUR ORIGINAL FILES WILL BE MODIFIED. PLEASE USE COPIES FOR MERGING.

======
#ToDo:
======
  - Adding ability to merge duplicates.
  - Adding ability to merge more sections.
  - Performance improvments.
  - Stability improvments.
  - Adding ability for more counter values.
